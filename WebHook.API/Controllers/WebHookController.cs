using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebHook.API.HttpClients;
using WebHook.API.Interfaces;
using WebHook.API.ModelBuilders;

namespace WebHook.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebHookController : ControllerBase
    {
        private readonly TrelloService _trello;
        private readonly Func<Type, ICardService> _factory;

        public WebHookController(TrelloService trello, Func<Type, ICardService> factory)
        {
            _trello = trello;
            _factory = factory;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JsonElement payload)
        {
            // Try to extract card id from common Trello webhook shapes
            string? cardId = null;

            if (payload.TryGetProperty("action", out var action) && action.ValueKind == JsonValueKind.Object)
            {
                if (action.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                {
                    if (data.TryGetProperty("card", out var cardJson) && cardJson.ValueKind == JsonValueKind.Object)
                    {
                        if (cardJson.TryGetProperty("id", out var idProp)) cardId = idProp.GetString();
                        if (string.IsNullOrEmpty(cardId) && cardJson.TryGetProperty("shortLink", out var sl)) cardId = sl.GetString();
                    }
                }
            }

            // fallback: try top-level card
            if (string.IsNullOrEmpty(cardId) && payload.TryGetProperty("card", out var topCard) && topCard.ValueKind == JsonValueKind.Object)
            {
                if (topCard.TryGetProperty("id", out var idProp2)) cardId = idProp2.GetString();
            }

            if (string.IsNullOrEmpty(cardId)) return BadRequest("Card id not found in payload");

            var card = await _trello.GetCardAsync(cardId) ?? payload;

            if (card.ValueKind != JsonValueKind.Object)
            {
                return BadRequest($"Card not found for id: {cardId}");
            }

            var cardModel = CardModelBuilder.BuildCardModel(cardId, card);

            if (cardModel == null)
            {
                return BadRequest($"Failed to build card model for id: {cardId}");
            }

            // Resolve corresponding card service using factory delegate registered in DI
            var cardService = _factory(cardModel.GetType());

            var isProcessed = await cardService.ProcessCard(cardModel);
            if (!isProcessed) return BadRequest($"Failed to process card: {cardId}");

            return Ok();
        }
    }
}

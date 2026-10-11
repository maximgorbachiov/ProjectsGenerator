using WebHook.API.Data;
using WebHook.API.Data.Repositories;
using WebHook.API.HttpClients;
using WebHook.API.Interfaces;
using WebHook.API.Models;

namespace WebHook.API.Services
{
    public abstract class CardService<T> : ICardService where T : Card
    {
        protected readonly ILogger<CardService<T>> _logger;
        protected readonly AppDbContext _db;
        protected readonly TrelloService _trello;
        protected readonly CardRequestMessageQueueService _queue;

        public CardService(ILogger<CardService<T>> logger, 
            AppDbContext db, 
            TrelloService trello, 
            CardRequestMessageQueueService queue)
        {
            _logger = logger;
            _db = db;
            _trello = trello;
            _queue = queue;
        }

        public abstract Task<bool> ProcessCard(T card);

        // non-generic adapter for DI factory usage
        public async Task<bool> ProcessCard(Card card)
        {
            return await ProcessCard((T)card);
        }
    }
}

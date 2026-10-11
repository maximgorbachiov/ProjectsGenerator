using WebHook.API.Models;

namespace WebHook.API.Interfaces
{
    public interface ICardService
    {
        Task<bool> ProcessCard(Card card);
    }
}

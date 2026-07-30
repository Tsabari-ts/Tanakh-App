using System.Threading.Tasks;
using Tanakh.Api.Model;

namespace Tanakh.Api.Services
{
    public interface ITanakhTextService
    {
        Task<TanakhContext?> GetChapterAsync(string book, string chapter);
    }
}

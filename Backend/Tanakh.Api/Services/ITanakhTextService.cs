using Tanakh.Api.Model;

namespace Tanakh.Api.Services
{
    public interface ITanakhTextService
    {
        TanakhContext? GetChapter(string book, string chapter);
    }
}

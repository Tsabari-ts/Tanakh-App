using System.Collections.Generic;
using System.Threading.Tasks;
using Tanakh.Infrastructure.Model;

namespace Tanakh.Infrastructure.Services
{
    public interface ITanakhStructureService
    {
        Task<List<BaseStructure>> GetAllAsync();

        Task<List<BaseStructure>> GetBySectionAsync(string section);

        Task<List<BaseStructure>> GetByTitleAsync(string title);
    }
}

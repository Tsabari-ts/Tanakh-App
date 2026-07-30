using System.Collections.Generic;
using Tanakh.Infrastructure.Model;

namespace Tanakh.Infrastructure.Services
{
    public interface ITanakhStructureService
    {
        List<BaseStructure> GetAll();

        List<BaseStructure> GetBySection(string section);

        List<BaseStructure> GetByTitle(string title);
    }
}

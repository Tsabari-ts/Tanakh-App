using System.Collections.Generic;

namespace Tanakh.Model
{
    public class TanakhStructure
    {
        public List<BaseStructure> Structures { get; set; }
    }

    public class BaseStructure
    {
        public string section { get; set; }
        public string heTitle { get; set; }
        public string title { get; set; }
        public int length { get; set; }
        public List<int> chapters { get; set; }
        public string book { get; set; }
        public string heBook { get; set; }
    }
}
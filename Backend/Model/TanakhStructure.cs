using System.Collections.Generic;

namespace Tanakh.Model
{
    // Every property is nullable to reflect what the deserializer can
    // actually produce from the source JSON. `section`, `title`, and `book`
    // (the fields this app reads) are validated non-null at the point of
    // deserialization in CacheProvider.GetTanakhStructureFromCache.
    public class TanakhStructure
    {
        public List<BaseStructure>? structures { get; set; }
    }

    public class BaseStructure
    {
        public string? section { get; set; }
        public string? heTitle { get; set; }
        public string? title { get; set; }
        public int length { get; set; }
        public List<int>? chapters { get; set; }
        public string? book { get; set; }
        public string? heBook { get; set; }
    }
}

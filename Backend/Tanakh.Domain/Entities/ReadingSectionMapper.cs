using System;

namespace Tanakh.Domain.Entities
{
    // Bridges the Tanakh structure data's section names ("Torah"/"Prophets"/
    // "Writings") to this app's ReadingSection enum (Torah/Neviim/Ketuvim) -
    // same three sections, different naming convention in each place.
    public static class ReadingSectionMapper
    {
        public static ReadingSection FromStructureSectionName(string structureSection) => structureSection switch
        {
            "Torah" => ReadingSection.Torah,
            "Prophets" => ReadingSection.Neviim,
            "Writings" => ReadingSection.Ketuvim,
            _ => throw new ArgumentException($"Unknown structure section '{structureSection}'.", nameof(structureSection))
        };
    }
}

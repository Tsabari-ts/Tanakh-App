namespace Tanakh.Model
{
    public class TanakhContext
    {
        public required string ChosenSection { get; set; }
        public required Book BookData { get; set; }
    }

    public class Book
    {
        public required string BookName { get; set; }
        public required string HebrewTitle { get; set; }
        public int length { get; set; }
        // Legitimately null for a book's first (no PrevChapter) or last
        // (no NextChapter) chapter.
        public string? NextChapter { get; set; }
        public string? PrevChapter { get; set; }
        public required string Verses { get; set; }
        public required string SectionRef { get; set; }
        public required string HebrewSectionRef { get; set; }
    }
}

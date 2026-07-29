namespace Tanakh.Model
{
    public class TanakhContext
    {
        public string ChosenSection { get; set; }
        public Book BookData { get; set; }
    }

    public class Book
    {
        public string BookName { get; set; }
        public string HebrewTitle { get; set; }
        public int length { get; set; }
        public string NextChapter { get; set; }
        public string PrevChapter { get; set; }
        public string Verses { get; set; }
        public string SectionRef { get; set; }
        public string HebrewSectionRef { get; set; }
    }
}
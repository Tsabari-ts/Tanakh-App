using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tanakh.Model
{
    public class TanakhContainer
    {
        public List<Structure> structures { get; set; }
    }

    public class Structure
    {
        public string _ref { get; set; }
        public string heRef { get; set; }
        public bool isComplex { get; set; }
        public List<string> text { get; set; }
        public List<string> he { get; set; }
        public List<Version> versions { get; set; }
        public int textDepth { get; set; }
        public List<string> sectionNames { get; set; }
        public List<string> addressTypes { get; set; }
        public List<int> lengths { get; set; }
        public int length { get; set; }
        public string heTitle { get; set; }
        public List<string> titleVariants { get; set; }
        public List<string> heTitleVariants { get; set; }
        public string type { get; set; }
        public string primary_category { get; set; }
        public string book { get; set; }
        public List<string> categories { get; set; }
        public List<int> order { get; set; }
        public List<int> sections { get; set; }
        public List<int> toSections { get; set; }
        public bool isDependant { get; set; }
        public string indexTitle { get; set; }
        public string heIndexTitle { get; set; }
        public string sectionRef { get; set; }
        public string firstAvailableSectionRef { get; set; }
        public string heSectionRef { get; set; }
        public bool isSpanning { get; set; }
        public string versionTitle { get; set; }
        public string versionTitleInHebrew { get; set; }
        public string shortVersionTitle { get; set; }
        public string shortVersionTitleInHebrew { get; set; }
        public string versionSource { get; set; }
        public string versionStatus { get; set; }
        public string versionNotes { get; set; }
        public string extendedNotes { get; set; }
        public string extendedNotesHebrew { get; set; }
        public string versionNotesInHebrew { get; set; }
        public bool digitizedBySefaria { get; set; }
        public string license { get; set; }
        public bool formatEnAsPoetry { get; set; }
        public string heVersionTitle { get; set; }
        public string heVersionTitleInHebrew { get; set; }
        public string heShortVersionTitle { get; set; }
        public string heShortVersionTitleInHebrew { get; set; }
        public string heVersionSource { get; set; }
        public string heVersionStatus { get; set; }
        public string heVersionNotes { get; set; }
        public string heExtendedNotes { get; set; }
        public string heExtendedNotesHebrew { get; set; }
        public string heVersionNotesInHebrew { get; set; }
        public bool heDigitizedBySefaria { get; set; }
        public string heLicense { get; set; }
        public bool formatHeAsPoetry { get; set; }
        public List<Alt> alts { get; set; }
        public Index_Offsets_By_Depth index_offsets_by_depth { get; set; }
        public string next { get; set; }
        public string prev { get; set; }
        public object[] commentary { get; set; }
        public object[] sheets { get; set; }
        public object[] layer { get; set; }
        public string[] sources { get; set; }
    }

    public class Index_Offsets_By_Depth
    {
    }

    public class Version
    {
        public string title { get; set; }
        public string versionTitle { get; set; }
        public string versionSource { get; set; }
        public string language { get; set; }
        public string status { get; set; }
        public string license { get; set; }
        public string versionNotes { get; set; }
        public object digitizedBySefaria { get; set; }
        public object priority { get; set; }
        public string versionTitleInHebrew { get; set; }
        public string versionNotesInHebrew { get; set; }
        public string extendedNotes { get; set; }
        public string extendedNotesHebrew { get; set; }
        public string purchaseInformationImage { get; set; }
        public string purchaseInformationURL { get; set; }
        public string shortVersionTitle { get; set; }
        public string shortVersionTitleInHebrew { get; set; }
        public object isBaseText { get; set; }
    }

    public class Alt
    {
        public List<string> en { get; set; }
        public List<string> he { get; set; }
        public bool whole { get; set; }
    }
}
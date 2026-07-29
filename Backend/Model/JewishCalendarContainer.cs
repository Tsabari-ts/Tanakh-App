using System;
using System.Collections.Generic;

namespace Tanakh.Model
{
    public class JewishCalendarContainer
    {
        public DateTime date { get; set; }
        public List<Item> items { get; set; }
        public Location location { get; set; }
        public Range range { get; set; }
        public string title { get; set; }
    }

    public class Location
    {
        public string admin1 { get; set; }
        public string asciiname { get; set; }
        public string cc { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string geo { get; set; }
        public int geonameid { get; set; }
        public float latitude { get; set; }
        public float longitude { get; set; }
        public string title { get; set; }
        public string tzid { get; set; }
    }

    public class Range
    {
        public string end { get; set; }
        public string start { get; set; }
    }

    public class Item
    {
        public string category { get; set; }
        public DateTime date { get; set; }
        public string hebrew { get; set; }
        public string memo { get; set; }
        public string subcat { get; set; }
        public string title { get; set; }
        public string hdate { get; set; }
        public Leyning leyning { get; set; }
        public string link { get; set; }
        public string title_orig { get; set; }
        public bool yomtov { get; set; }
    }

    public class Leyning
    {
        public string _1 { get; set; }
        public string _2 { get; set; }
        public string _3 { get; set; }
        public string torah { get; set; }
        public string _4 { get; set; }
        public string _5 { get; set; }
        public string _6 { get; set; }
        public string _7 { get; set; }
        public string haftarah { get; set; }
        public string maftir { get; set; }
        public string haftarah_sephardic { get; set; }
    }
}
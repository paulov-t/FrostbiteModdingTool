using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace v2k4FIFAModding.Career.CME.FIFA
{
    public class FIFALeague
    {
        public int countryid { get; set; }
        public string leaguename { get; set; }
        public int leaguetype { get; set; }
        public int level { get; set; }
        public int iscompetitionscarfenabled { get; set; }
        public int isbannerenabled { get; set; }
        public int leagueid { get; set; }
        public int iscompetitionpoleflagenabled { get; set; }
        public int iscompetitioncrowdcardsenabled { get; set; }
        public int leaguetimeslice { get; set; }
        public int iswithintransferwindow { get; set; }

        public static IEnumerable<FIFALeague> FIFALeagues;
        public static IEnumerable<FIFALeague> GetFIFALeagues()
        {
            if (FIFALeagues != null)
                return FIFALeagues;

            var dlllocation = Directory.GetParent(AppContext.BaseDirectory);
            var fulllocation = dlllocation + "\\CEM\\Data\\leagues.csv";
            using (var reader = new StreamReader(fulllocation))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                FIFALeagues = csv.GetRecords<FIFALeague>();
                return FIFALeagues;
            }

        }
    }
}

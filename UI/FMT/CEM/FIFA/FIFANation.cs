using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CareerExpansionMod.CEM.FIFA
{
    public class FIFANation
    {
        public string isocountrycode { get; set; }
        public string nationname { get; set; }
        public string confederation { get; set; }
        public string top_tier { get; set; }
        public string nationstartingfirstletter { get; set; }
        public string groupid { get; set; }
        public string nationid { get; set; }

        public static IEnumerable<FIFANation> FIFANations;
        public static IEnumerable<FIFANation> GetFIFANations()
        {
            if (FIFANations != null)
                return FIFANations;

            var dlllocation = Directory.GetParent(AppContext.BaseDirectory);
            var fulllocation = dlllocation + "\\CEM\\Data\\nations.csv";
            using (var reader = new StreamReader(fulllocation))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                FIFANations = csv.GetRecords<FIFANation>().ToList();
            }


            return FIFANations;


        }

    }
}

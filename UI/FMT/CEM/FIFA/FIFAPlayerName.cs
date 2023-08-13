using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using v2k4FIFAModding.Career.CME.FIFA;
using v2k4FIFAModdingCL;

namespace CareerExpansionMod.CEM.FIFA
{
    public class FIFAPlayerName
    {
        public string name { get; set; }
        public string nameid { get; set; }
        public string commentaryid { get; set; }

        public static IEnumerable<FIFAPlayerName> FIFAPlayerNames;
        public static IEnumerable<FIFAPlayerName> GetFIFAPlayerNames()
        {
            if (FIFAPlayerNames != null)
                return FIFAPlayerNames;

            var pnames = new List<FIFAPlayerName>();

            var dlllocation = AppContext.BaseDirectory;

            var fulllocation = dlllocation + "\\CEM\\Data\\playernames.csv";
            if (GameInstanceSingleton.Instance.GAMEVERSION == "FIFA21")
                fulllocation = dlllocation + "\\CEM\\Data\\playernames_f21.csv";


            using (var reader = new StreamReader(fulllocation))
            using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
            {
                //csv.Configuration.HeaderValidated = null;
                //csv.Configuration.MissingFieldFound = null;
                pnames = csv.GetRecords<FIFAPlayerName>().ToList();
            }

            // DLC / Squad File Names
            //fulllocation = dlllocation + "\\CEM\\Data\\dcplayernames.csv";
            //if (GameInstanceSingleton.Instance.GAMEVERSION == "FIFA21")
            //    fulllocation = dlllocation + "\\CEM\\Data\\dcplayernames_f21.csv";

            //using (var reader = new StreamReader(fulllocation))
            //using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
            //{
            //    csv.Configuration.HeaderValidated = null;
            //    csv.Configuration.MissingFieldFound = null;
            //    pnames.AddRange(csv.GetRecords<FIFAPlayerName>().ToList());
            //}

            FIFAPlayerNames = pnames;

            return FIFAPlayerNames;


        }

        //public static IDictionary<int,string> NamesInMemory = new 

        public static string GetNameFromFIFAPlayer(FIFAPlayer player)
        {
            var lstNames = GetFIFAPlayerNames();

            var firstname = lstNames.FirstOrDefault(x => x.nameid == player.firstnameid.ToString());
            var lastname = lstNames.FirstOrDefault(x => x.nameid == player.lastnameid.ToString());
            if (firstname != null && !string.IsNullOrEmpty(firstname.name) && lastname != null && !string.IsNullOrEmpty(lastname.name))
                return firstname.name + " " + lastname.name;

            var editplayername = CareerDB2.Current.editedplayernames.FirstOrDefault(x => x["playerid"].ToString() == player.playerid.ToString());
            if (editplayername != null)
                return editplayername["firstname"] + " " + editplayername["surname"];

            return string.Empty;
        }

    }
}

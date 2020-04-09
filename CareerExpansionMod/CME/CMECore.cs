﻿using CareerExpansionMod.CME.FIFA;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using v2k4FIFAModding.Career.CME.FIFA;
using v2k4FIFAModdingCL.CGFE;
using v2k4FIFAModdingCL.MemHack.Career;
using v2k4FIFAModdingCL.MemHack.Core;

namespace CareerExpansionMod.CME
{
    public class CMECore
    {
        public static string CMEMyDocumentsDirectory
        {
            get
            {
                var myDocuments = Microsoft.VisualBasic.FileIO.SpecialDirectories.MyDocuments + "\\FIFA 20\\CME\\";
                Directory.CreateDirectory(myDocuments);
                return myDocuments;
            }
        }

        public static CMECore CMECoreInstance;
        public CMECore()
        {

            // Initialize FIFA Leagues CSV to Enumerable
            FIFALeague.GetFIFALeagues();


            // Setup Singleton
            CMECoreInstance = this;
        }

        public void CoreHack_EventGameDateChanged(DateTime oldDate, DateTime newDate)
        {
            GameDateChanged(oldDate, newDate);
        }

        public CoreHack CoreHack = new CoreHack();
        public v2k4FIFAModdingCL.MemHack.Career.Finances Finances = new v2k4FIFAModdingCL.MemHack.Career.Finances();
        public Manager Manager = new Manager();

        public CareerFile CareerFile = null;
       


        internal void GameDateChanged(DateTime oldDate, DateTime newDate)
        {
            if (CreateCopyOfSave())
            {
                if (SetupCareerFile())
                {


                    // Refresh
                    v2k4FIFAModdingCL.MemHack.Career.Finances.GetTransferBudget();
                }
            }

        }

        private bool SetupCareerFile()
        {
            var baseDir = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
            var dataFolder = baseDir + "\\Data\\";
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            CareerFile = new CareerFile(dataFolder + CoreHack.SaveFileName, dataFolder + "fifa_ng_db-meta.XML");
            CareerFile.LoadXml(dataFolder + "fifa_ng_db-meta.XML");
            CareerFile.LoadEA(dataFolder + CoreHack.SaveFileName);
            // Setup Internal Career Entities
            CareerDB1.Current = new CareerDB1();
            CareerDB2.Current = new CareerDB2();


            if (CareerDB1.Current.career_users == null)
            {
                var usersDt = CareerFile.Databases[0].Table[3].ConvertToDataTable();
                CareerDB1.Current.career_users = new List<FIFAUsers>();
                    CareerDB1.Current.career_users.Add(CreateItemFromRow<FIFAUsers>(usersDt.Rows[0]));
            }

            if (CareerDB2.Current.teams == null)
            {
                tableNameToSearch = "teams";
                CareerDB2.Current.teams = new List<FIFATeam>();
                    var teamsDt = CareerFile.Databases[1].Table[3].ConvertToDataTable();
                    foreach (DataRow t in teamsDt.Rows)
                    {
                        if (CareerDB2.Current.teams != null)
                            CareerDB2.Current.teams.Add(CreateItemFromRow<FIFATeam>(t));
                    }
            }

            return true;
        }

        static string tableNameToSearch = "career_users";

        // function that creates an object from the given data row
        public static T CreateItemFromRow<T>(DataRow row) where T : new()
        {
            // create a new object
            T item = new T();

            // set the item
            SetItemFromRow(item, row);

            // return 
            return item;
        }

        public static void SetItemFromRow<T>(T item, DataRow row) where T : new()
        {
            // go through each column
            foreach (DataColumn c in row.Table.Columns)
            {
                // find the property for the column
                //PropertyInfo p = item.GetType().GetProperty(c.ColumnName);
                var long_name = GetColumnLongNameFromShortName(row.Table.TableName, c.ColumnName);
                if (!string.IsNullOrEmpty(long_name))
                {
                    PropertyInfo p = item.GetType().GetProperty(long_name);


                    // if exists, set the value
                    if (p != null && row[c] != DBNull.Value)
                    {
                        if (int.TryParse(row[c].ToString(), out int iV))
                            p.SetValue(item, iV, null);
                        else
                            p.SetValue(item, row[c], null);

                    }
                }
            }
        }

        public static string GetColumnLongNameFromShortName(string tableShortName, string shortName)
        {
            var baseDir = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
            var dataFolder = baseDir + "\\CME\\Data\\";
            var xmlMetaData = dataFolder + "fifa_ng_db-meta.XML";

            // Loading from a file, you can also load from a stream
            var xml = XDocument.Load(xmlMetaData);

            var table = from c in xml.Root.Descendants("table")
                        where (string)c.Attribute("name") == tableNameToSearch
                        select c;

            var columnName = from c in table.Descendants()
                             where (string)c.Attribute("shortname") == shortName
                             select (string)c.Attribute("name");

            
         return columnName.FirstOrDefault();

        }

        public static string GetTableLongNameFromShortName(string shortName)
        {
            var baseDir = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
            var dataFolder = baseDir + "\\CME\\Data\\";
            var xmlMetaData = dataFolder + "fifa_ng_db-meta.XML";

            // Loading from a file, you can also load from a stream
            var xml = XDocument.Load(xmlMetaData);

            var table = from c in xml.Root.Descendants("table")
                        where (string)c.Attribute("shortname") == shortName
                        select (string)c.Attribute("name");


            return table.FirstOrDefault();
        }

        private bool CreateCopyOfSave()
        {
            var baseDir = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
            var dataFolder = baseDir + "\\Data\\";
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);
            // backup save file
            var myDocuments = Microsoft.VisualBasic.FileIO.SpecialDirectories.MyDocuments + "\\FIFA 20\\settings\\";
#pragma warning disable CS0162 // Unreachable code detected
            for (int iAttempt = 0; iAttempt < 5; iAttempt++)
#pragma warning restore CS0162 // Unreachable code detected
            {
                try
                {
                    File.Copy(myDocuments + CoreHack.SaveFileName, dataFolder + CoreHack.SaveFileName, true);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }
}
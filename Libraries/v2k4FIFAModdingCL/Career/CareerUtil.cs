﻿using FifaLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using v2k4FIFAModdingCL;

namespace v2k4FIFAModding.Career
{
    public class CareerUtil
    {
        public static Dictionary<string,string> GetCareerSaves()
        {
            var myDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\"
                                            + GameInstanceSingleton.Instance.GAMEVERSION.Substring(0, 4) + " " + GameInstanceSingleton.Instance.GAMEVERSION.Substring(4, 2)
                                            + "\\settings\\";
            return GetCareerSaves(myDocs);
        }

        public static Dictionary<string, string> GetCareerSaves(string directory)
        {
            var myDocs = directory;

            var r = Directory.GetFiles(myDocs, "Career*", System.IO.SearchOption.AllDirectories);

            /// SWITCH THIS FOR 
            /*
             * 
             * DbReader dbReader = new DbReader(fileStream, FifaPlatform.PC);
            dbReader.BaseStream.Position = 18L;
            m_InGameName = FifaUtil.ReadNullTerminatedString(dbReader);
             */
            Dictionary<string, string> results = new Dictionary<string, string>();
            foreach (var i in r)
            {
                byte[] test = new byte[30];
                using (var fileStream = new FileStream(i, FileMode.Open))
                using (DbReader dbReader = new DbReader(fileStream, FifaPlatform.PC))
                {
                    dbReader.BaseStream.Position = 18L;
                    results.Add(i, FifaUtil.ReadNullTerminatedString(dbReader));
                }
            }

            return results;
        }

    }
}

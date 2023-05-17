using FrostySdk;
using FrostySdk.Frostbite.PluginInterfaces;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FIFA23Plugin
{
    public class TocSbReaderPGATour : IDisposable
    {
        private const uint ReadableSectionMagic = 3599661469;

        public AssetManager AssetManager { get; set; }

        public TOCFile TOCFile { get; set; }
        //public SBFile SBFile { get; set; }

        public int SBIndex { get; set; }


        /// <summary>
        /// Does the logging to Windows or Debuggers
        /// </summary>
        public bool DoLogging
        {
            get; set;
        } = true;


        /// <summary>
        /// Processes the Internal Data into the AssetManager EBX,RES,Chunk Lists
        /// </summary>
        public bool ProcessData = true;

        public string SbPath = string.Empty;

        public TocSbReaderPGATour()
        {

        }

        public TocSbReaderPGATour(in bool processData, in bool doLogging)
        {
            ProcessData = processData;
            DoLogging = doLogging;
        }

        public List<DbObject> Read(string tocPath, int sbIndex, string SBName, bool native_data = false, string nativePath = null)
        {
            if (nativePath == null)
                throw new ArgumentNullException("nativePath must be provided!");

            SBIndex = sbIndex;

            if (AssetManager == null)
                AssetManager = AssetManager.Instance;

            if (tocPath != "")
            {

                Debug.WriteLine($"[DEBUG] Loading TOC File: {tocPath}");
                List<DbObject> objs = new List<DbObject>();

                TOCFile = new TOCFile(nativePath, DoLogging, ProcessData, false);
                if (TOCFile.TOCObjects != null && TOCFile.TOCObjects.Count > 0)
                {
                    if (!ProcessData)
                        objs.AddRange(TOCFile.TOCObjects.List.Select(x => ((DbObject)x)));
                    else
                    {
                        foreach (var obj in TOCFile.Bundles)

                        {
                            AssetManager.Instance.AddBundle(obj.Name, BundleType.None, sbIndex);
                        }
                    }

                    //#if DEBUG
                    //                    var firstEntry = AssetManager.Instance.EBX.First();
                    //                    //AssetManager.Instance.AddEbx();
                    //                    var e = AssetManager.Instance.GetEbx(firstEntry.Value);
                    //#endif
                }



                return objs;
            }

            return null;
        }

        public void Dispose()
        {
            //if(TOCFile != null)
            //    TOCFile.Dispose();

            TOCFile = null;
        }
    }
}

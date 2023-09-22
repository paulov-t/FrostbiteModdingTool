using FifaLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FIFACareerDBTests
{
    [TestClass]
    public class FC24Tests : IDisposable
    {
        public Stream FC24TestSquadsResource
        {
            get
            {
                return FMT.FileTools.EmbeddedResourceHelper.GetEmbeddedResourceByName("FC24.Squads.Squads20230922190437");
            }
        }

        public Stream FC24ResourceDBMeta
        {
            get
            {
                return FMT.FileTools.EmbeddedResourceHelper.GetEmbeddedResourceByName("FC24.Db.fifa_ng_db-meta.XML");
            }
        }


        public Stream FC24ResourceLOCMeta
        {
            get
            {
                return FMT.FileTools.EmbeddedResourceHelper.GetEmbeddedResourceByName("FC24.Loc.eng_us-meta.XML");
            }
        }

        public void Dispose()
        {
        }

        [TestMethod]
        public void ReadFromSquadsFile()
        {
            CareerFile careerFile = new CareerFile(FC24TestSquadsResource, FC24ResourceDBMeta, "");
            if(careerFile != null)
            {

            }
        }

    }
}
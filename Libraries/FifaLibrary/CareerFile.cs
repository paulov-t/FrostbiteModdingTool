using System;
using System.Data;
using System.IO;

namespace FifaLibrary
{
    public class CareerFile : DbFile, IDisposable
    {

        public static CareerFile Current { get; set; }

        private string m_InGameName;

        protected string m_FileName;

        protected string m_XmlFileName;

        //protected DataSet m_DescriptorDataSet;

        //protected FifaPlatform m_Platform;

        //private char[] m_HeaderSignature;

        //private byte[] m_CryptArea;

        //private uint m_CrcEaHeader;

        //private long m_CrcEaHeaderPosition;

        private DbFile[] m_Database = new DbFile[4];

        private int m_NDatabases;

        private static uint[] s_CrcTable = new uint[256]
        {
            0u, 79764919u, 159529838u, 222504665u, 319059676u, 398814059u, 445009330u, 507990021u, 638119352u, 583659535u,
            797628118u, 726387553u, 890018660u, 835552979u, 1015980042u, 944750013u, 1276238704u, 1221641927u, 1167319070u, 1095957929u,
            1595256236u, 1540665371u, 1452775106u, 1381403509u, 1780037320u, 1859660671u, 1671105958u, 1733955601u, 2031960084u, 2111593891u,
            1889500026u, 1952343757u, 2552477408u, 2632100695u, 2443283854u, 2506133561u, 2334638140u, 2414271883u, 2191915858u, 2254759653u,
            3190512472u, 3135915759u, 3081330742u, 3009969537u, 2905550212u, 2850959411u, 2762807018u, 2691435357u, 3560074640u, 3505614887u,
            3719321342u, 3648080713u, 3342211916u, 3287746299u, 3467911202u, 3396681109u, 4063920168u, 4143685023u, 4223187782u, 4286162673u,
            3779000052u, 3858754371u, 3904687514u, 3967668269u, 881225847u, 809987520u, 1023691545u, 969234094u, 662832811u, 591600412u,
            771767749u, 717299826u, 311336399u, 374308984u, 453813921u, 533576470u, 25881363u, 88864420u, 134795389u, 214552010u,
            2023205639u, 2086057648u, 1897238633u, 1976864222u, 1804852699u, 1867694188u, 1645340341u, 1724971778u, 1587496639u, 1516133128u,
            1461550545u, 1406951526u, 1302016099u, 1230646740u, 1142491917u, 1087903418u, 2896545431u, 2825181984u, 2770861561u, 2716262478u,
            3215044683u, 3143675388u, 3055782693u, 3001194130u, 2326604591u, 2389456536u, 2200899649u, 2280525302u, 2578013683u, 2640855108u,
            2418763421u, 2498394922u, 3769900519u, 3832873040u, 3912640137u, 3992402750u, 4088425275u, 4151408268u, 4197601365u, 4277358050u,
            3334271071u, 3263032808u, 3476998961u, 3422541446u, 3585640067u, 3514407732u, 3694837229u, 3640369242u, 1762451694u, 1842216281u,
            1619975040u, 1682949687u, 2047383090u, 2127137669u, 1938468188u, 2001449195u, 1325665622u, 1271206113u, 1183200824u, 1111960463u,
            1543535498u, 1489069629u, 1434599652u, 1363369299u, 622672798u, 568075817u, 748617968u, 677256519u, 907627842u, 853037301u,
            1067152940u, 995781531u, 51762726u, 131386257u, 177728840u, 240578815u, 269590778u, 349224269u, 429104020u, 491947555u,
            4046411278u, 4126034873u, 4172115296u, 4234965207u, 3794477266u, 3874110821u, 3953728444u, 4016571915u, 3609705398u, 3555108353u,
            3735388376u, 3664026991u, 3290680682u, 3236090077u, 3449943556u, 3378572211u, 3174993278u, 3120533705u, 3032266256u, 2961025959u,
            2923101090u, 2868635157u, 2813903052u, 2742672763u, 2604032198u, 2683796849u, 2461293480u, 2524268063u, 2284983834u, 2364738477u,
            2175806836u, 2238787779u, 1569362073u, 1498123566u, 1409854455u, 1355396672u, 1317987909u, 1246755826u, 1192025387u, 1137557660u,
            2072149281u, 2135122070u, 1912620623u, 1992383480u, 1753615357u, 1816598090u, 1627664531u, 1707420964u, 295390185u, 358241886u,
            404320391u, 483945776u, 43990325u, 106832002u, 186451547u, 266083308u, 932423249u, 861060070u, 1041341759u, 986742920u,
            613929101u, 542559546u, 756411363u, 701822548u, 3316196985u, 3244833742u, 3425377559u, 3370778784u, 3601682597u, 3530312978u,
            3744426955u, 3689838204u, 3819031489u, 3881883254u, 3928223919u, 4007849240u, 4037393693u, 4100235434u, 4180117107u, 4259748804u,
            2310601993u, 2373574846u, 2151335527u, 2231098320u, 2596047829u, 2659030626u, 2470359227u, 2550115596u, 2947551409u, 2876312838u,
            2788305887u, 2733848168u, 3165939309u, 3094707162u, 3040238851u, 2985771188u
        };

        public string InGameName
        {
            get
            {
                return this.m_InGameName;
            }
            set
            {
                this.m_InGameName = value;
            }
        }

        public string FileName => this.m_FileName;

        public string XmlFileName => this.m_XmlFileName;

        //public DataSet DescriptorDataSet
        //{
        //	get
        //	{
        //		return this.m_DescriptorDataSet;
        //	}
        //	set
        //	{
        //		this.m_DescriptorDataSet = value;
        //	}
        //}

        //public FifaPlatform Platform
        //{
        //	get
        //	{
        //		return this.m_Platform;
        //	}
        //	set
        //	{
        //		this.m_Platform = value;
        //	}
        //}

        public DbFile[] Databases => this.m_Database;

        public int NDatabases => this.m_NDatabases;

        private bool isFIFA23 { get; set; }

        public CareerFile(string careerFileName, string xmlFileName)
        {
            this.m_FileName = careerFileName;
            this.m_XmlFileName = xmlFileName;
            this.m_DescriptorDataSet = null;
            this.Load();

            isFIFA23 = careerFileName.Contains("FIFA 23", System.StringComparison.OrdinalIgnoreCase)
                || careerFileName.Contains("FIFA23", System.StringComparison.OrdinalIgnoreCase);

            Current = this;
        }

        public CareerFile(Stream careerStream, Stream xmlStream, string careerFileName)
        {
            if (File.Exists("TempCareerXmlMeta.xml"))
                File.Delete("TempCareerXmlMeta.xml");

            MemoryStream msXmlMeta = new MemoryStream();
            xmlStream.CopyTo(msXmlMeta);
            File.WriteAllBytes("TempCareerXmlMeta.xml", msXmlMeta.ToArray());
            LoadXml("TempCareerXmlMeta.xml");

            OriginalFileName = careerFileName;
            DbReader dbReader = new DbReader(careerStream, FifaPlatform.PC);
            dbReader.BaseStream.Position = 18L;
            this.m_InGameName = FifaUtil.ReadNullTerminatedString(dbReader);
            while (dbReader.BaseStream.Position < dbReader.BaseStream.Length)
            {
                this.m_Database[this.m_NDatabases] = new DbFile();
                this.m_Database[this.m_NDatabases].DescriptorDataSet = this.m_DescriptorDataSet;
                if (!this.m_Database[this.m_NDatabases].LoadDb(dbReader, skipData: true))
                {
                    break;
                }
                this.m_NDatabases++;
                if (this.m_NDatabases == 3)
                {
                    break;
                }
            }
            dbReader.Close();
            Current = this;
        }

        public bool Load()
        {
            if (!this.LoadXml())
            {
                return false;
            }
            return this.LoadEA(this.m_FileName);
        }

        public bool LoadXml(string xmlFileName)
        {
            if (!File.Exists(xmlFileName))
            {
                return false;
            }
            this.m_XmlFileName = xmlFileName;
            this.m_DescriptorDataSet = new DataSet("XML_Descriptor");
            this.m_DescriptorDataSet.ReadXml(this.m_XmlFileName);
            return true;
        }

        public bool LoadXml()
        {
            if (this.m_XmlFileName == null)
            {
                return false;
            }
            return this.LoadXml(this.m_XmlFileName);
        }

        public string OriginalFileName { get; private set; }

        public bool LoadEA(string fileName)
        {
            OriginalFileName = fileName;
            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            DbReader dbReader = new DbReader(fileStream, FifaPlatform.PC);
            dbReader.BaseStream.Position = 18L;
            this.m_InGameName = FifaUtil.ReadNullTerminatedString(dbReader);
            while (dbReader.BaseStream.Position < dbReader.BaseStream.Length)
            {
                this.m_Database[this.m_NDatabases] = new DbFile();
                this.m_Database[this.m_NDatabases].DescriptorDataSet = this.m_DescriptorDataSet;
                if (!this.m_Database[this.m_NDatabases].LoadDb(dbReader, skipData: true))
                {
                    break;
                }
                this.m_NDatabases++;
                if (this.m_NDatabases == 3)
                {
                    break;
                }
            }
            dbReader.Close();
            fileStream.Close();
            return true;
        }

        public bool SaveEa(string fileName, string templateName)
        {
            string fileName2 = Path.GetFileName(fileName);
            if (fileName2.StartsWith("Squad"))
            {
                return this.SaveEa(fileName);
            }
            if (fileName2.StartsWith("Career"))
            {
                return this.SaveEa(fileName);
            }
            return false;
        }


        public bool SaveEa(string fileName)
        {
            FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite);
            DbWriter dbWriter = new DbWriter(fileStream, this.m_Platform);
            DbReader dbReader = new DbReader(fileStream, this.m_Platform);
            dbReader.BaseStream.Position = 18L;
            int num = 0;
            while (dbReader.ReadByte() != 0)
            {
                num++;
            }
            while (dbReader.ReadByte() == 0)
            {
                num++;
            }
            dbWriter.BaseStream.Position = 18L;
            if (this.m_InGameName.Length > num)
            {
                this.m_InGameName = this.m_InGameName.Substring(0, num);
            }
            FifaUtil.WriteNullPaddedString(dbWriter, this.m_InGameName, num);
            dbWriter.BaseStream.Position = 118L;
            if (!isFIFA23)
            {
                for (int i = 0; i < 8; i++)
                {
                    dbWriter.Write((byte)0);
                }
            }
            dbWriter.BaseStream.Position = 218L;
            for (int i = 0; i < 4; i++)
            {
                dbWriter.Write((byte)0);
            }
            for (int j = 0; j < this.m_NDatabases; j++)
            {
                dbWriter.BaseStream.Position = this.m_Database[j].SignaturePosition;
                this.m_Database[j].SaveDb(dbWriter);
            }
            for (int k = 0; k < this.m_NDatabases; k++)
            {
                this.m_Database[k].ComputeAllCrc(dbReader, dbWriter);
            }
            dbReader.Close();
            dbWriter.Close();
            fileStream.Close();
            return true;
        }

        public bool ExportDB(int dbIndex, string fileName)
        {
            FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
            DbWriter dbWriter = new DbWriter(fileStream, this.m_Platform);
            DbReader dbReader = new DbReader(fileStream, this.m_Platform);
            if (dbIndex >= this.m_NDatabases)
            {
                return false;
            }
            long signaturePosition = this.m_Database[dbIndex].SignaturePosition;
            this.m_Database[dbIndex].SignaturePosition = 0L;
            this.m_Database[dbIndex].SaveDb(dbWriter);
            this.m_Database[dbIndex].ComputeAllCrc(dbReader, dbWriter);
            this.m_Database[dbIndex].SignaturePosition = signaturePosition;
            dbReader.Close();
            dbWriter.Close();
            fileStream.Close();
            return true;
        }

        private uint ComputeCrc(BinaryReader r, int offset, int size)
        {
            uint num = uint.MaxValue;
            r.BaseStream.Position = offset;
            for (int i = 0; i < size; i++)
            {
                byte b = r.ReadByte();
                num = CareerFile.s_CrcTable[((num >> 24) ^ b) & 0xFF] ^ (num << 8);
            }
            return num ^ 0xFFFFFFFFu;
        }

        private uint ComputeChecksum24(BinaryReader r)
        {
            r.BaseStream.Position = 0L;
            uint num = r.ReadByte();
            num <<= 8;
            num |= r.ReadByte();
            num <<= 8;
            num |= r.ReadByte();
            num <<= 8;
            num |= r.ReadByte();
            num ^= 0xFFFFFFFFu;
            for (int num2 = 20; num2 > 0; num2--)
            {
                byte b = r.ReadByte();
                uint num3 = CareerFile.s_CrcTable[num >> 24];
                num <<= 8;
                num |= b;
                num ^= num3;
            }
            return num ^ 0xFFFFFFFFu;
        }

        public void ConvertFromDataSet(DataSet[] dataSet)
        {
            if (dataSet.Length == this.m_NDatabases)
            {
                for (int i = 0; i < this.m_NDatabases; i++)
                {
                    this.m_Database[i].ConvertFromDataSet(dataSet[i]);
                }
            }
        }

        public new DataSet[] ConvertToDataSet()
        {
            DataSet[] array = new DataSet[this.m_NDatabases];
            for (int i = 0; i < this.m_NDatabases; i++)
            {
                array[i] = this.m_Database[i].ConvertToDataSet();
            }
            return array;
        }
    }
}

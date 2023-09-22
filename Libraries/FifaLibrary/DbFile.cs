using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace FifaLibrary
{
    public class DbFile : IDisposable
    {
        protected char[] m_Signature;

        protected int m_FileLength;

        private long m_SignaturePosition;

        private uint m_CrcHeader;

        private long m_CrcHeaderPosition;

        private uint m_CrcShortNames;

        private long m_CrcShortNamesPosition;

        private uint[] m_TableOffset;

        private FifaTable[] m_Table;

        protected int m_NTables;

        protected DataSet m_DescriptorDataSet;

        private bool m_NeedToSaveXmlFile;

        protected FifaPlatform m_Platform;

        public long SignaturePosition
        {
            get
            {
                return m_SignaturePosition;
            }
            set
            {
                m_SignaturePosition = value;
            }
        }

        public FifaTable[] Table
        {
            get
            {
                return m_Table;
            }
            set
            {
                m_Table = value;
            }
        }

        public int NTables => m_NTables;

        public DataSet DescriptorDataSet
        {
            get
            {
                return m_DescriptorDataSet;
            }
            set
            {
                m_DescriptorDataSet = value;
            }
        }

        public FifaPlatform Platform
        {
            get
            {
                return m_Platform;
            }
            set
            {
                m_Platform = value;
            }
        }

        Stream DBStream;
        Stream XmlStream;

        public DbFile()
        {

        }

        public DbFile(Stream dbStream, Stream xmlStream)
        {
            DBStream = dbStream;
            XmlStream = xmlStream;
            m_DescriptorDataSet = null;
            m_Platform = FifaPlatform.PC;
            LoadXml(XmlStream);
            LoadDb(DBStream);
        }

        public void ComputeAllCrc(DbReader r, DbWriter w)
        {
            long signaturePosition = m_SignaturePosition;
            int num = (int)(m_CrcHeaderPosition - signaturePosition);
            ComputeAndWriteCrc(r, w, signaturePosition, num);
            signaturePosition += num + 4;
            num = (int)(m_CrcShortNamesPosition - signaturePosition);
            ComputeAndWriteCrc(r, w, signaturePosition, num);
            for (int i = 0; i < m_NTables; i++)
            {
                signaturePosition += num + 4;
                num = (int)(m_Table[i].CrcTableHeaderPosition - signaturePosition);
                ComputeAndWriteCrc(r, w, signaturePosition, num);
                signaturePosition += num + 4;
                num = (int)(m_Table[i].CrcRecordsPosition - signaturePosition);
                ComputeAndWriteCrc(r, w, signaturePosition, num);
            }
        }

        public bool ComputeAllCrc()
        {
            DbWriter dbWriter = new DbWriter(DBStream, m_Platform);
            DbReader dbReader = new DbReader(DBStream, m_Platform);
            ComputeAllCrc(dbReader, dbWriter);
            dbReader.Close();
            dbWriter.Close();
            DBStream.Close();
            return true;
        }

        public int ComputeAndWriteCrc(DbReader r, DbWriter w, long offset, int count)
        {
            byte[] array = new byte[count];
            r.BaseStream.Seek(offset, SeekOrigin.Begin);
            array = r.ReadBytes(count);
            int num = FifaUtil.ComputeCrcDb11(array);
            w.Write(num);
            return num;
        }

        public bool LoadDb(Stream dbStream)
        {
            DbReader dbReader = new DbReader(dbStream, FifaPlatform.PC);
            bool result = LoadDb(dbReader, skipData: false);
            //dbReader.Close();
            //dbStream.Close();
            return result;
        }

        public bool LoadDb(DbReader r, bool skipData)
        {
            bool flag = false;
            if (skipData)
            {
                while (r.BaseStream.Position <= r.BaseStream.Length - 4)
                {
                    if (r.ReadUInt32() == 134234692)
                    {
                        flag = true;
                        r.BaseStream.Position -= 4L;
                        break;
                    }
                    r.BaseStream.Position -= 3L;
                }
                if (!flag)
                {
                    return false;
                }
            }
            flag = true;
            m_SignaturePosition = r.BaseStream.Position;
            m_Signature = r.ReadChars(8);
            if (m_Signature[0] != 'D' || m_Signature[1] != 'B' || m_Signature[2] != 0 || m_Signature[3] != '\b' || m_Signature[5] != 0 || m_Signature[6] != 0 || m_Signature[7] != 0)
            {
                return false;
            }
            if (m_Signature[4] == '\0')
            {
                m_Platform = FifaPlatform.PC;
                r.Platform = m_Platform;
            }
            else
            {
                if (m_Signature[4] != '\u0001')
                {
                    return false;
                }
                m_Platform = FifaPlatform.XBox;
                r.Platform = m_Platform;
            }
            m_FileLength = r.ReadInt32();
            if (m_FileLength > r.BaseStream.Length)
            {
                return false;
            }
            r.ReadInt32();
            m_NTables = r.ReadInt32();
            m_TableOffset = new uint[m_NTables];
            TableDescriptor.DescriptorDataSet = m_DescriptorDataSet;
            m_Table = new FifaTable[m_NTables];

            m_CrcHeaderPosition = r.BaseStream.Position;
            m_CrcHeader = r.ReadUInt32();
            for (int i = 0; i < m_NTables; i++)
            {
                m_Table[i] = new FifaTable();
                m_Table[i].LoadTableName(r);
                m_TableOffset[i] = r.ReadUInt32();
            }
            m_CrcShortNamesPosition = r.BaseStream.Position;
            m_CrcShortNames = r.ReadUInt32();
            long position = r.BaseStream.Position;
            for (int j = 0; j < m_NTables; j++)
            {

                m_Table[j].Load(r, position + m_TableOffset[j]);
            }
            return flag;
        }

        public bool LoadXml(Stream xmlStream)
        {
            XmlStream = xmlStream;
            m_NeedToSaveXmlFile = false;
            m_DescriptorDataSet = new DataSet("XML_Descriptor");
            xmlStream.Position = 0;
            m_DescriptorDataSet.ReadXml(xmlStream);
            return true;
        }

        public Stream SaveDb()
        {
            DbWriter dbWriter = new DbWriter(DBStream, m_Platform);
            SaveDb(dbWriter);
            dbWriter.Close();
            ComputeAllCrc();
            return DBStream;
        }

        public bool SaveDb(DbWriter w)
        {

            w.Write(m_Signature);
            long position = w.BaseStream.Position;
            w.Write(-1L);
            w.Write(m_NTables);
            m_CrcHeaderPosition = w.BaseStream.Position;
            w.Write(-1);
            long position2 = w.BaseStream.Position + 4;
            for (int i = 0; i < m_NTables; i++)
            {
                w.Write(m_Table[i].TableDescriptor.ShortName);
                w.Write(-1);
            }
            m_CrcShortNamesPosition = w.BaseStream.Position;
            w.Write(-1);
            long position3 = w.BaseStream.Position;
            for (int j = 0; j < m_NTables; j++)
            {

                m_TableOffset[j] = (uint)(w.BaseStream.Position - position3);
                m_Table[j].Save(w);
            }
            m_FileLength = (int)(w.BaseStream.Position - m_SignaturePosition);
            w.BaseStream.Position = position;
            w.Write(m_FileLength);
            w.Write(0);
            w.BaseStream.Position = position2;
            for (int k = 0; k < m_NTables; k++)
            {
                w.Write(m_TableOffset[k]);
                w.BaseStream.Position += 4L;
            }
            w.Seek(0, SeekOrigin.End);

            return true;
        }

        public bool SaveXml(string xmlFileName)
        {
            if (m_NeedToSaveXmlFile)
            {
                File.Copy(xmlFileName, xmlFileName + ".bak", overwrite: true);
                m_DescriptorDataSet.WriteXml(xmlFileName, XmlWriteMode.IgnoreSchema);
            }
            m_NeedToSaveXmlFile = false;
            return true;
        }

        public DataSet ConvertToDataSet()
        {
            if (m_NTables == 0)
            {
                return null;
            }

            DataSet dataSet = new DataSet();
            for (int i = 0; i < m_NTables; i++)
            {

                dataSet.Tables.Add(Table[i].ConvertToDataTable());
            }

            return dataSet;
        }

        public void ConvertFromDataSet(DataSet dataSet)
        {
            for (int i = 0; i < m_NTables; i++)
            {

                Table[i].ConvertFromDataTable(dataSet.Tables[i]);
            }

        }

        public int GetTableIndex(string tableName)
        {
            for (int i = 0; i < NTables; i++)
            {
                if (Table[i].TableDescriptor.TableName == tableName)
                {
                    return i;
                }
            }
            return -1;
        }

        public bool Expand()
        {
            bool flag = ExpandTableField("formations", "formationid", 11);
            bool num = true && !flag;
            RecalculateFieldOffset("formations");
            flag = ExpandTableField("teamformationteamstylelinks", "formationid", 11);
            bool num2 = num && !flag;
            RecalculateFieldOffset("teamformationteamstylelinks");
            flag = ExpandTableField("temp_formations", "formationid", 11);
            bool num3 = num2 && !flag;
            RecalculateFieldOffset("temp_formations");
            flag = ExpandTableField("customformations", "formationid", 11);
            bool num4 = num3 && !flag;
            RecalculateFieldOffset("customformations");
            flag = ExpandTableField("teamsheets", "teamsheetid", 11);
            bool num5 = num4 && !flag;
            RecalculateFieldOffset("teamsheetid");
            flag = ExpandTableField("playernames", "nameid", 16);
            bool num6 = num5 && !flag;
            RecalculateFieldOffset("playernames");
            flag = ExpandTableField("career_lastnames", "lastname", 16);
            bool num7 = num6 && !flag;
            RecalculateFieldOffset("career_lastnames");
            flag = ExpandTableField("career_firstnames", "firstname", 16);
            bool num8 = num7 && !flag;
            RecalculateFieldOffset("career_firstnames");
            flag = ExpandTableField("career_commonnames", "firstname", 16);
            bool num9 = num8 && !flag;
            flag = ExpandTableField("career_lastnames", "lastname", 16);
            bool num10 = num9 && !flag;
            RecalculateFieldOffset("career_commonnames");
            flag = ExpandTableField("trainingteamplayernames", "nameid", 16);
            bool num11 = num10 && !flag;
            RecalculateFieldOffset("trainingteamplayernames");
            flag = ExpandTableField("players", "firstnameid", 16);
            bool num12 = num11 && !flag;
            flag = ExpandTableField("players", "lastnameid", 16);
            bool num13 = num12 && !flag;
            flag = ExpandTableField("players", "commonnameid", 16);
            bool num14 = num13 && !flag;
            flag = ExpandTableField("players", "playerjerseynameid", 16);
            bool num15 = num14 && !flag;
            RecalculateFieldOffset("players");
            flag = ExpandTableField("trainingteamplayernames", "firstnameid", 16);
            bool num16 = num15 && !flag;
            flag = ExpandTableField("trainingteamplayernames", "lastnameid", 16);
            bool num17 = num16 && !flag;
            flag = ExpandTableField("trainingteamplayernames", "commonnameid", 16);
            bool num18 = num17 && !flag;
            flag = ExpandTableField("trainingteamplayernames", "playerjerseynameid", 16);
            bool num19 = num18 && !flag;
            RecalculateFieldOffset("trainingteamplayernames");
            flag = ExpandTableField("referee", "refereeid", 10);
            bool num20 = num19 && !flag;
            RecalculateFieldOffset("referee");
            flag = ExpandTableField("leaguerefereelinks", "refereeid", 10);
            bool result = num20 && !flag;
            RecalculateFieldOffset("leaguerefereelinks");
            m_NeedToSaveXmlFile = true;
            return result;
        }

        private bool ExpandTableField(string tableName, string fieldName, int nBits)
        {
            return GetTable(tableName)?.ExpandField(fieldName, nBits) ?? false;
        }

        private bool ExpandTableField(string tableName, string fieldName, int nBits, int minValue)
        {
            return GetTable(tableName)?.ExpandField(fieldName, nBits, minValue) ?? false;
        }

        private bool RecalculateFieldOffset(string tableName)
        {
            FifaTable table = GetTable(tableName);
            if (table == null)
            {
                return false;
            }
            table.TableDescriptor.RecalculateFieldOffset();
            return true;
        }

        public IEnumerable<FifaTable> GetTables()
        {
            for (int i = 0; i < m_NTables; i++)
            {
                yield return m_Table[i];
            }
        }

        public FifaTable GetTable(string longName)
        {
            for (int i = 0; i < m_NTables; i++)
            {
                if (m_Table[i].TableDescriptor.TableName == longName)
                {
                    return m_Table[i];
                }
            }
            return null;
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);

            CareerFile.Current = null;
        }

        ~DbFile() => Dispose(false);

        // To detect redundant calls
        private bool _disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            // Managed Resources
            if (disposing)
            {
                if (DBStream != null && DBStream.CanSeek)
                {
                    DBStream.Close();
                    DBStream.Dispose();
                }

                if (XmlStream != null && XmlStream.CanSeek)
                {
                    XmlStream.Close();
                    XmlStream.Dispose();
                }
            }
        }
    }
}

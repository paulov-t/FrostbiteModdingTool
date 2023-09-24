using System;
using System.Data;
using System.Globalization;
using System.IO;

namespace FifaLibrary
{
    public class Record
    {
        private int[] m_CompressedStringOffset;

        private int[] m_CompressedStringFieldIndex;

        private string[] m_CompressedString;

        private static int m_HuffmannTreeSize;

        private bool m_IsInvalid;

        private int[] m_KeyField;

        private float[] m_FloatField;

        private int[] m_PtrToStringField;

        private int[] m_IntField;

        private TableDescriptor m_TableDescriptor;

        private string[] m_StringField;

        public int[] CompressedStringOffset
        {
            get
            {
                return m_CompressedStringOffset;
            }
            set
            {
                m_CompressedStringOffset = value;
            }
        }

        public int[] CompressedStringFieldIndex
        {
            get
            {
                return m_CompressedStringFieldIndex;
            }
            set
            {
                m_CompressedStringFieldIndex = value;
            }
        }

        public string[] CompressedString
        {
            get
            {
                return m_CompressedString;
            }
            set
            {
                m_CompressedString = value;
            }
        }

        public static int HuffmannTreeSize
        {
            get
            {
                return m_HuffmannTreeSize;
            }
            set
            {
                m_HuffmannTreeSize = value;
            }
        }

        public bool IsInvalid
        {
            get
            {
                return m_IsInvalid;
            }
            set
            {
                m_IsInvalid = value;
            }
        }

        public int[] KeyField
        {
            get
            {
                return m_KeyField;
            }
            set
            {
                m_KeyField = value;
            }
        }

        public float[] FloatField
        {
            get
            {
                return m_FloatField;
            }
            set
            {
                m_FloatField = value;
            }
        }

        public int[] PtrToStringField
        {
            get
            {
                return m_PtrToStringField;
            }
            set
            {
                m_PtrToStringField = value;
            }
        }

        public int[] IntField
        {
            get
            {
                return m_IntField;
            }
            set
            {
                m_IntField = value;
            }
        }

        public TableDescriptor TableDescriptor
        {
            get
            {
                return m_TableDescriptor;
            }
            set
            {
                m_TableDescriptor = value;
            }
        }

        public string[] StringField
        {
            get
            {
                return m_StringField;
            }
            set
            {
                m_StringField = value;
            }
        }

        public int GetIntField(string fieldName)
        {
            for (int i = 0; i < TableDescriptor.NFields; i++)
            {
                if (TableDescriptor.FieldDescriptors[i].FieldName == fieldName)
                {
                    return m_IntField[TableDescriptor.FieldDescriptors[i].TypeIndex];
                }
            }
            return 0;
        }

        public bool SetField(string fieldName, int value)
        {
            for (int i = 0; i < TableDescriptor.NFields; i++)
            {
                if (TableDescriptor.FieldDescriptors[i].FieldName == fieldName)
                {
                    m_IntField[TableDescriptor.FieldDescriptors[i].TypeIndex] = value;
                    return true;
                }
            }
            return false;
        }

        public float GetFloatField(string fieldName)
        {
            for (int i = 0; i < TableDescriptor.NFields; i++)
            {
                if (TableDescriptor.FieldDescriptors[i].FieldName == fieldName)
                {
                    return m_FloatField[TableDescriptor.FieldDescriptors[i].TypeIndex];
                }
            }
            return float.NaN;
        }

        public bool SetField(string fieldName, float value)
        {
            for (int i = 0; i < TableDescriptor.NFields; i++)
            {
                if (TableDescriptor.FieldDescriptors[i].FieldName == fieldName)
                {
                    m_FloatField[TableDescriptor.FieldDescriptors[i].TypeIndex] = value;
                    return true;
                }
            }
            return false;
        }

        public string GetStringField(string fieldName)
        {
            for (int i = 0; i < TableDescriptor.NFields; i++)
            {
                if (TableDescriptor.FieldDescriptors[i].FieldName == fieldName)
                {
                    if (TableDescriptor.FieldDescriptors[i].FieldType == FieldDescriptor.EFieldTypes.String)
                    {
                        return m_StringField[TableDescriptor.FieldDescriptors[i].TypeIndex];
                    }
                    return m_CompressedString[TableDescriptor.FieldDescriptors[i].TypeIndex];
                }
            }
            return string.Empty;
        }

        public bool SetField(string fieldName, string value)
        {
            for (int i = 0; i < TableDescriptor.NFields; i++)
            {
                if (TableDescriptor.FieldDescriptors[i].FieldName == fieldName)
                {
                    if (TableDescriptor.FieldDescriptors[i].FieldType == FieldDescriptor.EFieldTypes.String)
                    {
                        m_StringField[TableDescriptor.FieldDescriptors[i].TypeIndex] = value;
                    }
                    else
                    {
                        m_CompressedString[TableDescriptor.FieldDescriptors[i].TypeIndex] = value;
                    }
                    return true;
                }
            }
            return false;
        }

        public Record(TableDescriptor tableDescriptor)
        {
            m_TableDescriptor = tableDescriptor;
            if (m_TableDescriptor.NKeyFields > 0)
            {
                m_KeyField = new int[m_TableDescriptor.NKeyFields];
            }
            if (m_TableDescriptor.NFloatFields > 0)
            {
                m_FloatField = new float[m_TableDescriptor.NFloatFields];
            }
            if (m_TableDescriptor.NStringFields > 0)
            {
                m_PtrToStringField = new int[m_TableDescriptor.NStringFields];
                m_StringField = new string[m_TableDescriptor.NStringFields];
            }
            if (m_TableDescriptor.NCompressedStringFields > 0)
            {
                m_CompressedStringOffset = new int[m_TableDescriptor.NCompressedStringFields];
                m_CompressedString = new string[m_TableDescriptor.NCompressedStringFields];
                m_CompressedStringFieldIndex = new int[m_TableDescriptor.NCompressedStringFields];
            }
            if (m_TableDescriptor.NIntFields > 0)
            {
                m_IntField = new int[m_TableDescriptor.NIntFields];
            }
        }

        public void LoadKeyFields(DbReader r)
        {
            if (m_TableDescriptor.NKeyFields > 0)
            {
                for (int i = 0; i < m_TableDescriptor.NKeyFields; i++)
                {
                    m_KeyField[i] = r.ReadInt32();
                }
            }
        }

        public void LoadFloatFields(DbReader r)
        {
            if (m_TableDescriptor.NFloatFields > 0)
            {
                for (int i = 0; i < m_TableDescriptor.NFloatFields; i++)
                {
                    m_FloatField[i] = r.ReadSingle();
                }
            }
        }

        public void LoadStringFields(DbReader r)
        {
            if (m_TableDescriptor.NStringFields > 0)
            {
                for (int i = 0; i < m_TableDescriptor.NStringFields; i++)
                {
                    m_PtrToStringField[i] = r.ReadInt32();
                    m_StringField[i] = FifaUtil.ReadString(r, m_PtrToStringField[i]);
                }
            }
        }

        public bool LoadIntFields(DbReader r)
        {
            int num = 0;
            byte b = 0;
            bool result = true;
            int num2 = 0;
            for (int i = 0; i < m_TableDescriptor.NIntFields; i++)
            {
                int num3 = FifaUtil.ComputeBitUsed((uint)(m_TableDescriptor.MaxValues[i] - m_TableDescriptor.MinValues[i]));
                if (num3 == 32)
                {
                    m_IntField[i] = r.ReadInt32();
                    continue;
                }
                int num4 = 0;
                for (uint num5 = 0u; num5 < num3; num5++)
                {
                    if (num == 0)
                    {
                        b = r.ReadByte();
                        num = 8;
                        num2++;
                    }
                    num4 *= 2;
                    if ((b & (1 << num - 1)) != 0)
                    {
                        num4++;
                    }
                    num--;
                }
                m_IntField[i] = num4 + m_TableDescriptor.MinValues[i];
                _ = m_IntField[i] <= m_TableDescriptor.MaxValues[i];
            }
            return result;
        }

        public bool LoadCompressedStrings(DbReader r)
        {
            long position = r.BaseStream.Position;
            short num = 0;
            for (int i = 0; i < m_TableDescriptor.NCompressedStringFields; i++)
            {
                int num2 = m_CompressedStringFieldIndex[i];
                num = 0;
                if (m_CompressedStringOffset[i] != -1)
                {
                    r.BaseStream.Position = position + m_CompressedStringOffset[i];
                    switch (m_TableDescriptor.FieldDescriptors[num2].FieldType)
                    {
                        case FieldDescriptor.EFieldTypes.ShortCompressedString:
                            num = r.ReadByte();
                            break;
                        case FieldDescriptor.EFieldTypes.LongCompressedString:
                            num = r.ReadInt16();
                            num = FifaUtil.SwapEndian(num);
                            break;
                    }
                }
                m_CompressedString[i] = m_TableDescriptor.HuffmannTree.ReadString(r, num);
            }
            return true;
        }

        public bool Load(DbReader r)
        {
            long position = r.BaseStream.Position;
            int recordSize = m_TableDescriptor.RecordSize;
            for (int i = 0; i < m_TableDescriptor.NFields; i++)
            {
                int num = m_TableDescriptor.FieldDescriptors[i].BitOffset / 8;
                int typeIndex = m_TableDescriptor.FieldDescriptors[i].TypeIndex;
                switch (m_TableDescriptor.FieldDescriptors[i].FieldType)
                {
                    case FieldDescriptor.EFieldTypes.String:
                        {
                            r.Align(position + num);
                            int length = m_TableDescriptor.FieldDescriptors[i].Depth / 8;
                            m_StringField[typeIndex] = FifaUtil.ReadNullPaddedString(r, length);
                            break;
                        }
                    case FieldDescriptor.EFieldTypes.Integer:
                        m_IntField[typeIndex] = r.PopInteger(m_TableDescriptor.FieldDescriptors[i]);
                        break;
                    case FieldDescriptor.EFieldTypes.Float:
                        r.Align(position + num);
                        m_FloatField[typeIndex] = r.ReadSingle();
                        break;
                    case FieldDescriptor.EFieldTypes.ShortCompressedString:
                    case FieldDescriptor.EFieldTypes.LongCompressedString:
                        {
                            r.Align(position + num);
                            _ = m_TableDescriptor.FieldDescriptors[i].Depth / 8;
                            int num2 = r.ReadInt32();
                            m_CompressedStringOffset[typeIndex] = num2;
                            m_CompressedStringFieldIndex[typeIndex] = i;
                            if (num2 != -1 && num2 < m_HuffmannTreeSize)
                            {
                                m_HuffmannTreeSize = num2;
                            }
                            if (i < m_TableDescriptor.NFields - 1 && m_TableDescriptor.FieldDescriptors[i + 1].BitOffset != 32)
                            {
                                r.Align(position + m_TableDescriptor.FieldDescriptors[i + 1].BitOffset / 8);
                            }
                            break;
                        }
                }
            }
            r.BaseStream.Position = position + recordSize - 1;
            m_IsInvalid = (r.ReadByte() & 0x80) != 0;
            r.Align(position + recordSize);
            return true;
        }

        public bool Clean()
        {
            for (int i = 0; i < m_TableDescriptor.NFields; i++)
            {
                _ = m_TableDescriptor.FieldDescriptors[i].BitOffset / 8;
                int typeIndex = m_TableDescriptor.FieldDescriptors[i].TypeIndex;
                switch (m_TableDescriptor.FieldDescriptors[i].FieldType)
                {
                    case FieldDescriptor.EFieldTypes.String:
                        _ = m_TableDescriptor.FieldDescriptors[i].Depth / 8;
                        m_StringField[typeIndex] = string.Empty;
                        break;
                    case FieldDescriptor.EFieldTypes.Integer:
                        m_IntField[typeIndex] = m_TableDescriptor.FieldDescriptors[i].RangeLow;
                        break;
                    case FieldDescriptor.EFieldTypes.Float:
                        m_FloatField[typeIndex] = 0f;
                        break;
                }
            }
            return true;
        }

        public bool IsClean()
        {
            bool isClean = true;
            for (int i = 0; i < m_TableDescriptor.NFields; i++)
            {
                _ = m_TableDescriptor.FieldDescriptors[i].BitOffset / 8;
                int typeIndex = m_TableDescriptor.FieldDescriptors[i].TypeIndex;
                switch (m_TableDescriptor.FieldDescriptors[i].FieldType)
                {
                    case FieldDescriptor.EFieldTypes.String:
                        if (m_StringField[typeIndex] != string.Empty)
                        {
                            isClean = false;
                        }
                        break;
                    case FieldDescriptor.EFieldTypes.Integer:
                        if (m_IntField[typeIndex] != m_TableDescriptor.FieldDescriptors[i].RangeLow)
                        {
                            isClean = false;
                        }
                        break;
                    case FieldDescriptor.EFieldTypes.Float:
                        if (m_FloatField[typeIndex] != 0f)
                        {
                            isClean = false;
                        }
                        break;
                }
                if (!isClean)
                {
                    throw new Exception("Row isn't clean");
                    //break;
                }
            }
            return isClean;
        }

        public void Fill(DbWriter w)
        {
            for (int i = 0; i < m_TableDescriptor.RecordSize; i++)
            {
                w.Write((byte)205);
            }
        }

        public int Save(DbWriter w, long compressedStringBasePosition, int compressedStringOffset)
        {
            long position = w.BaseStream.Position;
            if (position >= 990807)
            {

            }
            long position2 = position + m_TableDescriptor.RecordSize;
            int recordSize = m_TableDescriptor.RecordSize;
            //for (int i = 0; i < recordSize; i++)
            //{
            //	w.Write((byte)0);
            //}
            //w.BaseStream.Position = position;
            for (int j = 0; j < m_TableDescriptor.NFields; j++)
            {
                int num = m_TableDescriptor.FieldDescriptors[j].BitOffset / 8;
                int typeIndex = m_TableDescriptor.FieldDescriptors[j].TypeIndex;
                switch (m_TableDescriptor.FieldDescriptors[j].FieldType)
                {
                    case FieldDescriptor.EFieldTypes.Integer:
                        w.PushInteger(m_IntField[typeIndex], m_TableDescriptor.FieldDescriptors[j]);
                        break;
                    case FieldDescriptor.EFieldTypes.Float:
                        w.WritePendingByte();
                        w.BaseStream.Position = position + num;
                        w.Write(m_FloatField[typeIndex]);
                        break;
                    case FieldDescriptor.EFieldTypes.String:
                        {
                            w.WritePendingByte();
                            w.BaseStream.Position = position + num;
                            int length = m_TableDescriptor.FieldDescriptors[j].Depth / 8;
                            FifaUtil.WriteNullPaddedString(w, m_StringField[typeIndex], length);
                            break;
                        }
                    case FieldDescriptor.EFieldTypes.ShortCompressedString:
                        w.WritePendingByte();
                        w.BaseStream.Position = position + num;
                        if (m_CompressedString[typeIndex] == string.Empty)
                        {
                            w.Write(-1);
                        }
                        else
                        {
                            w.Write(compressedStringOffset);
                        }
                        w.BaseStream.Position = compressedStringBasePosition + compressedStringOffset;
                        compressedStringOffset += m_TableDescriptor.HuffmannTree.WriteString(w, m_CompressedString[typeIndex], longString: false);
                        w.BaseStream.Position = position + num + 4;
                        break;
                    case FieldDescriptor.EFieldTypes.LongCompressedString:
                        w.WritePendingByte();
                        w.BaseStream.Position = position + num;
                        if (m_CompressedString[typeIndex] == string.Empty)
                        {
                            w.Write(-1);
                        }
                        else
                        {
                            w.Write(compressedStringOffset);
                        }
                        w.BaseStream.Position = compressedStringBasePosition + compressedStringOffset;
                        compressedStringOffset += m_TableDescriptor.HuffmannTree.WriteString(w, m_CompressedString[typeIndex], longString: true);
                        w.BaseStream.Position = position + num + 4;
                        break;
                    default:
                        break;
                }

            }
            w.WritePendingByte();
            w.Align(position2);
            return compressedStringOffset;
        }

        public void SaveKeyFields(BinaryWriter w)
        {
            if (m_TableDescriptor.NKeyFields > 0)
            {
                for (int i = 0; i < m_TableDescriptor.NKeyFields; i++)
                {
                    w.Write(m_KeyField[i]);
                }
            }
        }

        public void SaveFloatFields(BinaryWriter w)
        {
            if (m_TableDescriptor.NFloatFields > 0)
            {
                for (int i = 0; i < m_TableDescriptor.NFloatFields; i++)
                {
                    w.Write(m_FloatField[i]);
                }
            }
        }

        public void SaveIntFields(BinaryWriter w)
        {
            if (m_TableDescriptor.NIntFields <= 0)
            {
                return;
            }
            int num = 8;
            byte b = 0;
            for (int i = 0; i < m_TableDescriptor.NIntFields; i++)
            {
                int num2 = FifaUtil.ComputeBitUsed((uint)(m_TableDescriptor.MaxValues[i] - m_TableDescriptor.MinValues[i]));
                if (num2 == 32)
                {
                    w.Write(m_IntField[i]);
                    continue;
                }
                int num3 = m_IntField[i] - m_TableDescriptor.MinValues[i];
                for (int num4 = num2 - 1; num4 >= 0; num4--)
                {
                    b = (byte)(b * 2);
                    if ((num3 & (1 << num4)) != 0)
                    {
                        b = (byte)(b + 1);
                    }
                    num--;
                    if (num == 0)
                    {
                        w.Write(b);
                        num = 8;
                        b = 0;
                    }
                }
            }
            if (num != 8)
            {
                b = (byte)(b << num);
                w.Write(b);
            }
        }

        public object[] ConvertToDataRow()
        {
            object[] array = new object[m_TableDescriptor.NFields];
            for (int i = 0; i < m_TableDescriptor.NFields; i++)
            {
                int typeIndex = m_TableDescriptor.FieldDescriptors[i].TypeIndex;
                switch (m_TableDescriptor.FieldDescriptors[i].FieldType)
                {
                    case FieldDescriptor.EFieldTypes.String:
                        array[i] = m_StringField[typeIndex];
                        break;
                    case FieldDescriptor.EFieldTypes.Integer:
                        array[i] = m_IntField[typeIndex];
                        break;
                    case FieldDescriptor.EFieldTypes.Float:
                        array[i] = m_FloatField[typeIndex];
                        break;
                    case FieldDescriptor.EFieldTypes.ShortCompressedString:
                    case FieldDescriptor.EFieldTypes.LongCompressedString:
                        array[i] = m_CompressedString[typeIndex];
                        break;
                }
            }
            return array;
        }

        public void ConvertFromDataRow(DataRow dataRow)
        {
            for (int i = 0; i < dataRow.Table.Columns.Count; i++)
            {
                string columnName = dataRow.Table.Columns[i].ColumnName;
                for (int j = 0; j < m_TableDescriptor.FieldDescriptors.Length; j++)
                {
                    if (m_TableDescriptor.FieldDescriptors[j].FieldName == columnName)
                    {
                        int typeIndex = m_TableDescriptor.FieldDescriptors[j].TypeIndex;
                        switch (m_TableDescriptor.FieldDescriptors[j].FieldType)
                        {
                            case FieldDescriptor.EFieldTypes.Integer:
                                {
                                    int num = Convert.ToInt32(dataRow.ItemArray[i]);
                                    m_IntField[typeIndex] = num;
                                    break;
                                }
                            case FieldDescriptor.EFieldTypes.Float:
                                m_FloatField[typeIndex] = Convert.ToSingle(dataRow.ItemArray[i], NumberFormatInfo.InvariantInfo);
                                break;
                            case FieldDescriptor.EFieldTypes.String:
                                m_StringField[typeIndex] = Convert.ToString(dataRow.ItemArray[i]);
                                break;
                            case FieldDescriptor.EFieldTypes.ShortCompressedString:
                            case FieldDescriptor.EFieldTypes.LongCompressedString:
                                m_CompressedString[typeIndex] = Convert.ToString(dataRow.ItemArray[i]);
                                break;
                        }
                        break;
                    }
                }
            }
        }

        private int GetDataRowIndex(DataRow dataRow, string fieldName)
        {
            for (int i = 0; i < dataRow.Table.Columns.Count; i++)
            {
                if (dataRow.Table.Columns[i].ColumnName == fieldName)
                {
                    return i;
                }
            }
            return -1;
        }

        private int GetDataRowIntValue(DataRow dataRow, string fieldName)
        {
            for (int i = 0; i < dataRow.Table.Columns.Count; i++)
            {
                if (dataRow.Table.Columns[i].ColumnName == fieldName)
                {
                    return Convert.ToInt32(dataRow.ItemArray[i]);
                }
            }
            return 0;
        }

        public void ConvertFromDataRowFrom15To16(DataRow dataRow)
        {
            if (dataRow.Table.TableName == "formations")
            {
                SetField("playerinstruction0_2", 2);
                SetField("playerinstruction1_2", 2);
                SetField("playerinstruction2_2", 2);
                SetField("playerinstruction3_2", 2);
                SetField("playerinstruction4_2", 2);
                SetField("playerinstruction5_2", 2);
                SetField("playerinstruction6_2", 2);
                SetField("playerinstruction7_2", 2);
                SetField("playerinstruction8_2", 2);
                SetField("playerinstruction9_2", 2);
                SetField("playerinstruction10_2", 2);
                SetField("playerinstruction0_1", GetDataRowIntValue(dataRow, "playerinstruction0"));
                SetField("playerinstruction1_1", GetDataRowIntValue(dataRow, "playerinstruction1"));
                SetField("playerinstruction2_1", GetDataRowIntValue(dataRow, "playerinstruction2"));
                SetField("playerinstruction3_1", GetDataRowIntValue(dataRow, "playerinstruction3"));
                SetField("playerinstruction4_1", GetDataRowIntValue(dataRow, "playerinstruction4"));
                SetField("playerinstruction5_1", GetDataRowIntValue(dataRow, "playerinstruction5"));
                SetField("playerinstruction6_1", GetDataRowIntValue(dataRow, "playerinstruction6"));
                SetField("playerinstruction7_1", GetDataRowIntValue(dataRow, "playerinstruction7"));
                SetField("playerinstruction8_1", GetDataRowIntValue(dataRow, "playerinstruction8"));
                SetField("playerinstruction9_1", GetDataRowIntValue(dataRow, "playerinstruction9"));
                SetField("playerinstruction10_1", GetDataRowIntValue(dataRow, "playerinstruction10"));
            }
            else
            {
                _ = dataRow.Table.TableName == "stadium";
            }
            for (int i = 0; i < dataRow.Table.Columns.Count; i++)
            {
                string columnName = dataRow.Table.Columns[i].ColumnName;
                for (int j = 0; j < m_TableDescriptor.FieldDescriptors.Length; j++)
                {
                    if (m_TableDescriptor.FieldDescriptors[j].FieldName == columnName)
                    {
                        int typeIndex = m_TableDescriptor.FieldDescriptors[j].TypeIndex;
                        switch (m_TableDescriptor.FieldDescriptors[j].FieldType)
                        {
                            case FieldDescriptor.EFieldTypes.Integer:
                                {
                                    int num = Convert.ToInt32(dataRow.ItemArray[i]);
                                    m_IntField[typeIndex] = num;
                                    break;
                                }
                            case FieldDescriptor.EFieldTypes.Float:
                                m_FloatField[typeIndex] = Convert.ToSingle(dataRow.ItemArray[i], NumberFormatInfo.InvariantInfo);
                                break;
                            case FieldDescriptor.EFieldTypes.String:
                                m_StringField[typeIndex] = Convert.ToString(dataRow.ItemArray[i]);
                                break;
                            case FieldDescriptor.EFieldTypes.ShortCompressedString:
                            case FieldDescriptor.EFieldTypes.LongCompressedString:
                                m_CompressedString[typeIndex] = Convert.ToString(dataRow.ItemArray[i]);
                                break;
                        }
                        break;
                    }
                }
            }
        }

        public bool IsEmptyDataRow(DataRow dataRow)
        {
            int nKeyFields = m_TableDescriptor.NKeyFields;
            int nStringFields = m_TableDescriptor.NStringFields;
            int nFloatFields = m_TableDescriptor.NFloatFields;
            int nIntFields = m_TableDescriptor.NIntFields;
            int num = nKeyFields + nStringFields + nFloatFields + nIntFields;
            int num2 = 0;
            _ = new object[num];
            for (int i = 0; i < nKeyFields; i++)
            {
                m_KeyField[i] = Convert.ToInt32(dataRow.ItemArray[num2++]);
                if (m_KeyField[i] != 0)
                {
                    return false;
                }
            }
            for (int j = 0; j < nFloatFields; j++)
            {
                m_FloatField[j] = (float)Convert.ToDouble(dataRow.ItemArray[num2++]);
                if (m_FloatField[j] != 0.0)
                {
                    return false;
                }
            }
            for (int k = 0; k < nStringFields; k++)
            {
                if (dataRow.ItemArray[num2] == DBNull.Value)
                {
                    m_StringField[k] = string.Empty;
                    num2++;
                }
                else
                {
                    m_StringField[k] = (string)dataRow.ItemArray[num2++];
                }
                if (!string.IsNullOrEmpty(m_StringField[k]))
                {
                    return false;
                }
            }
            for (int l = 0; l < nIntFields; l++)
            {
                if (num2 < dataRow.ItemArray.Length)
                {
                    m_IntField[l] = Convert.ToInt32(dataRow.ItemArray[num2++]);
                    if (m_IntField[l] != 0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public int GetAndCheckIntField(int index)
        {
            if (index < 0 || index >= m_IntField.Length)
            {
                return 0;
            }
            int num = m_IntField[index];
            if (m_TableDescriptor.MinValues[index] > m_TableDescriptor.MaxValues[index])
            {
                return num;
            }
            if (num < m_TableDescriptor.MinValues[index])
            {
                return m_TableDescriptor.MinValues[index];
            }
            if (num > m_TableDescriptor.MaxValues[index])
            {
                return m_TableDescriptor.MaxValues[index];
            }
            return num;
        }

        public string GetAndCheckStringField(int index)
        {
            if (index < 0 || index >= m_StringField.Length)
            {
                return null;
            }
            return m_StringField[index];
        }

        public int GetAndCheckExtendedIntField(int index)
        {
            if (index < 0 || index >= m_IntField.Length)
            {
                return 0;
            }
            int num = m_IntField[index];
            if (m_TableDescriptor.MinValues[index] > m_TableDescriptor.MaxValues[index])
            {
                return num;
            }
            int num2 = m_TableDescriptor.MinValues[index];
            int num3 = m_TableDescriptor.MaxValues[index];
            uint num4 = (uint)(num3 - num2);
            if (num4 != 0)
            {
                int num5 = FifaUtil.ComputeBitUsed(num4);
                num3 = num2 + (1 << num5) - 1;
            }
            if (num < num2)
            {
                return num2;
            }
            if (num > num3)
            {
                return num3;
            }
            return num;
        }
    }
}

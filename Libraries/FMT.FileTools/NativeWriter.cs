using System;
using System.IO;
using System.Text;

namespace FMT.FileTools
{
    public class NativeWriter : BinaryWriter
    {
        public long Length { get { return BaseStream.Length; } }
        public long Position { get { return BaseStream.Position; } set { BaseStream.Position = value; } }

        private Encoding encoding;

        public NativeWriter(Stream inStream, bool leaveOpen = false, bool wide = false)
            : base(inStream, wide ? Encoding.UTF8 : Encoding.Default, leaveOpen)
        {
            encoding = wide ? Encoding.UTF8 : Encoding.Default;
        }

        public void Write(Guid value, Endian endian)
        {
            if (endian == Endian.Big)
            {
                byte[] array = value.ToByteArray();
                Write(array[3]);
                Write(array[2]);
                Write(array[1]);
                Write(array[0]);
                Write(array[5]);
                Write(array[4]);
                Write(array[7]);
                Write(array[6]);
                for (int i = 0; i < 8; i++)
                {
                    Write(array[8 + i]);
                }
            }
            else
            {
                Write(value);
            }
        }

        public void Write(short value, Endian endian)
        {
            if (endian == Endian.Big)
            {
                Write((short)(ushort)(((value & 0xFF) << 8) | ((value & 0xFF00) >> 8)));
            }
            else
            {
                Write(value);
            }
        }

        public void Write(ushort value, Endian endian)
        {
            if (endian == Endian.Big)
            {
                Write((ushort)(((value & 0xFF) << 8) | ((value & 0xFF00) >> 8)));
            }
            else
            {
                Write(value);
            }
        }

        public void Write(int value, Endian endian)
        {
            if (endian == Endian.Big)
            {
                Write(((value & 0xFF) << 24) | ((value & 0xFF00) << 8) | ((value >> 8) & 0xFF00) | ((value >> 24) & 0xFF));
            }
            else
            {
                Write(value);
            }
        }

        public void Write(uint value, Endian endian)
        {
            if (endian == Endian.Big)
            {
                Write(((value & 0xFF) << 24) | ((value & 0xFF00) << 8) | ((value >> 8) & 0xFF00) | ((value >> 24) & 0xFF));
            }
            else
            {
                Write(value);
            }
        }

        public void Write(long value, Endian endian)
        {
            if (endian == Endian.Big)
            {
                Write(((value & 0xFF) << 56) | ((value & 0xFF00) << 40) | ((value & 0xFF0000) << 24) | ((value & 4278190080u) << 8) | (((value >> 8) & 4278190080u) | ((value >> 24) & 0xFF0000) | ((value >> 40) & 0xFF00) | ((value >> 56) & 0xFF)));
            }
            else
            {
                Write(value);
            }
        }

        public void Write(ulong value, Endian endian)
        {
            if (endian == Endian.Big)
            {
                Write(((value & 0xFF) << 56) | ((value & 0xFF00) << 40) | ((value & 0xFF0000) << 24) | ((value & 4278190080u) << 8) | (((value >> 8) & 4278190080u) | ((value >> 24) & 0xFF0000) | ((value >> 40) & 0xFF00) | ((value >> 56) & 0xFF)));
            }
            else
            {
                Write(value);
            }
        }

        public void WriteInt16BigEndian(short value)
        {
            Write(value, Endian.Big);
        }
        public void WriteInt16LittleEndian(short value)
        {
            Write(value, Endian.Little);
        }

        public void WriteUInt16(ushort value, Endian endian)
        {
            switch (endian)
            {
                case Endian.Little:
                    WriteUInt16LittleEndian(value);
                    break;
                case Endian.Big:
                    WriteUInt16BigEndian(value);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void WriteUInt16BigEndian(ushort value)
        {
            Write(value, Endian.Big);
        }

        public void WriteUInt16LittleEndian(ushort value)
        {
            Write(value, Endian.Little);
        }

        public void WriteInt32BigEndian(int value)
        {
            Write(value, Endian.Big);
        }

        public void WriteInt32LittleEndian(int value)
        {
            Write(value);

        }

        public void WriteUInt32BigEndian(uint value)
        {
            Write(value, Endian.Big);
        }

        public void WriteUInt32LittleEndian(uint value)
        {
            Write(value);
        }

        public void WriteInt64LittleEndian(long value)
        {
            Write(value);
        }

        public void WriteInt64BigEndian(long value)
        {
            Write(value, Endian.Big);
        }

        public void WriteLong(long value, Endian endian = Endian.Little)
        {
            Write(value, endian);
        }

        public void WriteUInt64LittleEndian(ulong value)
        {
            WriteULong(value);
        }

        public void WriteULong(ulong value, Endian endian = Endian.Little)
        {
            Write(value, endian);
        }

        public void WriteUInt64BigEndian(ulong value)
        {
            Write(value, Endian.Big);
        }

        public void WriteSingleLittleEndian(float value)
        {
            Write(value);
        }
        public void WriteDoubleLittleEndian(double value)
        {
            Write(value);
        }

        public void WriteGuid(Guid value)
        {
            Write(value);
        }

        public void WriteBytes(byte[] value)
        {
            Write(value);
        }

        private void WriteString(string str)
        {
            var bytes = encoding.GetBytes(str);
            Write(bytes);
        }

        public void WriteNullTerminatedString(string str)
        {
            WriteString(str);
            Write('\0');
        }

        public void WriteSizedString(string str)
        {
            Write7BitEncodedInt(str.Length);
            WriteString(str);
        }
        public void WriteLengthPrefixedString(string str)
        {
            if (string.IsNullOrEmpty(str) || str.Length == 0)
            {
                Write7BitEncodedInt(0);
            }
            else
            {
                Write7BitEncodedInt(str.Length);
                WriteString(str);
            }
        }

        public void WriteLengthPrefixedBytes(byte[] data)
        {
            if (data.Length == 0)
            {
                Write7BitEncodedInt(0);
            }
            else
            {
                Write7BitEncodedInt(data.Length);
                Write(data);
            }
        }

        public void WriteFixedSizedString(string str, int size)
        {
            WriteString(str);
            for (int i = 0; i < size - str.Length; i++)
            {
                Write('\0');
            }
        }

        public new void Write7BitEncodedInt(int value)
        {
            uint num;
            for (num = (uint)value; num >= 128; num >>= 7)
            {
                Write((byte)(num | 0x80));
            }
            Write((byte)num);
        }

        public void Write7BitEncodedLong(long value)
        {
            ulong num;
            for (num = (ulong)value; num >= 128; num >>= 7)
            {
                Write((byte)(num | 0x80));
            }
            Write((byte)num);
        }

        public void Write(Guid value)
        {
            Write(value.ToByteArray(), 0, 16);
        }

        public void WriteLine(string str)
        {
            WriteString(str);
            Write('\r');
            Write('\n');
        }

        public void WritePadding(byte alignment)
        {
            while (BaseStream.Position % alignment != 0L)
            {
                Write((byte)0);
            }
        }

        public void WriteEmpty(int numberOfBytes)
        {
            WriteEmptyBytes(numberOfBytes);
        }

        public void WriteEmptyBytes(int numberOfBytes)
        {
            Write(new byte[numberOfBytes]);
        }

        public void WriteObject(object o)
        {
            foreach (var prop in o.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                var v = prop.GetValue(o);
                switch (v.GetType().Name.ToLower())
                {
                    case "string":
                        Write(v.ToString());
                        break;
                    case "byte":
                        Write((byte)v);
                        break;
                    case "byte[]":
                        WriteLengthPrefixedBytes((byte[])v);
                        break;
                }
            }
        }

        public byte[] ToByteArray()
        {
            if(BaseStream is MemoryStream memoryStream)
                return memoryStream.ToArray();

            return null;
        }

    }

    public class FileWriter : NativeWriter
    {
        public FileWriter(Stream inStream, bool leaveOpen = false, bool wide = false)
            : base(inStream, leaveOpen, wide)
        {
        }
    }
}

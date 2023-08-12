using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FMT.FileTools
{
    public partial class NativeReader : IDisposable
    {
        protected Stream stream { get; set; }

        protected byte[] buffer { get; private set; }

        protected char[] charBuffer { get; private set; }

        protected Encoding wideDecoder;

        public Stream BaseStream => stream;

        public virtual long Position
        {
            get
            {
                if (stream == null)
                {
                    return 0L;
                }
                return stream.Position;
            }
            set
            {
                if (stream == null)
                    return;

                stream.Position = value;
            }
        }

        public virtual long Length => stream.Length;

        public int ErrorCount { get; } = 0;

        public NativeReader(string filePath)
        {
            stream = new FileStream(filePath, FileMode.Open);
            wideDecoder = new UnicodeEncoding();
            buffer = new byte[1024];
            //buffer = new byte[20];
            //charBuffer = new char[2];
        }

        public NativeReader(Stream inStream)
        {

            stream = inStream;
            wideDecoder = new UnicodeEncoding();
            buffer = new byte[1024];
            //buffer = new byte[20];
            //charBuffer = new char[2];
        }


        public NativeReader(byte[] data)
        {
            stream = new MemoryStream(data);
            wideDecoder = new UnicodeEncoding();
            buffer = new byte[1024];
        }

        public NativeReader Skip(int numberOfBytesToSkip)
        {
            BaseStream.Position += numberOfBytesToSkip;
            return this;
        }

        public static byte[] ReadInStream(Stream inStream)
        {
            using (NativeReader nativeReader = new NativeReader(inStream))
            {
                return nativeReader.ReadToEnd();
            }
        }

        public char ReadWideChar()
        {
            FillBuffer(2);
            wideDecoder.GetChars(buffer, 0, 2, charBuffer, 0);
            return charBuffer[0];
        }

        public bool ReadBoolean()
        {
            var readByte = ReadByte();
            return readByte == 1;
        }

        public byte ReadByte()
        {
            FillBuffer(1);
            return buffer[0];
        }

        public sbyte ReadSByte()
        {
            FillBuffer(1);
            return (sbyte)buffer[0];
        }

        public short ReadShort(Endian inEndian = Endian.Little)
        {
            FillBuffer(2);
            if (inEndian == Endian.Little)
            {
                return (short)(buffer[0] | (buffer[1] << 8));
            }
            return (short)(buffer[1] | (buffer[0] << 8));
        }

        public short ReadInt16LittleEndian()
        {
            return ReadShort(Endian.Little);
        }

        public int ReadInt16BigEndian()
        {
            return ReadShort(Endian.Big);
        }

        public ushort ReadUShort(Endian inEndian = Endian.Little)
        {
            FillBuffer(2);
            if (inEndian == Endian.Little)
            {
                return (ushort)(buffer[0] | (buffer[1] << 8));
            }
            return (ushort)(buffer[1] | (buffer[0] << 8));
        }

        public ushort ReadUInt16LittleEndian()
        {
            return ReadUShort(Endian.Little);
        }

        public ushort ReadUInt16BigEndian()
        {
            return ReadUShort(Endian.Big);
        }


        public int ReadInt(Endian inEndian = Endian.Little)
        {
            FillBuffer(4);
            if (inEndian == Endian.Little)
            {
                return BitConverter.ToInt32(buffer.Take(4).ToArray());
            }
            return BinaryPrimitives.ReverseEndianness(BitConverter.ToInt32(buffer.Take(4).ToArray()));

        }

        public int ReadInt32LittleEndian()
        {
            return ReadInt(Endian.Little);
        }

        public int ReadInt32BigEndian()
        {
            return ReadInt(Endian.Big);
        }

        public uint ReadUInt(Endian inEndian = Endian.Little)
        {
            FillBuffer(4);
            if (inEndian == Endian.Little)
            {
                return BitConverter.ToUInt32(buffer.Take(4).ToArray());
            }
            return BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt32(buffer.Take(4).ToArray()));
        }

        public uint ReadUInt32LittleEndian()
        {
            return ReadUInt(Endian.Little);
        }

        public uint ReadUInt32BigEndian()
        {
            return ReadUInt(Endian.Big);
        }


        public long ReadLong(Endian inEndian = Endian.Little)
        {
            FillBuffer(8);
            if (inEndian == Endian.Little)
                return BitConverter.ToInt64(buffer.Take(8).ToArray());
            return BinaryPrimitives.ReverseEndianness(BitConverter.ToInt64(buffer.Take(8).ToArray()));
        }

        public long ReadInt64LittleEndian()
        {
            return ReadLong(Endian.Little);
        }

        public long ReadInt64BigEndian()
        {
            return ReadLong(Endian.Big);
        }

        public ulong ReadULong(Endian inEndian = Endian.Little)
        {
            //Span<byte> span = stackalloc byte[8];
            //ReadIntoSpan(span);
            //return inEndian == Endian.Little ? BinaryPrimitives.ReadUInt64LittleEndian(span) : BinaryPrimitives.ReadUInt64BigEndian(span);

            //FillBuffer(8);
            //if (inEndian == Endian.Little)
            //{
            //    return ((ulong)(uint)(buffer[4] | (buffer[5] << 8) | (buffer[6] << 16) | (buffer[7] << 24)) << 32) | (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
            //}
            //return ((ulong)(uint)(buffer[3] | (buffer[2] << 8) | (buffer[1] << 16) | (buffer[0] << 24)) << 32) | (uint)(buffer[7] | (buffer[6] << 8) | (buffer[5] << 16) | (buffer[4] << 24));

            FillBuffer(8);
            if (inEndian == Endian.Little)
                return BitConverter.ToUInt64(buffer.Take(8).ToArray());
            return BinaryPrimitives.ReverseEndianness(BitConverter.ToUInt64(buffer.Take(8).ToArray()));

        }

        public ulong ReadUInt64LittleEndian()
        {
            return ReadULong(Endian.Little);
        }

        public ulong ReadUInt64BigEndian()
        {
            return ReadULong(Endian.Big);
        }

        public unsafe float ReadFloat(Endian inEndian = Endian.Little)
        {
            FillBuffer(4);
            //if (inEndian == Endian.Big)
            //{
            //	uint num = 0u;
            //	num = (uint)((inEndian != 0) ? (buffer[3] | (buffer[2] << 8) | (buffer[1] << 16) | (buffer[0] << 24)) : (buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24)));
            //	return *(float*)(&num);
            //}
            //else
            //         {
            //	float myFloat = System.BitConverter.ToSingle(buffer.Take(4).ToArray(), 0);
            //	return myFloat;
            //}

            var int32bytes = BitConverter.ToInt32(buffer.Take(4).ToArray());
            var reInt32Bytes = BinaryPrimitives.ReverseEndianness(int32bytes);
            if (inEndian == Endian.Little)
            {
                return BitConverter.Int32BitsToSingle(int32bytes);
            }

            return BitConverter.Int32BitsToSingle(reInt32Bytes);

        }

        public unsafe float ReadSingleLittleEndian()
        {
            //Span<byte> span = stackalloc byte[4];
            //ReadIntoSpan(span);
            //return BitConverter.ToSingle(span);

            return ReadFloat();
        }

        public unsafe float ReadSingleBigEndian()
        {
            return ReadFloat(Endian.Big);
        }

        public unsafe double ReadDouble(Endian inEndian = Endian.Little)
        {
            FillBuffer(8);
            //uint num = 0u;
            //uint num2 = 0u;
            //if (inEndian == Endian.Little)
            //{
            //	num = (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
            //	num2 = (uint)(buffer[4] | (buffer[5] << 8) | (buffer[6] << 16) | (buffer[7] << 24));
            //}
            //else
            //{
            //	num = (uint)(buffer[3] | (buffer[2] << 8) | (buffer[1] << 16) | (buffer[0] << 24));
            //	num2 = (uint)(buffer[7] | (buffer[6] << 8) | (buffer[5] << 16) | (buffer[4] << 24));
            //}
            //ulong num3 = ((ulong)num2 << 32) | num;
            //return *(double*)(&num3);

            var int64bytes = BitConverter.ToInt64(buffer.Take(8).ToArray());
            var reInt64Bytes = BinaryPrimitives.ReverseEndianness(int64bytes);
            if (inEndian == Endian.Little)
            {
                return BitConverter.Int64BitsToDouble(int64bytes);
            }

            return BitConverter.Int64BitsToDouble(reInt64Bytes);
        }

        public unsafe double ReadDoubleLittleEndian()
        {
            return ReadDouble(Endian.Little);
        }

        public unsafe double ReadDoubleBigEndian()
        {
            return ReadDouble(Endian.Big);
        }

        public Guid ReadGuid(Endian endian = Endian.Little)
        {
            FillBuffer(16);

            if (endian == Endian.Little)
            {
                return new Guid(new byte[16]
                {
                    buffer[0],
                    buffer[1],
                    buffer[2],
                    buffer[3],
                    buffer[4],
                    buffer[5],
                    buffer[6],
                    buffer[7],
                    buffer[8],
                    buffer[9],
                    buffer[10],
                    buffer[11],
                    buffer[12],
                    buffer[13],
                    buffer[14],
                    buffer[15]
                });
            }
            return new Guid(new byte[16]
            {
                buffer[3],
                buffer[2],
                buffer[1],
                buffer[0],
                buffer[5],
                buffer[4],
                buffer[7],
                buffer[6],
                buffer[8],
                buffer[9],
                buffer[10],
                buffer[11],
                buffer[12],
                buffer[13],
                buffer[14],
                buffer[15]
            });
        }


        public Guid ReadGuidReverse()
        {
            byte[] guidArray = ReadBytes(16);
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(guidArray.Reverse().ToArray());
            //         Guid guid = new Guid(new byte[16]
            //{
            //	guidArray[15],
            //	guidArray[14],
            //	guidArray[13],
            //	guidArray[12],
            //	guidArray[11],
            //	guidArray[10],
            //	guidArray[9],
            //	guidArray[8],
            //	guidArray[7],
            //	guidArray[6],
            //	guidArray[5],
            //	guidArray[4],
            //	guidArray[3],
            //	guidArray[2],
            //	guidArray[1],
            //	guidArray[0]
            //});
            //return guid;
            return new Guid(span);
        }

        public int Read7BitEncodedInt()
        {
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            while (true)
            {
                num3 = ReadByte();
                num |= (num3 & 0x7F) << num2;
                if (num3 >> 7 == 0)
                {
                    break;
                }
                num2 += 7;
            }
            return num;
        }

        public long Read7BitEncodedLong()
        {
            long num = 0L;
            int num2 = 0;
            int num3 = 0;
            while (true)
            {
                num3 = ReadByte();
                num |= (num3 & 0x7F) << num2;
                if (num3 >> 7 == 0)
                {
                    break;
                }
                num2 += 7;
            }
            return num;
        }

        public string ReadNullTerminatedString(bool reverse = false, long? offset = null)
        {
            var startPosition = Position;

#if DEBUG
            Position = startPosition; // useful for debugging
#endif

            if (offset.HasValue)
                Position = offset.Value;

            StringBuilder stringBuilder = new StringBuilder();
            while (true)
            {
                var b = ReadBytes(1);
                if (b[0] == 0x0)
                {
                    break;
                }

                stringBuilder.Append((char)b[0]);

                if (stringBuilder.Length > 10000)
                    throw new Exception("Can't find a text in this byte array");
            }

            if (offset.HasValue)
                Position = startPosition;

            return !reverse ? stringBuilder.ToString() : stringBuilder.ToString().Reverse().ToString();
        }

        public string ReadSizedString(int strLen)
        {
            //FillBuffer(strLen);
            //var ssBuffer = new byte[strLen];
            //stream.Read(new Span<byte>(ssBuffer));
            //         //ReadOnlySpan<byte> b = new ReadOnlySpan<byte>(buffer.Take(strLen).ToArray());
            //         ReadOnlySpan<byte> b = new ReadOnlySpan<byte>(ssBuffer.Take(strLen).ToArray());
            //if(b.EndsWith(new byte[] { 0x0 }))
            //	b = b.Slice(0, strLen - 1);
            //         var result = Encoding.UTF8.GetString(b);
            //         return result;

            var ssBuffer = new byte[strLen];
            stream.Read(ssBuffer, 0, strLen);
            ReadOnlySpan<byte> b = new ReadOnlySpan<byte>(ssBuffer);
            if (b.EndsWith(new byte[] { 0x0 }))
                b = b.Slice(0, strLen - 1);
            var result = Encoding.UTF8.GetString(b);
            b = null;
            return result;

        }

        public string ReadLine()
        {
            StringBuilder stringBuilder = new StringBuilder();
            byte b = 0;
            while (b != 13 && b != 10)
            {
                b = ReadByte();
                stringBuilder.Append((char)b);
                if (b == 10 || b == 13 || Position >= Length)
                {
                    break;
                }
            }
            if (b == 13)
            {
                ReadByte();
            }
            return stringBuilder.ToString().Trim('\r', '\n');
        }

        public void Pad(int alignment)
        {
            if (alignment == 0)
                return;

            while (Position % alignment != 0L)
            {
                Position++;
            }
        }

        public string ReadLengthPrefixedString()
        {
            int byteCount = Read7BitEncodedInt();
            if (byteCount == 0)
            {
                return null;
            }
            return ReadSizedString(byteCount);
        }

        public byte[] ReadLengthPrefixedBytes()
        {
            int byteCount = Read7BitEncodedInt();
            if (byteCount == 0)
            {
                return null;
            }
            return ReadBytes(byteCount);
        }

        public byte[] ReadToEnd()
        {
            long num = Length - Position;
            if (num < int.MaxValue)
            {
                return ReadBytes((int)num);
            }
            byte[] array = new byte[num];
            while (num > 0)
            {
                int num2 = (int)((num > int.MaxValue) ? int.MaxValue : num);
                byte[] array2 = new byte[num2];
                int dstOffset = Read(array2, 0, num2);
                num -= num2;
                Buffer.BlockCopy(array2, 0, array, dstOffset, num2);
            }
            return array;
        }

        public byte[] ReadAll()
        {
            Position = 0;
            return ReadToEnd();
        }

        public byte[] ReadBytes(int count)
        {
            byte[] array = new byte[count];
            int num = 0;
            do
            {
                int num2 = Read(array, num, count);
                if (num2 == 0)
                {
                    break;
                }
                num += num2;
                count -= num2;
            }
            while (count > 0);
            return array;
        }

        public virtual int Read(byte[] inBuffer, int offset, int numBytes)
        {
            Span<byte> rosBuffer = new Span<byte>(inBuffer);
            //int result = stream.Read(inBuffer, offset, numBytes);
            int result = stream.Read(rosBuffer);
            return result;
        }

        public virtual object Read(Type type, long? readPosition = null, int? paddingAlignment = null)
        {
            if (readPosition.HasValue)
                Position = readPosition.Value;



            switch (type.Name)
            {
                case "Boolean":
                    return ReadByte() > 0;
                case "Int8":
                    return (sbyte)ReadByte();
                case "UInt8":
                    return ReadByte();
                case "Int16":
                    return ReadInt16LittleEndian();
                case "UInt16":
                    return ReadUInt16LittleEndian();
                case "Int32":
                    return ReadInt32LittleEndian();
                case "UInt32":
                    return ReadUInt32LittleEndian();
                case "Int64":
                    return ReadInt64LittleEndian();
                case "UInt64":
                    return ReadUInt64LittleEndian();
                case "Single":
                    return ReadSingleLittleEndian();
                case "Double":
                    return ReadDoubleLittleEndian();
                case "Guid":
                    return ReadGuid();
                case "Enum":
                    return ReadInt32LittleEndian();
                case "String":
                    return ReadSizedString(32);
                case "ResourceRef":
                    throw new NotImplementedException("ResourceRef is not known to FMT.FileTools");
                case "Sha1":
                    throw new NotImplementedException("Sha1 is not known to FMT.FileTools");
                case "CString":
                    throw new NotImplementedException("CString is not known to FMT.FileTools");
                case "FileRef":
                    throw new NotImplementedException("FileRef is not known to FMT.FileTools");
                case "TypeRef":
                    throw new NotImplementedException("TypeRef is not known to FMT.FileTools");
                case "BoxedValueRef":
                    throw new NotImplementedException("BoxedValueRef is not known to FMT.FileTools");
                case "Pointer":
                case "PointerRef":
                    throw new NotImplementedException("PointerRef is not known to FMT.FileTools");
                default: // attempt to create a class object instance
                    if (type.BaseType == typeof(Enum))
                    {
                        return ReadInt32LittleEndian();
                    }
                    else
                    {
                        var obj = Activator.CreateInstance(type);
                        if (paddingAlignment.HasValue)
                            Pad(paddingAlignment.Value);
                        obj = ReadClass(obj);
                        return obj;
                    }
            }
        }


        public virtual T Read<T>()
        {
            return (T)Read(typeof(T));
        }

        public virtual object ReadClass(object obj, PropertyInfo[] orderedProperties = null)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            if (orderedProperties == null)
            {
                orderedProperties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).ToArray();
            }
            foreach (var property in orderedProperties)
            {
                if (property.PropertyType.Name.Contains("List`"))
                {
                    ReadArray(obj, property);
                    continue;
                }

                object value = Read(property.PropertyType);
                property.SetValue(obj, value);

            }

            return obj;
        }

        public virtual object ReadArray(object obj, PropertyInfo property)
        {
            return null;
        }


        private void ReadIntoSpan(Span<byte> buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (buffer.Length == 0)
            {
                return;
            }
            int bytesRead = 0;
            do
            {
                Span<byte> span = buffer.Slice(bytesRead);
                int read = stream.Read(span);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }
                bytesRead += read;
            }
            while (bytesRead < buffer.Length);
        }

        /// <summary>
        /// Slow performance but supports Wildcards (i.e. ??)
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public unsafe IList<long> ScanAOB(string pattern)
        {
            if (pattern.StartsWith("?"))
                throw new ArgumentException("pattern cannot start with a wildcard!");

            List<long> offsets = new List<long>();
            pattern = pattern.Replace(" ", "");
            PatternType[] patternArray = new PatternType[pattern.Length / 2];
            for (int i = 0; i < patternArray.Length; i++)
            {
                string a = pattern.Substring(i * 2, 2);
                patternArray[i] = new PatternType
                {
                    isWildcard = (a == "??"),
                    value = (byte)((a != "??") ? byte.Parse(pattern.Substring(i * 2, 2), NumberStyles.HexNumber) : 0)
                };
            }
            long startPosition = Position;
            do
            {
                var positionOfOffset = Position;
                byte[] readBytes = ReadBytes(patternArray.Length);
                bool match = false;
                for (var i = 0; i < readBytes.Length; i++)
                {
                    if (!patternArray[i].isWildcard && readBytes[i] == patternArray[i].value)
                    {
                        match = true;
                    }
                    else if (!patternArray[i].isWildcard)
                    {
                        match = false;
                        break;
                    }

                }
                if (match)
                    offsets.Add(positionOfOffset);

                Position -= patternArray.Length;
                Position += 1;
            } while (Position + patternArray.Length < Length);
            return offsets;
        }

        /// <summary>
        /// Fast performance but does not support strings or wildcards, bytes only
        /// </summary>
        /// <param name="haystack"></param>
        /// <param name="needle"></param>
        /// <param name="startIndex"></param>
        /// <param name="includeOverlapping"></param>
        /// <returns></returns>
        public IEnumerable<int> ScanAOB2(byte[] needle,
    int startIndex = 0, bool includeOverlapping = false)
        {
            Position = 0;
            var haystack = ReadBytes((int)Length);
            int matchIndex = haystack.AsSpan(startIndex).IndexOf(needle);
            while (matchIndex >= 0)
            {
                yield return startIndex + matchIndex;
                startIndex += matchIndex + (includeOverlapping ? 1 : needle.Length);
                matchIndex = haystack.AsSpan(startIndex).IndexOf(needle);
            }
        }

        public Stream CreateViewStream(long offset, long size)
        {
            Position = offset;
            var bytes = ReadBytes((int)size);
            return new MemoryStream(bytes);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
        }

        protected virtual void FillBuffer(int numBytes)
        {
            if (stream == null || buffer == null)
            {
                throw new Exception("Cannot fill Buffer!");
            }

            numBytes = stream.Length < Position + numBytes
                ? (int)stream.Length - (int)Position
                : numBytes;

            //numBytes = numBytes < 0 ? 0 : numBytes;

            try
            {
                stream.Read(
                    buffer
                    , 0
                    , numBytes);
            }
            catch (Exception)
            {

            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stream stream = this.stream;
                this.stream = null;
                stream?.Close();
            }
            this.stream = null;
            buffer = null;
        }

        public Sha1 ReadSha1()
        {
            return new FMT.FileTools.Sha1(ReadBytes(20));
        }

    }


    public class FileReader : NativeReader
    {
        public FileReader(Stream inStream) : base(inStream)
        {
        }
    }
}

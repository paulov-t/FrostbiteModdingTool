using FMT.FileTools;
using System;
using System.Collections.Generic;
using System.IO;

namespace FrostySdk.IO
{
    public class DbWriter : NativeWriter
    {
        private bool writeHeader;

        public DbWriter(Stream inStream, bool inWriteHeader = false, bool leaveOpen = false)
            : base(inStream, leaveOpen)
        {
            writeHeader = inWriteHeader;
        }

        public virtual void Write(DbObject inObj)
        {
            if (writeHeader)
            {
                Write((ProfileManager.DataVersion == 20141118 || ProfileManager.DataVersion == 20141117 || ProfileManager.DataVersion == 20151103 || ProfileManager.DataVersion == 20150223 || ProfileManager.DataVersion == 20131115) ? 63885568 : 30331136);
                Write(new byte[552]);
            }
            Write(WriteDbObject("", inObj));
        }

        public byte[] WriteDbObject(string name, object inObj)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (NativeWriter nativeWriter = new NativeWriter(memoryStream, leaveOpen: true))
            {
                DbType dbType = GetDbType(inObj);
                byte b = (byte)((name == "") ? 128 : 0);
                nativeWriter.Write((byte)(b | (byte)dbType));
                if ((b & 0x80) == 0)
                {
                    nativeWriter.WriteNullTerminatedString(name);
                }
                switch (dbType)
                {
                    case DbType.Object:
                        {
                            DbObject obj2 = (DbObject)inObj;
                            MemoryStream memoryStream3 = new MemoryStream();
                            byte[] array2 = null;
                            foreach (KeyValuePair<string, object> item in obj2.hash)
                            {
                                array2 = WriteDbObject(item.Key, item.Value);
                                if (array2 != null)
                                {
                                    memoryStream3.Write(array2, 0, array2.Length);
                                }
                            }
                            array2 = memoryStream3.ToArray();
                            nativeWriter.Write7BitEncodedLong(array2.Length + 1);
                            nativeWriter.Write(array2);
                            nativeWriter.Write((byte)0);
                            break;
                        }
                    case DbType.List:
                        {
                            DbObject obj = (DbObject)inObj;
                            MemoryStream memoryStream2 = new MemoryStream();
                            byte[] array = null;
                            foreach (object item2 in obj.list)
                            {
                                array = WriteDbObject("", item2);
                                memoryStream2.Write(array, 0, array.Length);
                            }
                            array = memoryStream2.ToArray();
                            nativeWriter.Write7BitEncodedLong(array.Length + 1);
                            nativeWriter.Write(array);
                            nativeWriter.Write((byte)0);
                            break;
                        }
                    case DbType.Boolean:
                        nativeWriter.Write((byte)(((bool)inObj) ? 1 : 0));
                        break;
                    case DbType.String:
                        nativeWriter.WriteSizedString((string)inObj + "\0");
                        break;
                    case DbType.Int:
                        nativeWriter.Write((int)inObj);
                        break;
                    case DbType.Long:
                        nativeWriter.Write((long)inObj);
                        break;
                    case DbType.Float:
                        nativeWriter.Write((float)inObj);
                        break;
                    case DbType.Double:
                        nativeWriter.Write((double)inObj);
                        break;
                    case DbType.Guid:
                        nativeWriter.Write((Guid)inObj);
                        break;
                    case DbType.Sha1:
                        nativeWriter.Write((Sha1)inObj);
                        break;
                    case DbType.ByteArray:
                        nativeWriter.Write7BitEncodedInt(((byte[])inObj).Length);
                        nativeWriter.Write((byte[])inObj);
                        break;
                    default:
                        throw new InvalidDataException("Unsupported DB type detected");
                }
            }
            var finalArray = memoryStream.ToArray();
            memoryStream.Close();
            memoryStream.Dispose();
            return finalArray;
        }

        private DbType GetDbType(object inObj)
        {
            Type type = inObj.GetType();
            if (type == typeof(DbObject))
            {
                DbObject dbObject = (DbObject)inObj;
                if (dbObject.hash != null)
                {
                    return DbType.Object;
                }
                if (dbObject.list != null)
                {
                    return DbType.List;
                }
            }
            else
            {
                if (type == typeof(bool))
                {
                    return DbType.Boolean;
                }
                if (type == typeof(string))
                {
                    return DbType.String;
                }
                if (type == typeof(int))
                {
                    return DbType.Int;
                }
                if (type == typeof(long))
                {
                    return DbType.Long;
                }
                if (type == typeof(float))
                {
                    return DbType.Float;
                }
                if (type == typeof(double))
                {
                    return DbType.Double;
                }
                if (type == typeof(Guid))
                {
                    return DbType.Guid;
                }
                if (type == typeof(Sha1))
                {
                    return DbType.Sha1;
                }
                if (type == typeof(byte[]))
                {
                    return DbType.ByteArray;
                }
            }
            return DbType.Invalid;
        }
    }
}

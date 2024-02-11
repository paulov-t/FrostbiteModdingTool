using FMT.FileTools;
using FrostySdk.Ebx;
using FrostySdk.FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FrostySdk.IO
{
    public class EbxBaseWriter : NativeWriter
    {
        public bool HasErrorInWrite { get { return WriteErrors.Count > 0; } }

        public List<string> WriteErrors { get; } = new();

        protected EbxWriteFlags flags;

        protected List<string> strings = new List<string>();

        protected List<EbxBoxedValue> boxedValues = new List<EbxBoxedValue>();

        protected List<byte[]> boxedValueData = new List<byte[]>();

        protected uint stringsLength;
        public EbxBaseWriter(Stream inStream, EbxWriteFlags inFlags = EbxWriteFlags.None, bool leaveOpen = false)
                    : base(inStream, leaveOpen)
        {
            flags = inFlags;
        }

        public virtual void WriteAsset(EbxAsset asset)
        {
        }

        protected byte[] WriteBoxedValueRef(BoxedValueRef value)
        {
            MemoryStream ms = new MemoryStream();
            NativeWriter nativeWriter = new NativeWriter(ms);
            object value2 = value.Value;
            switch (value.Type)
            {
                case EbxFieldType.TypeRef:
                    nativeWriter.Write((UInt64)(AddString((TypeRef)value2)));
                    break;
                case EbxFieldType.FileRef:
                    nativeWriter.Write((UInt64)(AddString((FileRef)value2)));
                    break;
                case EbxFieldType.CString:
                    nativeWriter.Write(AddString((CString)value2));
                    break;
                case EbxFieldType.Enum:
                    nativeWriter.Write((int)value2);
                    break;
                case EbxFieldType.Float32:
                    nativeWriter.Write((float)value2);
                    break;
                case EbxFieldType.Float64:
                    nativeWriter.Write((double)value2);
                    break;
                case EbxFieldType.Boolean:
                    nativeWriter.Write((byte)(((bool)value2) ? 1u : 0u));
                    break;
                case EbxFieldType.Int8:
                    nativeWriter.Write((sbyte)value2);
                    break;
                case EbxFieldType.UInt8:
                    nativeWriter.Write((byte)value2);
                    break;
                case EbxFieldType.Int16:
                    nativeWriter.Write((short)value2);
                    break;
                case EbxFieldType.UInt16:
                    nativeWriter.Write((ushort)value2);
                    break;
                case EbxFieldType.Int32:
                    nativeWriter.Write((int)value2);
                    break;
                case EbxFieldType.UInt32:
                    nativeWriter.Write((uint)value2);
                    break;
                case EbxFieldType.Int64:
                    nativeWriter.Write((long)value2);
                    break;
                case EbxFieldType.UInt64:
                    nativeWriter.Write((ulong)value2);
                    break;
                case EbxFieldType.Guid:
                    nativeWriter.Write((Guid)value2);
                    break;
                case EbxFieldType.Sha1:
                    nativeWriter.Write((Sha1)value2);
                    break;
                case EbxFieldType.String:
                    nativeWriter.WriteFixedSizedString((string)value2, 32);
                    break;
                case EbxFieldType.ResourceRef:
                    nativeWriter.Write((UInt64)((ResourceRef)value2));
                    break;
            }
            return ms.ToArray();
        }

        protected uint AddString(string stringToAdd)
        {
            if (stringToAdd == "")
            {
                return uint.MaxValue;
            }
            uint num = 0u;
            if (strings.Contains(stringToAdd))
            {
                for (int i = 0; i < strings.Count && strings[i] != stringToAdd; i++)
                {
                    num += (uint)(strings[i].Length + 1);
                }
            }
            else
            {
                num = stringsLength;
                strings.Add(stringToAdd);
                stringsLength += (uint)(stringToAdd.Length + 1);
            }
            return num;
        }

        public static EbxBaseWriter GetEbxWriter()
        {
            EbxBaseWriter ebxBaseWriter = null;
            if (string.IsNullOrEmpty(ProfileManager.EBXWriter))
                throw new Exception("No EBX Writer provided for Game Profile");

            return ebxBaseWriter = (EbxBaseWriter)AssetManager.LoadTypeByName(ProfileManager.EBXWriter
                , new MemoryStream(), EbxWriteFlags.None, false);
        }

        public static byte[] GetEbxArrayDecompressed(EbxAssetEntry entry, out List<string> errors)
        {
            errors = null;
#if DEBUG
            System.Diagnostics.StackTrace t = new ();
#endif

            byte[] decompressedArray = null;
            //if (!Task.Run(() =>
            //{
                var ebxBaseWriter = GetEbxWriter();

                using (ebxBaseWriter)
                {
                    var newAsset = ((ModifiedAssetEntry)entry.ModifiedEntry).DataObject as EbxAsset;
                    newAsset.ParentEntry = entry;
                    ebxBaseWriter.WriteAsset(newAsset);

                    // Handle if there is an error in the Write process
                    if (ebxBaseWriter.HasErrorInWrite)
                    {
                        errors = ebxBaseWriter.WriteErrors;
                        Debug.WriteLine($"Unable to write entry {entry.Name}. There were {ebxBaseWriter.WriteErrors.Count} error(s) whilst writing.");
                        return null;
                    }

                    decompressedArray = ((MemoryStream)ebxBaseWriter.BaseStream).ToArray();
                }
            //}).Wait(TimeSpan.FromSeconds(GetEbxLoadWaitSeconds)))
            //{
            //    //AssetManager.Instance.LogError($"{entry.Name} failed to write!");
            //    return null;
            //}
            return decompressedArray;
        }

        public static bool TryGetEbxArrayDecompressed(EbxAssetEntry entry, ref byte[] decompressedArray, out List<string> errors)
        {
            decompressedArray = GetEbxArrayDecompressed(entry, out errors);
            return decompressedArray != null && (errors == null || errors.Count == 0);
        }

        public static bool TryGetEbxArrayDecompressedAsync(EbxAssetEntry entry, byte[] decompressedArray, List<string> errors)
        {
            if (!Task.Run(() =>
            {
                decompressedArray = GetEbxArrayDecompressed(entry, out errors);
                return decompressedArray != null && (errors == null || errors.Count == 0);
            }).Wait(TimeSpan.FromSeconds(GetEbxLoadWaitSeconds)))
            {
                return false;
            }

            return false;
        }

        public static double GetEbxLoadWaitSeconds { get; set; } = 30;
    }
}

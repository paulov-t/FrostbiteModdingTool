using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace FrostySdk.ThirdParty
{
    public static class Oodle
    {

        // https://www.zenhax.com/viewtopic.php?t=14842

        public delegate int DecompressFunc(IntPtr srcBuffer, long srcSize, IntPtr dstBuffer, long dstSize, int a5 = 0, int a6 = 0, long a7 = 0L, long a8 = 0L, long a9 = 0L, long a10 = 0L, long a11 = 0L, long a12 = 0L, long a13 = 0L, int a14 = 3);

        public delegate long CompressFunc(int cmpCode, IntPtr srcBuffer, long srcSize, IntPtr cmpBuffer, long cmpSize, long dict = 0L, long dictSize = 0L);

        public delegate long CompressFunc2(int cmpCode, IntPtr srcBuffer, long srcSize, IntPtr cmpBuffer, long cmpSize, long dict = 0L, long dictSize = 0L, long a8 = 0L, long a9 = 0L, long a10 = 0L);

        public delegate long CompressFuncWithCompLevel(int cmpCode, IntPtr srcBuffer, long srcSize, IntPtr cmpBuffer, int cmpLevel, long opts = 0L, long offs = 0L, long unused = 0L, long scratch = 0L, long scratchSize = 0L);

        public delegate long MemorySizeNeededFunc(int a1, long a2);

        public static DecompressFunc Decompress;

        public static CompressFunc Compress;

        public static CompressFunc2 Compress2;

        public static CompressFuncWithCompLevel CompressWithCompLevel;

        public static MemorySizeNeededFunc MemorySizeNeeded;

        internal static LoadLibraryHandle handle;

        public static bool IsBound { get; set; }

        public static void Bind(string basePath, int? specificVersion = null)
        {
            if (IsBound)
                return;

            string lib = Directory.EnumerateFiles(basePath, "oo2core_*").FirstOrDefault();
            if (lib == null)
            {
                if(specificVersion == null)
                    lib = Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "ThirdParty"), "oo2core_*", enumerationOptions: new EnumerationOptions() { RecurseSubdirectories = true }).LastOrDefault();
                else
                    lib = Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "ThirdParty"), $"oo2core_{specificVersion}*", enumerationOptions: new EnumerationOptions() { RecurseSubdirectories = true }).LastOrDefault(x => x.Contains(specificVersion.ToString()));

            }

            if (!string.IsNullOrEmpty(lib) && File.Exists(lib))
            {
                handle = new LoadLibraryHandle(lib);
                if (!(handle == IntPtr.Zero))
                {
                    Decompress = Marshal.GetDelegateForFunctionPointer<DecompressFunc>(NativeLibrary.GetExport(handle, "OodleLZ_Decompress"));
                    Compress = Marshal.GetDelegateForFunctionPointer<CompressFunc>(NativeLibrary.GetExport(handle, "OodleLZ_Compress"));
                    //if (ProfilesLibrary.DataVersion >= 20180914)
                    {
                        Compress2 = Marshal.GetDelegateForFunctionPointer<CompressFunc2>(NativeLibrary.GetExport(handle, "OodleLZ_Compress"));
                        CompressWithCompLevel = Marshal.GetDelegateForFunctionPointer<CompressFuncWithCompLevel>(NativeLibrary.GetExport(handle, "OodleLZ_Compress"));
                    }
                    MemorySizeNeeded = Marshal.GetDelegateForFunctionPointer<MemorySizeNeededFunc>(NativeLibrary.GetExport(handle, "OodleLZDecoder_MemorySizeNeeded"));
                }
            }
            else
            {

            }

            IsBound = true; 
        }

        static int GetCompressedBufferSizeNeeded(int size)
        {
            return size + 274 * ((size + 0x3FFFF) / 0x40000);
        }

        public static ulong CompressOodle(byte[] buffer, out byte[] compBuffer, out ushort compressCode, ref bool uncompressed, uint compressionOverride = 8)
        {
            compBuffer = new byte[524288];
            compressCode = 6512;
            uncompressed = false;

            if (ProfileManager.IsFIFA23DataVersion())
                return CompressOodle23(buffer, out compBuffer, out compressCode, ref uncompressed, compressionOverride);

            ulong compressedSize = 0uL;
            GCHandle gCHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            GCHandle gCHandle2 = GCHandle.Alloc(compBuffer, GCHandleType.Pinned);
            try
            {
                compressCode = 4464;
                compressedSize = (ulong)Oodle.Compress2(8, gCHandle.AddrOfPinnedObject(), buffer.Length, gCHandle2.AddrOfPinnedObject(), compBuffer.Length, 0L, 0L, 0L, 0L, 0L);
                return compressedSize;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                gCHandle.Free();
                gCHandle2.Free();
            }
            return 0;
        }

        public static ulong CompressOodle23(byte[] buffer, out byte[] compBuffer, out ushort compressCode, ref bool uncompressed, uint compressionOverride = 8)
        {
            var tSize = GetCompressedBufferSizeNeeded(buffer.Length);
            compBuffer = new byte[tSize];
            GCHandle gCHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            GCHandle gCHandle2 = GCHandle.Alloc(compBuffer, GCHandleType.Pinned);
            ulong compressedSize = 0uL;

            //string sCodeOfFifa23 = (28697).ToString("X");
            //ushort hexCodeOfFifa23 = Convert.ToUInt16(sCodeOfFifa23);

            //compressCode = 0x1170;
            //compressCode = 0x1970;
            compressCode = 6512;
            try
            {

                int compLevel = (int)compressionOverride > 16 ? 16 : (int)compressionOverride;
                compressedSize = (ulong)Oodle.CompressWithCompLevel(8, gCHandle.AddrOfPinnedObject(), buffer.Length, gCHandle2.AddrOfPinnedObject(), 4, 0L, 0L, 0L, 0L, 0L);
                if (compressedSize > (ulong)buffer.Length)
                {
                    uncompressed = true;
                    compressedSize = 0uL;
                }

                return compressedSize;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                gCHandle.Free();
                gCHandle2.Free();
            }
            return 0;
        }
    }
}

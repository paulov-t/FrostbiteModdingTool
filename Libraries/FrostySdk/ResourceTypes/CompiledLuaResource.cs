using FMT.FileTools;
using FMT.Logging;
using FrostySdk.Interfaces;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FrostySdk.Resources
{
    public class CompiledLuaResource
    {
        private class LuaDec
        {
            [DllImport("thirdparty/luacmp.dll")]
            public static extern bool Compile([MarshalAs(UnmanagedType.LPStr)] string code, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] out byte[] data, out int bytecount, [MarshalAs(UnmanagedType.LPStr)] out string errors);
        }

        public ushort Unknown1;

        public ushort Unknown2;

        public uint Unknown3;

        public uint Unknown4;

        public uint Unknown5;

        public uint ParameterCount;

        public uint BytecodeLength;

        public string EntrypointName;

        public string Parameters;

        public byte[] Bytecode;

        public CompiledLuaResource(Stream s)
        {
            using (NativeReader nativeReader = new NativeReader(s))
            {
                Unknown1 = nativeReader.ReadUShort();
                Unknown2 = nativeReader.ReadUShort();
                Unknown3 = nativeReader.ReadUInt();
                Unknown4 = nativeReader.ReadUInt();
                Unknown5 = nativeReader.ReadUInt();
                ParameterCount = nativeReader.ReadUInt();
                BytecodeLength = nativeReader.ReadUInt();
                EntrypointName = nativeReader.ReadNullTerminatedString();
                Parameters = nativeReader.ReadNullTerminatedString();
                Bytecode = nativeReader.ReadBytes((int)BytecodeLength);
            }
        }

        public void Write(Stream s)
        {
            using (NativeWriter nativeWriter = new NativeWriter(s))
            {
                nativeWriter.Write(Unknown1);
                nativeWriter.Write(Unknown2);
                nativeWriter.Write(Unknown3);
                nativeWriter.Write(Unknown4);
                nativeWriter.Write(Unknown5);
                nativeWriter.Write(ParameterCount);
                nativeWriter.Write(BytecodeLength);
                nativeWriter.Write(Encoding.ASCII.GetBytes(EntrypointName));
                s.WriteByte(0);
                nativeWriter.Write(Encoding.ASCII.GetBytes(Parameters));
                s.WriteByte(0);
                nativeWriter.Write(Bytecode);
            }
        }

        public string DecompileBytecode(ILogger logger = null)
        {
            using (NativeReader nativeReader = new NativeReader(new MemoryStream(Bytecode)))
            {
                nativeReader.Position = 12L;
                int strLen = nativeReader.ReadInt();
                string text = nativeReader.ReadSizedString(strLen);
                string[] array = text.Split('\n');
                text = "";
                string[] array2 = array;
                foreach (string text2 in array2)
                {
                    text = text + text2.Trim(' ') + "\n";
                }
                return text.TrimEnd();
            }
        }

        public void CompileSource(string[] source, ILogger logger = null)
        {
            string text = "";
            foreach (string str in source)
            {
                text = text + str + "\n";
            }
            if (!LuaDec.Compile(text, out byte[] data, out int bytecount, out string errors))
            {
                logger?.Log("Lua compilation error:");
                logger?.Log(errors);
            }
            else
            {
                logger?.Log("Lua compilation successful");
                Bytecode = data;
                BytecodeLength = (uint)bytecount;
            }
        }
    }
}

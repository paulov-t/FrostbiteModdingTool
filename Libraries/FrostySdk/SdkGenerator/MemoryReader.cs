using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace FrostbiteSdk
{
    public class MemoryReader : IDisposable
    {
        private const int PROCESS_WM_READ = 16;

        private IntPtr handle;

        protected byte[] buffer = new byte[20];

        protected long position;

        public virtual long Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, long lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool VirtualProtectEx(IntPtr hProcess, long lpAddress, UIntPtr dwSize, uint flNewProtect, ref uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        public MemoryReader()
        {
        }

        public MemoryReader(Process process, long initialAddr)
        {
            handle = OpenProcess(16, bInheritHandle: false, process.Id);
            position = initialAddr;
        }

        public virtual void Dispose()
        {
            CloseHandle(handle);
        }

        public byte ReadByte()
        {
            FillBuffer(1);
            return buffer[0];
        }

        public short ReadShort()
        {
            FillBuffer(2);
            return (short)(buffer[0] | (buffer[1] << 8));
        }

        public ushort ReadUShort()
        {
            FillBuffer(2);
            return (ushort)(buffer[0] | (buffer[1] << 8));
        }

        public int ReadInt()
        {
            FillBuffer(4);
            return buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
        }

        public uint ReadUInt()
        {
            FillBuffer(4);
            return (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
        }

        public long ReadLong()
        {
            FillBuffer(8);
            return (long)(((ulong)(uint)(buffer[4] | (buffer[5] << 8) | (buffer[6] << 16) | (buffer[7] << 24)) << 32) | (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24)));
        }

        public ulong ReadULong()
        {
            FillBuffer(8);
            return ((ulong)(uint)(buffer[4] | (buffer[5] << 8) | (buffer[6] << 16) | (buffer[7] << 24)) << 32) | (uint)(buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24));
        }

        public Guid ReadGuid()
        {
            FillBuffer(16);
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

        public string ReadNullTerminatedStringAtCurPosition(int maxLength = 1000)
        {
            var index = 0;

            StringBuilder stringBuilder = new StringBuilder();
            while (true)
            {
                if (index > maxLength)
                    break;

                index++;
                
                char c = (char)ReadByte();
                if (c == '\0')
                {
                    break;
                }
                stringBuilder.Append(c);
            }

            //Debug.WriteLine(stringBuilder.ToString());
            return stringBuilder.ToString();
        }

        public string ReadNullTerminatedString(int maxLength = 1000)
        {
            var index = 0;

            long num = ReadLong();
            long num2 = Position;
            Position = num;
            string result = "";
            while (true)
            {
                if (index > maxLength)
                    break;

                index++;

                char c = (char)ReadByte();
                if (c == '\0')
                {
                    break;
                }
                result += c;
            }
            Position = num2;

            //Debug.WriteLine(stringBuilder.ToString());
            return result;
        }

        public byte[] ReadBytes(int numBytes)
        {
            byte[] array = new byte[numBytes];
            uint lpflOldProtect = 0u;
            int lpNumberOfBytesRead = 0;
            VirtualProtectEx(handle, position, new UIntPtr((uint)numBytes), 2u, ref lpflOldProtect);
            if (!ReadProcessMemory(handle, position, array, numBytes, ref lpNumberOfBytesRead))
            {
                return null;
            }
            VirtualProtectEx(handle, position, new UIntPtr((uint)numBytes), lpflOldProtect, ref lpflOldProtect);
            position += numBytes;
            return array;
        }

        public unsafe IList<long> scan(string pattern)
        {
            List<long> list = new List<long>();
            pattern = pattern.Replace(" ", "");
            PatternType[] array = new PatternType[pattern.Length / 2];
            for (int i = 0; i < array.Length; i++)
            {
                string a = pattern.Substring(i * 2, 2);
                array[i] = new PatternType
                {
                    isWildcard = (a == "??"),
                    value = (byte)((a != "??") ? byte.Parse(pattern.Substring(i * 2, 2), NumberStyles.HexNumber) : 0)
                };
            }
            bool flag = false;
            long num = Position;
            byte[] array2 = ReadBytes(1048576);
            byte* ptr = (byte*)(void*)Marshal.UnsafeAddrOfPinnedArrayElement(array2, 0);
            byte* ptr2 = ptr;
            byte* ptr3 = ptr2 + 1048576;
            byte* ptr4 = ptr2;
            while (array2 != null)
            {
                if (*ptr2 == array[0].value)
                {
                    ptr4 = ptr2;
                    flag = true;
                    for (int j = 0; j < array.Length; j++)
                    {
                        if (!array[j].isWildcard && *ptr4 != array[j].value)
                        {
                            flag = false;
                            break;
                        }
                        ptr4++;
                    }
                    if (flag)
                    {
                        list.Add(ptr4 - ptr - array.Length + num);
                        flag = false;
                    }
                }
                ptr2++;
                if (ptr2 == ptr3)
                {
                    num = Position;
                    array2 = ReadBytes(1048576);
                    if (array2 == null)
                    {
                        break;
                    }
                    ptr = (byte*)(void*)Marshal.UnsafeAddrOfPinnedArrayElement(array2, 0);
                    ptr2 = ptr;
                    ptr3 = ptr2 + 1048576;
                }
            }
            return list;
        }

        protected virtual void FillBuffer(int numBytes)
        {
            uint lpflOldProtect = 0u;
            int lpNumberOfBytesRead = 0;
            VirtualProtectEx(handle, position, new UIntPtr((uint)numBytes), 2u, ref lpflOldProtect);
            ReadProcessMemory(handle, position, buffer, numBytes, ref lpNumberOfBytesRead);
            VirtualProtectEx(handle, position, new UIntPtr((uint)numBytes), lpflOldProtect, ref lpflOldProtect);
            position += numBytes;
        }
    }
}

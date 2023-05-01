using System;
using System.Text;

namespace FMT.FileTools
{
    public static class Fnv1
    {
        public static int HashString(string span)
        {
            return Fnv1a.HashString(span);
        }
    }

    public static class Fnv1a
    {
        public static int Hash(ReadOnlySpan<byte> data)
        {
            if (data.Length == 0)
            {
                return 5381;
            }
            uint hash = 5381u;
            for (int i = 0; i < data.Length; i++)
            {
                hash = (hash * 33) ^ data[i];
            }
            return (int)hash;
        }

        public static int HashString(string span)
        {
            if (span == null)
            {
                throw new ArgumentNullException("span");
            }
            int length = Encoding.UTF8.GetByteCount(span);
            Span<byte> span2 = ((length > 1024) ? ((Span<byte>)new byte[length]) : stackalloc byte[length]);
            Span<byte> bytes = span2;
            Encoding.UTF8.GetBytes(span, bytes);
            return Hash(bytes);
        }

        public static int HashStringByHashDepot(string s)
        {
            return (int)HashDepot.Fnv1a.Hash32(Encoding.UTF8.GetBytes(s));
        }
    }
    public static class Murmur2
    {
        private const ulong m = 14313749767032793493uL;

        private const int r = 47;

        public static ulong Hash64(byte[] data, ulong seed)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (data.Length == 0)
            {
                return 0uL;
            }
            return Hash(data, seed);
        }

        public static ulong HashString64(string data, ulong seed)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            return Hash64(Encoding.UTF8.GetBytes(data), seed);
        }

        private static ulong Hash(ushort key, ulong seed)
        {
            return Finish((seed ^ 0x8D494F26B7A3D32AuL ^ ((ulong)key << 8) ^ key) * 14313749767032793493uL);
        }

        private static ulong Hash(ulong key, ulong seed)
        {
            ulong h = seed ^ 0x35253C9ADE8F4CA8uL;
            h = Mix(key, h);
            return Finish(h);
        }

        private static ulong Hash(byte key, ulong seed)
        {
            return Finish((seed ^ 0xC6A4A7935BD1E995uL ^ key) * 14313749767032793493uL);
        }

        private unsafe static ulong Hash(byte[] key, ulong seed)
        {
            int len = key.Length;
            ulong h = seed ^ (ulong)(len * -4132994306676758123L);
            fixed (byte* ptr = &key[0])
            {
                int blocks = len / 8;
                int remainder = len & 7;
                ulong* b = (ulong*)ptr;
                while (blocks-- > 0)
                {
                    ulong* num = b;
                    b = num + 1;
                    h = Mix(*num, h);
                }
                byte* remainderBlock = (byte*)b;
                switch (remainder)
                {
                    default:
                        goto end_IL_001c;
                    case 7:
                        h ^= (ulong)(*(remainderBlock++)) << 48;
                        goto case 6;
                    case 6:
                        h ^= (ulong)(*(remainderBlock++)) << 40;
                        goto case 5;
                    case 5:
                        h ^= (ulong)(*(remainderBlock++)) << 32;
                        goto case 4;
                    case 4:
                        h ^= (ulong)(*(remainderBlock++)) << 24;
                        goto case 3;
                    case 3:
                        h ^= (ulong)(*(remainderBlock++)) << 16;
                        goto case 2;
                    case 2:
                        h ^= (ulong)(*(remainderBlock++)) << 8;
                        break;
                    case 1:
                        break;
                }
                h ^= *remainderBlock;
                h *= 14313749767032793493uL;
            end_IL_001c:;
            }
            return Finish(h);
        }

        private static ulong Mix(ulong k, ulong h)
        {
            k *= 14313749767032793493uL;
            k ^= k >> 47;
            k *= 14313749767032793493uL;
            h ^= k;
            h *= 14313749767032793493uL;
            return h;
        }

        private static ulong Finish(ulong h)
        {
            h ^= h >> 47;
            h *= 14313749767032793493uL;
            h ^= h >> 47;
            return h;
        }
    }
}

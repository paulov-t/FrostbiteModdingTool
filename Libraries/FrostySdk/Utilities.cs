using FMT.FileTools;
using FMT.Logging;
using Newtonsoft.Json;
using PInvoke;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Printing.IndexedProperties;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static PInvoke.BCrypt;

namespace FrostbiteSdk
{
    public static class Utilities
    {
        public static byte[] TOCKeyPart1 { get; private set; } = new byte[283]
            {
                82, 83, 65, 50, 0, 8, 0, 0, 3, 0,
            0, 0, 0, 1, 0, 0, 128, 0, 0, 0,
            128, 0, 0, 0, 1, 0, 1, 212, 21, 28,
            132, 38, 235, 95, 95, 86, 2, 145, 209, 198,
            20, 82, 63, 11, 253, 160, 37, 36, 191, 120,
            168, 71, 211, 23, 61, 89, 175, 59, 140, 234,
            110, 236, 139, 228, 154, 127, 61, 25, 232, 55,
            122, 146, 206, 91, 87, 122, 163, 33, 89, 231,
            115, 54, 125, 140, 79, 53, 220, 191, 13, 247,
            231, 214, 72, 213, 213, 47, 31, 165, 225, 229,
            213, 144, 225, 159, 76, 43, 119, 76, 52, 66,
            103, 65, 146, 3, 221, 75, 70, 248, 24, 135,
            41, 231, 165, 201, 62, 194, 116, 24, 213, 186,
            220, 227, 178, 134, 156, 57, 157, 160, 121, 163,
            205, 125, 15, 184, 36, 135, 173, 29, 208, 60,
            169, 190, 132, 6, 129, 172, 132, 177, 178, 185,
            99, 205, 65, 182, 40, 142, 20, 38, 148, 148,
            49, 151, 49, 5, 76, 10, 114, 135, 131, 0,
            88, 43, 177, 62, 80, 115, 31, 116, 99, 100,
            162, 78, 76, 14, 199, 175, 214, 63, 52, 238,
            2, 226, 211, 203, 110, 195, 50, 128, 166, 188,
            180, 210, 55, 177, 0, 85, 212, 176, 119, 83,
            93, 170, 244, 177, 209, 173, 190, 81, 135, 194,
            116, 145, 218, 41, 93, 139, 198, 141, 33, 24,
            181, 45, 252, 186, 207, 14, 107, 118, 199, 218,
            90, 55, 111, 171, 238, 126, 103, 70, 191, 195,
            29, 201, 110, 196, 225, 99, 145, 111, 172, 26,
            33, 216, 122, 125, 146, 121, 226, 46, 183, 78,
            13, 157, 7,
            };

        //public static byte[] TOCKeyPart1_Madden24 { get; } = new byte[283]
        //    {
        //        0x52, 0x53, 0x41, 0x31, 0x00, 0x08, 0x00 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 
        //        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0xB9, 0xEB, 0xAE,
        //        0x0D, 0x10, 0xEF, 0x66, 0x7E, 0xB0, 0x79, 0x20, 0xAD, 0x03, 0x94, 0x29, 0xC3, 0x61, 0x99,
        //        0x34, 0x14, 0x22, 0x78, 0x6D, 0xDB, 0x58, 0x95, 0xA9, 0x65, 0x11, 0x2E, 0xE2, 0xD2, 0x47, 
        //        0x82 B9 D2 82 35 E6 86 EC CE 54 60 BB CC 18 BF E9 B3 79 15 2B DF 4D 9C CE FD F3 C1 79 CB 8F 67 34 77 8E ED 84 17 A6 96 83 C0 AE 74 DB BB 94 93 ED E8 D8 E3 B7 6C 8A 6C CD 30 0B EF BF B6 3D 11 3C FE 17 AF EE 1F 4E DC B0 6A FC 06 FC 87 54 56 E3 86 54 7B 48 1D 66 EC 1E 0F E7 FA 10 BB 46 C4 0A DB E0 2E A0 23 5F 24 11 76 29 1C 83 79 82 86 FA 92 40 02 7D 73 1A C2 B0 D5 0C 72 34 BE 98 20 DB DD 0A CC 10 2B 1F 9B 32 1E 89 0B 45 A5 5A E0 05 5B FF 66 A3 A4 84 07 B2 87 7E C5 4D 76 C8 5C E5 D0 9F 10 F0 B1 0E 96 F7 39 DE 6E 3C 88 89 E5 73 DC 10 34 B2 13 0B CB CD B1 04 57 69 40 97 BA 38 90 B6 BF 6B D5 18 D6 04 EB 23 8D 93 8B C7 08 38 71 BB 90 0B 91 1B B0 BB 38 E7 E8 5A DE 6A 25
        //    };
        //    public static byte[] TOCKey { get; } = new byte[283]
        //    {
        //        0x52, 0x53, 0x41, 0x31, 0x00, 0x08, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00,
        //0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0xd4, 0x15, 0x1c, 0x84, 0x26,
        //0xeb, 0x5f, 0x5f, 0x56, 0x02, 0x91, 0xd1, 0xc6, 0x14, 0x52, 0x3f, 0x0b, 0xfd, 0xa0, 0x25, 0x24,
        //0xbf, 0x78, 0xa8, 0x47, 0xd3, 0x17, 0x3d, 0x59, 0xaf, 0x3b, 0x8c, 0xea, 0x6e, 0xec, 0x8b, 0xe4,
        //0x9a, 0x7f, 0x3d, 0x19, 0xe8, 0x37, 0x7a, 0x92, 0xce, 0x5b, 0x57, 0x7a, 0xa3, 0x21, 0x59, 0xe7,
        //0x73, 0x36, 0x7d, 0x8c, 0x4f, 0x35, 0xdc, 0xbf, 0x0d, 0xf7, 0xe7, 0xd6, 0x48, 0xd5, 0xd5, 0x2f,
        //0x1f, 0xa5, 0xe1, 0xe5, 0xd5, 0x90, 0xe1, 0x9f, 0x4c, 0x2b, 0x77, 0x4c, 0x34, 0x42, 0x67, 0x41,
        //0x92, 0x03, 0xdd, 0x4b, 0x46, 0xf8, 0x18, 0x87, 0x29, 0xe7, 0xa5, 0xc9, 0x3e, 0xc2, 0x74, 0x18,
        //0xd5, 0xba, 0xdc, 0xe3, 0xb2, 0x86, 0x9c, 0x39, 0x9d, 0xa0, 0x79, 0xa3, 0xcd, 0x7d, 0x0f, 0xb8,
        //0x24, 0x87, 0xad, 0x1d, 0xd0, 0x3c, 0xa9, 0xbe, 0x84, 0x06, 0x81, 0xac, 0x84, 0xb1, 0xb2, 0xb9,
        //0x63, 0xcd, 0x41, 0xb6, 0x28, 0x8e, 0x14, 0x26, 0x94, 0x94, 0x31, 0x97, 0x31, 0x05, 0x4c, 0x0a,
        //0x72, 0x87, 0x83, 0x00, 0x58, 0x2b, 0xb1, 0x3e, 0x50, 0x73, 0x1f, 0x74, 0x63, 0x64, 0xa2, 0x4e,
        //0x4c, 0x0e, 0xc7, 0xaf, 0xd6, 0x3f, 0x34, 0xee, 0x02, 0xe2, 0xd3, 0xcb, 0x6e, 0xc3, 0x32, 0x80,
        //0xa6, 0xbc, 0xb4, 0xd2, 0x37, 0xb1, 0x00, 0x55, 0xd4, 0xb0, 0x77, 0x53, 0x5d, 0xaa, 0xf4, 0xb1,
        //0xd1, 0xad, 0xbe, 0x51, 0x87, 0xc2, 0x74, 0x91, 0xda, 0x29, 0x5d, 0x8b, 0xc6, 0x8d, 0x21, 0x18,
        //0xb5, 0x2d, 0xfc, 0xba, 0xcf, 0x0e, 0x6b, 0x76, 0xc7, 0xda, 0x5a, 0x37, 0x6f, 0xab, 0xee, 0x7e,
        //0x67, 0x46, 0xbf, 0xc3, 0x1d, 0xc9, 0x6e, 0xc4, 0xe1, 0x63, 0x91, 0x6f, 0xac, 0x1a, 0x21, 0xd8,
        //0x7a, 0x7d, 0x92, 0x79, 0xe2, 0x2e, 0xb7, 0x4e, 0x0d, 0x9d, 0x07
        //    };

        public static byte[] TOCKeyPart2 { get; } = new byte[256]
        {
            242, 107, 33, 9, 69, 206, 42,
            189, 113, 9, 84, 77, 253, 129, 229, 99, 248,
            218, 84, 237, 12, 43, 161, 1, 136, 110, 153,
            138, 102, 43, 117, 102, 219, 150, 35, 128, 57,
            232, 46, 230, 86, 241, 221, 198, 78, 170, 198,
            156, 179, 58, 220, 107, 139, 204, 182, 224, 222,
            16, 89, 206, 67, 171, 13, 73, 92, 0, 1,
            209, 173, 169, 173, 201, 41, 51, 55, 200, 101,
            154, 236, 86, 13, 200, 254, 111, 220, 6, 86,
            88, 129, 237, 163, 198, 182, 198, 37, 57, 222,
            194, 13, 52, 65, 55, 196, 95, 226, 200, 65,
            23, 25, 94, 207, 103, 57, 55, 47, 131, 169,
            127, 146, 171, 2, 171, 132, 176, 126, 159, 244,
            67, 223, 246, 227, 226, 88, 0, 132, 159, 220,
            189, 243, 245, 252, 6, 77, 5, 1, 236, 220,
            55, 44, 252, 30, 232, 150, 210, 121, 237, 149,
            142, 13, 160, 171, 181, 39, 178, 194, 45, 27,
            130, 160, 103, 11, 168, 211, 223, 33, 149, 146,
            70, 136, 159, 171, 107, 2, 16, 230, 36, 189,
            165, 149, 62, 125, 42, 86, 13, 143, 112, 110,
            192, 116, 44, 245, 106, 217, 236, 131, 93, 188,
            126, 15, 203, 129, 220, 255, 181, 244, 58, 247,
            196, 87, 101, 175, 135, 210, 67, 15, 175, 177,
            242, 152, 109, 65, 247, 253, 2, 51, 149, 197,
            22, 224, 0, 127, 167, 51, 120, 110, 208, 145,
            207, 149, 229, 74, 183, 52, 3, 105, 237
        };

        public static byte[] TOCKey_F21 { get; } = new byte[539]
        {
            82, 83, 65, 50, 0, 8, 0, 0, 3, 0,
            0, 0, 0, 1, 0, 0, 128, 0, 0, 0,
            128, 0, 0, 0, 1, 0, 1, 212, 21, 28,
            132, 38, 235, 95, 95, 86, 2, 145, 209, 198,
            20, 82, 63, 11, 253, 160, 37, 36, 191, 120,
            168, 71, 211, 23, 61, 89, 175, 59, 140, 234,
            110, 236, 139, 228, 154, 127, 61, 25, 232, 55,
            122, 146, 206, 91, 87, 122, 163, 33, 89, 231,
            115, 54, 125, 140, 79, 53, 220, 191, 13, 247,
            231, 214, 72, 213, 213, 47, 31, 165, 225, 229,
            213, 144, 225, 159, 76, 43, 119, 76, 52, 66,
            103, 65, 146, 3, 221, 75, 70, 248, 24, 135,
            41, 231, 165, 201, 62, 194, 116, 24, 213, 186,
            220, 227, 178, 134, 156, 57, 157, 160, 121, 163,
            205, 125, 15, 184, 36, 135, 173, 29, 208, 60,
            169, 190, 132, 6, 129, 172, 132, 177, 178, 185,
            99, 205, 65, 182, 40, 142, 20, 38, 148, 148,
            49, 151, 49, 5, 76, 10, 114, 135, 131, 0,
            88, 43, 177, 62, 80, 115, 31, 116, 99, 100,
            162, 78, 76, 14, 199, 175, 214, 63, 52, 238,
            2, 226, 211, 203, 110, 195, 50, 128, 166, 188,
            180, 210, 55, 177, 0, 85, 212, 176, 119, 83,
            93, 170, 244, 177, 209, 173, 190, 81, 135, 194,
            116, 145, 218, 41, 93, 139, 198, 141, 33, 24,
            181, 45, 252, 186, 207, 14, 107, 118, 199, 218,
            90, 55, 111, 171, 238, 126, 103, 70, 191, 195,
            29, 201, 110, 196, 225, 99, 145, 111, 172, 26,
            33, 216, 122, 125, 146, 121, 226, 46, 183, 78,
            13, 157, 7, 242, 107, 33, 9, 69, 206, 42,
            189, 113, 9, 84, 77, 253, 129, 229, 99, 248,
            218, 84, 237, 12, 43, 161, 1, 136, 110, 153,
            138, 102, 43, 117, 102, 219, 150, 35, 128, 57,
            232, 46, 230, 86, 241, 221, 198, 78, 170, 198,
            156, 179, 58, 220, 107, 139, 204, 182, 224, 222,
            16, 89, 206, 67, 171, 13, 73, 92, 0, 1,
            209, 173, 169, 173, 201, 41, 51, 55, 200, 101,
            154, 236, 86, 13, 200, 254, 111, 220, 6, 86,
            88, 129, 237, 163, 198, 182, 198, 37, 57, 222,
            194, 13, 52, 65, 55, 196, 95, 226, 200, 65,
            23, 25, 94, 207, 103, 57, 55, 47, 131, 169,
            127, 146, 171, 2, 171, 132, 176, 126, 159, 244,
            67, 223, 246, 227, 226, 88, 0, 132, 159, 220,
            189, 243, 245, 252, 6, 77, 5, 1, 236, 220,
            55, 44, 252, 30, 232, 150, 210, 121, 237, 149,
            142, 13, 160, 171, 181, 39, 178, 194, 45, 27,
            130, 160, 103, 11, 168, 211, 223, 33, 149, 146,
            70, 136, 159, 171, 107, 2, 16, 230, 36, 189,
            165, 149, 62, 125, 42, 86, 13, 143, 112, 110,
            192, 116, 44, 245, 106, 217, 236, 131, 93, 188,
            126, 15, 203, 129, 220, 255, 181, 244, 58, 247,
            196, 87, 101, 175, 135, 210, 67, 15, 175, 177,
            242, 152, 109, 65, 247, 253, 2, 51, 149, 197,
            22, 224, 0, 127, 167, 51, 120, 110, 208, 145,
            207, 149, 229, 74, 183, 52, 3, 105, 237
        };

        public static byte[] LoadTocKeyFromPlugin()
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => x.FullName.Contains("Plugin", StringComparison.OrdinalIgnoreCase)))
            {
                var manifestResources = a.GetManifestResourceNames();
                foreach (var manifestR in manifestResources)
                {
                    FileLogger.WriteLine(manifestR);
                }
                var pluginname = a.GetName().Name;
                var tocKey = a.GetManifestResourceStream(pluginname + ".toc.key");
                if(tocKey != null)
                {
                    using var nr = new NativeReader(tocKey);
                    return nr.ReadToEnd();
                }
            }
            return null;
        }

        public unsafe static byte[] ToTOCSignature(this byte[] input)
        {
            return CreateTOCSignature(input);
        }

        //public unsafe static bool ToValidateTOCSignature(this byte[] input)
        //{
        //    return ValidateTOCSignature(input);
        //}

        private static byte[] BuildTOCKey()
        {
            byte[] TOCKey = new byte[539];
            var pluginTocKey = LoadTocKeyFromPlugin();
            //if (pluginTocKey != null)
            //{
            //    for (var i = 0; i < pluginTocKey.Length; i++)
            //    {
            //        TOCKey[i] = pluginTocKey[i];
            //    }
            //}
            //else
            //{
                for (var i = 0; i < TOCKeyPart1.Length; i++)
                {
                    TOCKey[i] = TOCKeyPart1[i];
                }
            //}
            for (var i = 0; i < TOCKeyPart2.Length; i++)
            {
                TOCKey[i + 283] = TOCKeyPart2[i];
            }
            return TOCKey;
        }

        /// <summary>
        /// SuperBundleBackend.cpp / Deploy.cpp
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public unsafe static byte[] CreateTOCSignature(byte[] input)
        {
            //CreateStrongSignSignature(input);


            byte[] resultHash = new byte[256];
            byte[] TOCKey = BuildTOCKey();

#if DEBUG
            for (var i = 0; i < TOCKey.Length; i++)
            {
                var tck1 = TOCKey[i];
                var tck2 = TOCKey_F21[i];
                if (tck1 != tck2)
                {

                }
            }
#endif

            //var pluginTocKey = LoadTocKeyFromPlugin();


            var status = BCrypt.BCryptOpenAlgorithmProvider(out var cryptAlgHandle, "RSA", null, 0);
            bool cryptAlgHandleIsValid = status == 0x0;
            if (!cryptAlgHandleIsValid)
                return null;

            var cryptPrivateKeyHandle = BCrypt.BCryptImportKeyPair(cryptAlgHandle, "RSAPRIVATEBLOB", TOCKey, 0);
            if (cryptPrivateKeyHandle.IsInvalid)
                return null;

            var sha1HashKey = Encoding.ASCII.GetBytes("Powered by Frostbite \\o/ EA Digital Illusions CE AB");
            byte[] frostbiteHash_old = new HMACSHA1(sha1HashKey).ComputeHash(input);
            byte[] frostbiteHash = HMACSHA1.HashData(sha1HashKey, input);

            fixed (char* BCRYPT_SHA1_ALGORITHM = &"SHA1".GetPinnableReference())
            {
                BCrypt.BCRYPT_PKCS1_PADDING_INFO bCRYPT_PKCS1_PADDING_INFO = default(BCrypt.BCRYPT_PKCS1_PADDING_INFO);
                bCRYPT_PKCS1_PADDING_INFO.pszAlgId = BCRYPT_SHA1_ALGORITHM;
                GCHandle handle = GCHandle.Alloc(bCRYPT_PKCS1_PADDING_INFO, GCHandleType.Pinned);
                try
                {

                    //resultHash = BCrypt.BCryptSignHash(cryptPrivateKeyHandle, frostbiteHash, handle.AddrOfPinnedObject(), BCrypt.BCryptSignHashFlags.BCRYPT_PAD_PKCS1).ToArray();

                    Span<byte> outB = new Span<byte>(resultHash);
                    status = BCrypt.BCryptSignHash(cryptPrivateKeyHandle,
                                       handle.AddrOfPinnedObject(),
                                       new ReadOnlySpan<byte>(frostbiteHash),
                                       outB,
                                       out var pcbResult,
                                       BCrypt.BCryptSignHashFlags.BCRYPT_PAD_PKCS1);

                }
                finally
                {
                    handle.Free();
                }
            }

            if (resultHash == null || resultHash.Length != 256)
            {
                throw new Exception("Result Hash must be 256 bytes in Length");
            }

            return resultHash;
        }

        static byte[] FileObfuscationHeader_rsaPrivateKey0 = new byte[]
{
    0x52, 0x53, 0x41, 0x32, 0x00, 0x08, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00,
    0x80, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0xd4, 0x15, 0x1c, 0x84, 0x26,
    0xeb, 0x5f, 0x5f, 0x56, 0x02, 0x91, 0xd1, 0xc6, 0x14, 0x52, 0x3f, 0x0b, 0xfd, 0xa0, 0x25, 0x24,
    0xbf, 0x78, 0xa8, 0x47, 0xd3, 0x17, 0x3d, 0x59, 0xaf, 0x3b, 0x8c, 0xea, 0x6e, 0xec, 0x8b, 0xe4,
    0x9a, 0x7f, 0x3d, 0x19, 0xe8, 0x37, 0x7a, 0x92, 0xce, 0x5b, 0x57, 0x7a, 0xa3, 0x21, 0x59, 0xe7,
    0x73, 0x36, 0x7d, 0x8c, 0x4f, 0x35, 0xdc, 0xbf, 0x0d, 0xf7, 0xe7, 0xd6, 0x48, 0xd5, 0xd5, 0x2f,
    0x1f, 0xa5, 0xe1, 0xe5, 0xd5, 0x90, 0xe1, 0x9f, 0x4c, 0x2b, 0x77, 0x4c, 0x34, 0x42, 0x67, 0x41,
    0x92, 0x03, 0xdd, 0x4b, 0x46, 0xf8, 0x18, 0x87, 0x29, 0xe7, 0xa5, 0xc9, 0x3e, 0xc2, 0x74, 0x18,
    0xd5, 0xba, 0xdc, 0xe3, 0xb2, 0x86, 0x9c, 0x39, 0x9d, 0xa0, 0x79, 0xa3, 0xcd, 0x7d, 0x0f, 0xb8,
    0x24, 0x87, 0xad, 0x1d, 0xd0, 0x3c, 0xa9, 0xbe, 0x84, 0x06, 0x81, 0xac, 0x84, 0xb1, 0xb2, 0xb9,
    0x63, 0xcd, 0x41, 0xb6, 0x28, 0x8e, 0x14, 0x26, 0x94, 0x94, 0x31, 0x97, 0x31, 0x05, 0x4c, 0x0a,
    0x72, 0x87, 0x83, 0x00, 0x58, 0x2b, 0xb1, 0x3e, 0x50, 0x73, 0x1f, 0x74, 0x63, 0x64, 0xa2, 0x4e,
    0x4c, 0x0e, 0xc7, 0xaf, 0xd6, 0x3f, 0x34, 0xee, 0x02, 0xe2, 0xd3, 0xcb, 0x6e, 0xc3, 0x32, 0x80,
    0xa6, 0xbc, 0xb4, 0xd2, 0x37, 0xb1, 0x00, 0x55, 0xd4, 0xb0, 0x77, 0x53, 0x5d, 0xaa, 0xf4, 0xb1,
    0xd1, 0xad, 0xbe, 0x51, 0x87, 0xc2, 0x74, 0x91, 0xda, 0x29, 0x5d, 0x8b, 0xc6, 0x8d, 0x21, 0x18,
    0xb5, 0x2d, 0xfc, 0xba, 0xcf, 0x0e, 0x6b, 0x76, 0xc7, 0xda, 0x5a, 0x37, 0x6f, 0xab, 0xee, 0x7e,
    0x67, 0x46, 0xbf, 0xc3, 0x1d, 0xc9, 0x6e, 0xc4, 0xe1, 0x63, 0x91, 0x6f, 0xac, 0x1a, 0x21, 0xd8,
    0x7a, 0x7d, 0x92, 0x79, 0xe2, 0x2e, 0xb7, 0x4e, 0x0d, 0x9d, 0x07, 0xf2, 0x6b, 0x21, 0x09, 0x45,
    0xce, 0x2a, 0xbd, 0x71, 0x09, 0x54, 0x4d, 0xfd, 0x81, 0xe5, 0x63, 0xf8, 0xda, 0x54, 0xed, 0x0c,
    0x2b, 0xa1, 0x01, 0x88, 0x6e, 0x99, 0x8a, 0x66, 0x2b, 0x75, 0x66, 0xdb, 0x96, 0x23, 0x80, 0x39,
    0xe8, 0x2e, 0xe6, 0x56, 0xf1, 0xdd, 0xc6, 0x4e, 0xaa, 0xc6, 0x9c, 0xb3, 0x3a, 0xdc, 0x6b, 0x8b,
    0xcc, 0xb6, 0xe0, 0xde, 0x10, 0x59, 0xce, 0x43, 0xab, 0x0d, 0x49, 0x5c, 0x00, 0x01, 0xd1, 0xad,
    0xa9, 0xad, 0xc9, 0x29, 0x33, 0x37, 0xc8, 0x65, 0x9a, 0xec, 0x56, 0x0d, 0xc8, 0xfe, 0x6f, 0xdc,
    0x06, 0x56, 0x58, 0x81, 0xed, 0xa3, 0xc6, 0xb6, 0xc6, 0x25, 0x39, 0xde, 0xc2, 0x0d, 0x34, 0x41,
    0x37, 0xc4, 0x5f, 0xe2, 0xc8, 0x41, 0x17, 0x19, 0x5e, 0xcf, 0x67, 0x39, 0x37, 0x2f, 0x83, 0xa9,
    0x7f, 0x92, 0xab, 0x02, 0xab, 0x84, 0xb0, 0x7e, 0x9f, 0xf4, 0x43, 0xdf, 0xf6, 0xe3, 0xe2, 0x58,
    0x00, 0x84, 0x9f, 0xdc, 0xbd, 0xf3, 0xf5, 0xfc, 0x06, 0x4d, 0x05, 0x01, 0xec, 0xdc, 0x37, 0x2c,
    0xfc, 0x1e, 0xe8, 0x96, 0xd2, 0x79, 0xed, 0x95, 0x8e, 0x0d, 0xa0, 0xab, 0xb5, 0x27, 0xb2, 0xc2,
    0x2d, 0x1b, 0x82, 0xa0, 0x67, 0x0b, 0xa8, 0xd3, 0xdf, 0x21, 0x95, 0x92, 0x46, 0x88, 0x9f, 0xab,
    0x6b, 0x02, 0x10, 0xe6, 0x24, 0xbd, 0xa5, 0x95, 0x3e, 0x7d, 0x2a, 0x56, 0x0d, 0x8f, 0x70, 0x6e,
    0xc0, 0x74, 0x2c, 0xf5, 0x6a, 0xd9, 0xec, 0x83, 0x5d, 0xbc, 0x7e, 0x0f, 0xcb, 0x81, 0xdc, 0xff,
    0xb5, 0xf4, 0x3a, 0xf7, 0xc4, 0x57, 0x65, 0xaf, 0x87, 0xd2, 0x43, 0x0f, 0xaf, 0xb1, 0xf2, 0x98,
    0x6d, 0x41, 0xf7, 0xfd, 0x02, 0x33, 0x95, 0xc5, 0x16, 0xe0, 0x00, 0x7f, 0xa7, 0x33, 0x78, 0x6e,
    0xd0, 0x91, 0xcf, 0x95, 0xe5, 0x4a, 0xb7, 0x34, 0x03, 0x69, 0xed
};

        //public unsafe static byte[] CreateStrongSignSignature(byte[] input)
        //{
        //    do
        //    {
        //        fixed (char* BCRYPT_SHA1_ALGORITHM = &"SHA1".GetPinnableReference()) 
        //        {

        //            //::NTSTATUS status;
        //            NTSTATUS status = BCrypt.BCryptOpenAlgorithmProvider(out var cryptAlgHandle, "RSA", null, 0);// &cryptAlgHandle, BCRYPT_RSA_ALGORITHM, 0, 0);
        //            var cryptAlgHandleIsValid = status >= 0;
        //            if (status < 0)
        //                break;

        //            //status = BCrypt.BCryptImportKeyPair(cryptAlgHandle, 0 /* import key */,  BCRYPT_RSAPRIVATE_BLOB, &cryptPrivateKeyHandle, (PUCHAR)FileObfuscationHeader_rsaPrivateKey0, sizeof(FileObfuscationHeader_rsaPrivateKey0), 0);
        //            status = BCrypt.BCryptImportKeyPair(cryptAlgHandle, null /* import key */, "RSAPRIVATEBLOB", out var cryptPrivateKeyHandle, FileObfuscationHeader_rsaPrivateKey0, FileObfuscationHeader_rsaPrivateKey0.Length, 0);
        //            var cryptKeyHandleIsValid = status >= 0;
        //            if (status < 0)
        //                break;

        //            // see FileObfuscationHeader::validateSignature for the verification process
        //            //SHA1 sha1;
        //            //sha1.calculateHmac(data, dataSize, (const u8*)"Powered by Frostbite \\o/ EA Digital Illusions CE AB" /* not secret */, 51);
        //            var sha1HashKey = Encoding.ASCII.GetBytes("Powered by Frostbite \\o/ EA Digital Illusions CE AB");
        //            //byte[] frostbiteHash = new HMACSHA1(sha1HashKey).ComputeHash(input);
        //            byte[] frostbiteHash = HMACSHA1.HashData(sha1HashKey, input);

        //            //BCRYPT_PKCS1_PADDING_INFO paddingInfo; paddingInfo.pszAlgId = BCRYPT_SHA1_ALGORITHM;
        //            BCRYPT_PKCS1_PADDING_INFO paddingInfo;
        //            paddingInfo.pszAlgId = BCRYPT_SHA1_ALGORITHM;
        //        GCHandle handle = GCHandle.Alloc(paddingInfo, GCHandleType.Pinned);
        //            Span <byte> outB = new Span<byte>();
        //            status = BCrypt.BCryptSignHash(cryptPrivateKeyHandle,
        //                               handle.AddrOfPinnedObject(),
        //                               new ReadOnlySpan<byte>(frostbiteHash),
        //                               outB,
        //                               out var pcbResult,
        //                               BCrypt.BCryptSignHashFlags.BCRYPT_PAD_PKCS1);
        //            //FB_ASSERT(status >= 0);
        //            if (status < 0)
        //                break;

        //            //FB_ASSERT(signatureSize == 256); // signature size must be 256, else we must re-write FileObfuscationHeader::validateSignature
        //            if (pcbResult != 256)
        //                break;

        //            // sign the hash and store the signature in the FileObfuscationHeader
        //            //status = ::BCryptSignHash(cryptPrivateKeyHandle, &paddingInfo,
        //            //    sha1.hash, sizeof(sha1.hash), hdr.signature, sizeof(hdr.signature), &signatureSize,
        //            //    BCRYPT_PAD_PKCS1);
        //            //FB_ASSERT(status >= 0);
        //            //success = status >= 0;
        //            //if (status < 0)
        //            //    break;
        //        }
        //    } while (false);

        //    return null;
        //}

        //public unsafe static bool ValidateTOCSignature(byte[] input)
        //{
        //    byte[] TOCKey = BuildTOCKey();

        //    var status = BCrypt.BCryptOpenAlgorithmProvider(out var cryptAlgHandle, "RSA", null, 0);
        //    bool cryptAlgHandleIsValid = status == 0x0;
        //    if (!cryptAlgHandleIsValid)
        //        return false;

        //    BCrypt.SafeKeyHandle cryptPrivateKeyHandle
        //        = BCrypt.BCryptImportKeyPair(BCrypt.BCryptOpenAlgorithmProvider("RSA"), "RSAPRIVATEBLOB", TOCKey);

        //    var sha1HashKey = Encoding.ASCII.GetBytes("Powered by Frostbite \\o/ EA Digital Illusions CE AB");
        //    //byte[] frostbiteHash = new HMACSHA1(sha1HashKey).ComputeHash(input);
        //    byte[] frostbiteHash = HMACSHA1.HashData(sha1HashKey, input);

        //    fixed (char* BCRYPT_SHA1_ALGORITHM = &"SHA1".GetPinnableReference())
        //    {
        //        BCrypt.BCRYPT_PKCS1_PADDING_INFO bCRYPT_PKCS1_PADDING_INFO = default(BCrypt.BCRYPT_PKCS1_PADDING_INFO);
        //        bCRYPT_PKCS1_PADDING_INFO.pszAlgId = BCRYPT_SHA1_ALGORITHM;
        //        GCHandle handle = GCHandle.Alloc(bCRYPT_PKCS1_PADDING_INFO, GCHandleType.Pinned);
        //        return BCrypt.BCryptVerifySignature(cryptPrivateKeyHandle, frostbiteHash, input, flags: BCrypt.BCryptSignHashFlags.BCRYPT_PAD_PKCS1);
        //        //try
        //        //{

        //        //    //resultHash = BCrypt.BCryptSignHash(cryptPrivateKeyHandle, frostbiteHash, handle.AddrOfPinnedObject(), BCrypt.BCryptSignHashFlags.BCRYPT_PAD_PKCS1).ToArray();
        //        //    resultHash = BCrypt.BCryptSignHash(cryptPrivateKeyHandle, frostbiteHash, handle.AddrOfPinnedObject(), BCrypt.BCryptSignHashFlags.BCRYPT_PAD_PKCS1).ToArray();
        //        //}
        //        //finally
        //        //{
        //        //    handle.Free();
        //        //}
        //    }

        //    //return true;
        //}

        public static void RebuildTOCSignature(Stream stream)
        {
            if (!stream.CanWrite)
                throw new IOException("Unable to Write to this Stream!");

            if (!stream.CanRead)
                throw new IOException("Unable to Read to this Stream!");

            if (!stream.CanSeek)
                throw new IOException("Unable to Seek this Stream!");

            byte[] streamBuffer = new byte[stream.Length - 556];
            stream.Position = 556;
            stream.Read(streamBuffer, 0, (int)stream.Length - 556);
            var newTocSig = streamBuffer.ToTOCSignature();
            stream.Position = 8;
            stream.Write(newTocSig);
        }

        public static void RebuildTOCSignature(string filePath)
        {
            using (var fsTocSig = new FileStream(filePath, FileMode.Open))
                RebuildTOCSignature(fsTocSig);
        }

        public static IEnumerable<DataRow> ToEnumerable(this DataTable dataTable)
        {
            foreach (DataRow row in dataTable.Rows)
                yield return row;
        }

        public static bool PropertyExists(object obj, string propName)
        {
            if (obj is ExpandoObject)
                return ((IDictionary<string, object>)obj).ContainsKey(propName);

            return obj.GetProperty(propName) != null;
        }

        public static bool PropertyExistsDynamic(this ExpandoObject obj, string propName)
        {
            if (obj is ExpandoObject)
                return ((IDictionary<string, object>)obj).ContainsKey(propName);

            return obj.GetProperty(propName) != null;
        }


        public static PropertyInfo[] GetProperties(this object obj)
        {
            Type t = obj.GetType();
            return t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        }

        public static PropertyInfo GetProperty(this object obj, string propName)
        {
            Type t = obj.GetType();
            return t.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
        }

        public static object GetPropertyValue(this object obj, string propName)
        {
            Type t = obj.GetType();
            var p = t.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
            return p.GetValue(obj);
        }

        public static void SetPropertyValue<T>(object obj, string propName, T value)
        {
            Type t = obj.GetType();
            Type t2 = value.GetType();
            var p = t.GetProperty(propName);
            if (propName != "BaseField")
                p.SetValue(obj, value);
        }

        public static void SetPropertyValue(object obj, string propName, dynamic value)
        {
            Type t = obj.GetType();
            Type t2 = value.GetType();
            var p = t.GetProperty(propName);
            var parseMethod = p.PropertyType.GetMethods().FirstOrDefault(x => x.Name == "Parse");
            if (parseMethod != null && p.PropertyType != value.GetType())
            {
                var v = parseMethod.Invoke(p, new[] { value });
                if (propName != "BaseField")
                    p.SetValue(obj, v);
            }
            else
            {
                if (propName != "BaseField")
                    p.SetValue(obj, value);
            }
        }

        public static bool HasProperty(object obj, string propName)
        {
            return obj.GetType().GetProperty(propName) != null;
        }

        public static bool HasProperty(ExpandoObject obj, string propertyName)
        {
            return ((IDictionary<String, object>)obj).ContainsKey(propertyName);
        }

        //public static void SetPropertyValue(this object obj, string propName, dynamic value)
        //{
        //    Type t = obj.GetType();
        //    var p = t.GetProperty(propName);
        //    p.SetValue(obj, value);
        //}

        public static T GetObjectAsType<T>(this object obj)
        {
            Type t = obj.GetType();
            var s = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<T>(s);
        }

        /// <summary>
        /// Perform a deep Copy of the object, using Json as a serialization method. NOTE: Private members are not cloned using this method.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>The copied object.</returns>
        //public static T CloneJson<T>(this T source)
        //{
        //    // Don't serialize a null object, simply return the default for that object
        //    if (ReferenceEquals(source, null)) return default;

        //    // initialize inner objects individually
        //    // for example in default constructor some list property initialized with some values,
        //    // but in 'source' these items are cleaned -
        //    // without ObjectCreationHandling.Replace default constructor values will be added to result
        //    var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

        //    return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source), deserializeSettings);
        //}

        /// <summary>
        /// Perform a deep Copy of the object, using Json as a serialization method. NOTE: Private members are not cloned using this method.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>The copied object.</returns>
        public static dynamic CloneJson(this object source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (source is null)
                return null;

            var sourceType = source.GetType();

            // initialize inner objects individually
            // for example in default constructor some list property initialized with some values,
            // but in 'source' these items are cleaned -
            // without ObjectCreationHandling.Replace default constructor values will be added to result
            var deserializeSettings = new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                MaxDepth = 10
            };

            var s = JsonConvert.SerializeObject(source, deserializeSettings);
            return JsonConvert.DeserializeAnonymousType(s, sourceType, deserializeSettings);
        }

        public static string ApplicationDirectory
        {
            get
            {
                return System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\";
            }
        }
    }

}

using FMT.FileTools.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostySdk.Frostbite.PluginInterfaces
{
    public static class BaseBinarySb
    {
        public enum Magic : uint
        {
            Standard = 0xED1CEDB8,
            Fifa = 0xC3889333,
            Encrypted = 0xC3E5D5C3
        }

        /// <summary>
        /// Dependent on the FB version, games have different salts.
        /// If the game uses a version newer than 2017 it uses "pecn" else it uses "pecm".
        /// <see cref="ProfileVersion.Battlefield5"/> is the only game that uses "arie".
        /// </summary>
        /// <returns>The salt, that the current game uses.</returns>
        public static uint GetSalt()
        {
            string salt = "pecm";

            if (ProfileManager.IsLoaded(FMT.FileTools.Modding.EGame.BFV))
                salt = "arie";

            else if (ProfileManager.DataVersion >= (int)EGame.FIFA19 && !ProfileManager.IsLoaded(EGame.StarWarsSquadrons))
                salt = "pecn";


            byte[] bytes = Encoding.ASCII.GetBytes(salt);
            return (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3] << 0);
        }

        /// <summary>
        /// Only the games using the FifaAssetLoader use a different Magic than <see cref="Magic.Standard"/>.
        /// </summary>
        /// <returns>The magic the current game uses.</returns>
        public static Magic GetMagic()
        {
            switch (ProfileManager.DataVersion)
            {
                // TODO: what game uses encrypted magic
                case (int)EGame.FIFA19:
                case (int)EGame.MADDEN20:
                case (int)EGame.FIFA20:
                case (int)EGame.MADDEN21:
                    return Magic.Fifa;
                default:
                    return Magic.Standard;
            }
        }

        public static bool IsValidMagic(Magic magic) => Enum.IsDefined(typeof(Magic), magic);
    }
}

using System.Collections.Generic;
using System.Data;
using v2k4FIFAModding.Career.CME.FIFA;

namespace CareerExpansionMod.CEM.FIFA
{
    public class CareerDB1
    {

        public DataSet ParentDataSet { get; set; }

        public static CareerDB1 Current;
        public static FIFAUsers FIFAUser { get; set; }
        public static FIFATeam UserTeam { get; set; }

        private static List<FIFAPlayer> _userPlayers;
        public static List<FIFAPlayer> UserPlayers
        {
            get
            {
                if (UserTeam != null)
                    _userPlayers = UserTeam.GetPlayers();

                return _userPlayers;
            }
            set
            {
                _userPlayers = value;
            }
        }
    }
}

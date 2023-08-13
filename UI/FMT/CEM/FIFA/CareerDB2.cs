using System.Data;

namespace v2k4FIFAModding.Career.CME.FIFA
{
    public partial class CareerDB2
    {
        public DataSet ParentDataSet { get; set; }

        public static CareerDB2 Current { get; set; }

        public CareerDB2()
        {
            Current = this;
        }

        public EnumerableRowCollection<DataRow> teamsEnumerable { get; set; }
        public DataTable teams { get; set; }
        public EnumerableRowCollection<DataRow> players { get; set; }
        public EnumerableRowCollection<DataRow> teamplayerlinks { get; set; }
        public EnumerableRowCollection<DataRow> teamstadiumlinks { get; set; }
        public EnumerableRowCollection<DataRow> cz_teams { get; set; }
        public EnumerableRowCollection<DataRow> playerloans { get; set; }
        public EnumerableRowCollection<DataRow> default_mentalities { get; set; }
        public EnumerableRowCollection<DataRow> formations { get; set; }
        public EnumerableRowCollection<DataRow> default_teamsheets { get; set; }
        public EnumerableRowCollection<DataRow> editedplayernames { get; set; }
        public EnumerableRowCollection<DataRow> leagueteamlinks { get; set; }
        public EnumerableRowCollection<DataRow> teamkits { get; set; }
        public EnumerableRowCollection<DataRow> competition { get; set; }
        public EnumerableRowCollection<DataRow> leagues { get; set; }
        public EnumerableRowCollection<DataRow> manager { get; set; }
        public EnumerableRowCollection<DataRow> competitionballs { get; set; }
        public EnumerableRowCollection<DataRow> rivals { get; set; }
        public EnumerableRowCollection<DataRow> fixtures { get; set; }
        public EnumerableRowCollection<DataRow> cz_teamkits { get; set; }
        public EnumerableRowCollection<DataRow> player_grudgelove { get; set; }
        public EnumerableRowCollection<DataRow> smrivals { get; set; }
        public EnumerableRowCollection<DataRow> dcplayernames { get; set; }
        public EnumerableRowCollection<DataRow> cz_assets { get; set; }
        public EnumerableRowCollection<DataRow> createplayer { get; set; }
        public EnumerableRowCollection<DataRow> rowteamnationlinks { get; set; }
        public EnumerableRowCollection<DataRow> playersuspensions { get; set; }
        public EnumerableRowCollection<DataRow> outfitarrangements { get; set; }
        public EnumerableRowCollection<DataRow> playerformdiff { get; set; }
        public EnumerableRowCollection<DataRow> competitionsponsorlinks { get; set; }
        public EnumerableRowCollection<DataRow> stories { get; set; }
        public EnumerableRowCollection<DataRow> modeadboardlinks { get; set; }
        public EnumerableRowCollection<DataRow> teamformationteamstylelinks { get; set; }
        public EnumerableRowCollection<DataRow> teamnationlinks { get; set; }
        public EnumerableRowCollection<DataRow> teamsponsorlinks { get; set; }
        public EnumerableRowCollection<DataRow> referee { get; set; }
        public EnumerableRowCollection<DataRow> competitionkits { get; set; }
        public EnumerableRowCollection<DataRow> stadiumassignments { get; set; }
        public EnumerableRowCollection<DataRow> presentationmodesettings { get; set; }
        public EnumerableRowCollection<DataRow> teamformdiff { get; set; }

        public EnumerableRowCollection<DataRow> leaguerefereelinks { get; set; }

        public EnumerableRowCollection<DataRow> previousteam { get; set; }
        public EnumerableRowCollection<DataRow> euroseeds { get; set; }
        public EnumerableRowCollection<DataRow> version { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using v2k4FIFAModding.Career.CME.FIFA;

namespace CareerExpansionMod.CEM.FIFA
{
    public class FIFATeam
    {
        public int assetid { get; set; }
        public int teamcolor1g { get; set; }
        public int teamcolor1r { get; set; }
        public int clubworth { get; set; }
        public int teamcolor2b { get; set; }
        public int teamcolor2r { get; set; }
        public int foundationyear { get; set; }
        public int teamcolor3r { get; set; }
        public int teamcolor1b { get; set; }
        public int opponentweakthreshold { get; set; }
        public int latitude { get; set; }
        public int teamcolor3g { get; set; }
        public int opponentstrongthreshold { get; set; }
        public int ballid { get; set; }
        public int teamcolor2g { get; set; }
        public string teamname { get; set; }
        public int teamcolor3b { get; set; }
        public int powid { get; set; }
        public int defensivestyle { get; set; }
        public int rightfreekicktakerid { get; set; }
        public int flamethrowercannon { get; set; }
        public int domesticprestige { get; set; }
        public int genericint2 { get; set; }
        public int defensivedepth { get; set; }
        public int hasvikingclap { get; set; }
        public int jerseytype { get; set; }
        public int popularity { get; set; }
        public int hastifo { get; set; }
        public int teamstadiumcapacity { get; set; }
        public int iscompetitionscarfenabled { get; set; }
        public int cityid { get; set; }
        public int defensivewidth { get; set; }
        public int rivalteam { get; set; }
        public int isbannerenabled { get; set; }
        public int midfieldrating { get; set; }
        public int matchdayoverallrating { get; set; }
        public int matchdaymidfieldrating { get; set; }
        public int attackrating { get; set; }
        public int playersinboxcorner { get; set; }
        public int longitude { get; set; }
        public int matchdaydefenserating { get; set; }
        public int hasstandingcrowd { get; set; }
        public int favoriteteamsheetid { get; set; }
        public int defenserating { get; set; }
        public int iscompetitionpoleflagenabled { get; set; }
        public int skinnyflags { get; set; }
        public int uefa_consecutive_wins { get; set; }
        public int longkicktakerid { get; set; }
        public int bodytypeid { get; set; }
        public int trait1vweak { get; set; }
        public int iscompetitioncrowdcardsenabled { get; set; }
        public int rightcornerkicktakerid { get; set; }
        public int suitvariationid { get; set; }
        public int uefa_cl_wins { get; set; }
        public int domesticcups { get; set; }
        public int ethnicity { get; set; }
        public int leftcornerkicktakerid { get; set; }
        public int youthdevelopment { get; set; }
        public int teamid { get; set; }
        public int uefa_el_wins { get; set; }
        public int trait1vequal { get; set; }
        public int suittypeid { get; set; }
        public int numtransfersin { get; set; }
        public int playersinboxfk { get; set; }
        public int stanchionflamethrower { get; set; }
        public int captainid { get; set; }
        public int offensivestyle { get; set; }
        public int personalityid { get; set; }
        public int playersinboxcross { get; set; }
        public int prev_el_champ { get; set; }
        public int leftfreekicktakerid { get; set; }
        public int leaguetitles { get; set; }
        public int genericbanner { get; set; }
        public int transferbudget { get; set; }
        public int overallrating { get; set; }
        public int offensivewidth { get; set; }
        public int profitability { get; set; }
        public int utcoffset { get; set; }
        public int penaltytakerid { get; set; }
        public int freekicktakerid { get; set; }
        public int crowdskintonecode { get; set; }
        public int internationalprestige { get; set; }
        public int trainingstadium { get; set; }
        public int form { get; set; }
        public int genericint1 { get; set; }
        public int trait1vstrong { get; set; }
        public int matchdayattackrating { get; set; }

        private static Dictionary<int, Dictionary<int, FIFAPlayer>> CachedPlayersToTeam { get; } = new Dictionary<int, Dictionary<int, FIFAPlayer>>();
        //private static Dictionary<int, FIFAPlayer> CachedPlayers = new Dictionary<int, FIFAPlayer>();
        private static DateTime CachedPlayersDate = DateTime.Now;

        public static void ClearCache()
        {
            CachedPlayersToTeam.Clear();
            //CachedPlayers.Clear();
            CachedPlayersDate = DateTime.Now;
        }

        public List<FIFAPlayer> GetPlayers()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (!CachedPlayersToTeam.ContainsKey(teamid) || CachedPlayersToTeam[teamid].Count == 0)
            {
                CachedPlayersToTeam.Add(teamid, new Dictionary<int, FIFAPlayer>());

                //var tplinks = CareerDB2.Current.teamplayerlinks.Where(x => x["teamid"].ToString() == CareerDB1.FIFAUser.clubteamid.ToString());
                var tplinks = CareerDB2.Current.teamplayerlinks.Where(x => x["teamid"].ToString() == teamid.ToString());
                var ps = (from tpl in tplinks
                          join p in CareerDB2.Current.players on tpl["playerid"].ToString() equals p["playerid"].ToString()
                          select p);

                foreach (var i in ps)
                {
                    var playerId = int.Parse(i["playerid"].ToString());
                    if (!CachedPlayersToTeam[teamid].ContainsKey(playerId))
                    {
                        var pl = CareerDB2.Current.players.FirstOrDefault(x => x["playerid"].ToString() == i["playerid"].ToString());
                        if (pl != null)
                        {
                            var plItem = CEMUtilities.CreateItemFromRow<FIFAPlayer>(pl);

                            CachedPlayersToTeam[teamid].Add(playerId, plItem);
                        }
                    }
                }

                CachedPlayersDate = DateTime.Now;
            }

            sw.Stop();
            //Debug.WriteLine($"GetPlayers() took :: {sw.Elapsed.TotalSeconds}s");

            return CachedPlayersToTeam[teamid].Values.ToList();
        }

        public static double InfluenceCalculation(FIFAPlayer p)
        {


            var leadership = (6 - p.emotion) + (p.personality * 1.25);

            var dateJoined = CEMUtilities.FIFACoreDateTime.AddDays(p.playerjointeamdate);

            leadership += Convert.ToInt32(Math.Ceiling(p.TimeAtClubInYears > 2 ? p.TimeAtClubInYears * 0.75 : p.TimeAtClubInYears));

            leadership += Convert.ToInt32(Math.Ceiling(p.AgeInYears * 0.09));

            leadership = Math.Max(0, Math.Min(20, leadership));

            if (leadership > 0)
            {
                leadership = Math.Round((leadership / 20) * 10);
            }

            return leadership;
        }

        public EmotionalTypes EmotionalTypeOfTeam
        {
            get
            {
                var avgEmotion = GetPlayers().Average(x => x.emotion);
                var t = (EmotionalTypes)Math.Round(avgEmotion);

                return t;
            }

        }

        public PersonalityTypes PersonalityTypeOfTeam
        {
            get
            {
                var avg = GetPlayers().Average(x => x.personality);
                var t = (PersonalityTypes)Math.Round(avg);

                return t;
            }
        }

        public string IdealCaptainOfTeam
        {
            get
            {
                return GetPlayers().OrderByDescending(x => InfluenceCalculation(x)).FirstOrDefault().Name;
            }
        }

        public List<FIFAPlayer> GetTeamLeaders()
        {
            List<FIFAPlayer> players = GetPlayers().OrderByDescending(x => InfluenceCalculation(x)).Take(3).ToList();

            return players;
        }

        public List<FIFAPlayer> GetTeamInfluences()
        {
            List<FIFAPlayer> players = GetPlayers().OrderByDescending(x => InfluenceCalculation(x)).Skip(3).Take(4).ToList();

            return players;
        }

        public List<FIFAPlayer> GetTeamTroubleMakers()
        {
            List<FIFAPlayer> players = GetPlayers().OrderBy(x => InfluenceCalculation(x)).Take(3).ToList();


            return players;
        }

        public FIFALeague GetLeague()
        {
            //CareerDB2.Current.leagueteamlinks.Where(x=> x.ItemArray."teamid"] == this.teamid.ToString()))
            if (CareerDB2.Current != null && CareerDB2.Current.leagueteamlinks != null)
            {
                var tl_link = CareerDB2.Current.leagueteamlinks.FirstOrDefault(x => x["teamid"].ToString() == this.teamid.ToString());
                if (tl_link != null && CareerDB2.Current.leagues != null)
                {
                    var leagueRow = CareerDB2.Current.leagues.FirstOrDefault(x => x["leagueid"].ToString() == tl_link["leagueid"].ToString());

                    var league = CEMUtilities.CreateItemFromRow<FIFALeague>(leagueRow);
                    Debug.WriteLine("[DEBUG] FIFATeam::GetLeague()::League::" + league.leaguename);
                    return league;

                }
                else
                {
                    Debug.WriteLine("[ERROR] FIFATeam::GetLeague()::Couldn't find Leagues");
                }
            }

            Debug.WriteLine("[INFO] FIFATeam::GetLeague()::Using a Mock League");

            // Send a test / mock league
            return new FIFALeague() { leaguename = "Mock League", level = 1 };
        }

        public static IEnumerable<FIFATeam> GetFIFATeams()
        {
            if (CareerDB2.Current != null && CareerDB2.Current.teams != null)
            {
                foreach (DataRow dr in CareerDB2.Current.teams.Rows)
                {
                    yield return CEMUtilities.CreateItemFromRow<FIFATeam>(dr);
                }
            }
        }

    }
}

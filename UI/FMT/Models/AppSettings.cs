namespace FrostbiteModdingUI.Models
{
    public static class AppSettings
    {
        private static Settings _Settings;
        public static Settings Settings
        {
            get
            {
                if (_Settings == null)
                    _Settings = new Settings();

                return _Settings;
            }
        }
    }

    public class Settings
    {
        public string GameInstallEXEPath { get; set; }
    }
}

namespace FrostySdk.Frostbite.IO
{
    /// <summary>
    /// A place for the sole locale ini modification
    /// </summary>
    public class LocaleIniManager
    {
        public byte[] OriginalData { get; set; }

        public bool OriginalDataWasEncrypted { get; set; } = FileSystem.Instance.LocaleIsEncrypted;

        public byte[] UserData { get; set; }

        public bool HasUserData { get { return UserData != null && UserData.Length > 0; } }

        public byte[] UserDataEncrypted
        {
            get
            {
                return FileSystem.Instance.WriteLocaleIni(UserData, false);
            }
        }


        public LocaleIniManager()
        {
            Load();
        }

        public LocaleIniManager(in byte[] data) : this()
        {
            UserData = data;
        }

        public byte[] Load()
        {
            if (FileSystem.Instance == null)
                return null;

            if (OriginalData == null || OriginalData.Length == 0)
                OriginalData = FileSystem.Instance.ReadLocaleIni();


            return UserData;
        }

        public void Save(in byte[] inData)
        {
            UserData = inData;
        }

        public void Reset()
        {
            UserData = null;
        }
    }
}

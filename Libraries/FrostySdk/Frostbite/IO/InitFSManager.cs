using System.Collections.Generic;

namespace FrostySdk.Frostbite.IO
{

    /// <summary>
    /// A place for all Initfs modifications
    /// </summary>
    public class InitFSManager
    {
        public Dictionary<string, byte[]> DataModifications { get; } = new Dictionary<string, byte[]>();

        public void ModifyFile(string key, byte[] data)
        {
            this.DataModifications[key] = data;
        }

        public bool RemoveFile(string key)
        {
            return this.DataModifications.Remove(key);
        }

        public void ClearAll(string key)
        {
            this.DataModifications.Clear();
        }

    }
}

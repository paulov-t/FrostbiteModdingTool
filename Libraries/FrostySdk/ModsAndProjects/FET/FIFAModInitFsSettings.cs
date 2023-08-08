using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostySdk.ModsAndProjects.FET
{
    public class FIFAModInitFsSettings
    {
        private readonly List<InitFSModification> initFsModifications = new List<InitFSModification>();

        private bool isLocallyDirty;

        public bool IsDirty
        {
            get
            {
                if (!isLocallyDirty)
                {
                    return initFsModifications.Any((InitFSModification f) => f.IsDirty);
                }
                return true;
            }
        }

        public IReadOnlyList<InitFSModification> InitFsModifications => initFsModifications.AsReadOnly();

        public void ClearDirtyFlag()
        {
            isLocallyDirty = false;
            foreach (InitFSModification initFsModification in initFsModifications)
            {
                initFsModification.ClearDirtyFlag();
            }
        }

        public void AddInitFsModification(InitFSModification file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            initFsModifications.Add(file);
            isLocallyDirty = true;
        }

        public void RemoveInitFsModification(InitFSModification file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            initFsModifications.Remove(file);
            isLocallyDirty = true;
        }

        public void RemoveAllInitFsModifications()
        {
            if (initFsModifications.Count > 0)
            {
                isLocallyDirty = true;
            }
            initFsModifications.Clear();
        }
    }

    public class InitFSModification
    {
        private string description = string.Empty;

        private readonly Dictionary<string, byte[]> contents = new Dictionary<string, byte[]>();

        public bool IsDirty { get; private set; }

        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (!description.Equals(value))
                {
                    description = value;
                    IsDirty = true;
                }
            }
        }

        public IReadOnlyDictionary<string, byte[]> Contents => contents;

        public void ModifyFile(string key, byte[] data)
        {
            contents[key] = data;
            IsDirty = true;
        }

        public void ClearModification(string key)
        {
            if (contents.Remove(key))
            {
                IsDirty = true;
            }
        }

        public void ClearDirtyFlag()
        {
            IsDirty = false;
        }
    }

}

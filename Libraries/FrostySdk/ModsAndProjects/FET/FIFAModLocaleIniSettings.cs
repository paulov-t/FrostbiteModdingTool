using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostySdk.ModsAndProjects.FET
{
    public class FIFAModLocaleIniSettings
    {
        private readonly List<LocaleIniFile> localeIniFiles = new List<LocaleIniFile>();

        private bool isLocallyDirty;

        public bool IsDirty
        {
            get
            {
                if (!isLocallyDirty)
                {
                    return localeIniFiles.Any((LocaleIniFile f) => f.IsDirty);
                }
                return true;
            }
        }

        public IReadOnlyList<LocaleIniFile> LocaleIniFiles => localeIniFiles.AsReadOnly();

        public void ClearDirtyFlag()
        {
            isLocallyDirty = false;
            foreach (LocaleIniFile localeIniFile in localeIniFiles)
            {
                localeIniFile.ClearDirtyFlag();
            }
        }

        public void AddLocaleIniFile(LocaleIniFile file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            localeIniFiles.Add(file);
            isLocallyDirty = true;
        }

        public void RemoveLocaleIniFile(LocaleIniFile file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            localeIniFiles.Remove(file);
            isLocallyDirty = true;
        }

        public void RemoveAllLocaleIniFiles()
        {
            if (localeIniFiles.Count > 0)
            {
                isLocallyDirty = true;
            }
            localeIniFiles.Clear();
        }

        
    }


    public class LocaleIniFile
    {
        private string description = string.Empty;

        private string contents = string.Empty;

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

        public string Contents
        {
            get
            {
                return contents;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (!contents.Equals(value))
                {
                    contents = value;
                    IsDirty = true;
                }
            }
        }

        public void ClearDirtyFlag()
        {
            IsDirty = false;
        }
    }


}

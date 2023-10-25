using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMT
{
    public class FMTAppSettings : INotifyPropertyChanged
    {

        private static FMTAppSettings instance;// = new LauncherConfig();

        public event PropertyChangedEventHandler PropertyChanged;

        public static FMTAppSettings Instance
        {
            get
            {
                if (instance == null)
                    Instance = Load();

                return instance;
            }
            set { instance = value; }
        }

        private bool doNotUseRegistry;

        public bool DoNotUseRegistry
        {
            get { return doNotUseRegistry; }
            set 
            { 
                doNotUseRegistry = value; 
                if (PropertyChanged != null) 
                    PropertyChanged(this, new PropertyChangedEventArgs("DoNotUseRegistry"));

                Save();
            }
        }


        public string GameInstallEXEPath { get; set; }


        private FMTAppSettings()
        {
            if (instance == null)
            {
                instance = this;
                instance.PropertyChanged += Instance_PropertyChanged;
                return;
            }
        }

        private void Instance_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Instance.Save();
        }

        public static string FMTAppSettingsPath { get; } = Path.Combine(App.ApplicationDirectory, "FMTAppSettings.json");

        private static FMTAppSettings Load()
        {
            FMTAppSettings appsettings = new FMTAppSettings()
            {
                DoNotUseRegistry = false,
            };
            if (File.Exists(FMTAppSettingsPath))
                appsettings = JsonConvert.DeserializeObject<FMTAppSettings>(File.ReadAllText(FMTAppSettingsPath));

            return appsettings;
        }

        public void Save()
        {
            File.WriteAllText(FMTAppSettingsPath, JsonConvert.SerializeObject(this));
        }
    }
}

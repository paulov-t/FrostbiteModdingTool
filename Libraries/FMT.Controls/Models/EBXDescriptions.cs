using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace FMT.Models
{
    public class EBXDescriptions
    {
        public class EBXDescription
        {
            public class EBXDescriptionProperty
            {
                public string PropertyName { get; set; }

                public string Description { get; set; }

            }

            public string TypeName { get; set; }

            public List<EBXDescriptionProperty> Properties { get; set; }


        }

        public List<EBXDescription> Descriptions { get; set; } = new List<EBXDescription>();

        private static bool ShouldUseCacheDescriptions = false;
        private static EBXDescriptions cachedDescriptions;

        public static EBXDescriptions CachedDescriptions
        {
            get
            {
                if (cachedDescriptions == null || !ShouldUseCacheDescriptions)
                {
                    cachedDescriptions = new EBXDescriptions();

                    var descriptionsDirectory = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Models", "EBX", "Descriptions"), "*.json", new EnumerationOptions() { RecurseSubdirectories = true });
                    foreach (var descFile in descriptionsDirectory)
                    {
                        var ebxD = JsonConvert.DeserializeObject<EBXDescriptions>(
                            System.IO.File.ReadAllText(descFile));
                        cachedDescriptions.Descriptions.AddRange(ebxD.Descriptions);
                        //cachedDescriptions.Descriptions = cachedDescriptions.Descriptions.Distinct().ToList();
                    }
                }

                return cachedDescriptions;
            }
            set { cachedDescriptions = value; }
        }

    }
}

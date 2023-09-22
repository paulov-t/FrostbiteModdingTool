using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FMT.FileTools
{
    public static class EmbeddedResourceHelper
    {
        public static Stream GetEmbeddedResourceByName(string name)
        {
            Stream stream = null;
            string resourceName = null;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetManifestResourceNames().Length > 0))
            {
                var allResources = a.GetManifestResourceNames();
                resourceName = allResources.LastOrDefault(x => x.EndsWith(name));
                if (!string.IsNullOrEmpty(resourceName))
                {
                    stream = a.GetManifestResourceStream(resourceName);
                    break;
                }
            }

            if (resourceName == null || stream == null)
                throw new FileNotFoundException(name + " cannot be found");

            return stream;
        }

        /// <summary>
        /// Gets a Key Value Pair of each Embedded Resource stream that contains the filter(s) provided
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Dictionary<string, Stream> GetEmbeddedResourcesByName(string[] filter)
        {
            if (filter == null || filter.Length == 0)
                throw new ArgumentNullException("filter must have a value");

            Dictionary<string, Stream> resourceDict = new Dictionary<string, Stream>();
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {

                var allResources = a.GetManifestResourceNames().ToList();
                allResources = allResources.Where(x => x.Contains(filter[0])).ToList();
                if (filter.Length > 1)
                {
                    for (var iFilter = 1; iFilter < filter.Length; iFilter++)
                    {
                        allResources = allResources.Where(x => x.Contains(filter[iFilter])).ToList();
                    }
                }
                foreach (var resourceName in allResources)
                {
                    Stream stream = a.GetManifestResourceStream(resourceName);
                    // there should only be 1 of the key
                    if (!resourceDict.ContainsKey(resourceName))
                    {
                        resourceDict.Add(resourceName, stream);
                    }
                }
            }

            return resourceDict;
        }
    }
}

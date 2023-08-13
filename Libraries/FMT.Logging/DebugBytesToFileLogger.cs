using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FMT.Logging
{
    public class DebugBytesToFileLogger
    {
        public static DebugBytesToFileLogger Instance { get; } = new DebugBytesToFileLogger();

        public DebugBytesToFileLogger() 
        {
            Directory.CreateDirectory(LoggingConstants.LoggingPath);
        }

        public void WriteAllBytes(string filename, byte[] bytes, string prefixPath = "")
        {
            if (!string.IsNullOrEmpty(prefixPath))
            {
                Directory.CreateDirectory(Path.Combine(LoggingConstants.LoggingPath, prefixPath));
                File.WriteAllBytes(Path.Combine(LoggingConstants.LoggingPath, prefixPath, filename), bytes);
            }
            else
                File.WriteAllBytes(Path.Combine(LoggingConstants.LoggingPath, filename), bytes);
        }
    }
}

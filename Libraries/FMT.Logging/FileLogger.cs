using System;
using System.IO;
using System.Linq;

namespace FMT.Logging
{
    public class FileLogger
    {
        public static DateTime LoggerStartDateTime { get; } = DateTime.Now;

        public static FileLogger Instance { get; } = new FileLogger();

        public static string GetFileLoggerPath()
        {
            var fmtLogPath = $"FMT.Log.{LoggerStartDateTime.ToString("yyyy-MM-dd.HH-mm")}.log";
            return fmtLogPath;
        }

        public FileLogger()
        {
            foreach(var oldFile in Directory.GetFiles(AppContext.BaseDirectory, "FMT.Log.*").Select(x=> new FileInfo(x)))
            {
                if (!oldFile.Exists)
                    continue;

                if (oldFile.LastWriteTime < DateTime.Now.AddDays(-3))
                    oldFile.Delete();
            }

            if (!File.Exists(GetFileLoggerPath()))
                File.WriteAllText(GetFileLoggerPath(), "");
        }

        public static void WriteLine(string text, bool prefixDateTime = true)
        {
            try
            {
                using var streamWriter = File.AppendText(GetFileLoggerPath());
                if (prefixDateTime)
                    streamWriter.WriteLine($"[{DateTime.Now.ToString()}]: {text}");
                else
                    streamWriter.WriteLine(text);
            }
            catch
            {

            }
        }
    }
}

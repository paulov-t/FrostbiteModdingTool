using FMT.Logging;
using SdkGenerator;

namespace SDKGeneratorConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Spinning up Sdk Generator...");

            var b = new Builder().BuildSdk();
            b.Wait();

        }

        public class Builder : ILogger
        {
            public async Task BuildSdk()
            {
                var buildSDK = new BuildSDK(this);
                await buildSDK.Build();
            }

            public void Log(string text, params object[] vars)
            {
                Console.WriteLine(text);
            }

            public void LogWarning(string text, params object[] vars)
            {
                Console.WriteLine(text);
            }

            public void LogError(string text, params object[] vars)
            {
                Console.WriteLine(text);
            }

            public void LogProgress(int progress)
            {
            }
        }
        
    }
}
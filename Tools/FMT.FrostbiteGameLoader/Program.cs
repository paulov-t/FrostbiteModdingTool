using System.Diagnostics;

if (File.Exists("FMT.FrostbiteGameLoader.Log.txt"))
    File.Delete("FMT.FrostbiteGameLoader.Log.txt");

using var sw = File.AppendText("FMT.FrostbiteGameLoader.Log.txt");

try
{

    var processDirPath = AppContext.BaseDirectory;
    //Console.WriteLine($"{processDirPath}");
    sw.WriteLine(processDirPath);

    var processDirectoryInfo = new DirectoryInfo(processDirPath);
    var isInFIFASetup = processDirectoryInfo.FullName.Contains("FIFASetup", StringComparison.OrdinalIgnoreCase);
    sw.WriteLine("isInFIFASetup?:" + isInFIFASetup);


    var gameExeDirPath = !isInFIFASetup ? processDirPath : processDirectoryInfo.Parent?.FullName;
    if (gameExeDirPath == null)
        return;

    sw.WriteLine("gameExeDirPath:" + gameExeDirPath);

    //Console.WriteLine($"{gameExeDirPath}");
    var gameExePath = gameExeDirPath;
    foreach (var f in Directory.GetFiles(gameExeDirPath))
    {
        //Console.WriteLine($"{f}");
        if (f.EndsWith("Trial.exe"))
        {
            gameExePath = f.Replace("Trial.exe", "").Replace("_", "");
            gameExePath += ".exe";


            break;
        }
    }

    sw.WriteLine("gameExePath:" + gameExePath);

    var processName = args.Length > 0 && args.Any(x => x.Contains(".exe")) ? args.First(x=>x.Contains(".exe")) : gameExePath;

    sw.WriteLine("processName:" + processName);

    var programArgs = "";
    if (args.Length > 0)
        programArgs = string.Join(" ", args.Where(x => !x.Contains(".exe")).ToArray());

    Console.WriteLine($"Launching {processName} {programArgs}");
    sw.WriteLine($"Launching {processName} {programArgs}");

    if (Process.GetProcessesByName(processName).Length > 0)
        return;

    using (Process process = new())
    {
        FileInfo fileInfo = new FileInfo(processName);
        process.StartInfo.FileName = processName;
        process.StartInfo.WorkingDirectory = fileInfo.DirectoryName;
        process.StartInfo.Arguments = "\"" + processName + "\"" + " " + programArgs;
        process.StartInfo.UseShellExecute = false;
        if (!process.Start())
        {
            throw new Exception($"Unable to start {processName}");
        }
    }
}
catch (Exception ex)
{
    sw.WriteLine(ex);
}
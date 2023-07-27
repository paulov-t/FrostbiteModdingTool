﻿// See https://aka.ms/new-console-template for more information
using System.Diagnostics;

var processDirPath = AppContext.BaseDirectory;
//Console.WriteLine($"{processDirPath}");

var processDirectoryInfo = new DirectoryInfo(processDirPath);
var isInFIFASetup = processDirectoryInfo.FullName.Contains("FIFASetup", StringComparison.OrdinalIgnoreCase);

var gameExeDirPath = !isInFIFASetup ? processDirPath : processDirectoryInfo.Parent?.FullName;
if (gameExeDirPath == null)
    return;

//Console.WriteLine($"{gameExeDirPath}");
var gameExePath = gameExeDirPath;
foreach(var f in Directory.GetFiles(gameExeDirPath))
{
    //Console.WriteLine($"{f}");
    if(f.EndsWith("Trial.exe"))
    {
        gameExePath = f.Replace("Trial.exe", "").Replace("_", "");
        gameExePath += ".exe";
        break;
    }
}

var processName = args.Length > 0 ? args[0] : gameExePath;
var programArgs = "";
if (args.Length > 0)
    programArgs = string.Join(" ", args.Skip(1).ToArray());

Console.WriteLine($"Launching {processName} {programArgs}");

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
#pragma warning disable CS0618 // Type or member is obsolete
using FMT.Logging;
using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace v2k4FIFAModdingCL
{
    /// <summary>
    /// The Game Instance Singleton is the Initializer for the Game
    /// </summary>
    public class GameInstanceSingleton : IDisposable
    {
        #region Properties

        public static GameInstanceSingleton Instance { get; set; }

        public bool INITIALIZED = false;

        private string gameVersion;

        public string GAMEVERSION { set { gameVersion = value; INITIALIZED = !string.IsNullOrEmpty(value); } get { return gameVersion; } }
        public string FIFAVERSION_NODEMO { get { return !string.IsNullOrEmpty(GAMEVERSION) ? GAMEVERSION.Replace("_demo", "") : null; } }

        public string GAMERootPath = "";

        public string GameDataPath { get { return GAMERootPath + "\\Data\\"; } }

        public string FIFALocaleINIPath { get { return GAMERootPath + "\\Data\\locale.ini"; } }
        public string FIFALocaleINIModDataPath { get { return ModDataPath + "\\Data\\locale.ini"; } }
        public string GamePatchPath { get { return GAMERootPath + "\\Patch\\"; } }
        public string FIFA_INITFS_Win32 { get { return GamePatchPath + "\\initfs_Win32"; } }

        public string LegacyModsPath { get { return GAMERootPath + "\\LegacyMods\\Legacy\\"; } }

        public string GameEXE { get { return GAMERootPath + "\\" + GAMEVERSION + ".exe"; } }

        public string ModDataPath { get { return GAMERootPath + "\\ModData\\"; } }

        #endregion

        /// <summary>
        /// Load the Game Profile and game into File System, optionally load Cache file to fully know where all files are
        /// </summary>
        /// <param name="filePath">The Game Path to Load</param>
        /// <param name="loadCache"></param>
        /// <param name="logger"></param>
        /// <param name="loadSDK"></param>
        /// <returns></returns>
        public static bool InitializeSingleton(in string filePath, in bool loadCache = false, in ILogger logger = null, in bool loadSDK = true)
        {
            if (Instance != null)
            {
                Instance.Dispose();
                Instance = null;
            }
            Logger = logger;

            if (!string.IsNullOrEmpty(filePath))
            {
                Instance = new GameInstanceSingleton();
                var GameDirectory = Directory.GetParent(filePath).FullName;//.Substring(0, filePath.LastIndexOf("\\") + 1);
                Instance.GAMERootPath = GameDirectory;
                var fileName = filePath.Substring(filePath.LastIndexOf("\\") + 1, filePath.Length - filePath.LastIndexOf("\\") - 1);
                if (!string.IsNullOrEmpty(fileName))// && GameInstanceSingleton.CompatibleGameVersions.Contains(fileName))
                {
                    Instance.GAMEVERSION = fileName.Replace(".exe", "").Replace(" ", "");
                    Instance.INITIALIZED = true;
                }
                // Initialize the Profile Manager & File System
                if (ProfileManager.ProfileName == null && !string.IsNullOrEmpty(Instance.GAMEVERSION))
                {
                    ProfileManager.Initialize(Instance.GAMEVERSION);
                    if (FileSystem.Instance == null)
                    {
                        new FileSystem(GameDirectory);
                    }
                }
                if (loadCache)
                {
                    CacheManager buildCache = new CacheManager();
                    return buildCache.LoadData(Instance.GAMEVERSION, Instance.GAMERootPath, Logger, false, loadSDK);
                }
            }

            return Instance.INITIALIZED;
        }

        /// <summary>
        /// Initialize Asyncronously InitializeSingleton method
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="loadCache"></param>
        /// <param name="logger"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<bool> InitializeSingletonAsync(
            string filePath
            , bool loadCache = false
            , ILogger logger = null
            , CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Task.Run(() => { return InitializeSingleton(filePath, loadCache, logger); }, cancellationToken);
        }

        /// <summary>
        /// Gets the Game Version (Game Name really)
        /// </summary>
        /// <returns></returns>
        public static string GetGameVersion()
        {
            return Instance != null ? Instance.GAMEVERSION : string.Empty;
        }

        /// <summary>
        /// Gets the true or false for support for fbmod file type for this Game Profile
        /// </summary>
        /// <returns></returns>
        public static bool IsCompatibleWithFbMod()
        {
            return ProfileManager.SupportedLauncherFileTypes.Contains("fbmod");
        }


        /// <summary>
        /// Gets the true or false for support for lmod file type for this Game Profile
        /// </summary>
        /// <returns></returns>
        public static bool IsCompatibleWithLegacyMod()
        {
            return ProfileManager.SupportedLauncherFileTypes.Contains("lmod");
        }

        public static ILogger Logger;

        public static bool LegacyInjectionExtraAssertions = true;

        /// <summary>
        /// Finds the Game Profile EXE running in Processes
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static async Task<int?> GetProcIDFromName(string name)
        {
            if (name.Contains(".exe"))
                name = name.Replace(".exe", "");

            int? id = null;
            int attempts = 0;
            while (!id.HasValue && attempts < 300)
            {
                Process[] processlist = Process.GetProcesses();
                if (name.Contains("FIFA", StringComparison.OrdinalIgnoreCase))
                    name = "FIFA";

                await Task.Delay(1000);
                foreach (Process theprocess in processlist)
                {
                    //find (name).exe in the process list (use task manager to find the name)
                    if (theprocess.ProcessName.Contains(name, StringComparison.OrdinalIgnoreCase))
                    {
                        id = theprocess.Id;
                        break;
                    }
                }

                attempts++;

                if (attempts == 120)
                {
                    Logger.LogError("Still waiting for " + name + " to start...");
                }
                else if (attempts == 180)
                {
                    Logger.LogError("Still waiting for " + name + " to start...");
                }
                else if (attempts == 240)
                {
                    Logger.LogError("Still waiting for " + name + " to start...");
                }
            }

            if (id.HasValue)
                return id;


            throw new ArgumentException($"Unable to find Process {name} running on your PC");
        }

        public static async Task<int> InjectDLL_GetProcess(bool waitForModules = true)
        {
            int attempts = 0;

            bool ModuleLoaded = !LegacyInjectionExtraAssertions;


            int? proc = await GetProcIDFromName(Instance.GAMEVERSION);
            while ((!proc.HasValue || proc == 0 || !ModuleLoaded) && attempts < 300)
            {
                Debug.WriteLine($"Waiting for {Instance.GAMEVERSION} to appear");
                await Task.Delay(100);
                proc = await GetProcIDFromName(Instance.GAMEVERSION);
                if (proc.HasValue)
                {
                    if (!waitForModules)
                    {
                        return proc.Value;
                    }

                    if (LegacyInjectionExtraAssertions)
                    {
                        Process actualProcess = null;
                        try
                        {
                            //var processes = Process.GetProcessesByName(GAMEVERSION);
                            actualProcess = Process.GetProcessById(proc.Value);

                            foreach (ProcessModule m in actualProcess.Modules)
                            {
                                if (m.FileName.Contains("powdll_Win64_retail", StringComparison.OrdinalIgnoreCase))
                                {
                                    ModuleLoaded = true;
                                }
                                if (m.FileName.Contains("sysdll_Win64_retail", StringComparison.OrdinalIgnoreCase))
                                {
                                    ModuleLoaded = true;
                                }
                            }
                        }
                        catch
                        {
                            //throw new Exception("Injector: Unable to access process");
                        }
                    }

                    if (ModuleLoaded)
                        return proc.Value;
                }
                attempts++;
            }

            throw new Exception("Injector has failed to inject");

        }

        //public static bool InjectDLL_Bleak(int proc, string dllpath)
        //{
        //    using (var bl = new Bleak.Injector(proc, File.ReadAllBytes(dllpath), Bleak.InjectionMethod.CreateThread))
        //    {
        //        var ptr = bl.InjectDll();
        //        return true; // Bleak Injected
        //    }
        //}

        //public static bool InjectDLL_Lunar(int proc, string dllpath)
        //{
        //    try
        //    {
        //        LibraryMapper libraryMapper = new LibraryMapper(Process.GetProcessById(proc), dllpath);
        //        libraryMapper.MapLibrary();
        //        return true;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        public static bool InjectDLL_Own(int proc, string dllpath)
        {
            return InjectDLLIntoProcessFromPath(dllpath, proc);
        }

        public static async Task<bool> InjectDLL(string dllpath, bool waitForModules = true)
        {
            dllpath = dllpath.Replace(@"\\\\", @"\");
            dllpath = dllpath.Replace(@"\\", @"\");
            //dllpath = dllpath.Replace(@"\", @"/");
            if (File.Exists(dllpath))
            {
                int proc = await InjectDLL_GetProcess(waitForModules);
                //if (!LegacyInjectionExtraAssertions)
                {
                    // Waiting for process to fully awake
                    //Thread.Sleep(2750);
                    await Task.Delay(350);
                }



                var dll_File = dllpath.Split('\\')[dllpath.Split('\\').Length - 1];
                dll_File = dll_File.Replace(".dll", "");
                try
                {
                    // Use Bleak first
                    bool injected = InjectDLL_Own(proc, dllpath);
                    //bool injected = InjectDLL_Bleak(proc, dllpath);
                    //if (!injected)
                    //{
                    //    // Own second
                    //    //injected = InjectDLL_Bleak(proc, dllpath);
                    //    injected = InjectDLL_Own(proc, dllpath);
                    //    //if (!injected)
                    //    //{
                    //    //    // Last resort Lunar
                    //    //    injected = InjectDLL_Lunar(proc, dllpath);
                    //    //}
                    //}

                    return injected;
                }
                catch (Exception ex)
                {
                    if (Logger != null)
                    {
                        Logger.LogError($"DLL Injector for {dll_File} Failed with Exception: {ex.ToString()}");
                    }
                    else
                    {
                        Debug.WriteLine($"DLL Injector Failed with Exception: {ex.ToString()}");
                    }
                }
            }
            else
            {
                Debug.WriteLine("[ERROR] InjectDLL :: " + dllpath + " not found");
            }

            return false;
        }

        public static async Task<bool> InjectDLLAsync(string dllpath, bool waitForModules = true)
        {
            return await InjectDLL(dllpath, waitForModules);
        }

        public static bool InjectDLLSync(string dllpath)
        {
            return InjectDLL(dllpath).Result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="DllPath"></param>
        /// <param name="ProcId"></param>
        /// <returns></returns>
        private static bool InjectDLLIntoProcessFromPath(string DllPath, int ProcId)
        {
            if (!File.Exists(DllPath))
            {
                throw new FileNotFoundException($"{DllPath} was not found!");
            }

            Debug.WriteLine($"[+] Injecting {DllPath} into {ProcId}");

            // Open handle to the target process
            IntPtr ProcHandle = OpenProcess(
                ProcessAccessFlags.All,
                false,
                ProcId);
            if (ProcHandle == -1)
            {
                Logger.LogError("[!] Handle to target process could not be obtained!");
                return false;
            }
            else
            {
                Debug.WriteLine("[+] Handle (0x" + ProcHandle + ") to target process has been be obtained.");
            }

            IntPtr Size = DllPath.Length;

            // Allocate DLL space
            IntPtr DllSpace = VirtualAllocEx(
                ProcHandle,
                IntPtr.Zero,
                Size,
                AllocationType.Reserve | AllocationType.Commit,
                MemoryProtection.ExecuteReadWrite);

            if (DllSpace == -1)
            {
                Logger.LogError("[!] DLL space allocation failed.");
                return false;
            }
            else
            {
                Debug.WriteLine("[+] DLL space (0x" + DllSpace + ") allocation is successful.");
            }

            // Write DLL content to VAS of target process
            byte[] bytes = Encoding.ASCII.GetBytes(DllPath);
            bool DllWrite = WriteProcessMemory(
                ProcHandle,
                DllSpace,
                bytes,
                bytes.Length,
                out var bytesread
                );

            if (DllWrite == false || bytes.Length == 0)
            {
                Logger.LogError("[!] Writing DLL content to target process failed.");
                return false;
            }
            else
            {
                Debug.WriteLine("[+] Writing DLL content to target process is successful.");
            }

            // Get handle to Kernel32.dll and get address for LoadLibraryA
            IntPtr Kernel32Handle = GetModuleHandle("Kernel32.dll");
            IntPtr LoadLibraryAAddress = GetProcAddress(Kernel32Handle, "LoadLibraryA");

            if (LoadLibraryAAddress == -1)
            {
                Logger.LogError("[!] Obtaining an addess to LoadLibraryA function has failed.");
                return false;
            }
            else
            {
                Debug.WriteLine("[+] LoadLibraryA function address (0x" + LoadLibraryAAddress + ") has been obtained.");
            }

            // Create remote thread in the target process
            IntPtr RemoteThreadHandle = CreateRemoteThread(
                ProcHandle,
                IntPtr.Zero,
                0,
                LoadLibraryAAddress,
                DllSpace,
                0,
                IntPtr.Zero
                );

            if (RemoteThreadHandle == -1)
            {
                Logger.LogError("[!] Obtaining a handle to remote thread in target process failed.");
                return false;
            }
            else
            {
                Debug.WriteLine("[+] Obtaining a handle to remote thread (0x" + RemoteThreadHandle + ") in target process is successful.");
            }

            return true;

        }


        // OpenProcess signture https://www.pinvoke.net/default.aspx/kernel32.openprocess
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
        ProcessAccessFlags processAccess,
        bool bInheritHandle,
        int processId);
        public static IntPtr OpenProcess(Process proc, ProcessAccessFlags flags)
        {
            return OpenProcess(flags, false, proc.Id);
        }

        // VirtualAllocEx signture https://www.pinvoke.net/default.aspx/kernel32.virtualallocex
        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        // VirtualFreeEx signture  https://www.pinvoke.net/default.aspx/kernel32.virtualfreeex
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress,
        int dwSize, AllocationType dwFreeType);

        [Flags]
        public enum MemoryProtection
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            IntPtr dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect);

        // WriteProcessMemory signture https://www.pinvoke.net/default.aspx/kernel32/WriteProcessMemory.html
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        [MarshalAs(UnmanagedType.AsAny)] object lpBuffer,
        int dwSize,
        out IntPtr lpNumberOfBytesWritten);

        // GetProcAddress signture https://www.pinvoke.net/default.aspx/kernel32.getprocaddress
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        // GetModuleHandle signture http://pinvoke.net/default.aspx/kernel32.GetModuleHandle
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        // CreateRemoteThread signture https://www.pinvoke.net/default.aspx/kernel32.createremotethread
        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(
        IntPtr hProcess,
        IntPtr lpThreadAttributes,
        uint dwStackSize,
        IntPtr lpStartAddress,
        IntPtr lpParameter,
        uint dwCreationFlags,
        IntPtr lpThreadId);

        // CloseHandle signture https://www.pinvoke.net/default.aspx/kernel32.closehandle
        [DllImport("kernel32.dll", SetLastError = true)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        // To detect redundant calls
        private bool _disposed = false;

        ~GameInstanceSingleton() => Dispose(false);

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            // Managed Resources
            if (disposing)
            {
                GAMEVERSION = null;
            }
        }
    }
}

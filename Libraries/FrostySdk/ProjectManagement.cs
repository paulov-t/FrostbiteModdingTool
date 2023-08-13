using FMT.Logging;
using FrostbiteSdk;
using FrostySdk;
using FrostySdk.Frostbite;
using FrostySdk.Interfaces;
using FrostySdk.Managers;
using FrostySdk.ModsAndProjects.Projects;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using v2k4FIFAModdingCL;

namespace v2k4FIFAModding.Frosty
{

    /// <summary>
    /// Project Management is a singleton class. Cannot create 2 instances of this class.
    /// </summary>
    public class ProjectManagement : ILogger
    {
        public static ProjectManagement Instance;

        //public FrostbiteProject Project { get; set; } = new FrostbiteProject();
        public IProject Project { get; set; }

        public ILogger Logger = null;

        string lastMessage = null;


        private static string PreviousGameVersion { get; set; }

        public ProjectManagement(in string gamePath, in ILogger logger = null)
        {
            if (AssetManager.Instance == null)
                throw new NullReferenceException("AssetManager Instance must be instantiated before ProjectManagement can be used!");

            if (Instance == null)
            {
                if(logger != null)
                    Logger = logger;

                Instance = this;
                Project = new FrostbiteProject();
                Project.ModSettings.Title = "FMT Editor Project";
            }
            else
            {
                throw new OverflowException("Cannot create 2 instances of ProjectManagement");
            }
        }

        public async Task<IProject> LoadProjectFromFile(string filePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            var fiFile = new FileInfo(filePath);
            var fileExtension = fiFile.Extension.ToLower();
            switch (fileExtension)
            {

                case ".fbproject":
                    try
                    {
                        Project = new FrostbiteProject();
                        await Project.LoadAsync(filePath, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        LogError("Unable to load project. This may be due to a Title Update. Message: " + ex.Message);
                    }
                    break;
                case ".fmtproj":
                    //var mbFmtProj = MessageBox.Show("This is a NEW/UPCOMMING file format. This does nothing right now!", "EXPERIMENTAL");
                    try
                    {
                        Project = FMTProject.Read(filePath);
                    }
                    catch (Exception ex)
                    {
                        LogError("Unable to load project. This may be due to a Title Update. Message: " + ex.Message);
                    }
                    break;
                case ".fifamod":

                    var mbFIFAMod = MessageBox.Show(
                        "You are opening a compiled FIFAMod from FIFA Editor Tool. " + Environment.NewLine +
                        "This is experimental and may crash the Application. " + Environment.NewLine +
                        "This process may be missing or losing files whilst processing. " + Environment.NewLine +
                        "Please always give credit to other's work!", "EXPERIMENTAL");
                    if (mbFIFAMod == MessageBoxResult.OK)
                    {
                        using (FIFAModReader reader = new FIFAModReader(new FileStream(fiFile.FullName, FileMode.Open)))
                        {
                            Project = new FrostbiteProject();
                            Project.Load(reader);
                        }
                    }
                    break;
                case ".fbmod":

                    var mbFBMod = MessageBox.Show(
                        "You are opening a compiled FBMod from Frostbite Modding Tool. " + Environment.NewLine +
                        "This is experimental and may crash the Application. " + Environment.NewLine +
                        "This process may be missing or losing files whilst processing. " + Environment.NewLine +
                        "Please always give credit to other's work!", "EXPERIMENTAL");
                    if (mbFBMod == MessageBoxResult.OK)
                    {
                        FrostbiteMod frostbiteMod = new FrostbiteMod(fiFile.FullName);
                        Project = new FMTProject("loadInFbMod");
                        if (Project.Load(frostbiteMod))
                        {
                            Log($"Successfully opened {fiFile.FullName}");
                        }
                        else
                        {
                            Log($"Failed to open {fiFile.FullName}");
                        }
                    }
                    break;
            }
            return Project;
        }

        public void Log(string text, params object[] vars)
        {
            if (text != lastMessage)
            {
                Debug.WriteLine(text);

                Console.WriteLine(text);
                lastMessage = text;

                if (Logger != null)
                {
                    Logger.Log(text, vars);
                }
            }
        }


        public void LogError(string text, params object[] vars)
        {
            if (text != lastMessage)
            {
                Debug.WriteLine(text);

                Console.WriteLine(text);
                lastMessage = text;

                if (Logger != null)
                {
                    Logger.LogWarning(text, vars);
                }
            }
        }

        public void LogWarning(string text, params object[] vars)
        {
            if (text != lastMessage)
            {
                Debug.WriteLine(text);

                Console.WriteLine(text);
                lastMessage = text;

                if (Logger != null)
                {
                    Logger.LogError(text, vars);
                }
            }
        }

        /// <summary>
        /// Starts the process of Loading Cache etc and Creates a New Project
        /// </summary>
        /// <returns></returns>
        public async Task<IProject> StartNewProjectAsync()
        {
            return await new TaskFactory().StartNew(() =>
            {
                Project = new FrostbiteProject(AssetManager.Instance, AssetManager.Instance.FileSystem);
                return Project;
            });
        }

        public IProject StartNewProject()
        {
            if (AssetManager.Instance == null)
            {
                CacheManager buildCache = new CacheManager();
                buildCache.LoadData(GameInstanceSingleton.Instance.GAMEVERSION, GameInstanceSingleton.Instance.GAMERootPath, Logger, false, true);
            }

            Project = new FrostbiteProject(AssetManager.Instance, AssetManager.Instance.FileSystem);
            Project.ModSettings.Title = "Test Project";
            return Project;
        }

        public void LogProgress(int progress)
        {
            throw new NotImplementedException();
        }
    }
}

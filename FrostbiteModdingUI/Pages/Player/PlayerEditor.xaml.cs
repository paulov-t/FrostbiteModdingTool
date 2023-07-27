using FifaLibrary;
using Frostbite.Textures;
using FrostbiteModdingUI.Models;
using FrostySdk;
using FrostySdk.Frostbite.IO.Output;
using FrostySdk.Managers;
using FrostySdk.Resources;
using HelixToolkit.SharpDX.Core.Assimp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FrostbiteModdingUI.Pages.Player
{
    /// <summary>
    /// Interaction logic for PlayerEditor.xaml
    /// </summary>
    public partial class PlayerEditor : UserControl
    {
        public class PlayerVM
        {
            public int PlayerId { get; set; }

            public string Firstname { get; set; }
            public string Lastname { get; set; }
            public string Nickname { get; set; }
            public string PlayerName
            {
                get
                {
                    if (!string.IsNullOrEmpty(Nickname))
                        return Nickname;
                    else
                        return Firstname + " " + Lastname;
                }
            }
            public DataRow Data { get; set; }
        }

        public List<PlayerVM> PlayerList = new List<PlayerVM>(27000);
        public ObservableCollection<PlayerVM> PlayerListFiltered
        {
            get
            {
                return new ObservableCollection<PlayerVM>(PlayerList.Take(50));
            }
        }

        public PlayerVM Player { get; set; }

        public MainViewModel ViewportViewModel { get; } = new MainViewModel();

        public PlayerEditor()
        {
            InitializeComponent();

            Loaded += PlayerEditor_Loaded;
            Viewport.DataContext = ViewportViewModel;
            this.DataContext = this;
        }

        private void PlayerEditor_Loaded(object sender, RoutedEventArgs e)
        {
            PlayerList = new List<PlayerVM>()
            {
                new PlayerVM() { PlayerId = -1, Nickname = "Loading..." },
            };



            this.txtPlayerSearch.KeyUp += TxtPlayerSearch_KeyUp;

            // SetupHeadScene();

        }

        public async void InitPlayerSearch()
        {
            await Task.Run(() =>
            {
                LoadSquadFile();

                var legacyFiles = AssetManager.Instance.EnumerateCustomAssets("legacy").OrderBy(x => x.Path).ToList();
                var mainDb = legacyFiles.FirstOrDefault(x => x.Filename.Contains("fifa_ng_db") && x.Type == "DB");
                var mainDbMeta = legacyFiles.FirstOrDefault(x => x.Filename.Contains("fifa_ng_db-meta") && x.Type == "XML");
                LoadPlayerList(mainDb as LegacyFileEntry, mainDbMeta as LegacyFileEntry);
            });

            // Binding  
            Dispatcher.Invoke(() =>
            {
                lstPlayerSearch.ItemsSource = PlayerListFiltered;
            });
        }

        CareerFile squadFile;
        DataTable dtPlayers;
        CareerFile locFile;
        DataTable dtPlayerNames;


        public bool LoadSquadFile()
        {
            //List<string> files = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\FIFA 21\\settings\\").ToList();
            //if (files != null && files.Any())
            //{
            //    var squadFilePath = files.Where(x => x.Contains("\\Squads")).OrderByDescending(x => new FileInfo(x).LastWriteTime).FirstOrDefault();
            //    using (var fsSquadFile = new FileStream(squadFilePath, FileMode.Open))
            //    {
            //        using (var fsMeta = new FileStream("Resources/FIFA21DBMeta.xml", FileMode.Open))
            //        {
            //            squadFile = new CareerFile(fsSquadFile, fsMeta);

            //            dtPlayers = squadFile.Databases[0].GetTable("players").ConvertToDataTable();
            //            return true;
            //        }
            //    }
            //}
            return false;
        }

        public void LoadPlayerList(LegacyFileEntry locDB, LegacyFileEntry locMeta)
        {
            if (squadFile == null)
            {
                var resultOfSquadload = LoadSquadFile();
                if (!resultOfSquadload)
                {
                    throw new Exception("Unable to Load Squad File");
                }
            }
            if (squadFile != null && dtPlayerNames == null)
            {
                var dbAsset1 = AssetManager.Instance.GetCustomAsset("legacy", locDB);
                var dbAsset2 = AssetManager.Instance.GetCustomAsset("legacy", locDB);
                var dbMetaAsset = AssetManager.Instance.GetCustomAsset("legacy", locMeta);

                DbFile dbFile = new DbFile(dbAsset2, dbMetaAsset);

                dtPlayerNames = dbFile.GetTable("playernames").ConvertToDataTable();
                PlayerList.Clear();
                foreach (DataRow pRow in dtPlayers.Rows)
                {
                    PlayerVM player = new PlayerVM();
                    player.PlayerId = int.Parse(pRow["playerid"].ToString());
                    var fnid = int.Parse(pRow["firstnameid"].ToString());
                    var lnid = int.Parse(pRow["lastnameid"].ToString());

                    int? cnid = null;
                    if (pRow["commonnameid"] != null)
                    {
                        cnid = int.Parse(pRow["commonnameid"].ToString());
                    }

                    player.Data = pRow;
                    player.Firstname = "Unknown Firstname";
                    player.Lastname = "Unknown Lastname";
                    if (fnid < dtPlayerNames.Rows.Count)
                    {
                        player.Firstname = dtPlayerNames.Rows[fnid]["name"].ToString();
                        if (lnid < dtPlayerNames.Rows.Count)
                            player.Lastname = dtPlayerNames.Rows[lnid]["name"].ToString();
                    }

                    if (cnid.HasValue)
                    {
                        if (cnid < dtPlayerNames.Rows.Count)
                        {
                            player.Nickname = dtPlayerNames.Rows[cnid.Value]["name"].ToString();
                        }
                    }

                    PlayerList.Add(player);
                }

            }


        }

        private void TxtPlayerSearch_KeyUp(object sender, KeyEventArgs e)
        {
            SearchForPlayer(txtPlayerSearch.Text);
        }

        private void CbPlayerSelection_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            SearchForPlayer(e.Text);
            txtPlayerSearch.Text = e.Text;
        }

        private void SearchForPlayer(string searchText)
        {
            var plCopy = PlayerList.ToList().Where(x => x.PlayerName.ToLower().Contains(searchText.ToLower())).Take(200);
            lstPlayerSearch.ItemsSource = plCopy;
        }

        private void lstPlayerSearch_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView listView = sender as ListView;
            var p = listView.SelectedItem as PlayerVM;

            if (p != null)
            {
                ExportAssetToRenderer(p.PlayerId);
                Player = p;
                this.DataContext = this;
            }
        }

        public void ExportHead()
        {

        }

        public void SetupHeadScene()
        {
            if (!File.Exists("test_noSkel.obj"))
                return;




            var import = new Importer();
            var scene = import.Load("test_noSkel.obj", new ImporterConfiguration() { GlobalScale = 100, FlipWindingOrder = true, CullMode = SharpDX.Direct3D11.CullMode.None, });
            //ViewportViewModel.BunnyModel = scene;

            //ViewportViewModel.GroupModel.RemoveNode(scene.Root);
            //ViewportViewModel.GroupModel.AddNode(scene.Root);

        }




        private void ExportAssetToRenderer(int id)
        {
            if (!Directory.Exists("Renderer"))
                Directory.CreateDirectory("Renderer");

            try
            {
                var m = new PlayerViewportViewModel();

                var texture_entries = AssetManager.Instance.EnumerateEbx("TextureAsset")
                    .Where(x =>
                    (x.Name.Contains("head_" + id.ToString() + "_")
                    || x.Name.Contains("hair_" + id.ToString() + "_")
                    || x.Name.Contains("haircap_" + id.ToString() + "_")
                    || x.Name.Contains("face_" + id.ToString() + "_")
                    )
                    && x.Name.Contains("player")
                    ).ToList();
                foreach (var ebxEntry in texture_entries)
                {
                    var ebx = AssetManager.Instance.GetEbx(ebxEntry);
                    if (ebx != null)
                    {
                        var textureEbx = AssetManager.Instance.GetEbx(ebxEntry);
                        if (textureEbx != null)
                        {
                            var resEntry = AssetManager.Instance.GetResEntry(ebxEntry.Name);
                            var res = AssetManager.Instance.GetRes(resEntry);
                            Texture t = new Texture(res, resEntry);
                            TextureExporter textureExporter = new TextureExporter();

                            Task.Run(() =>
                            {
                                if (ebxEntry.Name.Contains("hair_") && ebxEntry.Name.Contains("_color"))
                                    textureExporter.Export(t, "Renderer/HairTexture.png", "*.png");
                                else if (ebxEntry.Name.Contains("face_") && ebxEntry.Name.Contains("_color"))
                                    textureExporter.Export(t, "Renderer/FaceTexture.png", "*.png");
                                else if (ebxEntry.Name.Contains("haircap_") && ebxEntry.Name.Contains("_color"))
                                    textureExporter.Export(t, "Renderer/HairCapTexture.png", "*.png");
                            }).Wait();

                        }
                    }
                }


                var character_entries = AssetManager.Instance.EnumerateEbx("SkinnedMeshAsset")
                    .Where(x =>
                    (x.Name.Contains("head_" + id.ToString() + "_")
                    || x.Name.Contains("hair_" + id.ToString() + "_")
                    || x.Name.Contains("haircap_" + id.ToString() + "_")
                    )
                    && x.Name.EndsWith("_mesh")
                    ).ToList();
                foreach (var ebxEntry in character_entries)
                {
                    if (ebxEntry.Type == "SkinnedMeshAsset")
                    {
                        if (ebxEntry == null || ebxEntry.Type == "EncryptedAsset")
                        {
                            return;
                        }
                        var ebx = AssetManager.Instance.GetEbx(ebxEntry);
                        if (ebx != null)
                        {
                            var skinnedMeshEbx = AssetManager.Instance.GetEbx(ebxEntry);
                            if (skinnedMeshEbx != null)
                            {
                                var resentry = AssetManager.Instance.GetResEntry(ebxEntry.Name);
                                var res = AssetManager.Instance.GetRes(resentry);
                                MeshSet meshSet = new MeshSet(res);

                                Task.Run(() =>
                                {
                                    var exporter = new MeshSetToFbxExport();
                                    //exporter.OnlyFirstLOD = true;
                                    if (ebxEntry.Name.Contains("head_"))
                                        exporter.Export(AssetManager.Instance, skinnedMeshEbx.RootObject, "Renderer/FaceModel.obj", "2016", "Millimeters", true, null, "*.obj", meshSet);
                                    else if (ebxEntry.Name.Contains("hair_"))
                                        exporter.Export(AssetManager.Instance, skinnedMeshEbx.RootObject, "Renderer/HairModel.obj", "2016", "Millimeters", true, null, "*.obj", meshSet);
                                    else if (ebxEntry.Name.Contains("haircap_"))
                                        exporter.Export(AssetManager.Instance, skinnedMeshEbx.RootObject, "Renderer/HairCapModel.obj", "2016", "Millimeters", true, null, "*.obj", meshSet);
                                }).Wait();


                            }


                        }
                    }
                }

                this.Viewport.DataContext = m;



            }
            catch
            {

            }
        }


    }
}

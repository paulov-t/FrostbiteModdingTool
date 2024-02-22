using AvalonDock.Layout;
using CSharpImageLibrary;
using FIFAModdingUI.Pages.Common;
using FMT.FileTools;
using FMT.FileTools.AssetEntry;
using FMT.Logging;
using FMT.SharedWindowFunctions;
using FMT.Sound;
using Frostbite.Textures;
using FrostbiteModdingUI.Models;
using FrostbiteModdingUI.Windows;
using FrostySdk;
using FrostySdk.Frostbite.IO;
using FrostySdk.Frostbite.IO.Input;
using FrostySdk.Frostbite.IO.Output;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using v2k4FIFAModding.Frosty;

namespace FMT.Pages.Common
{
    /// <summary>
    /// Interaction logic for OpenedFile.xaml
    /// </summary>
    public partial class OpenedFile : UserControl
    {

        MainViewModel ModelViewerModel;
        public HelixToolkit.Wpf.SharpDX.Camera Camera { get; set; }

        #region Entry Properties

        //private AssetEntry assetEntry1;

        //public AssetEntry SelectedEntry
        //{
        //    get
        //    {
        //        if (assetEntry1 == null && SelectedLegacyEntry != null)
        //            return SelectedLegacyEntry;

        //        return assetEntry1;
        //    }
        //    set { assetEntry1 = value; }
        //}

        //public LegacyFileEntry SelectedLegacyEntry { get; set; }

        public static readonly DependencyProperty SelectedEntryProperty = DependencyProperty.Register("SelectedEntry", typeof(IAssetEntry), typeof(OpenedFile), new FrameworkPropertyMetadata(null));
        public IAssetEntry SelectedEntry
        {
            get => (IAssetEntry)GetValue(SelectedEntryProperty);
            set => SetValue(SelectedEntryProperty, value);
        }

        //private EbxAsset ebxAsset;

        //public EbxAsset SelectedEbxAsset
        //{
        //    get { return ebxAsset; }
        //    set { ebxAsset = value; }
        //}

        public static readonly DependencyProperty SelectedEbxAssetProperty = DependencyProperty.Register("SelectedEbxAsset", typeof(EbxAsset), typeof(OpenedFile), new FrameworkPropertyMetadata(null));
        public EbxAsset SelectedEbxAsset
        {
            get => (EbxAsset)GetValue(SelectedEbxAssetProperty);
            set => SetValue(SelectedEbxAssetProperty, value);
        }


        public static readonly DependencyProperty LoadingVisibilityProperty = DependencyProperty.Register("LoadingVisibility", typeof(Visibility), typeof(OpenedFile), new FrameworkPropertyMetadata(null));
        public Visibility LoadingVisibility
        {
            get => (Visibility)GetValue(LoadingVisibilityProperty);
            set => SetValue(LoadingVisibilityProperty, value);
        }

        public static readonly DependencyProperty AssetPathProperty = DependencyProperty.Register("AssetPath", typeof(AssetPath), typeof(Editor), new FrameworkPropertyMetadata(null));
        public AssetPath AssetPath
        {
            get => (AssetPath)GetValue(AssetPathProperty);
            set => SetValue(AssetPathProperty, value);
        }


        #endregion

        private IEditorWindow MainEditorWindow
        {
            get
            {
                return (IEditorWindow)App.MainEditorWindow;
            }
        }

        public OpenedFile(IAssetEntry entry, AssetPath assetPath)
        {
            InitializeComponent();

            SelectedEntry = entry;
            AssetPath = assetPath;

            Loaded += OpenedFile_Loaded;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            TracksList = new ObservableCollection<SoundDataTrack>();
            audioPlayer = new AudioPlayer();
            DataContext = null;
            DataContext = this;
        }

        private async void OpenedFile_Loaded(object sender, RoutedEventArgs e)
        {
            await UpdateLoadingVisibility(true);
            await Task.Delay(200);
            await Dispatcher.InvokeAsync(async () =>
            {
                await OpenAsset(SelectedEntry);
            });
            await Task.Delay(200);
            await UpdateLoadingVisibility(false);
        }

        public async Task UpdateLoadingVisibility(bool show)
        {
            LoadingVisibility = show ? Visibility.Visible : Visibility.Hidden;
            await Task.CompletedTask;
            //await Dispatcher.InvokeAsync(() =>
            //{
            //    this.borderLoading.Visibility = show ? Visibility.Visible : Visibility.Hidden;
            //});
        }

        private async Task OpenAsset(IAssetEntry entry)
        {
            if (entry is AssetEntryStub stub)
            {
                switch (stub.StubType)
                {
                    case AssetEntryStub.EntryStubType.Frostbite_LTU:
                        entry = FileSystem.Instance.LiveTuningUpdate.LiveTuningUpdateEntries[entry.Name];
                        break;
                    case AssetEntryStub.EntryStubType.Frostbite_ChunkFile:
                        entry = AssetManager.Instance.GetLegacyAssetManager().GetAssetEntry(entry.Name);
                        break;
                    case AssetEntryStub.EntryStubType.Frostbite_Ebx:
                    default:
                        entry = AssetManager.Instance.GetEbxEntry(entry.Name);
                        break;
                }
            }

            if (entry is EbxAssetEntry ebxEntry)
            {
                await OpenEbxAsset(ebxEntry);
                return;
            }

            if (entry is LiveTuningUpdate.LiveTuningUpdateEntry ltuEntry)
            {
                OpenLTUAsset(ltuEntry);
                return;
            }

            try
            {



                LegacyFileEntry chunkFileEntry = entry as LegacyFileEntry;
                if (chunkFileEntry != null)
                {
                    btnImport.IsEnabled = true;
                    if(layoutEbxViewer.Content != null)
                        layoutEbxViewer.Close();

                    if(layoutMeshViewer.Content != null)
                        layoutMeshViewer.Close();

                    List<string> textViewers = new List<string>()
                        {
                            "LUA",
                            "XML",
                            "INI",
                            "NAV",
                            "JSON",
                            "TXT",
                            "CSV",
                            "TG", // some custom XML / JS / LUA file that is used in FIFA
							"JLT", // some custom XML / LUA file that is used in FIFA
							"PLS", // some custom XML / LUA file that is used in FIFA
                            "JS", // JavaScript file
                            "HTML", // HTML file
                            "LESS" // LESS file
						};

                    List<string> imageViewers = new List<string>()
                        {
                            "PNG",
                            "DDS"
                        };

                    List<string> bigViewers = new List<string>()
                        {
                            "BIG",
                            "AST"
                        };

                    var legacyAssetStream = (MemoryStream)ProjectManagement.Instance.Project.AssetManager.GetCustomAsset("legacy", chunkFileEntry);
                    //DisplayUnknownFileViewer(legacyAssetStream);

                    if (textViewers.Contains(chunkFileEntry.Type))
                    {
                        MainEditorWindow.Log("Loading Legacy File " + SelectedEntry.Filename);

                        btnImport.IsEnabled = true;
                        btnExport.IsEnabled = true;
                        btnRevert.IsEnabled = true;

                        TextViewer.Visibility = Visibility.Visible;
                        using (var nr = new NativeReader(legacyAssetStream))
                        {
                            TextViewer.Text = Encoding.UTF8.GetString(nr.ReadToEnd());
                        }

                        if (layoutImageViewer.Content != null)
                            layoutImageViewer.Close();

                        if (layoutSoundViewer.Content != null)
                            layoutSoundViewer.Close();

                        layoutTextViewer.IsSelected = true;
                    }
                    else if (imageViewers.Contains(chunkFileEntry.Type))
                    {
                        MainEditorWindow.Log("Loading Legacy File " + SelectedEntry.Filename);
                        btnImport.IsEnabled = true;
                        btnExport.IsEnabled = true;

                        if (layoutTextViewer.Content != null)
                            layoutTextViewer.Close();

                        if (layoutUnknownDocument.Content != null)
                            layoutUnknownDocument.Close();

                        if (layoutSoundViewer.Content != null)
                            layoutSoundViewer.Close();

                        layoutImageViewer.IsSelected = true;

                        BuildTextureViewerFromStream(legacyAssetStream);


                    }
                    else if (bigViewers.Contains(chunkFileEntry.Type))
                    {
                        BIGViewer.Visibility = Visibility.Visible;
                        BIGViewer.AssetEntry = chunkFileEntry;
                        //BIGViewer.ParentBrowser = this;
                        switch (chunkFileEntry.Type)
                        {
                            //case "BIG":
                            //	BIGViewer.LoadBig();
                            //	break;
                            default:
                                BIGViewer.LoadBig();
                                break;

                        }

                        btnImport.IsEnabled = true;
                        btnExport.IsEnabled = true;
                        btnRevert.IsEnabled = true;
                    }
                    else
                    {
                        MainEditorWindow.Log("Loading Unknown Legacy File " + SelectedEntry.Filename);
                        btnExport.IsEnabled = true;
                        btnImport.IsEnabled = true;
                        btnRevert.IsEnabled = true;

                        //unknownFileDocumentsPane.Children.Clear();
                        //var newLayoutDoc = new LayoutDocument();
                        //newLayoutDoc.Title = SelectedEntry.DisplayName;
                        //WpfHexaEditor.HexEditor hexEditor = new WpfHexaEditor.HexEditor();
                        //using (var nr = new NativeReader(ProjectManagement.Instance.Project.AssetManager.GetCustomAsset("legacy", legacyFileEntry)))
                        //{
                        //    hexEditor.Stream = new MemoryStream(nr.ReadToEnd());
                        //}
                        //newLayoutDoc.Content = hexEditor;
                        ////hexEditor.BytesModified += HexEditor_BytesModified;
                        //unknownFileDocumentsPane.Children.Insert(0, newLayoutDoc);
                        //unknownFileDocumentsPane.SelectedContentIndex = 0;


                        //UnknownFileViewer.Visibility = Visibility.Visible;
                    }

                }

            }
            catch (Exception e)
            {
                MainEditorWindow.Log($"Failed to load file with message {e.Message}");
                Debug.WriteLine(e.ToString());

                //DisplayUnknownFileViewer(AssetManager.Instance.GetEbxStream(ebxEntry));

            }

            DataContext = null;
            DataContext = this;
        }

        private bool OpenEbxTextureAsset(EbxAssetEntry ebxEntry, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (ebxEntry.Type == "TextureAsset"
                
                || ebxEntry.Name == "fifa/fesplash/splashscreen/splashscreen"

                )
            {
                try
                {
                    MainEditorWindow.Log("Loading Texture " + ebxEntry.Filename);
                    var res = AssetManager.Instance.GetResEntry(ebxEntry.Name);
                    if (res != null)
                    {
                        MainEditorWindow.Log("Loading RES " + ebxEntry.Filename);

                        BuildTextureViewerFromAssetEntry(res);
                        return true;
                    }
                    else
                    {
                        res = AssetManager.Instance.GetResEntry(((dynamic)SelectedEbxAsset.RootObject).Resource.ResourceId);
                        if (res != null)
                        {
                            BuildTextureViewerFromAssetEntry(res);
                            return true;
                        }
                        throw new Exception("Unable to find RES Entry for " + ebxEntry.Name);
                    }
                }
                catch (Exception e)
                {
                    MainEditorWindow.Log($"Failed to load texture with the message :: {e.Message}");
                }
            }
            return false;
        }

        private async Task OpenEbxAsset(EbxAssetEntry ebxEntry, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                SelectedEntry = ebxEntry;
                //await AssetEntryViewer.LoadEntry(SelectedEntry, MainEditorWindow);
                if (ebxEntry == null || ebxEntry.Type == "EncryptedAsset")
                    return;

                SelectedEbxAsset = await AssetManager.Instance.GetEbxAsync(ebxEntry, cancellationToken: cancellationToken);

                Dispatcher.Invoke(() =>
                {
                    btnRevert.IsEnabled = true;
                    btnImport.IsEnabled = true;
                    btnExport.IsEnabled = true;
                });

                if (!OpenEbxTextureAsset(ebxEntry, cancellationToken))
                    layoutImageViewer.Close();

                if (!await OpenEbxMeshAsset(ebxEntry, cancellationToken))
                    layoutMeshViewer.Close();

                await OpenEbxSrandHairAsset(ebxEntry, cancellationToken);
                await OpenEbxSoundAsset(ebxEntry, cancellationToken);
                

                if (string.IsNullOrEmpty(ebxEntry.Type) || ebxEntry.Type == "UnknownType")
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        btnRevert.IsEnabled = false;
                        btnImport.IsEnabled = false;
                        btnExport.IsEnabled = false;
                    });
                    return;
                }

                MainEditorWindow.Log("Loading EBX " + ebxEntry.Filename);

                if (SelectedEbxAsset == null)
                {
                    MainEditorWindow.Log($"Failed to load Selected Ebx Asset for {ebxEntry.Filename}");
                    return;
                }

                var successful = await EBXViewer.LoadEbx(ebxEntry, SelectedEbxAsset, MainEditorWindow, AssetPath);
                await Dispatcher.InvokeAsync(() =>
                {
                    EBXViewer.Visibility = Visibility.Visible;
                });
               
            }
            catch (Exception e)
            {
                MainEditorWindow.Log($"Failed to load file with message {e.Message}");
                Debug.WriteLine(e.ToString());

                //DisplayUnknownFileViewer(AssetManager.Instance.GetEbxStream(ebxEntry));

            }
        }

        private Task OpenEbxSoundAsset(EbxAssetEntry ebxEntry, CancellationToken cancellationToken)
        {
            if (ebxEntry.Type != "NewWaveAsset" && ebxEntry.Type != "HarmonySampleBankAsset")
                return Task.CompletedTask;

            // New Wave
            if (ebxEntry.Type == "NewWaveAsset")
            {
                OpenNewWaveAsset(ebxEntry);
            }
            // Harmony Bank
            else if (ebxEntry.Type == "HarmonySampleBankAsset")
            {
                OpenHarmonyAsset(ebxEntry);
            }
            App.ShowUnsupportedMessageBox(ebxEntry.Type);

            return Task.CompletedTask;
        }

        private Task OpenEbxSrandHairAsset(EbxAssetEntry ebxEntry, CancellationToken cancellationToken)
        {
            if (ebxEntry.Type != "StrandHairAsset" && ebxEntry.Type != "StrandHairBindAsset")
                return Task.CompletedTask;


            // Strand Hair
            if (ebxEntry.Type == "StrandHairAsset")
            {
                var strandHairAssetResource = AssetManager.Instance.GetResEntry(((dynamic)SelectedEbxAsset.RootObject).StrandHairAssetResource);
                MemoryStream strandHairAssetResourceData = AssetManager.Instance.GetRes(strandHairAssetResource);
                DebugBytesToFileLogger.Instance.WriteAllBytes(SelectedEntry.Filename + "HairAsset.dat", strandHairAssetResourceData.ToArray(), "StrandHair");
                var strandHairSetResource = AssetManager.Instance.GetResEntry(((dynamic)SelectedEbxAsset.RootObject).StrandHairSetResource);
                MemoryStream strandHairSetResourceData = AssetManager.Instance.GetRes(strandHairAssetResource);
                DebugBytesToFileLogger.Instance.WriteAllBytes(SelectedEntry.Filename + "HairSet.dat", strandHairSetResourceData.ToArray(), "StrandHair");
            }
            else if (ebxEntry.Type == "StrandHairBindAsset")
            {

            }
            App.ShowUnsupportedMessageBox(ebxEntry.Type);

            return Task.CompletedTask;

        }

        private async Task<bool> OpenEbxMeshAsset(EbxAssetEntry ebxEntry, CancellationToken cancellationToken)
        {
            if (ebxEntry.Type == "SkinnedMeshAsset" || ebxEntry.Type == "CompositeMeshAsset" || ebxEntry.Type == "RigidMeshAsset")
            {
                MainEditorWindow.Log("Loading 3D Model " + ebxEntry.Filename);
                var exporter = new MeshSetToFbxExport();
                MeshSet meshSet = exporter.LoadMeshSet(ebxEntry);
                exporter.Export(AssetManager.Instance, SelectedEbxAsset.RootObject, "test_noSkel.obj", "2012", "Meters", true, null, "*.obj", meshSet);
                Thread.Sleep(150);

                if (ModelViewerModel != null)
                    ModelViewerModel.Dispose();

                ModelViewerModel = new MainViewModel(meshAsset: SelectedEbxAsset, meshSet: meshSet, ebxAssetEntry: ebxEntry);
                this.ModelViewer.DataContext = ModelViewerModel;
                this.ModelDockingManager.Visibility = Visibility.Visible;

                await ModelViewerEBX.LoadEbx(ebxEntry, SelectedEbxAsset, MainEditorWindow, AssetPath);

                await Dispatcher.InvokeAsync(() =>
                {
                    this.layoutMeshViewer.IsSelected = true;
                    this.btnExport.IsEnabled = ProfileManager.CanExportMeshes;
                    this.btnImport.IsEnabled = ProfileManager.Instance.CanImportMeshes;
                    this.btnRevert.IsEnabled = ebxEntry.HasModifiedData;
                });

                return true;
            }

            return false;
        }

        private void OpenNewWaveAsset(EbxAssetEntry ebxEntry)
        {
            layoutSoundViewer.IsEnabled = true;

            List<SoundDataTrack> retVal = new List<SoundDataTrack>();
            dynamic soundWave = AssetManager.Instance.GetEbx(ebxEntry).RootObject;
            foreach (var chunk in soundWave.Chunks)
            {
                var chunkEntry = AssetManager.Instance.GetChunkEntry(chunk.ChunkId);
                if (chunkEntry == null)
                    continue;

                MemoryStream chunkData = (MemoryStream)AssetManager.Instance.GetChunk(chunkEntry);
                if (chunkData == null)
                    continue;

#if DEBUG
                DebugBytesToFileLogger.Instance.WriteAllBytes(ebxEntry.Filename + ".dat", chunkData.ToArray(), "NewWave");
#endif

                using NativeReader reader = new NativeReader(chunkData);
                if (reader.ReadUShort() != 0x48) // This aint a correct part of the file
                    return;

                SoundDataTrack track = new();
                ushort headersize = reader.ReadUShort(Endian.Big);
                byte codec = (byte)(reader.ReadByte() & 0xF);
                int channels = (reader.ReadByte() >> 2) + 1;
                ushort sampleRate = reader.ReadUShort(Endian.Big);
                uint sampleCount = reader.ReadUInt(Endian.Big) & 0xFFFFFFF;
                List<short> decodedSoundBuf = new List<short>();

                switch (codec)
                {
                    case 0x1: track.Codec = "Unknown"; break;
                    case 0x2: track.Codec = "PCM 16 Big"; break;
                    case 0x3: track.Codec = "EA-XMA"; break;
                    case 0x4: track.Codec = "XAS Interleaved v1"; break;
                    case 0x5: track.Codec = "EALayer3 Interleaved v1"; break;
                    case 0x6: track.Codec = "EALayer3 Interleaved v2 PCM"; break;
                    case 0x7: track.Codec = "EALayer3 Interleaved v2 Spike"; break;
                    case 0x9: track.Codec = "EASpeex"; break;
                    case 0xA: track.Codec = "Unknown"; break;
                    case 0xB: track.Codec = "EA-MP3"; break;
                    case 0xC: track.Codec = "EAOpus"; break;
                    case 0xD: track.Codec = "EAAtrac9"; break;
                    case 0xE: track.Codec = "MultiStream Opus"; break;
                    case 0xF: track.Codec = "MultiStream Opus (Uncoupled)"; break;
                }
                byte[] soundBuf = reader.ReadToEnd();
                if (codec == 0x2)
                {
                    short[] data = Pcm16b.Decode(soundBuf);
                    decodedSoundBuf.AddRange(data);
                    sampleCount = (uint)data.Length;
                }
                else if (codec == 0x4)
                {
                    short[] data = XAS.Decode(soundBuf);
                    decodedSoundBuf.AddRange(data);
                    sampleCount = (uint)data.Length;
                }
                else if (codec == 0x5 || codec == 0x6)
                {
                    sampleCount = 0;
                    EALayer3.Decode(soundBuf, soundBuf.Length, (short[] data, int count, EALayer3.StreamInfo info) =>
                    {
                        if (info.streamIndex == -1)
                            return;
                        sampleCount += (uint)data.Length;
                        decodedSoundBuf.AddRange(data);
                    });
                }
                else if (codec == 0xE)
                {
                    Concentus.Structs.OpusDecoder decoder = Concentus.Structs.OpusDecoder.Create(48000, 1);

                    // Decoding loop
                    int frameSize = Concentus.Structs.OpusPacketInfo.GetNumSamples(soundBuf, 0, soundBuf.Length, sampleRate); // 960; // must be same as framesize used in input, you can use OpusPacketInfo.GetNumSamples() to determine this dynamically
                    short[] outputBuffer = new short[frameSize];

                    try
                    {
                        int thisFrameSize = decoder.Decode(soundBuf, 0, soundBuf.Length, outputBuffer, 0, frameSize, false);
                    }
                    catch
                    {

                    }
                }
                track.SampleRate = sampleRate;
                track.ChannelCount = channels;
                track.Samples = decodedSoundBuf.ToArray();

            }
        }

        public static readonly DependencyProperty TracksListProperty = DependencyProperty.Register("TracksList", typeof(ObservableCollection<SoundDataTrack>), typeof(OpenedFile), new FrameworkPropertyMetadata(null));
        public ObservableCollection<SoundDataTrack> TracksList
        {
            get => (ObservableCollection<SoundDataTrack>)GetValue(TracksListProperty);
            set => SetValue(TracksListProperty, value);
        }

        public static readonly DependencyProperty SelectedTrackProperty = DependencyProperty.Register("SelectedTrack", typeof(SoundDataTrack), typeof(OpenedFile), new FrameworkPropertyMetadata(null));
        public SoundDataTrack SelectedTrack
        {
            get => (SoundDataTrack)GetValue(SelectedTrackProperty);
            set => SetValue(SelectedTrackProperty, value);
        }

        private AudioPlayer audioPlayer;

        private void OpenHarmonyAsset(EbxAssetEntry ebxEntry)
        {
            if (ebxEntry == null)
                return;



            TracksList.Clear();

            dynamic soundWave = AssetManager.Instance.GetEbx(ebxEntry).RootObject;
            dynamic ramChunk = soundWave.Chunks[soundWave.RamChunkIndex];

            int index = 0;

            ChunkAssetEntry ramChunkEntry = AssetManager.Instance.GetChunkEntry(ramChunk.ChunkId);

            NativeReader streamChunkReader = null;
            if (soundWave.StreamChunkIndex != 255)
            {
                dynamic streamChunk = soundWave.Chunks[soundWave.StreamChunkIndex];
                ChunkAssetEntry streamChunkEntry = AssetManager.Instance.GetChunkEntry(streamChunk.ChunkId);
                streamChunkReader = new NativeReader(AssetManager.Instance.GetChunk(streamChunkEntry));
            }

            DebugBytesToFileLogger.Instance.WriteAllBytes(ebxEntry.Filename, ((MemoryStream)AssetManager.Instance.GetChunk(ramChunkEntry)).ToArray(), "Sound");

            using (NativeReader reader = new NativeReader(AssetManager.Instance.GetChunk(ramChunkEntry)))
            {
                Endian endian = reader.ReadUInt() switch
                {
                    1701593683u => Endian.Little,
                    1700938323u => Endian.Big,
                    _ => throw new InvalidDataException("Wrong format for harmony sample bank."),
                };
                reader.ReadUInt(endian);
                byte alignment = (byte)(1 << (int)reader.ReadByte());
                byte version = reader.ReadByte();

                reader.Position = 0x0a;
                int datasetCount = reader.ReadUShort(endian);

                int bankKey = reader.ReadInt(endian);
                int projectKey = reader.ReadInt(endian);
                _ = reader.ReadInt(endian);

                int newWaveDataSetEntryOffset = reader.ReadInt(endian);
                _ = reader.ReadUInt(endian);
                reader.Position = 0x20;
                int dataOffset = reader.ReadInt(endian);
                _ = reader.ReadUInt(endian);

                _ = reader.ReadUInt(endian);
                _ = reader.ReadUInt(endian);

                _ = reader.ReadUInt(endian);
                _ = reader.ReadUInt(endian);

                _ = reader.ReadUInt(endian);
                _ = reader.ReadUInt(endian);

                _ = reader.ReadUInt(endian);
                _ = reader.ReadUInt(endian);

                _ = reader.ReadUInt(endian);
                _ = reader.ReadUInt(endian);

                if (datasetCount <= 0 || newWaveDataSetEntryOffset <= 0)
                    return;

                reader.Position = newWaveDataSetEntryOffset;
                List<int> offsets = new List<int>();
                for (int i = 0; i < datasetCount; i++)
                {
                    reader.Position = newWaveDataSetEntryOffset + (i * 8);
                    offsets.Add(reader.ReadInt(endian));
                    var unk1 = reader.ReadUInt(endian);
                }

                foreach (int offset in offsets)
                {
                    reader.Position = offset + 0x3c;
                    int blockCount = reader.ReadUShort();
                    reader.Position += 0x0a;

                    int fileOffset = -1;
                    bool streaming = false;

                    for (int i = 0; i < blockCount; i++)
                    {
                        uint blockType = reader.ReadUInt();
                        if (blockType == 0x2e4f4646)
                        {
                            reader.Position += 4;
                            fileOffset = reader.ReadInt();
                            reader.Position += 0x0c;

                            streaming = true;
                        }
                        else if (blockType == 0x2e52414d)
                        {
                            reader.Position += 4;
                            fileOffset = reader.ReadInt() + dataOffset;
                            reader.Position += 0x0c;
                        }
                        else
                        {
                            reader.Position += 0x14;
                        }
                    }

                    if (fileOffset != -1)
                    {
                        NativeReader actualReader = reader;
                        if (streaming)
                            actualReader = streamChunkReader;

                        SoundDataTrack track = new SoundDataTrack { Name = "Track #" + (index++) };

                        actualReader.Position = fileOffset;
                        List<short> decodedSoundBuf = new List<short>();

                        uint headerSize = actualReader.ReadUInt(Endian.Big) & 0x00ffffff;
                        byte codec = actualReader.ReadByte();
                        int channels = (actualReader.ReadByte() >> 2) + 1;
                        ushort sampleRate = actualReader.ReadUShort(Endian.Big);
                        uint sampleCount = actualReader.ReadUInt(Endian.Big) & 0x00ffffff;

                        switch (codec)
                        {
                            case 0x14: track.Codec = "XAS"; break;
                            case 0x15: track.Codec = "EALayer3 v5"; break;
                            case 0x16: track.Codec = "EALayer3 v6"; break;
                            default: 
                                track.Codec = "Unknown (" + codec.ToString("x2") + ")"; 
                                break;
                        }

                        actualReader.Position = fileOffset;
                        byte[] soundBuf = actualReader.ReadToEnd();
                        double duration = 0.0;

                        if (codec == 0x14)
                        {
                            short[] data = XAS.Decode(soundBuf);
                            decodedSoundBuf.AddRange(data);
                            duration += (data.Length / channels) / (double)sampleRate;
                        }
                        else if (codec == 0x15 || codec == 0x16)
                        {
                            sampleCount = 0;
                            EALayer3.Decode(soundBuf, soundBuf.Length, (short[] data, int count, EALayer3.StreamInfo info) =>
                            {
                                if (info.streamIndex == -1)
                                    return;
                                sampleCount += (uint)data.Length;
                                decodedSoundBuf.AddRange(data);
                            });
                            duration += (sampleCount / channels) / (double)sampleRate;
                        }

                        track.Duration += duration;
                        track.Samples = decodedSoundBuf.ToArray();
                        track.SegmentCount = 1;
                        track.SampleRate = sampleRate;
                        track.ChannelCount = channels;
                        TracksList.Add(track);
                    }
                }
            }

            if(TracksList.Count > 0)
                this.layoutSoundViewer.IsSelected = true;
        }


        private void DisplayUnknownFileViewer(Stream stream)
        {
            //btnExport.IsEnabled = true;
            //btnImport.IsEnabled = true;
            //btnRevert.IsEnabled = true;

            //unknownFileDocumentsPane.Children.Clear();
            var newLayoutDoc = new LayoutDocument();
            newLayoutDoc.Title = SelectedEntry.DisplayName;
            WpfHexaEditor.HexEditor hexEditor = new WpfHexaEditor.HexEditor();
            hexEditor.Stream = stream;
            newLayoutDoc.Content = hexEditor;
            //hexEditor.BytesModified -= HexEditor_BytesModified;
            //hexEditor.BytesModified += HexEditor_BytesModified;
            unknownFileDocumentsPane.Children.Insert(0, newLayoutDoc);
            unknownFileDocumentsPane.SelectedContentIndex = 0;

            //UnknownFileViewer.Visibility = Visibility.Visible;
        }


        private void OpenLTUAsset(LiveTuningUpdate.LiveTuningUpdateEntry entry)
        {
            MainEditorWindow.Log("Loading EBX " + entry.Filename);
            var ebx = entry.GetAsset();
            var successful = EBXViewer.LoadEbx(entry, ebx, MainEditorWindow, AssetPath);
            EBXViewer.Visibility = Visibility.Visible;
        }

        private async void btnImport_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                AssetEntryImporter assetEntryImporter = new AssetEntryImporter(SelectedEntry);
                var openFileDialog = assetEntryImporter.GetOpenDialogWithFilter();
                var dialogResult = openFileDialog.ShowDialog();
                if (dialogResult.HasValue && dialogResult.Value == true)
                {
                    MainEditorWindow.Log("Importing " + openFileDialog.FileName);

                    await UpdateLoadingVisibility(true);

                    if (SelectedEntry.Type == "SkinnedMeshAsset")
                    {
                        var skeletonEntryText = "content/character/rig/skeleton/player/skeleton_player";
                        MeshSkeletonSelector meshSkeletonSelector = new MeshSkeletonSelector();
                        var meshSelectorResult = meshSkeletonSelector.ShowDialog();
                        if (meshSelectorResult.HasValue && meshSelectorResult.Value)
                        {
                            if (!meshSelectorResult.Value)
                            {
                                MessageBox.Show("Cannot import without a Skeleton");
                                return;
                            }

                            skeletonEntryText = meshSkeletonSelector.AssetEntry.Name;
                            assetEntryImporter.ImportEbxSkinnedMesh(openFileDialog.FileName, skeletonEntryText);

                        }
                        else
                        {
                            MessageBox.Show("Cannot import without a Skeleton");
                            return;
                        }
                    }
                    else
                    {
                        var importResult = await assetEntryImporter.ImportAsync(openFileDialog.FileName);
                        if (!importResult)
                        {
                            MainEditorWindow.LogError("Failed to import file to " + SelectedEntry.Name);
                            return;
                        }

                        if (SelectedEntry.Type == "TextureAsset")
                        {
                            BuildTextureViewerFromAssetEntry(AssetManager.Instance.GetResEntry(SelectedEntry.Name));
                        }
                        else
                        {
                            await OpenAsset(SelectedEntry);
                        }
                        MainEditorWindow.Log($"Imported file {openFileDialog.FileName} successfully.");

                    }
                }

            }
            catch (Exception ex)
            {
                MainEditorWindow.LogError(ex.Message + Environment.NewLine);
            }

            await UpdateLoadingVisibility(false);

        }

        private async void btnExport_Click(object sender, RoutedEventArgs e)
        {
            _ = UpdateLoadingVisibility(true);
            try
            {
                await new Exporting().Export(SelectedEntry);
            }
            finally
            {
                _ = UpdateLoadingVisibility(false);
            }
        }

        private async void btnRevert_Click(object sender, RoutedEventArgs e)
        {
            _ = UpdateLoadingVisibility(true);
            try
            {
                if (SelectedEntry != null)
                {
                    if (EBXViewer != null && EBXViewer.Visibility == Visibility.Visible)
                    {
                        EBXViewer.RevertAsset();
                    }
                    else
                    {
                        AssetManager.Instance.RevertAsset(SelectedEntry);
                        if (SelectedEntry.Type == "DDS")
                        {
                            BuildTextureViewerFromStream((MemoryStream)AssetManager.Instance.GetCustomAsset("legacy", (AssetEntry)SelectedEntry));
                        }
                    }
                }

                await OpenAsset(SelectedEntry);
            }
            finally
            {
                _ = UpdateLoadingVisibility(false);
            }

        }

        private void TextViewer_LostFocus(object sender, RoutedEventArgs e)
        {
            var bytes = ASCIIEncoding.UTF8.GetBytes(TextViewer.Text);

            //if (SelectedLegacyEntry != null)
            //{
            //    AssetManager.Instance.ModifyLegacyAsset(SelectedLegacyEntry.Name
            //                , bytes
            //                , false);
            //    UpdateAssetListView();
            //}
        }

        private void BuildTextureViewerFromAssetEntry(ResAssetEntry res)
        {
            ImageViewer.Source = null;

            using (Texture textureAsset = new Texture(res))
            {
                //MemoryStream memoryStream = new MemoryStream();
                Stream expToStream = null;

                try
                {
                    CurrentDDSImageFormat = textureAsset.PixelFormat;

                    var bPath = Directory.GetCurrentDirectory() + @"\temp.png";

                    TextureExporter textureExporter = new TextureExporter();

                    try
                    {
                        expToStream = textureExporter.ExportToStream(textureAsset, TextureUtils.ImageFormat.PNG);
                        expToStream.Position = 0;

                    }
                    catch (Exception exception_ToStream)
                    {
                        MainEditorWindow.LogError($"Error loading texture with message :: {exception_ToStream.Message}");
                        MainEditorWindow.LogError(exception_ToStream.ToString());
                        ImageViewer.Source = null;
                        //ImageViewerScreen.Visibility = Visibility.Collapsed;

                        textureExporter.Export(textureAsset, res.Filename + ".DDS", "*.DDS");
                        MainEditorWindow.LogError($"As the viewer failed. The image has been exported to {res.Filename}.dds instead.");
                        return;
                    }

                    ImageViewer.Source = LoadImage(((MemoryStream)expToStream).ToArray());

                    //ImageViewerScreen.Visibility = Visibility.Visible;
                    ImageViewer.MaxHeight = textureAsset.Height;
                    ImageViewer.MaxWidth = textureAsset.Width;
                    layoutImageViewer.IsSelected = true;

                    btnExport.IsEnabled = true;
                    btnImport.IsEnabled = true;
                    btnRevert.IsEnabled = true;

                }
                catch (Exception e)
                {
                    MainEditorWindow.LogError($"Error loading texture with message :: {e.Message}");
                    MainEditorWindow.LogError(e.ToString());
                    ImageViewer.Source = null; 
                    //ImageViewerScreen.Visibility = Visibility.Collapsed;
                }
                finally
                {
                    // clean up resources
                    if (expToStream != null)
                    {
                        expToStream.Close();
                        expToStream.Dispose();
                        expToStream = null;
                    }
                }
            }
        }

        public string CurrentDDSImageFormat { get; set; }

        //private void BuildTextureViewerFromStream(Stream stream, AssetEntry assetEntry = null)
        private void BuildTextureViewerFromStream(MemoryStream stream)
        {

            try
            {
                ImageViewer.Source = null;

                var bPath = Directory.GetCurrentDirectory() + @"\temp.png";

                ImageEngineImage imageEngineImage = new ImageEngineImage(stream.ToArray());
                var iData = imageEngineImage.Save(new ImageFormats.ImageEngineFormatDetails(ImageEngineFormat.BMP), MipHandling.KeepTopOnly, removeAlpha: false);

                ImageViewer.Source = LoadImage(iData);
                //ImageViewerScreen.Visibility = Visibility.Visible;

                btnExport.IsEnabled = true;
                btnImport.IsEnabled = true;
                btnRevert.IsEnabled = true;

            }
            catch (Exception e)
            {
                MainEditorWindow.LogError(e.Message);
                ImageViewer.Source = null;
                //ImageViewerScreen.Visibility = Visibility.Collapsed;
            }

        }

        System.Windows.Media.Imaging.BitmapImage bitmapImage = new System.Windows.Media.Imaging.BitmapImage();

        private System.Windows.Media.Imaging.BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            if (bitmapImage != null)
            {
                bitmapImage = null;
                bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
            }

            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.UriSource = null;
                bitmapImage.StreamSource = mem;
                bitmapImage.EndInit();
            }
            bitmapImage.Freeze();

            imageData = null;

            return bitmapImage;
        }

        private async Task ExportAsset(AssetEntry assetEntry, string saveLocation)
        {
            bool isFolder = false;
            if (new DirectoryInfo(saveLocation).Exists)
            {
                saveLocation += "\\" + assetEntry.Filename;
                isFolder = true;
            }

            if (assetEntry is LegacyFileEntry)
            {
                var legacyData = ((MemoryStream)AssetManager.Instance.GetCustomAsset("legacy", assetEntry)).ToArray();
                if (assetEntry.Type == "DDS"
                    &&
                    (saveLocation.Contains("PNG", StringComparison.OrdinalIgnoreCase)
                     || isFolder)
                    )
                {
                    if (isFolder)
                    {
                        saveLocation += ".png";
                    }

                    ImageEngineImage originalImage = new ImageEngineImage(legacyData);

                    var imageBytes = originalImage.Save(
                        new ImageFormats.ImageEngineFormatDetails(
                            ImageEngineFormat.PNG)
                        , MipHandling.KeepTopOnly
                        , removeAlpha: false);

                    await File.WriteAllBytesAsync(saveLocation, imageBytes);
                }
                else if (assetEntry.Type == "DDS" && saveLocation.Contains("DDS", StringComparison.OrdinalIgnoreCase))
                {
                    ImageEngineImage originalImage = new ImageEngineImage(legacyData);

                    await originalImage.Save(saveLocation
                        , new ImageFormats.ImageEngineFormatDetails(ImageEngineFormat.DDS_DXT5)
                        , GenerateMips: MipHandling.KeepExisting
                        , removeAlpha: false);
                }
                else
                {
                    if (isFolder)
                        saveLocation += "." + assetEntry.Type;

                    await File.WriteAllBytesAsync(saveLocation, legacyData);
                }
                MainEditorWindow.Log($"Exported {assetEntry.Filename} to {saveLocation}");
            }
            else if (assetEntry is EbxAssetEntry)
            {
                if (assetEntry.Type == "TextureAsset")
                {
                    var resEntry = AssetManager.Instance.GetResEntry(assetEntry.Name);
                    if (resEntry != null)
                    {
                        using (var resStream = AssetManager.Instance.GetRes(resEntry))
                        {
                            Texture texture = new Texture(resStream, resEntry);
                            TextureExporter textureExporter = new TextureExporter();
                            if (isFolder)
                                saveLocation += ".png";
                            textureExporter.Export(texture, saveLocation, "*.png");
                            MainEditorWindow.Log($"Exported {assetEntry.Filename} to {saveLocation}");
                        }
                    }
                }
                else
                {
                    var ebx = AssetManager.Instance.GetEbxStream((EbxAssetEntry)assetEntry);
                    if (ebx != null)
                    {
                        if (isFolder)
                            saveLocation += ".bin";
                        File.WriteAllBytes(saveLocation, ((MemoryStream)ebx).ToArray());
                        MainEditorWindow.Log($"Exported {assetEntry.Filename} to {saveLocation}");

                    }
                    else
                    {
                        MainEditorWindow.Log("Failed to export file");
                    }
                }
            }
        }

        private void lbTracksList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbTracksList.SelectedItem == null)
                return;

            if (!(lbTracksList.SelectedItem is SoundDataTrack currentTrack))
                return;

            SelectedTrack = currentTrack;

            btnPlaySound.IsEnabled = true;
            //btnPauseSound.IsEnabled = true;
        }

        private async void btnPlaySound_Click(object sender, RoutedEventArgs e)
        {
            if (lbTracksList.SelectedItem == null)
                return;

            if (!(lbTracksList.SelectedItem is SoundDataTrack currentTrack))
                return;

            SelectedTrack = currentTrack;

            try
            {
                audioPlayer.PlaySound(currentTrack);
                btnPauseSound.IsEnabled = true;

                await Dispatcher.InvokeAsync(async () =>
                {
                    currentTrack.Progress = audioPlayer.Progress * 800.0;
                    await Task.Delay(30);

                    currentTrack.Progress = 0;
                    btnPauseSound.IsEnabled = false;
                    btnPauseSound.IsEnabled = true;
                });
            }
            catch { }

        }

        private void btnPauseSound_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                audioPlayer.SoundDispose();
                btnPauseSound.IsEnabled = false;
            }
            catch { }

            if (lbTracksList.SelectedItem != null)
                btnPlaySound.IsEnabled = true;
        }
    }

}

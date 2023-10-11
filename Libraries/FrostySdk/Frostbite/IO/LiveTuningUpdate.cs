using FMT.FileTools;
using FrostySdk.FrostbiteSdk.Managers;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FrostySdk.Frostbite.IO
{
    public class LiveTuningUpdate
    {
        public LiveTuningUpdate()
        {
            ReadFIFALiveTuningUpdate();
        }

        public string FIFALiveTuningUpdatePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", ProfileManager.DisplayName, "onlinecache0", "attribdb.bin");
        public bool HasFIFALiveTuningUpdate => File.Exists(FIFALiveTuningUpdatePath);
        public Dictionary<string, (int, int)> LiveTuningUpdates { get; } = new Dictionary<string, (int, int)>();

        public Dictionary<string, LiveTuningUpdateEntry> LiveTuningUpdateEntries =>
            LiveTuningUpdates.ToDictionary(x => x.Key
                , x => new LiveTuningUpdateEntry(x.Key, x.Value.Item1, x.Value.Item2)
                {
                });

        public List<Guid> LiveTuningUpdateGuids { get; } = new List<Guid>();

        public EbxAsset GetLiveTuningUpdateAsset(string entry)
        {
            if (!HasFIFALiveTuningUpdate)
                return null;

            var bytesOfFile = File.ReadAllBytes(FIFALiveTuningUpdatePath);
            if (bytesOfFile.Length > 0)
            {
                using (var nr = new NativeReader(new MemoryStream(bytesOfFile)))
                {
                    nr.Position = LiveTuningUpdates[entry].Item1;
                    var bytes = nr.ReadBytes(LiveTuningUpdates[entry].Item2);
                    using (var ms = new MemoryStream(bytes))
                    {
                        return AssetManager.Instance.GetEbxAssetFromStream(ms);
                    }
                }
            }
            return null;

        }

        public Dictionary<string, (int, int)> ReadFIFALiveTuningUpdate()
        {
            if (LiveTuningUpdates.Count > 0)
                return LiveTuningUpdates;

            if (!HasFIFALiveTuningUpdate)
                return LiveTuningUpdates;

            var bytesOfFile = File.ReadAllBytes(FIFALiveTuningUpdatePath);
            if (bytesOfFile.Length > 0)
            {
                var searchByte = new byte[] { 0x52, 0x49, 0x46, 0x46 };

                BoyerMoore boyerMoore = new BoyerMoore(searchByte);
                var possibleEbxFound = boyerMoore.SearchAll(bytesOfFile);
                using (NativeReader nr = new NativeReader(new MemoryStream(bytesOfFile)))
                {
                    var headerSize = 48;
                    var unkCount1 = nr.ReadInt();
                    var unkCount2 = nr.ReadInt();
                    var unkCount3 = nr.ReadInt();
                    var unk4 = nr.ReadUInt();
                    var unkGuid5 = nr.ReadGuid();
                    var unk6 = nr.ReadInt();
                    var fileSize = nr.ReadInt() + 40;
                    nr.Pad(16);
                    var guidPositions = nr.ReadInt() + headerSize;
                    var unk7 = nr.ReadInt();
                    var riffPositions = nr.ReadInt() + headerSize;
                    var riffCount = nr.ReadInt(); // 1575
                                                  // 31488

                    _ = nr.ReadInt();
                    _ = nr.ReadInt();
                    // look at positon 64 next and read the following data
                    //for (var iUnk1 = 0; iUnk1 < riffCount; iUnk1++)
                    //{
                    //    _ = nr.ReadInt();
                    //}


                    nr.Position = guidPositions;
                    for (var iGuid = 0; iGuid < riffCount; iGuid++)
                    {
                        LiveTuningUpdateGuids.Add(nr.ReadGuid());
                    }

                    // this is sanity check, it should be here anyway
                    nr.Position = riffPositions;
                    foreach (var possEbxPositionsFound in possibleEbxFound)
                    // TODO: We should be able to read these riffs in turn, but for some reason my current calculation is wrong!
                    //for(var iRiff = 0; iRiff < riffCount; iRiff++)
                    {
                        var passChecks = false;
                        //var possEbxPositionsFound = (int)nr.Position;
                        nr.Position = possEbxPositionsFound;
                        passChecks = nr.ReadInt() == 1179011410;
                        int size = (int)nr.ReadUInt() + 6;
                        passChecks = size > 0;
                        passChecks = nr.ReadInt() == 5784133;
                        passChecks = nr.ReadInt() == 1146634821;
                        if (passChecks)
                        {
                            nr.Position += 56;
                            var n = nr.ReadUInt();
                            nr.Position += n - 4;
                            var nameOfEbx = nr.ReadNullTerminatedString();
                            if (!string.IsNullOrEmpty(nameOfEbx))
                            {
                                nr.Position = possEbxPositionsFound + size;
                                nr.Pad(16);
                                size = (int)nr.Position - possEbxPositionsFound;
                                LiveTuningUpdates.Add(nameOfEbx.ToLower(), (possEbxPositionsFound, size));
                            }
                        }


                    }
                }
            }

            return LiveTuningUpdates;

        }

        public class LiveTuningUpdateEntry : IAssetEntry
        {
            public string FullPath { get; set; }

            public string Filename { get; set; }

            public string Path { get; set; }

            public string Name { get; set; }

            public bool IsModified { get; set; }

            public string DisplayName { get; }

            public string AssetType { get; }

            public EbxAssetEntry MatchingEbxAssetEntry => AssetManager.Instance.GetEbxEntry(FullPath);

            public string Type { get { return MatchingEbxAssetEntry.Type; } set { } }

            public int Position { get; }

            public int Size { get; }

            public ModifiedAssetEntry ModifiedAssetEntry { get; set; }

            public FMT.FileTools.Sha1 Sha1 { get; set; }

            public IModifiedAssetEntry ModifiedEntry { get; set; }

            public long OriginalSize { get; set; }

            public List<int> Bundles { get; set; }

            public string ExtraInformation { get; set; }

            public LiveTuningUpdateEntry(in string entry, in int position, in int size)
            {
                FullPath = entry;
                var lastIndexOfSlash = entry.LastIndexOf('/');
                var splitEntry = entry.Split('/');

                Filename = entry.Substring(lastIndexOfSlash + 1, entry.Length - lastIndexOfSlash - 1);
                DisplayName = Filename;
                AssetType = "ebx";
                Path = entry.Substring(0, lastIndexOfSlash);
                Name = entry;
                IsModified = false;
                Position = position;
                Size = size;
            }

            public EbxAsset GetAsset()
            {
                return FileSystem.Instance.LiveTuningUpdate.GetLiveTuningUpdateAsset(FullPath);
            }
        }
    }
}

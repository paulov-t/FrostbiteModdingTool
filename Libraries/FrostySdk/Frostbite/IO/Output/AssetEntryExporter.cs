using FMT.FileTools;
using Frostbite.Textures;
using FrostbiteSdk;
using FrostySdk.Ebx;
using FrostySdk.Managers;
using FrostySdk.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using static FrostySdk.Frostbite.IO.Input.AssetEntryImporter;

namespace FrostySdk.Frostbite.IO.Output
{
    public class AssetEntryExporter : IDisposable
    {
        private bool disposedValue;

        private IAssetEntry Entry { get; set; }

        public AssetEntryExporter(IAssetEntry entry)
        {

            Entry = entry;
        }

        public void Export(string filePath, string fbxSkeleton = null)
        {
            if (Entry == null)
                return;

            if (Entry is EbxAssetEntry ebxAssetEntry)
            {
                var obj = AssetManager.Instance.GetEbx((EbxAssetEntry)Entry).RootObject;

                if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    var serialisedObj = JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings()
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        MaxDepth = 4,
                    });

                    File.WriteAllText(filePath, serialisedObj);
                }

                if (ebxAssetEntry.Type == "SkinnedMeshAsset" || ebxAssetEntry.Type == "CompositeMeshAsset" || ebxAssetEntry.Type == "RigidMeshAsset")
                {
                    var exporter = new MeshSetToFbxExport();
                    MeshSet meshSet = exporter.LoadMeshSet(ebxAssetEntry);
                    exporter.Export(AssetManager.Instance, obj, filePath + (fbxSkeleton != null ? ".fbx" : ".obj") , "FBX_2012", "Meters", true, fbxSkeleton, fbxSkeleton != null ? "*.fbx" : "*.obj", meshSet);
                    exporter = null;
                    meshSet = null;
                }
                else if (ebxAssetEntry.Type == "TextureAsset")
                {
                    var resEntry = AssetManager.Instance.GetResEntry(Entry.Name);
                    if (resEntry != null)
                    {
                        using (var resStream = AssetManager.Instance.GetRes(resEntry))
                        {
                            Texture texture = new Texture(resStream, resEntry);
                            TextureExporter textureExporter = new TextureExporter();
                            textureExporter.Export(texture, filePath, "*.png");
                        }
                    }
                }

            }


        }

        public string ExportToJson()
        {
            var ebxAssetEntry = (EbxAssetEntry)Entry;
#if DEBUG
            if(ebxAssetEntry.Type == "TextureAsset")
            {

            }
#endif
            var obj = AssetManager.Instance.GetEbx(ebxAssetEntry).RootObject;
            var serialisedObj = JsonConvert.SerializeObject(
                obj
                , Formatting.Indented
                , new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                MaxDepth = 4,
                Converters = {
                        new ReplaceArrayConverter()
                        , new PointerRefConverter()
                        //, new ResourceRefConverter()
                    },
                });

            return serialisedObj;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    Entry = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public class ResourceRefConverter : JsonConverter
        {
            public override bool CanRead => base.CanRead;

            public override bool CanWrite => base.CanWrite;

            public override bool CanConvert(Type objectType)
            {
                return objectType.Name == "ResourceRef";
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var input = JsonSerializer.CreateDefault().Deserialize(reader, typeof(string)).ToString();
                var r = new ResourceRef(ulong.Parse(input, System.Globalization.NumberStyles.HexNumber));
                return r;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var s = value.ToString();
                JsonSerializer.CreateDefault().Serialize(writer, s);
            }
        }
    }
}

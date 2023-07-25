using FMT;
using Frostbite.Textures;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using Matrix = SharpDX.Matrix;

namespace FrostbiteModdingUI.Models
{
    public class MainViewModel : BaseViewModel, IDisposable
    {
        public Geometry3D FloorModel { get; }
        public Geometry3D MeshModel { get; set; }
        public Geometry3D MeshModel2 { get; set; }

        public PhongMaterial FloorMaterial { get; }

        public PhongMaterial MeshMaterial { get; }

        public PhongMaterial MeshMaterial2 { get; }

        public Matrix[] MeshInstances { get; }
        public Matrix[] MeshInstances2 { get; }

        public SSAOQuality[] SSAOQualities { get; } = new SSAOQuality[] { SSAOQuality.High, SSAOQuality.Low };

        public SceneNodeGroupModel3D GroupModel { get; } = new SceneNodeGroupModel3D();

        public TextureModel EnvironmentMap { get; }

        //private bool renderEnvironmentMap = true;
        //public bool RenderEnvironmentMap
        //{
        //    set
        //    {
        //        if (SetValue(ref renderEnvironmentMap, value) && scene != null && scene.Root != null)
        //        {
        //            foreach (var node in scene.Root.Traverse())
        //            {
        //                if (node is MaterialGeometryNode m && m.Material is PBRMaterialCore material)
        //                {
        //                    material.RenderEnvironmentMap = value;
        //                }
        //            }
        //        }
        //    }
        //    get => renderEnvironmentMap;
        //}

        private HelixToolkit.SharpDX.Core.Assimp.HelixToolkitScene scene;
        Stream textureDDSStreamColour;
        Stream textureDDSStreamNormal;

        public MainViewModel(string file = "test_noSkel.obj", EbxAsset meshAsset = null, MeshSet meshSet = null, EbxAssetEntry textureAsset = null, EbxAssetEntry ebxAssetEntry = null)
        {
            try
            {
                EffectsManager = new DefaultEffectsManager()
                {
                };
                Camera = new PerspectiveCamera()
                {
                    //Position = new System.Windows.Media.Media3D.Point3D(-100, -100, -100),
                    //LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 0),
                    UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                    //FarPlaneDistance = 3000,
                    //NearPlaneDistance = 1

                };

                var builder = new MeshBuilder();
                builder.AddBox(new Vector3(0, -0.1f, 0), 10, 0.1f, 10);
                FloorModel = builder.ToMesh();

                var htImporter = new HelixToolkit.SharpDX.Core.Assimp.Importer();
                scene = htImporter.Load(file);

                EbxAssetEntry variationDbAssetEntry = null;

                if (ebxAssetEntry != null)
                {
                    variationDbAssetEntry =
                        AssetManager.Instance.EnumerateEbx("MeshVariationDatabase")
                        .ToArray()
                        .FirstOrDefault(x => x.Name.StartsWith(ebxAssetEntry.Path + "_", StringComparison.OrdinalIgnoreCase));
                }

                MeshNode firstModel = null;
                var index = 0;
                if (scene != null)
                {
                    if (scene.Root != null)
                    {
                        GroupModel.Clear();
                        GroupModel.AddNode(scene.Root);
                        foreach (SceneNode rItem in scene.Root.Items)
                        {
                            foreach (SceneNode item in rItem.Items)
                            {
                                MeshNode meshNode = item as MeshNode;
                                if (meshNode != null && meshAsset != null)
                                {
                                    if (!meshNode.Name.Contains("lod0"))
                                    {
                                        meshNode.Visible = false;
                                    }
                                    else
                                    {
                                        if (firstModel == null)
                                            firstModel = meshNode;

                                        textureDDSStreamColour = LoadTexture(meshAsset, index, "colorTexture");
                                        PhongMaterial material = new PhongMaterial
                                        {
                                            AmbientColor = Colors.Gray.ToColor4(),
                                            DiffuseColor = Colors.Gray.ToColor4(),
                                            SpecularColor = Colors.Black.ToColor4(),
                                            SpecularShininess = 0.0015f
                                        };

                                        if (textureDDSStreamColour != null)
                                        {
                                            material.DiffuseMap = new TextureModel(textureDDSStreamColour);
                                            material.SpecularColorMap = material.DiffuseMap;
                                        }

                                        textureDDSStreamNormal = LoadTexture(meshAsset, index, "normalTexture");
                                        if (textureDDSStreamNormal != null)
                                        {
                                            material.NormalMap = new TextureModel(textureDDSStreamNormal);
                                        }
                                        meshNode.Material = material;

                                    }
                                }
                            }
                            if (rItem.Items.Count > 0 && meshAsset != null)
                                index++;

                        }
                    }
                }

                FloorMaterial = PhongMaterials.White;
                FloorMaterial.AmbientColor = FloorMaterial.DiffuseColor * 0.7f;

                if (Camera != null && firstModel != null)
                {
                    Camera.Position = new System.Windows.Media.Media3D.Point3D(0.0, firstModel.Geometry.Positions[0].Y, 0.65);
                }
            }
            catch
            {

            }
        }


        private Stream LoadTexture(EbxAsset ebxAsset, int materialId, string textureName)
        {
            var rootObject = ((dynamic)ebxAsset.RootObject);
            if (rootObject == null)
                return null;

            if (rootObject.Materials == null)
                return null;

            if (rootObject.Materials.Count == 0)
                return null;

            dynamic meshMaterial = rootObject.Materials[materialId].Internal;
            dynamic shader = meshMaterial.Shader;
            dynamic desiredTextureParameter = null;
            if (shader.TextureParameters == null)
                return null;

            foreach (dynamic textureParameter2 in shader.TextureParameters)
            {
                if (textureParameter2.ParameterName.Equals(textureName, StringComparison.OrdinalIgnoreCase))
                {
                    desiredTextureParameter = textureParameter2;
                    break;
                }
            }
            if (desiredTextureParameter == null)
            {
                Guid shaderGuid = ((PointerRef)shader.Shader).External.FileGuid;
                if (shaderGuid == Guid.Empty)
                {
                    return null;
                }
                EbxAssetEntry shaderAssetEntry = AssetManager.Instance.GetEbxEntry(shaderGuid.ToString());
                if (shaderAssetEntry == null)
                {
                    return null;
                }
                EbxAsset shaderAsset = AssetManager.Instance.GetEbx(shaderAssetEntry);
                dynamic shaderPreset = ((dynamic)shaderAsset.RootObject).ShaderPreset;
                foreach (dynamic textureParameter in shaderPreset.TextureParameters)
                {
                    if (textureParameter.ParameterName.Equals(textureName, StringComparison.OrdinalIgnoreCase))
                    {
                        desiredTextureParameter = textureParameter;
                        break;
                    }
                }
                if (desiredTextureParameter == null)
                {
                    return null;
                }
            }
            Guid textureGuid = ((PointerRef)desiredTextureParameter.Value).External.FileGuid;
            if (textureGuid == Guid.Empty)
            {
                return null;
            }
            EbxAssetEntry textureAssetEntry = AssetManager.Instance.GetEbxEntry(textureGuid);
            if (textureAssetEntry == null)
            {
                return null;
            }
            EbxAsset textureAsset = AssetManager.Instance.GetEbx(textureAssetEntry);
            ulong textureResRid = ((dynamic)textureAsset.RootObject).Resource;
            Texture texture = new Texture(AssetManager.Instance.GetRes(AssetManager.Instance.GetResEntry(textureResRid)), AssetManager.Instance);
            MemoryStream textureDDSStream = new MemoryStream();
            TextureExporter textureExporter = new TextureExporter();
            textureDDSStream = textureExporter.ExportToStream(texture) as MemoryStream;
            //new DDSTextureExporter().Export(texture, textureDDSStream, dispose: false);
            textureDDSStream.Position = 0L;
            return textureDDSStream;
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (textureDDSStreamColour != null)
                        textureDDSStreamColour.Dispose();
                    if (textureDDSStreamNormal != null)
                        textureDDSStreamNormal.Dispose();
                }



                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                if (EffectsManager != null)
                {
                    var effectManager = EffectsManager as IDisposable;
                    Disposer.RemoveAndDispose(ref effectManager);
                }

                if (scene != null && scene.Root != null)
                    scene.Root.Dispose();


                disposedValue = true;
                GC.SuppressFinalize(this);
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~MainViewModel()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public new void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }



    public abstract class BaseViewModel : ObservableObject, IDisposable
    {
        public const string Orthographic = "Orthographic Camera";

        public const string Perspective = "Perspective Camera";

        private string cameraModel;

        private Camera camera;


        public List<string> CameraModelCollection { get; private set; }

        public string CameraModel
        {
            get
            {
                return cameraModel;
            }
            set
            {
                cameraModel = value;
                //if (SetValue(ref cameraModel, value, "CameraModel"))
                //{
                //    OnCameraModelChanged();
                //}
            }
        }

        public Camera Camera
        {
            get
            {
                return camera;
            }

            protected set
            {
                camera = value;
                //SetValue(ref camera, value, "Camera");
                CameraModel = value is PerspectiveCamera
                                       ? Perspective
                                       : value is OrthographicCamera ? Orthographic : null;
            }
        }
        private IEffectsManager effectsManager;
        public IEffectsManager EffectsManager
        {
            get { return effectsManager; }
            protected set
            {
                //SetValue(ref effectsManager, value);
                effectsManager = value;
            }
        }

        protected OrthographicCamera defaultOrthographicCamera = new OrthographicCamera { Position = new System.Windows.Media.Media3D.Point3D(0, 0, 5), LookDirection = new System.Windows.Media.Media3D.Vector3D(-0, -0, -5), UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0), NearPlaneDistance = 1, FarPlaneDistance = 100 };

        protected PerspectiveCamera defaultPerspectiveCamera = new PerspectiveCamera { Position = new System.Windows.Media.Media3D.Point3D(0, 0, 5), LookDirection = new System.Windows.Media.Media3D.Vector3D(-0, -0, -5), UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0), NearPlaneDistance = 0.5, FarPlaneDistance = 150 };

        public event EventHandler CameraModelChanged;

        protected BaseViewModel()
        {
            // camera models
            CameraModelCollection = new List<string>()
            {
                Orthographic,
                Perspective,
            };

            // on camera changed callback
            CameraModelChanged += (s, e) =>
            {
                if (cameraModel == Orthographic)
                {
                    if (!(Camera is OrthographicCamera))
                        Camera = defaultOrthographicCamera;
                }
                else if (cameraModel == Perspective)
                {
                    if (!(Camera is PerspectiveCamera))
                        Camera = defaultPerspectiveCamera;
                }
                else
                {
                    throw new HelixToolkitException("Camera Model Error.");
                }
            };

            // default camera model
            CameraModel = Perspective;

            //Title = "Demo (HelixToolkitDX)";
            //SubTitle = "Default Base View Model";
        }

        protected virtual void OnCameraModelChanged()
        {
            var eh = CameraModelChanged;
            if (eh != null)
            {
                eh(this, new EventArgs());
            }
        }

        public static MemoryStream LoadFileToMemory(string filePath)
        {
            using (var file = new FileStream(filePath, FileMode.Open))
            {
                var memory = new MemoryStream();
                file.CopyTo(memory);
                return memory;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                if (EffectsManager != null)
                {
                    var effectManager = EffectsManager as IDisposable;
                    Disposer.RemoveAndDispose(ref effectManager);
                }
                disposedValue = true;
                GC.SuppressFinalize(this);
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~BaseViewModel()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

}

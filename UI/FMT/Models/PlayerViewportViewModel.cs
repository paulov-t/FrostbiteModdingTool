using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using System.IO;

namespace FrostbiteModdingUI.Models
{
    public class PlayerViewportViewModel : BaseViewModel
    {
        public PhongMaterial FloorMaterial { get; }

        public Geometry3D FloorModel { get; }
        public Geometry3D FaceModel { get; }
        public Geometry3D HairCapModel { get; }
        public Geometry3D HairModel { get; }

        public GroupModel3D FaceModels { get; }

        public PhongMaterial FaceMaterial { get; }
        public PhongMaterial HairCapMaterial { get; }
        public PhongMaterial HairMaterial { get; }

        public Matrix[] InstanceTranslation { get; }

        public PlayerViewportViewModel()
        {
            EffectsManager = new DefaultEffectsManager();
            Camera = new PerspectiveCamera()
            {
                Position = new System.Windows.Media.Media3D.Point3D(0, 0.001f, 0.001f),
                LookDirection = new System.Windows.Media.Media3D.Vector3D(0, -1, -1),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                FarPlaneDistance = 200,
                NearPlaneDistance = 0.1
            };

            var builder = new HelixToolkit.SharpDX.Core.MeshBuilder();
            builder.AddBox(new Vector3(0, -0.1f, 0), 10, 0.1f, 10);
            FloorModel = builder.ToMesh();
            FloorMaterial = PhongMaterials.PureWhite;
            FloorMaterial.AmbientColor = FloorMaterial.DiffuseColor * 0.5f;

            var reader = new HelixToolkit.SharpDX.Core.ObjReader();
            if (File.Exists("Renderer/FaceModel.obj"))
            {
                var models = reader.Read("Renderer/FaceModel.obj");
                if (models.Count > 0)
                {
                    FaceModel = models[0].Geometry;
                    FaceMaterial = new PhongMaterial()
                    {
                        DiffuseMap = new HelixToolkit.SharpDX.Core.DefaultTextureLoader().Load("Renderer/FaceModel.obj", "Renderer/FaceTexture.png", new HelixToolkit.Logger.DebugLogger())
                    };

                }

                reader = new HelixToolkit.SharpDX.Core.ObjReader();
                var hair_models = reader.Read("Renderer/HairModel.obj");
                if (hair_models.Count > 0)
                {
                    HairModel = hair_models[0].Geometry;
                    FaceMaterial = new PhongMaterial()
                    {
                        DiffuseMap = new HelixToolkit.SharpDX.Core.DefaultTextureLoader().Load("Renderer/HairModel.obj", "Renderer/HairTexture.png", new HelixToolkit.Logger.DebugLogger())
                    };
                    //HairMaterial.DiffuseMap = new HelixToolkit.SharpDX.Core.DefaultTextureLoader().Load("Renderer/HairModel.obj", "Renderer/HairTexture.png", new HelixToolkit.Logger.NullLogger());


                }

                reader = new HelixToolkit.SharpDX.Core.ObjReader();
                var haircap_models = reader.Read("Renderer/HairCapModel.obj");
                if (haircap_models.Count > 0)
                {
                    HairCapModel = haircap_models[0].Geometry;
                    HairCapMaterial = new PhongMaterial()
                    {
                        AmbientColor = Color.White,
                        DiffuseColor = Color.White,
                        SpecularColor = Color.White,
                        SpecularShininess = 10f,
                        DiffuseAlphaMap = new HelixToolkit.SharpDX.Core.DefaultTextureLoader().Load("Renderer/HairCapModel.obj", "Renderer/HairCapTexture.png", new HelixToolkit.Logger.DebugLogger()),
                        DiffuseMap = new HelixToolkit.SharpDX.Core.DefaultTextureLoader().Load("Renderer/HairCapModel.obj", "Renderer/HairCapTexture.png", new HelixToolkit.Logger.DebugLogger())
                    };
                }
            }

            InstanceTranslation = new Matrix[1]
            {
                Matrix.Translation(0, 0, 0),
            };

        }
    }
}

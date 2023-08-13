//using SharpDX;
//using SharpDX.Direct3D;
//using SharpDX.Direct3D11;
//using SharpDX.DXGI;
//using SharpDX.Mathematics.Interop;
//using System;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;
//using System.Windows.Input;

//namespace FrostbiteModdingUI.Viewport
//{
//	public class DeferredRenderScreen2 : Screen
//	{
//		private struct ViewConstants
//		{
//			public Vector4 Time;

//			public Vector4 ScreenSize;

//			public SharpDX.Matrix ViewMatrix;

//			public Matrix ProjMatrix;

//			public Matrix ViewProjMatrix;

//			public Matrix CrViewProjMatrix;

//			public Matrix PrevViewProjMatrix;

//			public Matrix CrPrevViewProjMatrix;

//			public Matrix4x3 NormalBasisTransforms1;

//			public Matrix4x3 NormalBasisTransforms2;

//			public Matrix4x3 NormalBasisTransforms3;

//			public Matrix4x3 NormalBasisTransforms4;

//			public Matrix4x3 NormalBasisTransforms5;

//			public Matrix4x3 NormalBasisTransforms6;

//			public Vector4 ExposureMultipliers;

//			public Vector4 CameraPos;
//		}

//		private struct CommonConstants
//		{
//			public Matrix InvViewProjMatrix;

//			public Matrix InvProjMatrix;

//			public Vector4 CameraPos;

//			public Vector4 InvScreenSize;

//			public Vector4 ExposureMultipliers;

//			public Matrix4x3 NormalBasisTransforms1;

//			public Matrix4x3 NormalBasisTransforms2;

//			public Matrix4x3 NormalBasisTransforms3;

//			public Matrix4x3 NormalBasisTransforms4;

//			public Matrix4x3 NormalBasisTransforms5;

//			public Matrix4x3 NormalBasisTransforms6;

//			public Vector4 LightProbeIntensity;

//			public static float ComputeEV100(float aperture, float shutterTime, float ISO)
//			{
//				return (float)Math.Log(aperture * aperture / shutterTime * 100f / ISO, 2.0);
//			}

//			public static float ConvertEV100ToExposure(float EV100)
//			{
//				float num = 1.2f * (float)Math.Pow(2.0, EV100);
//				return 1f / num;
//			}

//			public static float ComputeEV100FromAvgLuminance(float avgLuminance)
//			{
//				return (float)Math.Log(avgLuminance * 100f / 12.5f, 2.0);
//			}

//			public static Vector2 ComputeExposure(float avgLuminance, float min, float max)
//			{
//				//IL_0020: Unknown result type (might be due to invalid IL or missing references)
//				//IL_003c: Unknown result type (might be due to invalid IL or missing references)
//				float num = ComputeEV100FromAvgLuminance(avgLuminance);
//				if (num < min)
//				{
//					num = min;
//				}
//				if (num > max)
//				{
//					num = max;
//				}
//				ConvertEV100ToExposure(num);
//				Vector2 result = default(Vector2);
//				result.X = avgLuminance;
//				result.Y = 1f / avgLuminance;
//				return result;
//			}
//		}

//		private struct LightConstants
//		{
//			public Vector4 LightPosAndInvSqrRadius;

//			public Vector4 LightColorAndIntensity;
//		}

//		private struct FunctionConstants
//		{
//			public Matrix WorldMatrix;

//			public Vector4 LightProbe1;

//			public Vector4 LightProbe2;

//			public Vector4 LightProbe3;

//			public Vector4 LightProbe4;

//			public Vector4 LightProbe5;

//			public Vector4 LightProbe6;

//			public Vector4 LightProbe7;

//			public Vector4 LightProbe8;

//			public Vector4 LightProbe9;
//		}

//		private struct CubeMapConstants
//		{
//			public int CubeFace;

//			public uint MipIndex;

//			public uint NumMips;

//			public uint Pad;
//		}

//		private struct TableLookupConstants
//		{
//			public float LutSize;

//			public float FlipY;

//			public Vector2 Pad;
//		}

//		protected List<MeshRenderInstance> meshes;

//		protected List<MeshRenderInstance> editorMeshes;

//		protected List<LightRenderInstance> lights;

//		protected GBufferCollection gBufferCollection;

//		protected TextureLibrary textureLibrary;

//		protected ShaderLibrary shaderLibrary;

//		private ConstantBuffer<ViewConstants> viewConstants;

//		private ConstantBuffer<FunctionConstants> functionConstants;

//		private ConstantBuffer<CommonConstants> commonConstants;

//		private ConstantBuffer<LightConstants> lightConstants;

//		private ConstantBuffer<CubeMapConstants> cubeMapConstants;

//		private ConstantBuffer<TableLookupConstants> lookupTableConstants;

//		private Buffer postProcessConstants;

//		private BindableTexture normalBasisCubemapTexture;

//		private BindableTexture lightAccumulationTexture;

//		private BindableTexture preintegratedDFGTexture;

//		private BindableCubeTexture preintegratedDLDTexture;

//		private BindableCubeTexture preintegratedSLDTexture;

//		private BindableTexture scaledSceneTexture;

//		private BindableTexture[] toneMapTextures = new BindableTexture[7];

//		private BindableTexture postProcessTexture;

//		private BindableTexture editorCompositeTexture;

//		private BindableDepthTexture editorCompositeDepthTexture;

//		private BindableTexture finalColorTexture;

//		private BindableDepthTexture selectionDepthTexture;

//		private BindableTexture selectionOutlineTexture;

//		private BindableTexture worldNormalsForHBAOTexture;

//		private BindableTexture brightPassTexture;

//		private BindableTexture blurTexture;

//		private BindableTexture bloomSourceTexture;

//		private BindableTexture[] bloomTextures = new BindableTexture[3];

//		private PixelShader psSunLight;

//		private PixelShader psPointLight;

//		private PixelShader psSphereLight;

//		private PixelShader psIntegrateDFG;

//		private PixelShader psIntegrateDiffuseLD;

//		private PixelShader psIntegrateSpecularLD;

//		private PixelShader psIBLRender;

//		private VertexShader vsFullscreenQuad;

//		private PixelShader psResolve;

//		private PixelShader psResolveDepthToMsaa;

//		private PixelShader psResolveWorldNormals;

//		private PixelShader psDownscale4x4;

//		private PixelShader psSampleLumInitial;

//		private PixelShader psSampleLumIterative;

//		private PixelShader psSampleLumFinal;

//		private PixelShader psCalcAdaptedLum;

//		private PixelShader psLookupTable;

//		private PixelShader psEditorComposite;

//		private PixelShader psSelectionOutline;

//		private PixelShader psDebugRenderMode;

//		private PixelShader psBrightPass;

//		private PixelShader psGaussianBlur5x5;

//		private PixelShader psDownSample2x2;

//		private PixelShader psBloomBlur;

//		private PixelShader psRenderBloom;

//		private IntPtr txaaContext;

//		private IntPtr txaaMotionVectorGenerator;

//		private BindableTexture txaaMotionVectorsTexture;

//		private BindableTexture txaaFeedbackTeture;

//		private GFSDK_ShadowLib.Context shadowContext;

//		private GFSDK_ShadowLib.Map shadowMapHandle;

//		private GFSDK_ShadowLib.Buffer shadowBufferHandle;

//		private ShaderResourceView shadowSRV;

//		private GFSDK_SSAO.Context hbaoContext;

//		public BaseCamera camera;

//		private ShaderResourceView distantLightProbe;

//		private ShaderResourceView defaultDistantLightProbe;

//		private bool bRecalculateLightProbe;

//		private MeshRenderShape skySphere;

//		private MeshRenderShape groundBox;

//		private MeshRenderShape gridPlane;

//		private Histogram luminanceHistogram = new Histogram(1);

//		private double totalTime;

//		private double lastDeltaTime;

//		private const float NearPlane = 0.1f;

//		private const float FarPlane = 1000000f;

//		public bool ShadowsEnabled;

//		public bool HBAOEnabled;

//		public bool TXAAEnabled;

//		protected RenderCreateState RenderCreateState => new RenderCreateState(base.Viewport.Device, textureLibrary, shaderLibrary);

//		public float CameraAperture
//		{
//			get;
//			set;
//		} = 16f;


//		public float CameraShutterSpeed
//		{
//			get;
//			set;
//		} = 0.01f;


//		public float CameraISO
//		{
//			get;
//			set;
//		} = 100f;


//		public Vector3 SunPosition
//		{
//			get;
//			set;
//		} = new Vector3(10f, 20f, 20f);


//		public float SunIntensity
//		{
//			get;
//			set;
//		} = 1000f;


//		public float SunAngularRadius
//		{
//			get;
//			set;
//		} = 0.029f;


//		public ShaderResourceView DistantLightProbe
//		{
//			get
//			{
//				return distantLightProbe;
//			}
//			set
//			{
//				distantLightProbe = value;
//				if (value == null)
//				{
//					distantLightProbe = defaultDistantLightProbe;
//				}
//				bRecalculateLightProbe = true;
//			}
//		}

//		public float LightProbeIntensity
//		{
//			get;
//			set;
//		} = 1f;


//		public ShaderResourceView LookupTable
//		{
//			get;
//			set;
//		}

//		public Vector4[] SHLightProbe
//		{
//			get;
//			set;
//		} = (Vector4[])(object)new Vector4[9];


//		public DebugRenderMode RenderMode
//		{
//			get;
//			set;
//		}

//		public bool GroundVisible
//		{
//			get;
//			set;
//		} = true;


//		public bool GridVisible
//		{
//			get;
//			set;
//		} = true;


//		public float MinEV100
//		{
//			get;
//			set;
//		} = 8f;


//		public float MaxEV100
//		{
//			get;
//			set;
//		} = 20f;


//		public int iDepthBias
//		{
//			get;
//			set;
//		} = 100;


//		public float fSlopeScaledDepthBias
//		{
//			get;
//			set;
//		} = 5f;


//		public float fDistanceBiasMin
//		{
//			get;
//			set;
//		} = 1E-08f;


//		public float fDistanceBiasFactor
//		{
//			get;
//			set;
//		} = 1E-08f;


//		public float fDistanceBiasThreshold
//		{
//			get;
//			set;
//		} = 700f;


//		public float fDistanceBiasPower
//		{
//			get;
//			set;
//		} = 0.3f;


//		public override void CreateSizeDependentBuffers()
//		{
//			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0120: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0154: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0166: Unknown result type (might be due to invalid IL or missing references)
//			//IL_019b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0223: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0228: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0230: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0235: Unknown result type (might be due to invalid IL or missing references)
//			//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
//			//IL_02db: Unknown result type (might be due to invalid IL or missing references)
//			//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
//			//IL_031c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_033e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_034f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0354: Unknown result type (might be due to invalid IL or missing references)
//			//IL_035c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0361: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0390: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03c3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03d5: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0448: Unknown result type (might be due to invalid IL or missing references)
//			//IL_046a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_047b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0480: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0488: Unknown result type (might be due to invalid IL or missing references)
//			//IL_048d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_04c8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_04ea: Unknown result type (might be due to invalid IL or missing references)
//			//IL_04fb: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0500: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0508: Unknown result type (might be due to invalid IL or missing references)
//			//IL_050d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0546: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0558: Unknown result type (might be due to invalid IL or missing references)
//			//IL_058d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0592: Unknown result type (might be due to invalid IL or missing references)
//			//IL_059a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_059f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_05ce: Unknown result type (might be due to invalid IL or missing references)
//			//IL_05e0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0615: Unknown result type (might be due to invalid IL or missing references)
//			//IL_061a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0622: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0627: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0656: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0668: Unknown result type (might be due to invalid IL or missing references)
//			//IL_069d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_06a2: Unknown result type (might be due to invalid IL or missing references)
//			//IL_06aa: Unknown result type (might be due to invalid IL or missing references)
//			//IL_06af: Unknown result type (might be due to invalid IL or missing references)
//			//IL_06de: Unknown result type (might be due to invalid IL or missing references)
//			//IL_06f0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0725: Unknown result type (might be due to invalid IL or missing references)
//			//IL_072a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0732: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0737: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0766: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0778: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07ad: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07b2: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07ba: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07bf: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07c3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07cc: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07d5: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07e1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07e7: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07f0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07f9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0802: Unknown result type (might be due to invalid IL or missing references)
//			//IL_080e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0814: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0833: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0845: Unknown result type (might be due to invalid IL or missing references)
//			//IL_087a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_087f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0887: Unknown result type (might be due to invalid IL or missing references)
//			//IL_088c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_08bb: Unknown result type (might be due to invalid IL or missing references)
//			//IL_08cd: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0902: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0907: Unknown result type (might be due to invalid IL or missing references)
//			//IL_090f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0914: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0918: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0921: Unknown result type (might be due to invalid IL or missing references)
//			//IL_092a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0933: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0941: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0943: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0948: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0951: Unknown result type (might be due to invalid IL or missing references)
//			//IL_095a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0963: Unknown result type (might be due to invalid IL or missing references)
//			//IL_096c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0982: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0984: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0989: Unknown result type (might be due to invalid IL or missing references)
//			//IL_09a8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_09ba: Unknown result type (might be due to invalid IL or missing references)
//			//IL_09ef: Unknown result type (might be due to invalid IL or missing references)
//			//IL_09f4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_09fc: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0a01: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0ac3: Unknown result type (might be due to invalid IL or missing references)
//			gBufferCollection = new GBufferCollection(base.Viewport.Device, base.Viewport.ViewportWidth, base.Viewport.ViewportHeight, new GBufferDescription
//			{
//				Format = (Format)24,
//				ClearColor = new Color4(0f, 0f, 0f, 0f),
//				DebugName = "GBufferA"
//			}, new GBufferDescription
//			{
//				Format = (Format)91,
//				ClearColor = new Color4(0f, 0f, 0f, 0f),
//				DebugName = "GBufferB"
//			}, new GBufferDescription
//			{
//				Format = (Format)87,
//				ClearColor = new Color4(0f, 0f, 0f, 0f),
//				DebugName = "GBufferC"
//			}, new GBufferDescription
//			{
//				Format = (Format)10,
//				ClearColor = new Color4(0f, 0f, 0f, 0f),
//				DebugName = "GBufferD"
//			});
//			Device device = base.Viewport.Device;
//			Texture2DDescription val = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Format = (Format)28,
//				Width = base.Viewport.ViewportWidth,
//				Height = base.Viewport.ViewportHeight,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			finalColorTexture = new BindableTexture(device, val, srv: true, rtv: true);
//			Device device2 = base.Viewport.Device;
//			val = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Format = (Format)10,
//				Width = base.Viewport.ViewportWidth,
//				Height = base.Viewport.ViewportHeight,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			lightAccumulationTexture = new BindableTexture(device2, val, srv: true, rtv: true);
//			int num = (base.Viewport.ViewportWidth - base.Viewport.ViewportWidth % 8) / 4;
//			if (num < 1)
//			{
//				num = 1;
//			}
//			int num2 = (base.Viewport.ViewportHeight - base.Viewport.ViewportHeight % 8) / 4;
//			if (num2 < 1)
//			{
//				num2 = 1;
//			}
//			Device device3 = base.Viewport.Device;
//			val = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Width = num,
//				Height = num2,
//				Format = (Format)10,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			scaledSceneTexture = new BindableTexture(device3, val, srv: true, rtv: true);
//			Device device4 = base.Viewport.Device;
//			val = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Width = num,
//				Height = num2,
//				Format = (Format)28,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			brightPassTexture = new BindableTexture(device4, val, srv: true, rtv: true);
//			Device device5 = base.Viewport.Device;
//			val = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Width = num,
//				Height = num2,
//				Format = (Format)28,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			blurTexture = new BindableTexture(device5, val, srv: true, rtv: true);
//			num = (base.Viewport.ViewportWidth - base.Viewport.ViewportWidth % 8) / 8;
//			if (num < 1)
//			{
//				num = 1;
//			}
//			num2 = (base.Viewport.ViewportHeight - base.Viewport.ViewportHeight % 8) / 8;
//			if (num2 < 1)
//			{
//				num2 = 1;
//			}
//			Device device6 = base.Viewport.Device;
//			val = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Width = num,
//				Height = num2,
//				Format = (Format)28,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			bloomSourceTexture = new BindableTexture(device6, val, srv: true, rtv: true);
//			for (int i = 0; i < 3; i++)
//			{
//				BindableTexture[] array = bloomTextures;
//				int num3 = i;
//				Device device7 = base.Viewport.Device;
//				val = new Texture2DDescription
//				{
//					ArraySize = 1,
//					Width = num,
//					Height = num2,
//					Format = (Format)28,
//					MipLevels = 1,
//					SampleDescription = new SampleDescription(1, 0),
//					Usage = (ResourceUsage)0
//				};
//				array[num3] = new BindableTexture(device7, val, srv: true, rtv: true);
//			}
//			Device device8 = base.Viewport.Device;
//			val = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Format = (Format)10,
//				Width = base.Viewport.ViewportWidth,
//				Height = base.Viewport.ViewportHeight,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			postProcessTexture = new BindableTexture(device8, val, srv: true, rtv: true);
//			Device device9 = base.Viewport.Device;
//			val = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Format = (Format)34,
//				Width = base.Viewport.ViewportWidth,
//				Height = base.Viewport.ViewportHeight,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			txaaMotionVectorsTexture = new BindableTexture(device9, val, srv: true, rtv: true);
//			Device device10 = base.Viewport.Device;
//			val = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Format = (Format)10,
//				Width = base.Viewport.ViewportWidth,
//				Height = base.Viewport.ViewportHeight,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			txaaFeedbackTeture = new BindableTexture(device10, val, srv: true, rtv: false);
//			Device device11 = base.Viewport.Device;
//			val = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Format = (Format)28,
//				Width = base.Viewport.ViewportWidth,
//				Height = base.Viewport.ViewportHeight,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(4, 0),
//				Usage = (ResourceUsage)0
//			};
//			editorCompositeTexture = new BindableTexture(device11, val, srv: true, rtv: true);
//			Device device12 = base.Viewport.Device;
//			val = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Format = (Format)44,
//				Width = base.Viewport.ViewportWidth,
//				Height = base.Viewport.ViewportHeight,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(4, 0),
//				Usage = (ResourceUsage)0
//			};
//			Texture2DDescription description = val;
//			DepthStencilViewDescription value = new DepthStencilViewDescription
//			{
//				Dimension = (DepthStencilViewDimension)5,
//				Format = (Format)45,
//				Texture2DMS = default(Texture2DMultisampledResource)
//			};
//			DepthStencilViewDescription? dsvDesc = value;
//			ShaderResourceViewDescription value2 = new ShaderResourceViewDescription
//			{
//				Dimension = (ShaderResourceViewDimension)6,
//				Format = (Format)46,
//				Texture2DMS = default(Texture2DMultisampledResource)
//			};
//			editorCompositeDepthTexture = new BindableDepthTexture(device12, description, srv: true, dsvDesc, value2);
//			Device device13 = base.Viewport.Device;
//			val = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Format = (Format)28,
//				Width = base.Viewport.ViewportWidth,
//				Height = base.Viewport.ViewportHeight,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			selectionOutlineTexture = new BindableTexture(device13, val, srv: true, rtv: true);
//			Device device14 = base.Viewport.Device;
//			val = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Format = (Format)44,
//				Width = base.Viewport.ViewportWidth,
//				Height = base.Viewport.ViewportHeight,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			Texture2DDescription description2 = val;
//			value = new DepthStencilViewDescription
//			{
//				Dimension = (DepthStencilViewDimension)3,
//				Format = (Format)45,
//				Texture2D = new Texture2DResource
//				{
//					MipSlice = 0
//				}
//			};
//			DepthStencilViewDescription? dsvDesc2 = value;
//			value2 = new ShaderResourceViewDescription
//			{
//				Dimension = (ShaderResourceViewDimension)6,
//				Format = (Format)46,
//				Texture2D = new Texture2DResource
//				{
//					MipLevels = 1,
//					MostDetailedMip = 0
//				}
//			};
//			selectionDepthTexture = new BindableDepthTexture(device14, description2, srv: true, dsvDesc2, value2);
//			Device device15 = base.Viewport.Device;
//			val = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Format = (Format)28,
//				Width = base.Viewport.ViewportWidth,
//				Height = base.Viewport.ViewportHeight,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			worldNormalsForHBAOTexture = new BindableTexture(device15, val, srv: true, rtv: true);
//			if (ShadowsEnabled)
//			{
//				GFSDK_ShadowLib.InitSizeDependent(shadowContext, base.Viewport.ViewportWidth, base.Viewport.ViewportHeight, ref shadowMapHandle, ref shadowBufferHandle);
//			}
//			if (camera != null)
//			{
//				camera.SetProjParams((float)Math.PI / 4f, (float)base.Viewport.ViewportWidth / (float)base.Viewport.ViewportHeight, 0.1f, 1000000f);
//			}
//			if (toneMapTextures[5] != null)
//			{
//				toneMapTextures[5].Clear(base.Viewport.Context, new Color4(0.00177f, 0f, 0f, 0f));
//			}
//		}

//		public override void CreateBuffers()
//		{
//			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0148: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0154: Unknown result type (might be due to invalid IL or missing references)
//			//IL_015c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0178: Unknown result type (might be due to invalid IL or missing references)
//			//IL_017d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_017f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0189: Expected O, but got Unknown
//			//IL_043a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_044c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0475: Unknown result type (might be due to invalid IL or missing references)
//			//IL_047a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0482: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0487: Unknown result type (might be due to invalid IL or missing references)
//			//IL_04b7: Unknown result type (might be due to invalid IL or missing references)
//			//IL_04c9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_04eb: Unknown result type (might be due to invalid IL or missing references)
//			//IL_04f4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_04f9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0501: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0506: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0536: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0548: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0571: Unknown result type (might be due to invalid IL or missing references)
//			//IL_057a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_057f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0587: Unknown result type (might be due to invalid IL or missing references)
//			//IL_058c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_05de: Unknown result type (might be due to invalid IL or missing references)
//			//IL_05f0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0609: Unknown result type (might be due to invalid IL or missing references)
//			//IL_060e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0616: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0623: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0676: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0694: Unknown result type (might be due to invalid IL or missing references)
//			//IL_06a6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_06bf: Unknown result type (might be due to invalid IL or missing references)
//			//IL_06c4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_06cc: Unknown result type (might be due to invalid IL or missing references)
//			//IL_06e0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_06e5: Unknown result type (might be due to invalid IL or missing references)
//			//IL_079f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07b1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07d1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07da: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07df: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07e7: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07ec: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0877: Unknown result type (might be due to invalid IL or missing references)
//			ShadowsEnabled = Config.Get("Render", "ShadowsEnabled", defaultValue: true);
//			HBAOEnabled = Config.Get("Render", "HBAOEnabled", defaultValue: true);
//			TXAAEnabled = Config.Get("Render", "TXAAEnabled", defaultValue: true);
//			textureLibrary = new TextureLibrary(base.Viewport.Device);
//			shaderLibrary = new ShaderLibrary(Shader.CreateFallback(base.Viewport.Device));
//			viewConstants = new ConstantBuffer<ViewConstants>(base.Viewport.Device, default(ViewConstants));
//			functionConstants = new ConstantBuffer<FunctionConstants>(base.Viewport.Device, default(FunctionConstants));
//			commonConstants = new ConstantBuffer<CommonConstants>(base.Viewport.Device, default(CommonConstants));
//			lightConstants = new ConstantBuffer<LightConstants>(base.Viewport.Device, default(LightConstants));
//			cubeMapConstants = new ConstantBuffer<CubeMapConstants>(base.Viewport.Device, default(CubeMapConstants));
//			lookupTableConstants = new ConstantBuffer<TableLookupConstants>(base.Viewport.Device, default(TableLookupConstants));
//			postProcessConstants = (Buffer)(object)new Buffer(base.Viewport.Device, new BufferDescription
//			{
//				BindFlags = (BindFlags)4,
//				CpuAccessFlags = (CpuAccessFlags)65536,
//				OptionFlags = (ResourceOptionFlags)0,
//				SizeInBytes = 512,
//				StructureByteStride = 0,
//				Usage = (ResourceUsage)2
//			});
//			psSunLight = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "SunLight");
//			psPointLight = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "PointLight");
//			psSphereLight = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "SphereLight");
//			vsFullscreenQuad = FrostyShaderDb.GetShader<VertexShader>(base.Viewport.Device, "FullscreenQuad");
//			psResolve = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "Resolve");
//			psResolveDepthToMsaa = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "ResolveDepthToMsaa");
//			psResolveWorldNormals = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "ResolveWorldNormals");
//			psIntegrateDFG = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "IBL_IntegrateDFG");
//			psIntegrateDiffuseLD = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "IBL_IntegrateDiffuseLD");
//			psIntegrateSpecularLD = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "IBL_IntegrateSpecularLD");
//			psIBLRender = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "IBL_Main");
//			psDownscale4x4 = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "DownScale4x4");
//			psSampleLumInitial = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "SampleLumInitial");
//			psSampleLumIterative = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "SampleLumIterative");
//			psSampleLumFinal = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "SampleLumFinal");
//			psCalcAdaptedLum = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "CalculateAdaptedLum");
//			psLookupTable = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "LookupTable");
//			psEditorComposite = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "EditorComposite");
//			psSelectionOutline = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "SelectionOutline");
//			psDebugRenderMode = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "DebugRenderMode");
//			psBrightPass = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "BrightPass");
//			psGaussianBlur5x5 = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "GaussianBlur5x5");
//			psDownSample2x2 = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "DownSample2x2");
//			psBloomBlur = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "BloomBlur");
//			psRenderBloom = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "RenderBloom");
//			Device device = base.Viewport.Device;
//			Texture2DDescription description = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Format = (Format)10,
//				Width = 128,
//				Height = 128,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			preintegratedDFGTexture = new BindableTexture(device, description, srv: true, rtv: true);
//			Device device2 = base.Viewport.Device;
//			description = new Texture2DDescription
//			{
//				ArraySize = 6,
//				Format = (Format)10,
//				Height = 32,
//				Width = 32,
//				MipLevels = 1,
//				OptionFlags = (ResourceOptionFlags)4,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			preintegratedDLDTexture = new BindableCubeTexture(device2, description, srv: true, rtv: true);
//			Device device3 = base.Viewport.Device;
//			description = new Texture2DDescription
//			{
//				ArraySize = 6,
//				Format = (Format)10,
//				Height = 256,
//				Width = 256,
//				MipLevels = 9,
//				OptionFlags = (ResourceOptionFlags)4,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			preintegratedSLDTexture = new BindableCubeTexture(device3, description, srv: true, rtv: true);
//			int num = 0;
//			for (int i = 0; i < 6; i++)
//			{
//				num = 1 << 2 * i;
//				if (i >= 4)
//				{
//					num = 1;
//				}
//				BindableTexture[] array = toneMapTextures;
//				int num2 = i;
//				Device device4 = base.Viewport.Device;
//				description = new Texture2DDescription
//				{
//					ArraySize = 1,
//					Format = (Format)41,
//					Height = num,
//					MipLevels = 1,
//					SampleDescription = new SampleDescription(1, 0),
//					Usage = (ResourceUsage)0,
//					Width = num
//				};
//				array[num2] = new BindableTexture(device4, description, srv: true, rtv: true);
//			}
//			toneMapTextures[5].Clear(base.Viewport.Context, new Color4(0.00177f, 0f, 0f, 0f));
//			BindableTexture[] array2 = toneMapTextures;
//			Device device5 = base.Viewport.Device;
//			description = new Texture2DDescription
//			{
//				ArraySize = 1,
//				Format = (Format)41,
//				Height = num,
//				MipLevels = 1,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)3,
//				Width = num,
//				CpuAccessFlags = (CpuAccessFlags)131072
//			};
//			array2[6] = new BindableTexture(device5, description, srv: false, rtv: false);
//			if (TXAAEnabled)
//			{
//				GFSDK_TXAA.Init(base.Viewport.Device, ref txaaContext, ref txaaMotionVectorGenerator);
//			}
//			if (ShadowsEnabled)
//			{
//				GFSDK_ShadowLib.Init(base.Viewport.Device, base.Viewport.Context, base.Viewport.ViewportWidth, base.Viewport.ViewportHeight, ref shadowContext, ref shadowMapHandle, ref shadowBufferHandle);
//			}
//			if (HBAOEnabled)
//			{
//				GFSDK_SSAO.Init(base.Viewport.Device, ref hbaoContext);
//			}
//			Device device6 = base.Viewport.Device;
//			description = new Texture2DDescription
//			{
//				ArraySize = 6,
//				Format = (Format)87,
//				Height = 1,
//				MipLevels = 1,
//				Width = 1,
//				OptionFlags = (ResourceOptionFlags)4,
//				SampleDescription = new SampleDescription(1, 0),
//				Usage = (ResourceUsage)0
//			};
//			normalBasisCubemapTexture = new BindableTexture(device6, description, srv: true, rtv: false);
//			GCHandle gCHandle = GCHandle.Alloc(new uint[6]
//			{
//				0u,
//				16843009u,
//				33686018u,
//				50529027u,
//				67372036u,
//				84215045u
//			}, GCHandleType.Pinned);
//			DataBox val = default(DataBox);
//			for (int j = 0; j < 6; j++)
//			{
//				//int num3 = 0;
//				//int num4 = ((Resource)normalBasisCubemapTexture.Texture).CalculateSubResourceIndex(0, j, ref num3);
//				//IntPtr intPtr = gCHandle.AddrOfPinnedObject();
//				//intPtr += j * 4;
//				//((DataBox)(ref val))._002Ector(intPtr, num3, 0);
//				//base.Viewport.Device.get_ImmediateContext().UpdateSubresource(val, (Resource)(object)normalBasisCubemapTexture.Texture, num4);
//			}
//			gCHandle.Free();
//			skySphere = MeshRenderShape.CreateSphere(RenderCreateState, "SkySphere", "Skybox", 200000f, 32);
//			groundBox = MeshRenderShape.CreateCube(RenderCreateState, "GroundBox", "GroundPlane", 1, 1, 1);
//			gridPlane = MeshRenderShape.CreatePlane(RenderCreateState, "Grid", "Grid", 1, 1);
//			defaultDistantLightProbe = textureLibrary.LoadTextureAsset("Resources/Textures/DefaultLightProbe.dds", generateMips: true);
//			DistantLightProbe = defaultDistantLightProbe;
//			if (camera != null)
//			{
//				camera.SetProjParams((float)Math.PI / 4f, (float)base.Viewport.ViewportWidth / (float)base.Viewport.ViewportHeight, 0.1f, 1000000f);
//			}
//		}

//		public override void Update(double timestep)
//		{
//			//GFSDK_TXAA.Update(base.Viewport.ViewportWidth, base.Viewport.ViewportHeight);
//			camera.FrameMove((float)timestep);
//			totalTime += timestep;
//			lastDeltaTime = timestep;
//		}

//		public override void Render()
//		{
//			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
//			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0103: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
//			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0208: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0209: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0210: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0211: Unknown result type (might be due to invalid IL or missing references)
//			//IL_021e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0232: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0237: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0243: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0248: Unknown result type (might be due to invalid IL or missing references)
//			//IL_028b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0290: Unknown result type (might be due to invalid IL or missing references)
//			//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
//			//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
//			//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_033a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_033f: Unknown result type (might be due to invalid IL or missing references)
//			//GFSDK_TXAA.TxaaEnabled = (RenderMode == DebugRenderMode.Default && TXAAEnabled);
//			BeginFrameActions();
//			meshes = CollectMeshInstances();
//			lights = CollectLightInstances();
//			MeshRenderInstance item;
//			if (GroundVisible)
//			{
//				List<MeshRenderInstance> list = meshes;
//				item = new MeshRenderInstance
//				{
//					RenderMesh = groundBox,
//					Transform = Matrix.Scaling(8f, 0.25f, 8f) * Matrix.Translation(0f, -0.125f, 0f)
//				};
//				list.Add(item);
//			}
//			editorMeshes = new List<MeshRenderInstance>();
//			if (GridVisible)
//			{
//				List<MeshRenderInstance> list2 = editorMeshes;
//				item = new MeshRenderInstance
//				{
//					RenderMesh = gridPlane,
//					Transform = Matrix.Translation(0f, GroundVisible ? (-0.125f) : 0f, 0f)
//				};
//				list2.Add(item);
//			}
//			//GFSDK_TXAA.GetJitter(out float[] _);
//			UpdateViewConstants(bJitter: true);
//			Matrix projMatrix = camera.GetProjMatrix();
//			Matrix viewProjMatrix = camera.GetViewProjMatrix();
//			((Matrix)(projMatrix)).Invert();
//			((Matrix)(projMatrix)).Transpose();
//			((Matrix)(viewProjMatrix)).Invert();
//			((Matrix)(viewProjMatrix)).Transpose();
//			Matrix4x3[] array = new Matrix4x3[6]
//			{
//				new Matrix4x3(new float[12]
//				{
//					0f,
//					0f,
//					-1f,
//					0f,
//					0f,
//					-1f,
//					0f,
//					0f,
//					-1f,
//					0f,
//					0f,
//					0f
//				}),
//				new Matrix4x3(new float[12]
//				{
//					0f,
//					0f,
//					1f,
//					0f,
//					0f,
//					-1f,
//					0f,
//					0f,
//					1f,
//					0f,
//					0f,
//					0f
//				}),
//				new Matrix4x3(new float[12]
//				{
//					-1f,
//					0f,
//					0f,
//					0f,
//					0f,
//					0f,
//					1f,
//					0f,
//					0f,
//					1f,
//					0f,
//					0f
//				}),
//				new Matrix4x3(new float[12]
//				{
//					-1f,
//					0f,
//					0f,
//					0f,
//					0f,
//					0f,
//					-1f,
//					0f,
//					0f,
//					-1f,
//					0f,
//					0f
//				}),
//				new Matrix4x3(new float[12]
//				{
//					-1f,
//					0f,
//					0f,
//					0f,
//					0f,
//					-1f,
//					0f,
//					0f,
//					0f,
//					0f,
//					1f,
//					0f
//				}),
//				new Matrix4x3(new float[12]
//				{
//					1f,
//					0f,
//					0f,
//					0f,
//					0f,
//					-1f,
//					0f,
//					0f,
//					0f,
//					0f,
//					-1f,
//					0f
//				})
//			};
//			commonConstants.UpdateData(base.Viewport.Context, new CommonConstants
//			{
//				InvViewProjMatrix = viewProjMatrix,
//				InvProjMatrix = projMatrix,
//				CameraPos = new Vector4(camera.GetEyePt() * new Vector3(-1f, 1f, 1f), (float)RenderMode),
//				InvScreenSize = new Vector4(1f / (float)base.Viewport.ViewportWidth, 1f / (float)base.Viewport.ViewportHeight, (float)base.Viewport.ViewportWidth, (float)base.Viewport.ViewportHeight),
//				ExposureMultipliers = new Vector4(CommonConstants.ComputeExposure(luminanceHistogram.GetAverage(), MinEV100, MaxEV100), MinEV100, MaxEV100),
//				NormalBasisTransforms1 = array[0],
//				NormalBasisTransforms2 = array[1],
//				NormalBasisTransforms3 = array[2],
//				NormalBasisTransforms4 = array[3],
//				NormalBasisTransforms5 = array[4],
//				NormalBasisTransforms6 = array[5],
//				LightProbeIntensity = new Vector4(LightProbeIntensity, 0f, 0f, 0f)
//			});
//			ClearRenderTargets();
//			if (bRecalculateLightProbe)
//			{
//				PreintegrateIBL();
//				CalculateSphericalHarmonics();
//				bRecalculateLightProbe = false;
//			}
//			RenderBasePass();
//			RenderShadows();
//			RenderLights();
//			RenderIBL();
//			ResolveNormalsForHBAO();
//			RenderEmissive();
//			PostProcess();
//			Resolve();
//			EndFrameActions();
//		}

//		public virtual List<MeshRenderInstance> CollectMeshInstances()
//		{
//			return new List<MeshRenderInstance>();
//		}

//		public virtual List<LightRenderInstance> CollectLightInstances()
//		{
//			return new List<LightRenderInstance>();
//		}

//		private void UpdateViewConstants(bool bJitter)
//		{
//			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0113: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0118: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0152: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0157: Unknown result type (might be due to invalid IL or missing references)
//			//IL_019a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_019f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01af: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01be: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0232: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0243: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0248: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0255: Unknown result type (might be due to invalid IL or missing references)
//			//IL_025f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0264: Unknown result type (might be due to invalid IL or missing references)
//			Matrix4x3[] array = new Matrix4x3[6]
//			{
//				new Matrix4x3(new float[12]
//				{
//					0f,
//					0f,
//					1f,
//					0f,
//					0f,
//					-1f,
//					0f,
//					0f,
//					-1f,
//					0f,
//					0f,
//					0f
//				}),
//				new Matrix4x3(new float[12]
//				{
//					0f,
//					0f,
//					1f,
//					0f,
//					0f,
//					-1f,
//					0f,
//					0f,
//					1f,
//					0f,
//					0f,
//					0f
//				}),
//				new Matrix4x3(new float[12]
//				{
//					1f,
//					0f,
//					0f,
//					0f,
//					0f,
//					0f,
//					1f,
//					0f,
//					0f,
//					1f,
//					0f,
//					0f
//				}),
//				new Matrix4x3(new float[12]
//				{
//					1f,
//					0f,
//					0f,
//					0f,
//					0f,
//					0f,
//					1f,
//					0f,
//					0f,
//					-1f,
//					0f,
//					0f
//				}),
//				new Matrix4x3(new float[12]
//				{
//					1f,
//					0f,
//					0f,
//					0f,
//					0f,
//					-1f,
//					0f,
//					0f,
//					0f,
//					0f,
//					1f,
//					0f
//				}),
//				new Matrix4x3(new float[12]
//				{
//					-1f,
//					0f,
//					0f,
//					0f,
//					0f,
//					-1f,
//					0f,
//					0f,
//					0f,
//					0f,
//					1f,
//					0f
//				})
//			};
//			Matrix viewMatrix = camera.GetViewMatrix();
//			((Matrix)(ref viewMatrix)).Transpose();
//			Matrix projMatrix = camera.GetProjMatrix();
//			((Matrix)(ref projMatrix)).Transpose();
//			Matrix viewProjMatrix = camera.GetViewProjMatrix();
//			((Matrix)(ref viewProjMatrix)).Transpose();
//			Matrix crViewProjMatrix = camera.GetCrViewProjMatrix();
//			if (bJitter)
//			{
//				GFSDK_TXAA.GetJitter(out float[] outJitter);
//				crViewProjMatrix = camera.GetCrViewProjMatrix(outJitter);
//			}
//			((Matrix)(ref crViewProjMatrix)).Transpose();
//			viewConstants.UpdateData(base.Viewport.Context, new ViewConstants
//			{
//				Time = new Vector4((float)totalTime, 0f, 0f, 0f),
//				ScreenSize = new Vector4((float)base.Viewport.ViewportWidth, (float)base.Viewport.ViewportHeight, 1f / (float)base.Viewport.ViewportWidth, 1f / (float)base.Viewport.ViewportHeight),
//				ViewMatrix = viewMatrix,
//				ProjMatrix = projMatrix,
//				ViewProjMatrix = viewProjMatrix,
//				CrViewProjMatrix = crViewProjMatrix,
//				NormalBasisTransforms1 = array[0],
//				NormalBasisTransforms2 = array[1],
//				NormalBasisTransforms3 = array[2],
//				NormalBasisTransforms4 = array[3],
//				NormalBasisTransforms5 = array[4],
//				NormalBasisTransforms6 = array[5],
//				ExposureMultipliers = new Vector4(CommonConstants.ComputeExposure(luminanceHistogram.GetAverage(), MinEV100, MaxEV100), MinEV100, MaxEV100),
//				CameraPos = new Vector4(camera.GetEyePt(), 1f)
//			});
//		}

//		public override void MouseMove(int x, int y)
//		{
//			camera?.MouseMove(x, y);
//		}

//		public override void MouseDown(int x, int y, MouseButton button)
//		{
//			camera?.MouseButtonDown(x, y, button);
//		}

//		public override void MouseUp(int x, int y, MouseButton button)
//		{
//			camera?.MouseButtonUp(button);
//		}

//		public override void MouseScroll(int delta)
//		{
//			camera?.MouseWheel(delta);
//		}

//		public override void KeyDown(int key)
//		{
//			camera?.KeyDown((Key)key);
//		}

//		public override void KeyUp(int key)
//		{
//			camera?.KeyUp((Key)key);
//		}

//		public override void DisposeSizeDependentBuffers()
//		{
//			gBufferCollection.Dispose();
//			lightAccumulationTexture.Dispose();
//			scaledSceneTexture.Dispose();
//			postProcessTexture.Dispose();
//			txaaFeedbackTeture.Dispose();
//			txaaMotionVectorsTexture.Dispose();
//			editorCompositeDepthTexture.Dispose();
//			editorCompositeTexture.Dispose();
//			finalColorTexture.Dispose();
//			selectionDepthTexture.Dispose();
//			worldNormalsForHBAOTexture.Dispose();
//			brightPassTexture.Dispose();
//			blurTexture.Dispose();
//			bloomSourceTexture.Dispose();
//			BindableTexture[] array = bloomTextures;
//			for (int i = 0; i < array.Length; i++)
//			{
//				array[i].Dispose();
//			}
//			if (ShadowsEnabled)
//			{
//				shadowContext.RemoveMap(ref shadowMapHandle);
//				shadowContext.RemoveBuffer(ref shadowBufferHandle);
//			}
//		}

//		public override void DisposeBuffers()
//		{
//			textureLibrary.Dispose();
//			shaderLibrary.Dispose();
//			viewConstants.Dispose();
//			functionConstants.Dispose();
//			commonConstants.Dispose();
//			lightConstants.Dispose();
//			((DisposeBase)postProcessConstants).Dispose();
//			cubeMapConstants.Dispose();
//			lookupTableConstants.Dispose();
//			((DisposeBase)psPointLight).Dispose();
//			((DisposeBase)psSunLight).Dispose();
//			((DisposeBase)psSphereLight).Dispose();
//			((DisposeBase)vsFullscreenQuad).Dispose();
//			((DisposeBase)psResolve).Dispose();
//			((DisposeBase)psResolveDepthToMsaa).Dispose();
//			((DisposeBase)psResolveWorldNormals).Dispose();
//			((DisposeBase)psIntegrateDFG).Dispose();
//			((DisposeBase)psIntegrateDiffuseLD).Dispose();
//			((DisposeBase)psIntegrateSpecularLD).Dispose();
//			((DisposeBase)psIBLRender).Dispose();
//			((DisposeBase)psDownscale4x4).Dispose();
//			((DisposeBase)psSampleLumInitial).Dispose();
//			((DisposeBase)psSampleLumIterative).Dispose();
//			((DisposeBase)psSampleLumFinal).Dispose();
//			((DisposeBase)psCalcAdaptedLum).Dispose();
//			((DisposeBase)psLookupTable).Dispose();
//			((DisposeBase)psEditorComposite).Dispose();
//			((DisposeBase)psSelectionOutline).Dispose();
//			((DisposeBase)psDebugRenderMode).Dispose();
//			((DisposeBase)psBrightPass).Dispose();
//			((DisposeBase)psGaussianBlur5x5).Dispose();
//			((DisposeBase)psDownSample2x2).Dispose();
//			((DisposeBase)psBloomBlur).Dispose();
//			((DisposeBase)psRenderBloom).Dispose();
//			normalBasisCubemapTexture.Dispose();
//			preintegratedDFGTexture.Dispose();
//			preintegratedDLDTexture.Dispose();
//			preintegratedSLDTexture.Dispose();
//			for (int i = 0; i < 7; i++)
//			{
//				toneMapTextures[i].Dispose();
//			}
//			if (TXAAEnabled)
//			{
//				GFSDK_TXAA.Destroy(ref txaaContext, ref txaaMotionVectorGenerator);
//			}
//			if (HBAOEnabled)
//			{
//				hbaoContext.Release();
//			}
//			if (ShadowsEnabled)
//			{
//				shadowContext.Destroy();
//			}
//			skySphere.Dispose();
//			groundBox.Dispose();
//			gridPlane.Dispose();
//		}

//		private void CalculateSphericalHarmonics()
//		{
//			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
//			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
//			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00e7: Expected O, but got Unknown
//			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
//			//IL_010e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0116: Expected O, but got Unknown
//			//IL_0122: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0129: Expected O, but got Unknown
//			//IL_01af: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_021f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0224: Unknown result type (might be due to invalid IL or missing references)
//			//IL_028a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0290: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03af: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03c5: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0421: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0426: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0484: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0486: Unknown result type (might be due to invalid IL or missing references)
//			//IL_048b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
//			//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
//			//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0510: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0512: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0517: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0556: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0558: Unknown result type (might be due to invalid IL or missing references)
//			//IL_055d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_060c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_065f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_067c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0698: Unknown result type (might be due to invalid IL or missing references)
//			//IL_06c7: Unknown result type (might be due to invalid IL or missing references)
//			//IL_06e9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0765: Unknown result type (might be due to invalid IL or missing references)
//			//IL_076a: Unknown result type (might be due to invalid IL or missing references)
//			if (DistantLightProbe == null)
//			{
//				return;
//			}
//			RawViewportF[] viewports = base.Viewport.Context.get_Rasterizer().GetViewports<RawViewportF>();
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "Spherical Harmonics");
//			PixelShader shader = FrostyShaderDb.GetShader<PixelShader>(base.Viewport.Device, "ResolveCubeMapFace");
//			Texture2DDescription val = default(Texture2DDescription);
//			val.ArraySize = 1;
//			val.BindFlags = (BindFlags)32;
//			val.CpuAccessFlags = (CpuAccessFlags)0;
//			val.Format = (Format)10;
//			val.Height = preintegratedSLDTexture.Texture.get_Description().Height;
//			val.MipLevels = 1;
//			val.OptionFlags = (ResourceOptionFlags)0;
//			val.SampleDescription = new SampleDescription(1, 0);
//			val.Usage = (ResourceUsage)0;
//			val.Width = preintegratedSLDTexture.Texture.get_Description().Width;
//			Texture2DDescription val2 = val;
//			Texture2D val3 = (Texture2D)(object)new Texture2D(base.Viewport.Device, val2);
//			val2.CpuAccessFlags = (CpuAccessFlags)131072;
//			val2.BindFlags = (BindFlags)0;
//			val2.Usage = (ResourceUsage)3;
//			Texture2D val4 = (Texture2D)(object)new Texture2D(base.Viewport.Device, val2);
//			RenderTargetView val5 = (RenderTargetView)(object)new RenderTargetView(base.Viewport.Device, (Resource)(object)val3);
//			float[] array = new float[9];
//			float[] array2 = new float[9];
//			float[] array3 = new float[9];
//			float[] array4 = new float[9];
//			float num = 0f;
//			DataStream val6 = default(DataStream);
//			Vector3 val8 = default(Vector3);
//			for (int i = 0; i < 6; i++)
//			{
//				cubeMapConstants.UpdateData(base.Viewport.Context, new CubeMapConstants
//				{
//					CubeFace = i
//				});
//				base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, val5);
//				base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, val2.Width, val2.Height)));
//				base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//				base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//				base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//				base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//				base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//				base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//				((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//				((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(shader);
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffer(0, cubeMapConstants.Buffer);
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResource(0, preintegratedSLDTexture.SRV);
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//				base.Viewport.Context.Draw(6, 0);
//				base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, (RenderTargetView[])(object)new RenderTargetView[0]);
//				base.Viewport.Context.CopyResource((Resource)(object)val3, (Resource)(object)val4);
//				base.Viewport.Context.MapSubresource((Resource)(object)val4, 0, (MapMode)1, (MapFlags)0, ref val6);
//				float num2 = 1f / (float)preintegratedSLDTexture.Texture.get_Description().Width;
//				float num3 = -1f + num2;
//				float num4 = 2f / (float)preintegratedSLDTexture.Texture.get_Description().Width;
//				for (int j = 0; j < preintegratedSLDTexture.Texture.get_Description().Height; j++)
//				{
//					float num5 = num3 + (float)j * num4;
//					for (int k = 0; k < preintegratedSLDTexture.Texture.get_Description().Width; k++)
//					{
//						float num6 = num3 + (float)k * num4;
//						Vector3 val7 = Vector3.Zero;
//						switch (i)
//						{
//							case 0:
//								val7.X = 1f;
//								val7.Y = 1f - (num4 * (float)j + num2);
//								val7.Z = 1f - (num4 * (float)k + num2);
//								val7 = -val7;
//								break;
//							case 1:
//								val7.X = -1f;
//								val7.Y = 1f - (num4 * (float)j + num2);
//								val7.Z = -1f + (num4 * (float)k + num2);
//								val7 = -val7;
//								break;
//							case 2:
//								val7.X = -1f + (num4 * (float)k + num2);
//								val7.Y = 1f;
//								val7.Z = -1f + (num4 * (float)j + num2);
//								val7 = -val7;
//								break;
//							case 3:
//								val7.X = -1f + (num4 * (float)k + num2);
//								val7.Y = -1f;
//								val7.Z = 1f - (num4 * (float)j + num2);
//								val7 = -val7;
//								break;
//							case 4:
//								val7.X = -1f + (num4 * (float)k + num2);
//								val7.Y = 1f - (num4 * (float)j + num2);
//								val7.Z = 1f;
//								break;
//							case 5:
//								val7.X = 1f - (num4 * (float)k + num2);
//								val7.Y = 1f - (num4 * (float)j + num2);
//								val7.Z = -1f;
//								break;
//						}
//						((Vector3)(ref val7)).Normalize();
//						float num7 = 4f / ((1f + num6 * num6 + num5 * num5) * (float)Math.Sqrt(1f + num6 * num6 + num5 * num5));
//						float[] input = SphericalHarmonicsHelper.shEvaluateDir(val7);
//						num += num7;
//						float num8 = HalfUtils.Unpack(val6.Read<ushort>());
//						float num9 = HalfUtils.Unpack(val6.Read<ushort>());
//						float num10 = HalfUtils.Unpack(val6.Read<ushort>());
//						HalfUtils.Unpack(val6.Read<ushort>());
//						((Vector3)(ref val8))._002Ector(num8, num9, num10);
//						array4 = SphericalHarmonicsHelper.shScale(input, val8.X * num7);
//						array = SphericalHarmonicsHelper.shAdd(array, array4);
//						array4 = SphericalHarmonicsHelper.shScale(input, val8.Y * num7);
//						array2 = SphericalHarmonicsHelper.shAdd(array2, array4);
//						array4 = SphericalHarmonicsHelper.shScale(input, val8.Z * num7);
//						array3 = SphericalHarmonicsHelper.shAdd(array3, array4);
//					}
//				}
//				base.Viewport.Context.UnmapSubresource((Resource)(object)val4, 0);
//			}
//			float scale = (float)Math.PI * 4f / num;
//			array = SphericalHarmonicsHelper.shScale(array, scale);
//			array2 = SphericalHarmonicsHelper.shScale(array2, scale);
//			array3 = SphericalHarmonicsHelper.shScale(array3, scale);
//			for (int l = 0; l < 9; l++)
//			{
//				SHLightProbe[l] = new Vector4(array[l], array2[l], array3[l], 1f);
//			}
//			((DisposeBase)shader).Dispose();
//			((DisposeBase)val5).Dispose();
//			((DisposeBase)val3).Dispose();
//			((DisposeBase)val4).Dispose();
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//			base.Viewport.Context.get_Rasterizer().SetViewports(viewports, 0);
//		}

//		private void PreintegrateIBL()
//		{
//			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0119: Unknown result type (might be due to invalid IL or missing references)
//			//IL_011e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0184: Unknown result type (might be due to invalid IL or missing references)
//			//IL_018a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0274: Unknown result type (might be due to invalid IL or missing references)
//			//IL_02be: Unknown result type (might be due to invalid IL or missing references)
//			//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
//			//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
//			//IL_033f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0344: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03aa: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03b0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0562: Unknown result type (might be due to invalid IL or missing references)
//			//IL_05ae: Unknown result type (might be due to invalid IL or missing references)
//			//IL_05c9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_05d9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_05de: Unknown result type (might be due to invalid IL or missing references)
//			//IL_063b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0640: Unknown result type (might be due to invalid IL or missing references)
//			//IL_06a6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_06ac: Unknown result type (might be due to invalid IL or missing references)
//			RawViewportF[] viewports = base.Viewport.Context.get_Rasterizer().GetViewports<RawViewportF>();
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "Preintegrate DFG");
//			preintegratedDFGTexture.Clear(base.Viewport.Context, new Color4(0f, 0f, 0f, 0f));
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, preintegratedDFGTexture.RTV);
//			base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, preintegratedDFGTexture.Texture.get_Description().Width, preintegratedDFGTexture.Texture.get_Description().Height)));
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psIntegrateDFG);
//			base.Viewport.Context.Draw(6, 0);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//			if (DistantLightProbe != null)
//			{
//				D3DUtils.BeginPerfEvent(base.Viewport.Context, "Preintegrate Diffuse LD");
//				CubeMapConstants value;
//				for (int i = 0; i < 6; i++)
//				{
//					ConstantBuffer<CubeMapConstants> constantBuffer = cubeMapConstants;
//					DeviceContext context = base.Viewport.Context;
//					value = new CubeMapConstants
//					{
//						CubeFace = i
//					};
//					constantBuffer.UpdateData(context, value);
//					preintegratedDLDTexture.Clear(base.Viewport.Context, i, 0, new Color4(0f, 0f, 0f, 0f));
//					base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, preintegratedDLDTexture.GetRTV(i, 0));
//					base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, preintegratedDLDTexture.Texture.get_Description().Width, preintegratedDLDTexture.Texture.get_Description().Height)));
//					base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//					base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//					base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//					base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//					base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//					base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//					((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//					((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psIntegrateDiffuseLD);
//					((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffers(0, (Buffer[])(object)new Buffer[1]
//					{
//						cubeMapConstants.Buffer
//					});
//					((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[1]
//					{
//						DistantLightProbe
//					});
//					((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSamplers(0, (SamplerState[])(object)new SamplerState[1]
//					{
//						D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0)
//					});
//					base.Viewport.Context.Draw(6, 0);
//				}
//				D3DUtils.EndPerfEvent(base.Viewport.Context);
//				D3DUtils.BeginPerfEvent(base.Viewport.Context, "Preintegrate Specular LD");
//				base.Viewport.Context.GenerateMips(DistantLightProbe);
//				for (int j = 0; j < 9; j++)
//				{
//					for (int k = 0; k < 6; k++)
//					{
//						ConstantBuffer<CubeMapConstants> constantBuffer2 = cubeMapConstants;
//						DeviceContext context2 = base.Viewport.Context;
//						value = new CubeMapConstants
//						{
//							CubeFace = k,
//							MipIndex = (uint)j,
//							NumMips = 9u
//						};
//						constantBuffer2.UpdateData(context2, value);
//						preintegratedSLDTexture.Clear(base.Viewport.Context, k, j, new Color4(0f, 0f, 0f, 0f));
//						base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, preintegratedSLDTexture.GetRTV(k, j));
//						base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, preintegratedSLDTexture.Texture.get_Description().Width >> j, preintegratedSLDTexture.Texture.get_Description().Height >> j)));
//						base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//						base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//						base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//						base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//						base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//						base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//						((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//						((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psIntegrateSpecularLD);
//						((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffers(0, (Buffer[])(object)new Buffer[1]
//						{
//							cubeMapConstants.Buffer
//						});
//						((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[1]
//						{
//							DistantLightProbe
//						});
//						((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSamplers(0, (SamplerState[])(object)new SamplerState[1]
//						{
//							D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0)
//						});
//						base.Viewport.Context.Draw(6, 0);
//					}
//				}
//				D3DUtils.EndPerfEvent(base.Viewport.Context);
//			}
//			base.Viewport.Context.get_Rasterizer().SetViewports(viewports, 0);
//		}

//		private void ClearRenderTargets()
//		{
//			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00da: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0110: Unknown result type (might be due to invalid IL or missing references)
//			//IL_012b: Unknown result type (might be due to invalid IL or missing references)
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "ClearTargets");
//			base.Viewport.Context.ClearRenderTargetView(base.Viewport.ColorBufferRTV, Color4.op_Implicit(Color4.Black));
//			base.Viewport.Context.ClearDepthStencilView(base.Viewport.DepthBufferDSV, (DepthStencilClearFlags)3, 1f, (byte)0);
//			editorCompositeDepthTexture.Clear(base.Viewport.Context, clearDepth: true, clearStencil: true, 1f, 0);
//			selectionDepthTexture.Clear(base.Viewport.Context, clearDepth: true, clearStencil: true, 1f, 0);
//			gBufferCollection.Clear(base.Viewport.Context);
//			lightAccumulationTexture.Clear(base.Viewport.Context, Color4.Black);
//			finalColorTexture.Clear(base.Viewport.Context, Color4.Black);
//			editorCompositeTexture.Clear(base.Viewport.Context, Color4.Black);
//			scaledSceneTexture.Clear(base.Viewport.Context, Color4.Black);
//			worldNormalsForHBAOTexture.Clear(base.Viewport.Context, Color4.Black);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//		}

//		private void RenderBasePass()
//		{
//			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "BasePass");
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets(base.Viewport.DepthBufferDSV, gBufferCollection.GBufferRTVs);
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: true, (DepthWriteMask)1, (Comparison)4));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)2, (FillMode)((RenderMode == DebugRenderMode.Wireframe) ? 2 : 3), antialiasedLines: false, depthClip: true));
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, viewConstants.Buffer);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffer(0, viewConstants.Buffer);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResource(0, normalBasisCubemapTexture.SRV);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//			RenderMeshes(MeshRenderPath.Deferred, meshes);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//		}

//		private void RenderShadows()
//		{
//			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
//			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0087: Unknown result type (might be due to invalid IL or missing references)
//			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
//			//IL_010d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0112: Unknown result type (might be due to invalid IL or missing references)
//			//IL_011e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0123: Unknown result type (might be due to invalid IL or missing references)
//			//IL_012f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0134: Unknown result type (might be due to invalid IL or missing references)
//			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_013c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0146: Unknown result type (might be due to invalid IL or missing references)
//			//IL_014b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0152: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0153: Unknown result type (might be due to invalid IL or missing references)
//			//IL_015d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0162: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0367: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0374: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0379: Unknown result type (might be due to invalid IL or missing references)
//			//IL_037b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_037d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0382: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
//			if (ShadowsEnabled)
//			{
//				D3DUtils.BeginPerfEvent(base.Viewport.Context, "Shadows");
//				BoundingBox val = CalcWorldBoundingBox();
//				val = BoundingBox.Merge(val, new BoundingBox(new Vector3(-4f, -1f, -4f), new Vector3(4f, 1f, 4f)));
//				GFSDK_ShadowLib.MapRenderParams pShadowMapRenderParams = new GFSDK_ShadowLib.MapRenderParams(dummy: true);
//				pShadowMapRenderParams.LightDesc.eLightType = GFSDK_ShadowLib.LightType.Directional;
//				pShadowMapRenderParams.LightDesc.fLightSize = 1f;
//				pShadowMapRenderParams.m4x4EyeViewMatrix = GFSDK_ShadowLib.Matrix.FromSharpDX(camera.GetViewMatrix());
//				pShadowMapRenderParams.m4x4EyeProjectionMatrix = GFSDK_ShadowLib.Matrix.FromSharpDX(camera.GetProjMatrix());
//				pShadowMapRenderParams.LightDesc.v3LightPos_1 = SunPosition;
//				pShadowMapRenderParams.LightDesc.v3LightPos_2 = SunPosition;
//				pShadowMapRenderParams.LightDesc.v3LightPos_3 = SunPosition;
//				pShadowMapRenderParams.LightDesc.v3LightPos_4 = SunPosition;
//				pShadowMapRenderParams.LightDesc.v3LightLookAt_1 = Vector3.Zero;
//				pShadowMapRenderParams.LightDesc.v3LightLookAt_2 = Vector3.Zero;
//				pShadowMapRenderParams.LightDesc.v3LightLookAt_3 = Vector3.Zero;
//				pShadowMapRenderParams.LightDesc.v3LightLookAt_4 = Vector3.Zero;
//				pShadowMapRenderParams.v3WorldSpaceBBox_1 = val.Minimum * 1.05f;
//				pShadowMapRenderParams.v3WorldSpaceBBox_2 = val.Maximum * 1.05f;
//				pShadowMapRenderParams.eCullModeType = GFSDK_ShadowLib.CullModeType.Front;
//				pShadowMapRenderParams.eTechniqueType = GFSDK_ShadowLib.TechniqueType.PCF;
//				pShadowMapRenderParams.eCascadedShadowMapType = GFSDK_ShadowLib.CascadedShadowMapType.SampleDistribution;
//				pShadowMapRenderParams.fCascadeMaxDistancePercent = 50f;
//				pShadowMapRenderParams.fCascadeZLinearScale_1 = 1E-05f;
//				pShadowMapRenderParams.fCascadeZLinearScale_2 = 2E-05f;
//				pShadowMapRenderParams.fCascadeZLinearScale_3 = 5E-05f;
//				pShadowMapRenderParams.fCascadeZLinearScale_4 = 1f;
//				pShadowMapRenderParams.ZBiasParams.iDepthBias = iDepthBias;
//				pShadowMapRenderParams.ZBiasParams.fSlopeScaledDepthBias = fSlopeScaledDepthBias;
//				pShadowMapRenderParams.ZBiasParams.bUseReceiverPlaneBias = 0;
//				pShadowMapRenderParams.ZBiasParams.fDistanceBiasMin = fDistanceBiasMin;
//				pShadowMapRenderParams.ZBiasParams.fDistanceBiasFactor = fDistanceBiasFactor;
//				pShadowMapRenderParams.ZBiasParams.fDistanceBiasThreshold = fDistanceBiasThreshold;
//				pShadowMapRenderParams.ZBiasParams.fDistanceBiasPower = fDistanceBiasPower;
//				pShadowMapRenderParams.PCSSPenumbraParams.fMaxThreshold = 247f;
//				pShadowMapRenderParams.PCSSPenumbraParams.fMinSizePercent_1 = 1.8f;
//				pShadowMapRenderParams.PCSSPenumbraParams.fMinSizePercent_2 = 1.8f;
//				pShadowMapRenderParams.PCSSPenumbraParams.fMinSizePercent_3 = 1.8f;
//				pShadowMapRenderParams.PCSSPenumbraParams.fMinSizePercent_4 = 1.8f;
//				pShadowMapRenderParams.PCSSPenumbraParams.fMinWeightThresholdPercent = 3f;
//				pShadowMapRenderParams.FrustumTraceMapRenderParams.eConservativeRasterType = GFSDK_ShadowLib.ConservativeRasterType.HW;
//				pShadowMapRenderParams.FrustumTraceMapRenderParams.eCullModeType = GFSDK_ShadowLib.CullModeType.None;
//				pShadowMapRenderParams.FrustumTraceMapRenderParams.fHitEpsilon = 0.009f;
//				pShadowMapRenderParams.RayTraceMapRenderParams.fHitEpsilon = 0.02f;
//				pShadowMapRenderParams.RayTraceMapRenderParams.eCullModeType = GFSDK_ShadowLib.CullModeType.None;
//				pShadowMapRenderParams.RayTraceMapRenderParams.eConservativeRasterType = GFSDK_ShadowLib.ConservativeRasterType.HW;
//				pShadowMapRenderParams.DepthBufferDesc.eDepthType = GFSDK_ShadowLib.DepthType.DepthBuffer;
//				pShadowMapRenderParams.DepthBufferDesc.DepthSRV = ((CppObject)base.Viewport.DepthBufferSRV).get_NativePointer();
//				shadowContext.SetMapRenderParams(shadowMapHandle, pShadowMapRenderParams);
//				shadowContext.UpdateMapBounds(shadowMapHandle, out GFSDK_ShadowLib.Matrix[] pm4x4LightViewMatrix, out GFSDK_ShadowLib.Matrix[] pm4x4LightProjectionMatrix, out GFSDK_ShadowLib.Frustum[] _);
//				shadowContext.InitializeMapRendering(shadowMapHandle, GFSDK_ShadowLib.MapRenderType.Depth);
//				for (uint num = 0u; num < GFSDK_ShadowLib.NumCSMLevels; num++)
//				{
//					Matrix val2 = pm4x4LightViewMatrix[num].ToSharpDX();
//					Matrix val3 = pm4x4LightProjectionMatrix[num].ToSharpDX();
//					Matrix crViewProjMatrix = val2 * val3;
//					((Matrix)(ref crViewProjMatrix)).Transpose();
//					viewConstants.UpdateData(base.Viewport.Context, new ViewConstants
//					{
//						CrViewProjMatrix = crViewProjMatrix
//					});
//					shadowContext.BeginMapRendering(shadowMapHandle, GFSDK_ShadowLib.MapRenderType.Depth, num);
//					RenderMeshes(MeshRenderPath.Shadows, meshes);
//					shadowContext.EndMapRendering(shadowMapHandle, GFSDK_ShadowLib.MapRenderType.Depth, num);
//				}
//				shadowContext.ClearBuffer(shadowBufferHandle);
//				shadowContext.RenderBuffer(shadowMapHandle, shadowBufferHandle, default(GFSDK_ShadowLib.BufferRenderParams));
//				shadowContext.FinalizeBuffer(shadowBufferHandle, ref shadowSRV);
//				D3DUtils.EndPerfEvent(base.Viewport.Context);
//			}
//		}

//		private void RenderMeshes(MeshRenderPath renderPath, List<MeshRenderInstance> meshList)
//		{
//			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
//			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_009d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
//			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0122: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0127: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0135: Unknown result type (might be due to invalid IL or missing references)
//			//IL_013a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0148: Unknown result type (might be due to invalid IL or missing references)
//			//IL_014d: Unknown result type (might be due to invalid IL or missing references)
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "RenderMeshes");
//			base.Viewport.Context.get_Rasterizer().get_State().get_Description();
//			foreach (MeshRenderInstance mesh in meshList)
//			{
//				D3DUtils.BeginPerfEvent(base.Viewport.Context, mesh.RenderMesh.DebugName);
//				Matrix transform = mesh.Transform;
//				((Matrix)(ref transform)).Transpose();
//				functionConstants.UpdateData(base.Viewport.Context, new FunctionConstants
//				{
//					WorldMatrix = Matrix.Scaling(-1f, 1f, 1f) * transform,
//					LightProbe1 = SHLightProbe[0],
//					LightProbe2 = SHLightProbe[1],
//					LightProbe3 = SHLightProbe[2],
//					LightProbe4 = SHLightProbe[3],
//					LightProbe5 = SHLightProbe[4],
//					LightProbe6 = SHLightProbe[5],
//					LightProbe7 = SHLightProbe[6],
//					LightProbe8 = SHLightProbe[7],
//					LightProbe9 = SHLightProbe[8]
//				});
//				((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(1, functionConstants.Buffer);
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffer(1, functionConstants.Buffer);
//				mesh.RenderMesh.Render(base.Viewport.Context, renderPath);
//				D3DUtils.EndPerfEvent(base.Viewport.Context);
//			}
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//		}

//		private void RenderLights()
//		{
//			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
//			//IL_013d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0143: Unknown result type (might be due to invalid IL or missing references)
//			//IL_025e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0263: Unknown result type (might be due to invalid IL or missing references)
//			//IL_026b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_027f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0284: Unknown result type (might be due to invalid IL or missing references)
//			//IL_028f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0294: Unknown result type (might be due to invalid IL or missing references)
//			//IL_033a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0346: Unknown result type (might be due to invalid IL or missing references)
//			//IL_034b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0354: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0359: Unknown result type (might be due to invalid IL or missing references)
//			//IL_035d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0371: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0376: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03ad: Unknown result type (might be due to invalid IL or missing references)
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "Lights");
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, lightAccumulationTexture.RTV);
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState((RenderTargetBlendDescription[])(object)new RenderTargetBlendDescription[1]
//			{
//				new RenderTargetBlendDescription
//				{
//					IsBlendEnabled = RawBool.op_Implicit(true),
//					SourceBlend = (BlendOption)2,
//					DestinationBlend = (BlendOption)2,
//					BlendOperation = (BlendOperation)1,
//					SourceAlphaBlend = (BlendOption)2,
//					DestinationAlphaBlend = (BlendOption)2,
//					AlphaBlendOperation = (BlendOperation)1,
//					RenderTargetWriteMask = (ColorWriteMaskFlags)15
//				}
//			}));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffers(0, (Buffer[])(object)new Buffer[2]
//			{
//				commonConstants.Buffer,
//				lightConstants.Buffer
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, gBufferCollection.GBufferSRVs);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(4, (ShaderResourceView[])(object)new ShaderResourceView[1]
//			{
//				base.Viewport.DepthBufferSRV
//			});
//			LightConstants value;
//			if (SunIntensity > 0f)
//			{
//				ConstantBuffer<LightConstants> constantBuffer = lightConstants;
//				DeviceContext context = base.Viewport.Context;
//				value = new LightConstants
//				{
//					LightColorAndIntensity = new Vector4(0f, 0f, 0f, SunIntensity),
//					LightPosAndInvSqrRadius = new Vector4(SunPosition * new Vector3(-1f, 1f, 1f), SunAngularRadius)
//				};
//				constantBuffer.UpdateData(context, value);
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(5, (ShaderResourceView[])(object)new ShaderResourceView[1]
//				{
//					shadowSRV
//				});
//				((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psSunLight);
//				base.Viewport.Context.Draw(6, 0);
//			}
//			foreach (LightRenderInstance light in lights)
//			{
//				if (light.Intensity > 0f)
//				{
//					ConstantBuffer<LightConstants> constantBuffer2 = lightConstants;
//					DeviceContext context2 = base.Viewport.Context;
//					value = new LightConstants
//					{
//						LightColorAndIntensity = new Vector4(light.Color, light.Intensity)
//					};
//					Matrix transform = light.Transform;
//					value.LightPosAndInvSqrRadius = new Vector4(((Matrix)(ref transform)).get_TranslationVector() * new Vector3(-1f, 1f, 1f), (light.SphereRadius > 0f) ? light.SphereRadius : (1f / (light.AttenuationRadius * light.AttenuationRadius)));
//					constantBuffer2.UpdateData(context2, value);
//					((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set((light.SphereRadius > 0f) ? psSphereLight : psPointLight);
//					base.Viewport.Context.Draw(6, 0);
//				}
//			}
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//		}

//		private void RenderIBL()
//		{
//			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
//			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00da: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0146: Unknown result type (might be due to invalid IL or missing references)
//			//IL_014c: Unknown result type (might be due to invalid IL or missing references)
//			if (DistantLightProbe != null)
//			{
//				D3DUtils.BeginPerfEvent(base.Viewport.Context, "IBL");
//				base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, lightAccumulationTexture.RTV);
//				base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//				base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState((RenderTargetBlendDescription[])(object)new RenderTargetBlendDescription[1]
//				{
//					new RenderTargetBlendDescription
//					{
//						IsBlendEnabled = RawBool.op_Implicit(true),
//						SourceBlend = (BlendOption)2,
//						DestinationBlend = (BlendOption)2,
//						BlendOperation = (BlendOperation)1,
//						SourceAlphaBlend = (BlendOption)2,
//						DestinationAlphaBlend = (BlendOption)2,
//						AlphaBlendOperation = (BlendOperation)1,
//						RenderTargetWriteMask = (ColorWriteMaskFlags)15
//					}
//				}));
//				base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//				base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//				base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//				base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//				((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//				((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//				((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psIBLRender);
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffers(0, (Buffer[])(object)new Buffer[1]
//				{
//					commonConstants.Buffer
//				});
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, gBufferCollection.GBufferSRVs);
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(4, (ShaderResourceView[])(object)new ShaderResourceView[5]
//				{
//					base.Viewport.DepthBufferSRV,
//					preintegratedDFGTexture.SRV,
//					preintegratedDLDTexture.SRV,
//					preintegratedSLDTexture.SRV,
//					DistantLightProbe
//				});
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)21));
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(1, D3DUtils.CreateSamplerState((TextureAddressMode)1, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)21));
//				base.Viewport.Context.Draw(6, 0);
//				D3DUtils.EndPerfEvent(base.Viewport.Context);
//			}
//		}

//		private void ResolveNormalsForHBAO()
//		{
//			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
//			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "ResolveNormalsForHBAO");
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, worldNormalsForHBAOTexture.RTV);
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psResolveWorldNormals);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffers(0, (Buffer[])(object)new Buffer[1]
//			{
//				commonConstants.Buffer
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, gBufferCollection.GBufferSRVs);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(4, (ShaderResourceView[])(object)new ShaderResourceView[1]
//			{
//				base.Viewport.DepthBufferSRV
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//			base.Viewport.Context.Draw(6, 0);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//		}

//		private void RenderEmissive()
//		{
//			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0197: Unknown result type (might be due to invalid IL or missing references)
//			//IL_019c: Unknown result type (might be due to invalid IL or missing references)
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "Emissive");
//			UpdateViewConstants(bJitter: true);
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets(base.Viewport.DepthBufferDSV, lightAccumulationTexture.RTV);
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: true, (DepthWriteMask)1, (Comparison)4));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)2, (FillMode)3, antialiasedLines: false, depthClip: true));
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, viewConstants.Buffer);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffer(0, viewConstants.Buffer);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResource(0, normalBasisCubemapTexture.SRV);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//			RenderMeshes(MeshRenderPath.Deferred, new List<MeshRenderInstance>
//			{
//				new MeshRenderInstance
//				{
//					RenderMesh = skySphere,
//					Transform = Matrix.Identity
//				}
//			});
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//		}

//		private void PostProcess()
//		{
//			RawViewportF[] viewports = base.Viewport.Context.get_Rasterizer().GetViewports<RawViewportF>();
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "PostProcess");
//			PostProcessCollectSelections();
//			PostProcessEditorPrimitives();
//			PostProcessHBAO();
//			PostProcessTAA();
//			PostProcessDownScaleScene();
//			PostProcessMeasureLuminance();
//			PostProcessBloom();
//			PostProcessColorLookupTable();
//			PostProcessSelectionOutline();
//			PostProcessEditorComposite();
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//			base.Viewport.Context.get_Rasterizer().SetViewports(viewports, 0);
//		}

//		private void Resolve()
//		{
//			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0107: Unknown result type (might be due to invalid IL or missing references)
//			//IL_010d: Unknown result type (might be due to invalid IL or missing references)
//			if (RenderMode != 0 && RenderMode != DebugRenderMode.HBAO)
//			{
//				D3DUtils.BeginPerfEvent(base.Viewport.Context, "DebugRenderMode");
//				base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, base.Viewport.ColorBufferRTV);
//				base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//				base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//				base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//				base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//				base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//				base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//				((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//				((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//				((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psDebugRenderMode);
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, gBufferCollection.GBufferSRVs);
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(4, (ShaderResourceView[])(object)new ShaderResourceView[1]
//				{
//					base.Viewport.DepthBufferSRV
//				});
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffer(0, commonConstants.Buffer);
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//				base.Viewport.Context.Draw(6, 0);
//				D3DUtils.EndPerfEvent(base.Viewport.Context);
//				base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, (RenderTargetView[])(object)new RenderTargetView[5]);
//			}
//		}

//		private void PostProcessTAA()
//		{
//			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
//			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
//			if (GFSDK_TXAA.TxaaEnabled)
//			{
//				D3DUtils.BeginPerfEvent(base.Viewport.Context, "TXAA");
//				D3DUtils.BeginPerfEvent(base.Viewport.Context, "CameraMotionVectors");
//				base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, new VertexBufferBinding((Buffer)null, 0, 0));
//				base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//				txaaMotionVectorsTexture.Clear(base.Viewport.Context, new Color4(0f, 0f, 0f, 0f));
//				Matrix viewProjMatrix = camera.GetViewProjMatrix();
//				((Matrix)(ref viewProjMatrix)).Transpose();
//				Matrix prevViewProjMatrix = camera.GetPrevViewProjMatrix();
//				((Matrix)(ref prevViewProjMatrix)).Transpose();
//				IntPtr intPtr = Marshal.AllocHGlobal(64);
//				IntPtr intPtr2 = Marshal.AllocHGlobal(64);
//				Marshal.Copy(((Matrix)(ref viewProjMatrix)).ToArray(), 0, intPtr, 16);
//				Marshal.Copy(((Matrix)(ref prevViewProjMatrix)).ToArray(), 0, intPtr2, 16);
//				GFSDK_TXAA.MotionVectorParameters inParams = default(GFSDK_TXAA.MotionVectorParameters);
//				inParams.viewProj = intPtr;
//				inParams.prevViewProj = intPtr2;
//				inParams.samples = 1;
//				Marshal.GetDelegateForFunctionPointer<GFSDK_TXAA.GenerateMotionVectorFunc>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(Marshal.ReadIntPtr(txaaMotionVectorGenerator, 0), 0), 8))(Marshal.ReadIntPtr(txaaMotionVectorGenerator), ((CppObject)base.Viewport.Context).get_NativePointer(), ((CppObject)txaaMotionVectorsTexture.RTV).get_NativePointer(), ((CppObject)base.Viewport.DepthBufferSRV).get_NativePointer(), inParams);
//				Marshal.FreeHGlobal(intPtr);
//				Marshal.FreeHGlobal(intPtr2);
//				D3DUtils.EndPerfEvent(base.Viewport.Context);
//				D3DUtils.BeginPerfEvent(base.Viewport.Context, "Resolve");
//				base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, (RenderTargetView[])(object)new RenderTargetView[4]);
//				GFSDK_TXAA.NvTxaaFeedbackParameters nvTxaaDefaultFeedback = GFSDK_TXAA.NvTxaaFeedbackParameters.NvTxaaDefaultFeedback;
//				IntPtr intPtr3 = Marshal.AllocHGlobal(Marshal.SizeOf<GFSDK_TXAA.NvTxaaFeedbackParameters>());
//				Marshal.StructureToPtr(nvTxaaDefaultFeedback, intPtr3, fDeleteOld: true);
//				GFSDK_TXAA.GetJitter(out float[] outJitter);
//				GFSDK_TXAA.NvTxaaPerFrameConstants structure = default(GFSDK_TXAA.NvTxaaPerFrameConstants);
//				structure.xJitter = outJitter[0];
//				structure.yJitter = outJitter[1];
//				structure.mvScale = 1024f;
//				structure.motionVecSelection = 3u;
//				structure.useRGB = 0u;
//				structure.frameBlendFactor = 0.04f;
//				structure.dbg1 = 0u;
//				structure.bbScale = 1f;
//				structure.enableClipping = 1u;
//				structure.useBHFilters = 1u;
//				IntPtr intPtr4 = Marshal.AllocHGlobal(Marshal.SizeOf<GFSDK_TXAA.NvTxaaPerFrameConstants>());
//				Marshal.StructureToPtr(structure, intPtr4, fDeleteOld: true);
//				GFSDK_TXAA.NvTxaaResolveParametersDX11 structure2 = default(GFSDK_TXAA.NvTxaaResolveParametersDX11);
//				structure2.txaaContext = txaaContext;
//				structure2.deviceContext = ((CppObject)base.Viewport.Context).get_NativePointer();
//				structure2.resolveTarget = ((CppObject)postProcessTexture.RTV).get_NativePointer();
//				structure2.msaaSource = ((CppObject)lightAccumulationTexture.SRV).get_NativePointer();
//				structure2.msaaDepth = ((CppObject)base.Viewport.DepthBufferSRV).get_NativePointer();
//				structure2.feedbackSource = ((CppObject)txaaFeedbackTeture.SRV).get_NativePointer();
//				structure2.alphaResolveMode = 1;
//				structure2.feedback = intPtr3;
//				structure2.perFrameConstants = intPtr4;
//				GFSDK_TXAA.NvTxaaMotionDX11 structure3 = default(GFSDK_TXAA.NvTxaaMotionDX11);
//				structure3.motionVectors = ((CppObject)txaaMotionVectorsTexture.SRV).get_NativePointer();
//				structure3.motionVectorsMS = ((CppObject)txaaMotionVectorsTexture.SRV).get_NativePointer();
//				IntPtr intPtr5 = Marshal.AllocHGlobal(Marshal.SizeOf<GFSDK_TXAA.NvTxaaResolveParametersDX11>());
//				Marshal.StructureToPtr(structure2, intPtr5, fDeleteOld: true);
//				IntPtr intPtr6 = Marshal.AllocHGlobal(Marshal.SizeOf<GFSDK_TXAA.NvTxaaMotionDX11>());
//				Marshal.StructureToPtr(structure3, intPtr6, fDeleteOld: true);
//				GFSDK_TXAA.ResolveFromMotionVectors(intPtr5, intPtr6);
//				base.Viewport.Context.CopyResource((Resource)(object)postProcessTexture.Texture, (Resource)(object)txaaFeedbackTeture.Texture);
//				Marshal.FreeHGlobal(intPtr6);
//				Marshal.FreeHGlobal(intPtr5);
//				Marshal.FreeHGlobal(intPtr4);
//				Marshal.FreeHGlobal(intPtr3);
//				D3DUtils.EndPerfEvent(base.Viewport.Context);
//				D3DUtils.EndPerfEvent(base.Viewport.Context);
//			}
//			else
//			{
//				D3DUtils.BeginPerfEvent(base.Viewport.Context, "Resolve");
//				base.Viewport.Context.CopyResource((Resource)(object)lightAccumulationTexture.Texture, (Resource)(object)postProcessTexture.Texture);
//				D3DUtils.EndPerfEvent(base.Viewport.Context);
//			}
//		}

//		private void PostProcessDownScaleScene()
//		{
//			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0142: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0147: Unknown result type (might be due to invalid IL or missing references)
//			//IL_019f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01be: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "Downscale4x4");
//			DataStream val = null;
//			base.Viewport.Context.MapSubresource((Resource)(object)postProcessConstants, 0, (MapMode)4, (MapFlags)0, ref val);
//			float num = 1f / (float)postProcessTexture.Texture.get_Description().Width;
//			float num2 = 1f / (float)postProcessTexture.Texture.get_Description().Height;
//			for (int i = 0; i < 4; i++)
//			{
//				for (int j = 0; j < 4; j++)
//				{
//					val.Write<float>(((float)j - 1.5f) * num);
//					val.Write<float>(((float)i - 1.5f) * num2);
//					val.Write<Vector2>(Vector2.Zero);
//				}
//			}
//			base.Viewport.Context.UnmapSubresource((Resource)(object)postProcessConstants, 0);
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, scaledSceneTexture.RTV);
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, scaledSceneTexture.Texture.get_Description().Width, scaledSceneTexture.Texture.get_Description().Height)));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psDownscale4x4);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[1]
//			{
//				postProcessTexture.SRV
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffer(1, postProcessConstants);
//			base.Viewport.Context.Draw(6, 0);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//		}

//		private void PostProcessMeasureLuminance()
//		{
//			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0180: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0185: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01df: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0200: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0205: Unknown result type (might be due to invalid IL or missing references)
//			//IL_023a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0240: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03cd: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03ef: Unknown result type (might be due to invalid IL or missing references)
//			//IL_042f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_048d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_050f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0514: Unknown result type (might be due to invalid IL or missing references)
//			//IL_056e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0585: Unknown result type (might be due to invalid IL or missing references)
//			//IL_058f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0594: Unknown result type (might be due to invalid IL or missing references)
//			//IL_05c9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_05cf: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0721: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0739: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0759: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0799: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07f7: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0879: Unknown result type (might be due to invalid IL or missing references)
//			//IL_087e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_08d8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_08ef: Unknown result type (might be due to invalid IL or missing references)
//			//IL_08f9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_08fe: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0933: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0939: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0a7e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0ad0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0b52: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0b57: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0bb1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0bc8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0bd2: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0bd7: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0c0c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0c12: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0da1: Unknown result type (might be due to invalid IL or missing references)
//			int num = 3;
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "SampleLuminanceInitial");
//			DataStream val = null;
//			base.Viewport.Context.MapSubresource((Resource)(object)postProcessConstants, 0, (MapMode)4, (MapFlags)0, ref val);
//			float num2 = 1f / (3f * (float)toneMapTextures[num].Texture.get_Description().Width);
//			float num3 = 1f / (3f * (float)toneMapTextures[num].Texture.get_Description().Height);
//			for (int i = -1; i <= 1; i++)
//			{
//				for (int j = -1; j <= 1; j++)
//				{
//					val.Write<float>((float)i * num2);
//					val.Write<float>((float)j * num3);
//					val.Write<Vector2>(Vector2.Zero);
//				}
//			}
//			base.Viewport.Context.UnmapSubresource((Resource)(object)postProcessConstants, 0);
//			toneMapTextures[num].Clear(base.Viewport.Context, new Color4(0f, 0f, 0f, 0f));
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, toneMapTextures[num].RTV);
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, toneMapTextures[num].Texture.get_Description().Width, toneMapTextures[num].Texture.get_Description().Height)));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psSampleLumInitial);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[2]
//			{
//				scaledSceneTexture.SRV,
//				toneMapTextures[5].SRV
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffers(0, (Buffer[])(object)new Buffer[2]
//			{
//				commonConstants.Buffer,
//				postProcessConstants
//			});
//			base.Viewport.Context.Draw(6, 0);
//			num--;
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "SampleLuminanceIterative");
//			DataStream val2 = default(DataStream);
//			while (num > 0)
//			{
//				base.Viewport.Context.MapSubresource((Resource)(object)postProcessConstants, 0, (MapMode)4, (MapFlags)0, ref val2);
//				float num4 = 1f / (float)toneMapTextures[num + 1].Texture.get_Description().Width;
//				float num5 = 1f / (float)toneMapTextures[num + 1].Texture.get_Description().Height;
//				for (int k = 0; k < 4; k++)
//				{
//					for (int l = 0; l < 4; l++)
//					{
//						val2.Write<float>(((float)l - 1.5f) * num4);
//						val2.Write<float>(((float)k - 1.5f) * num5);
//						val2.Write<Vector2>(Vector2.Zero);
//					}
//				}
//				base.Viewport.Context.UnmapSubresource((Resource)(object)postProcessConstants, 0);
//				toneMapTextures[num].Clear(base.Viewport.Context, new Color4(0f, 0f, 0f, 0f));
//				base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, toneMapTextures[num].RTV);
//				base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//				base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//				base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//				base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, toneMapTextures[num].Texture.get_Description().Width, toneMapTextures[num].Texture.get_Description().Height)));
//				base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//				base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//				base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//				((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//				((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//				((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psSampleLumIterative);
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(1, (ShaderResourceView[])(object)new ShaderResourceView[1]
//				{
//					toneMapTextures[num + 1].SRV
//				});
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffer(1, postProcessConstants);
//				base.Viewport.Context.Draw(6, 0);
//				num--;
//			}
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "SampleLuminanceFinal");
//			DataStream val3 = default(DataStream);
//			base.Viewport.Context.MapSubresource((Resource)(object)postProcessConstants, 0, (MapMode)4, (MapFlags)0, ref val3);
//			float num6 = 1f / (float)toneMapTextures[1].Texture.get_Description().Width;
//			float num7 = 1f / (float)toneMapTextures[1].Texture.get_Description().Height;
//			for (int m = 0; m < 4; m++)
//			{
//				for (int n = 0; n < 4; n++)
//				{
//					val3.Write<float>(((float)n - 1.5f) * num6);
//					val3.Write<float>(((float)m - 1.5f) * num7);
//					val3.Write<Vector2>(Vector2.Zero);
//				}
//			}
//			base.Viewport.Context.UnmapSubresource((Resource)(object)postProcessConstants, 0);
//			toneMapTextures[0].Clear(base.Viewport.Context, new Color4(0f, 0f, 0f, 0f));
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, toneMapTextures[0].RTV);
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, toneMapTextures[0].Texture.get_Description().Width, toneMapTextures[0].Texture.get_Description().Height)));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psSampleLumFinal);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(1, (ShaderResourceView[])(object)new ShaderResourceView[1]
//			{
//				toneMapTextures[1].SRV
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffer(1, postProcessConstants);
//			base.Viewport.Context.Draw(6, 0);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "CalculateAdaptedLuminance");
//			DataStream val4 = default(DataStream);
//			base.Viewport.Context.MapSubresource((Resource)(object)postProcessConstants, 0, (MapMode)4, (MapFlags)0, ref val4);
//			val4.Write<float>((float)lastDeltaTime);
//			base.Viewport.Context.UnmapSubresource((Resource)(object)postProcessConstants, 0);
//			toneMapTextures[4].Clear(base.Viewport.Context, new Color4(0f, 0f, 0f, 0f));
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, toneMapTextures[4].RTV);
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, toneMapTextures[4].Texture.get_Description().Width, toneMapTextures[4].Texture.get_Description().Height)));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psCalcAdaptedLum);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[2]
//			{
//				toneMapTextures[5].SRV,
//				toneMapTextures[0].SRV
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffer(1, postProcessConstants);
//			base.Viewport.Context.Draw(6, 0);
//			base.Viewport.Context.ResolveSubresource((Resource)(object)toneMapTextures[4].Texture, 0, (Resource)(object)toneMapTextures[5].Texture, 0, (Format)41);
//			base.Viewport.Context.CopyResource((Resource)(object)toneMapTextures[4].Texture, (Resource)(object)toneMapTextures[6].Texture);
//			base.Viewport.Context.MapSubresource((Resource)(object)toneMapTextures[6].Texture, 0, (MapMode)1, (MapFlags)0, ref val4);
//			float value = val4.Read<float>();
//			luminanceHistogram.Add(value);
//			base.Viewport.Context.UnmapSubresource((Resource)(object)toneMapTextures[6].Texture, 0);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//		}

//		private void PostProcessBloom()
//		{
//			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
//			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0110: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
//			//IL_012f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0134: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0191: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0196: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0202: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0339: Unknown result type (might be due to invalid IL or missing references)
//			//IL_034f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_036d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03e5: Unknown result type (might be due to invalid IL or missing references)
//			//IL_03ee: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0401: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0406: Unknown result type (might be due to invalid IL or missing references)
//			//IL_044c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0453: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0458: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0473: Unknown result type (might be due to invalid IL or missing references)
//			//IL_047e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_049e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0509: Unknown result type (might be due to invalid IL or missing references)
//			//IL_051e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0528: Unknown result type (might be due to invalid IL or missing references)
//			//IL_052d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_058a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_058f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_05f5: Unknown result type (might be due to invalid IL or missing references)
//			//IL_05fb: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0752: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0768: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0786: Unknown result type (might be due to invalid IL or missing references)
//			//IL_07c6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_083b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0850: Unknown result type (might be due to invalid IL or missing references)
//			//IL_085a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_085f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_08bc: Unknown result type (might be due to invalid IL or missing references)
//			//IL_08c1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0927: Unknown result type (might be due to invalid IL or missing references)
//			//IL_092d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0a84: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0a9a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0ab8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0b2b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0b30: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0b39: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0b4c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0b51: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0b97: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0b9e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0ba3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0bbf: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0bcb: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0bec: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0c5b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0c72: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0c7c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0c81: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0cde: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0ce3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0d49: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0d4f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0ea6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0ebe: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0f09: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0f0e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0f56: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0f5b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0f7a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0f7f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0fb0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0fd1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1040: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1057: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1061: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1066: Unknown result type (might be due to invalid IL or missing references)
//			//IL_10c3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_10c8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_112e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1134: Unknown result type (might be due to invalid IL or missing references)
//			//IL_128d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_12a5: Unknown result type (might be due to invalid IL or missing references)
//			//IL_12f0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_12f5: Unknown result type (might be due to invalid IL or missing references)
//			//IL_133d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1342: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1361: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1366: Unknown result type (might be due to invalid IL or missing references)
//			//IL_13a3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_13c4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1433: Unknown result type (might be due to invalid IL or missing references)
//			//IL_144a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1454: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1459: Unknown result type (might be due to invalid IL or missing references)
//			//IL_14b6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_14bb: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1521: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1527: Unknown result type (might be due to invalid IL or missing references)
//			//IL_16a5: Unknown result type (might be due to invalid IL or missing references)
//			//IL_16ba: Unknown result type (might be due to invalid IL or missing references)
//			//IL_16c4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_16c9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1727: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1730: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1735: Unknown result type (might be due to invalid IL or missing references)
//			//IL_173d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1745: Unknown result type (might be due to invalid IL or missing references)
//			//IL_174d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1755: Unknown result type (might be due to invalid IL or missing references)
//			//IL_175d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1765: Unknown result type (might be due to invalid IL or missing references)
//			//IL_176e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1773: Unknown result type (might be due to invalid IL or missing references)
//			//IL_1775: Unknown result type (might be due to invalid IL or missing references)
//			//IL_17db: Unknown result type (might be due to invalid IL or missing references)
//			//IL_17e1: Unknown result type (might be due to invalid IL or missing references)
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "Bloom");
//			brightPassTexture.Clear(base.Viewport.Context, Color4.Black);
//			blurTexture.Clear(base.Viewport.Context, Color4.Black);
//			bloomSourceTexture.Clear(base.Viewport.Context, Color4.Black);
//			bloomTextures[0].Clear(base.Viewport.Context, Color4.Black);
//			bloomTextures[1].Clear(base.Viewport.Context, Color4.Black);
//			bloomTextures[2].Clear(base.Viewport.Context, Color4.Black);
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "BrightPass");
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, brightPassTexture.RTV);
//			base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, brightPassTexture.Texture.get_Description().Width, brightPassTexture.Texture.get_Description().Height)));
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psBrightPass);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[2]
//			{
//				scaledSceneTexture.SRV,
//				toneMapTextures[4].SRV
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//			base.Viewport.Context.Draw(6, 0);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "Blur");
//			DataStream val = null;
//			base.Viewport.Context.MapSubresource((Resource)(object)postProcessConstants, 0, (MapMode)4, (MapFlags)0, ref val);
//			float num = 1f / (float)blurTexture.Texture.get_Description().Width;
//			float num2 = 1f / (float)blurTexture.Texture.get_Description().Height;
//			Vector4 val2 = default(Vector4);
//			((Vector4)(ref val2))._002Ector(1f, 1f, 1f, 1f);
//			Vector4[] array = (Vector4[])(object)new Vector4[16];
//			Vector2[] array2 = (Vector2[])(object)new Vector2[16];
//			float num3 = 0f;
//			int num4 = 0;
//			for (int i = -2; i <= 2; i++)
//			{
//				for (int j = -2; j <= 2; j++)
//				{
//					if (Math.Abs(i) + Math.Abs(j) <= 2)
//					{
//						array2[num4] = new Vector2((float)i * num, (float)j * num2);
//						array[num4] = val2 * GaussianDistribution(i, j, 1f);
//						num3 += array[num4].X;
//						num4++;
//					}
//				}
//			}
//			for (int k = 0; k < num4; k++)
//			{
//				ref Vector4 reference = ref array[k];
//				reference /= num3;
//			}
//			for (int l = 0; l < 16; l++)
//			{
//				val.Write<Vector2>(array2[l]);
//				val.Write<Vector2>(Vector2.Zero);
//			}
//			for (int m = 0; m < 16; m++)
//			{
//				val.Write<Vector4>(array[m]);
//			}
//			base.Viewport.Context.UnmapSubresource((Resource)(object)postProcessConstants, 0);
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, blurTexture.RTV);
//			base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, blurTexture.Texture.get_Description().Width, blurTexture.Texture.get_Description().Height)));
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psGaussianBlur5x5);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[1]
//			{
//				brightPassTexture.SRV
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffers(1, (Buffer[])(object)new Buffer[1]
//			{
//				postProcessConstants
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSamplers(0, (SamplerState[])(object)new SamplerState[1]
//			{
//				D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)21)
//			});
//			base.Viewport.Context.Draw(6, 0);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "BloomSource");
//			DataStream val3 = null;
//			base.Viewport.Context.MapSubresource((Resource)(object)postProcessConstants, 0, (MapMode)4, (MapFlags)0, ref val3);
//			float num5 = 1f / (float)brightPassTexture.Texture.get_Description().Width;
//			float num6 = 1f / (float)brightPassTexture.Texture.get_Description().Height;
//			for (int n = 0; n < 2; n++)
//			{
//				for (int num7 = 0; num7 < 2; num7++)
//				{
//					val3.Write<float>(((float)num7 - 0.5f) * num5);
//					val3.Write<float>(((float)n - 0.5f) * num6);
//					val3.Write<Vector2>(Vector2.Zero);
//				}
//			}
//			base.Viewport.Context.UnmapSubresource((Resource)(object)postProcessConstants, 0);
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, bloomSourceTexture.RTV);
//			base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, bloomSourceTexture.Texture.get_Description().Width, bloomSourceTexture.Texture.get_Description().Height)));
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psDownSample2x2);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[1]
//			{
//				blurTexture.SRV
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffers(1, (Buffer[])(object)new Buffer[1]
//			{
//				postProcessConstants
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSamplers(0, (SamplerState[])(object)new SamplerState[1]
//			{
//				D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)21)
//			});
//			base.Viewport.Context.Draw(6, 0);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "Blur");
//			DataStream val4 = null;
//			base.Viewport.Context.MapSubresource((Resource)(object)postProcessConstants, 0, (MapMode)4, (MapFlags)0, ref val4);
//			float num8 = 1f / (float)bloomSourceTexture.Texture.get_Description().Width;
//			float num9 = 1f / (float)bloomSourceTexture.Texture.get_Description().Height;
//			Vector4 val5 = default(Vector4);
//			((Vector4)(ref val5))._002Ector(1f, 1f, 1f, 1f);
//			Vector4[] array3 = (Vector4[])(object)new Vector4[16];
//			Vector2[] array4 = (Vector2[])(object)new Vector2[16];
//			float num10 = 0f;
//			int num11 = 0;
//			for (int num12 = -2; num12 <= 2; num12++)
//			{
//				for (int num13 = -2; num13 <= 2; num13++)
//				{
//					if (Math.Abs(num12) + Math.Abs(num13) <= 2)
//					{
//						array4[num11] = new Vector2((float)num12 * num8, (float)num13 * num9);
//						array3[num11] = val5 * GaussianDistribution(num12, num13, 1f);
//						num10 += array3[num11].X;
//						num11++;
//					}
//				}
//			}
//			for (int num14 = 0; num14 < num11; num14++)
//			{
//				ref Vector4 reference2 = ref array3[num14];
//				reference2 /= num10;
//			}
//			for (int num15 = 0; num15 < 16; num15++)
//			{
//				val4.Write<Vector2>(array4[num15]);
//				val4.Write<Vector2>(Vector2.Zero);
//			}
//			for (int num16 = 0; num16 < 16; num16++)
//			{
//				val4.Write<Vector4>(array3[num16]);
//			}
//			base.Viewport.Context.UnmapSubresource((Resource)(object)postProcessConstants, 0);
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, bloomTextures[2].RTV);
//			base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, bloomTextures[2].Texture.get_Description().Width, bloomTextures[2].Texture.get_Description().Height)));
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psGaussianBlur5x5);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[1]
//			{
//				bloomSourceTexture.SRV
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffers(1, (Buffer[])(object)new Buffer[1]
//			{
//				postProcessConstants
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSamplers(0, (SamplerState[])(object)new SamplerState[1]
//			{
//				D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)21)
//			});
//			base.Viewport.Context.Draw(6, 0);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "HorizontalBlur");
//			DataStream val6 = null;
//			base.Viewport.Context.MapSubresource((Resource)(object)postProcessConstants, 0, (MapMode)4, (MapFlags)0, ref val6);
//			float num17 = 1f / (float)bloomTextures[2].Texture.get_Description().Width;
//			float num18 = 2f * GaussianDistribution(0f, 0f, 3f);
//			Vector4[] array5 = (Vector4[])(object)new Vector4[16];
//			float[] array6 = new float[16];
//			array5[0] = new Vector4(num18, num18, num18, 1f);
//			array6[0] = 0f;
//			for (int num19 = 1; num19 < 8; num19++)
//			{
//				num18 = 2f * GaussianDistribution(num19, 0f, 3f);
//				array6[num19] = (float)num19 * num17;
//				array5[num19] = new Vector4(num18, num18, num18, 1f);
//			}
//			for (int num20 = 8; num20 < 15; num20++)
//			{
//				array5[num20] = array5[num20 - 7];
//				array6[num20] = 0f - array6[num20 - 7];
//			}
//			for (int num21 = 0; num21 < 16; num21++)
//			{
//				val6.Write<float>(array6[num21]);
//				val6.Write<Vector3>(Vector3.Zero);
//			}
//			for (int num22 = 0; num22 < 16; num22++)
//			{
//				val6.Write<Vector4>(array5[num22]);
//			}
//			base.Viewport.Context.UnmapSubresource((Resource)(object)postProcessConstants, 0);
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, bloomTextures[1].RTV);
//			base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, bloomTextures[1].Texture.get_Description().Width, bloomTextures[1].Texture.get_Description().Height)));
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psBloomBlur);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[1]
//			{
//				bloomTextures[2].SRV
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffers(1, (Buffer[])(object)new Buffer[1]
//			{
//				postProcessConstants
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSamplers(0, (SamplerState[])(object)new SamplerState[1]
//			{
//				D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)21)
//			});
//			base.Viewport.Context.Draw(6, 0);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "VerticalBlur");
//			DataStream val7 = null;
//			base.Viewport.Context.MapSubresource((Resource)(object)postProcessConstants, 0, (MapMode)4, (MapFlags)0, ref val7);
//			float num23 = 1f / (float)bloomTextures[1].Texture.get_Description().Height;
//			float num24 = 2f * GaussianDistribution(0f, 0f, 3f);
//			Vector4[] array7 = (Vector4[])(object)new Vector4[16];
//			float[] array8 = new float[16];
//			array7[0] = new Vector4(num24, num24, num24, 1f);
//			array8[0] = 0f;
//			for (int num25 = 1; num25 < 8; num25++)
//			{
//				num24 = 2f * GaussianDistribution(num25, 0f, 3f);
//				array8[num25] = (float)num25 * num23;
//				array7[num25] = new Vector4(num24, num24, num24, 1f);
//			}
//			for (int num26 = 8; num26 < 15; num26++)
//			{
//				array7[num26] = array7[num26 - 7];
//				array8[num26] = 0f - array8[num26 - 7];
//			}
//			for (int num27 = 0; num27 < 16; num27++)
//			{
//				val7.Write<float>(0f);
//				val7.Write<float>(array8[num27]);
//				val7.Write<Vector2>(Vector2.Zero);
//			}
//			for (int num28 = 0; num28 < 16; num28++)
//			{
//				val7.Write<Vector4>(array7[num28]);
//			}
//			base.Viewport.Context.UnmapSubresource((Resource)(object)postProcessConstants, 0);
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, bloomTextures[0].RTV);
//			base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, bloomTextures[0].Texture.get_Description().Width, bloomTextures[0].Texture.get_Description().Height)));
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psBloomBlur);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[1]
//			{
//				bloomTextures[1].SRV
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffers(1, (Buffer[])(object)new Buffer[1]
//			{
//				postProcessConstants
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSamplers(0, (SamplerState[])(object)new SamplerState[1]
//			{
//				D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)21)
//			});
//			base.Viewport.Context.Draw(6, 0);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "RenderBloom");
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, postProcessTexture.RTV);
//			base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, lightAccumulationTexture.Texture.get_Description().Width, lightAccumulationTexture.Texture.get_Description().Height)));
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState((RenderTargetBlendDescription[])(object)new RenderTargetBlendDescription[1]
//			{
//				new RenderTargetBlendDescription
//				{
//					IsBlendEnabled = RawBool.op_Implicit(true),
//					SourceBlend = (BlendOption)2,
//					DestinationBlend = (BlendOption)2,
//					BlendOperation = (BlendOperation)1,
//					SourceAlphaBlend = (BlendOption)2,
//					DestinationAlphaBlend = (BlendOption)2,
//					AlphaBlendOperation = (BlendOperation)1,
//					RenderTargetWriteMask = (ColorWriteMaskFlags)15
//				}
//			}));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psRenderBloom);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[1]
//			{
//				bloomTextures[0].SRV
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSamplers(0, (SamplerState[])(object)new SamplerState[1]
//			{
//				D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)21)
//			});
//			base.Viewport.Context.Draw(6, 0);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//		}

//		private void PostProcessHBAO()
//		{
//			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0126: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0131: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0136: Unknown result type (might be due to invalid IL or missing references)
//			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
//			if (HBAOEnabled)
//			{
//				D3DUtils.BeginPerfEvent(base.Viewport.Context, "HBAO");
//				GFSDK_TXAA.GetJitter(out float[] outJitter);
//				GFSDK_SSAO.InputData inputData = default(GFSDK_SSAO.InputData);
//				inputData.DepthData.pFullResDepthTextureSRV = ((CppObject)base.Viewport.DepthBufferSRV).get_NativePointer();
//				inputData.DepthData.DepthTextureType = GFSDK_SSAO.DepthTextureType.HardwareDepths;
//				inputData.DepthData.MetersToViewSpaceUnits = 1f;
//				inputData.DepthData.ProjectionMatrix.Data = camera.GetProjMatrix(outJitter);
//				inputData.DepthData.ProjectionMatrix.Layout = GFSDK_SSAO.MatrixLayout.RowMajorOrder;
//				inputData.DepthData.Viewport = GFSDK_SSAO.InputViewport.FromViewport(new Viewport(0, 0, base.Viewport.DepthBuffer.get_Description().Width, base.Viewport.DepthBuffer.get_Description().Height, 0f, 1f));
//				inputData.NormalData.Enable = true;
//				inputData.NormalData.pFullResNormalTextureSRV = ((CppObject)worldNormalsForHBAOTexture.SRV).get_NativePointer();
//				inputData.NormalData.WorldToViewMatrix.Data = Matrix.Scaling(-1f, 1f, 1f) * camera.GetViewMatrix();
//				inputData.NormalData.DecodeScale = 2f;
//				inputData.NormalData.DecodeBias = -1f;
//				inputData.NormalData.WorldToViewMatrix.Layout = GFSDK_SSAO.MatrixLayout.RowMajorOrder;
//				GFSDK_SSAO.Output output = default(GFSDK_SSAO.Output);
//				output.pRenderTargetView = ((CppObject)lightAccumulationTexture.RTV).get_NativePointer();
//				output.Blend.Mode = ((RenderMode != DebugRenderMode.HBAO) ? GFSDK_SSAO.BlendMode.MultiplyRGB : GFSDK_SSAO.BlendMode.OverwriteRGB);
//				hbaoContext.RenderAO(base.Viewport.Context, inputData, new GFSDK_SSAO.Parameters(dummy: true), output);
//				D3DUtils.EndPerfEvent(base.Viewport.Context);
//			}
//		}

//		private void PostProcessColorLookupTable()
//		{
//			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
//			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0105: Unknown result type (might be due to invalid IL or missing references)
//			//IL_010a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0145: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "ColorLookupTable");
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, finalColorTexture.RTV);
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_Rasterizer().SetViewport(Viewport.op_Implicit(new Viewport(0, 0, base.Viewport.ColorBuffer.get_Description().Width, base.Viewport.ColorBuffer.get_Description().Height)));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			if (LookupTable != null)
//			{
//				Texture2D val = ((ResourceView)LookupTable).ResourceAs<Texture2D>();
//				lookupTableConstants.UpdateData(base.Viewport.Context, new TableLookupConstants
//				{
//					LutSize = val.get_Description().Width,
//					FlipY = ((val.get_Description().Width == 33) ? 1f : 0f)
//				});
//				((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psLookupTable);
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffer(1, lookupTableConstants.Buffer);
//			}
//			else
//			{
//				((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psResolve);
//			}
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[2]
//			{
//				postProcessTexture.SRV,
//				LookupTable
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(1, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)21));
//			base.Viewport.Context.Draw(6, 0);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//		}

//		private void PostProcessCollectSelections()
//		{
//			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
//			if (meshes.Count != 0)
//			{
//				D3DUtils.BeginPerfEvent(base.Viewport.Context, "CollectSelections");
//				UpdateViewConstants(bJitter: false);
//				base.Viewport.Context.get_OutputMerger().SetRenderTargets(selectionDepthTexture.DSV, (RenderTargetView[])(object)new RenderTargetView[2]);
//				base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: true, (DepthWriteMask)1, (Comparison)4));
//				base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//				base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)2, (FillMode)3, antialiasedLines: false, depthClip: true));
//				((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, viewConstants.Buffer);
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffer(0, viewConstants.Buffer);
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResource(0, normalBasisCubemapTexture.SRV);
//				((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//				RenderMeshes(MeshRenderPath.Selection, new List<MeshRenderInstance>
//				{
//					meshes[0]
//				});
//				D3DUtils.EndPerfEvent(base.Viewport.Context);
//			}
//		}

//		private void PostProcessEditorPrimitives()
//		{
//			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0267: Unknown result type (might be due to invalid IL or missing references)
//			//IL_026c: Unknown result type (might be due to invalid IL or missing references)
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "EditorPrimitives");
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets(editorCompositeDepthTexture.DSV, (RenderTargetView[])(object)new RenderTargetView[2]);
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: true, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psResolveDepthToMsaa);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[1]
//			{
//				base.Viewport.DepthBufferSRV
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//			base.Viewport.Context.Draw(6, 0);
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets(editorCompositeDepthTexture.DSV, editorCompositeTexture.RTV);
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: true, (DepthWriteMask)0, (Comparison)4));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)2, (FillMode)3, antialiasedLines: false, depthClip: true));
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, viewConstants.Buffer);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetConstantBuffer(0, viewConstants.Buffer);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResource(0, normalBasisCubemapTexture.SRV);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//			RenderMeshes(MeshRenderPath.Forward, editorMeshes);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//		}

//		private void PostProcessSelectionOutline()
//		{
//			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
//			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "SelectionOutline");
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, selectionOutlineTexture.RTV);
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psSelectionOutline);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[2]
//			{
//				finalColorTexture.SRV,
//				selectionDepthTexture.SRV
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//			base.Viewport.Context.Draw(6, 0);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//		}

//		private void PostProcessEditorComposite()
//		{
//			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
//			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
//			D3DUtils.BeginPerfEvent(base.Viewport.Context, "EditorComposite");
//			base.Viewport.Context.get_OutputMerger().SetRenderTargets((DepthStencilView)null, base.Viewport.ColorBufferRTV);
//			base.Viewport.Context.get_OutputMerger().set_DepthStencilState(D3DUtils.CreateDepthStencilState(depthEnabled: false, (DepthWriteMask)1, (Comparison)2));
//			base.Viewport.Context.get_OutputMerger().set_BlendState(D3DUtils.CreateBlendState(D3DUtils.CreateBlendStateRenderTarget()));
//			base.Viewport.Context.get_Rasterizer().set_State(D3DUtils.CreateRasterizerState((CullMode)1, (FillMode)3));
//			base.Viewport.Context.get_InputAssembler().SetIndexBuffer((Buffer)null, (Format)0, 0);
//			base.Viewport.Context.get_InputAssembler().SetVertexBuffers(0, default(VertexBufferBinding));
//			base.Viewport.Context.get_InputAssembler().set_InputLayout((InputLayout)null);
//			((CommonShaderStage<VertexShader>)(object)base.Viewport.Context.get_VertexShader()).Set(vsFullscreenQuad);
//			((CommonShaderStage)base.Viewport.Context.get_VertexShader()).SetConstantBuffer(0, commonConstants.Buffer);
//			((CommonShaderStage<PixelShader>)(object)base.Viewport.Context.get_PixelShader()).Set(psEditorComposite);
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetShaderResources(0, (ShaderResourceView[])(object)new ShaderResourceView[2]
//			{
//				selectionOutlineTexture.SRV,
//				editorCompositeTexture.SRV
//			});
//			((CommonShaderStage)base.Viewport.Context.get_PixelShader()).SetSampler(0, D3DUtils.CreateSamplerState((TextureAddressMode)3, (TextureAddressMode)0, (TextureAddressMode)0, (TextureAddressMode)0, null, (Comparison)8, (Filter)0));
//			base.Viewport.Context.Draw(6, 0);
//			D3DUtils.EndPerfEvent(base.Viewport.Context);
//		}

//		protected virtual BoundingBox CalcWorldBoundingBox()
//		{
//			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
//			return default(BoundingBox);
//		}

//		private void BeginFrameActions()
//		{
//		}

//		private void EndFrameActions()
//		{
//		}

//		private float GaussianDistribution(float x, float y, float rho)
//		{
//			return 1f / (float)Math.Sqrt(Math.PI * 2.0 * (double)rho * (double)rho) * (float)Math.Exp((0f - (x * x + y * y)) / (2f * rho * rho));
//		}
//	}



//	internal struct Matrix4x3
//	{
//		public float M11;

//		public float M12;

//		public float M13;

//		public float M14;

//		public float M21;

//		public float M22;

//		public float M23;

//		public float M24;

//		public float M31;

//		public float M32;

//		public float M33;

//		public float M34;

//		public Matrix4x3(float[] v)
//		{
//			if (v.Length < 12)
//			{
//				throw new InvalidOperationException();
//			}
//			M11 = v[0];
//			M12 = v[1];
//			M13 = v[2];
//			M14 = v[3];
//			M21 = v[4];
//			M22 = v[5];
//			M23 = v[6];
//			M24 = v[7];
//			M31 = v[8];
//			M32 = v[9];
//			M33 = v[10];
//			M34 = v[11];
//		}
//	}
//}

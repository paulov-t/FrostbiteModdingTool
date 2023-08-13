//using SharpDX.Direct3D11;
//using FrostySdk.Resources;
//using SharpDX;
//using SharpDX.Direct3D;
//using SharpDX.DXGI;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using Buffer = SharpDX.Direct3D11.Buffer;

//namespace FrostbiteModdingUI.Viewport
//{
//	public class MeshRenderBase
//	{
//		public virtual string DebugName
//		{
//			get;
//		}

//		public virtual void Render(DeviceContext context, MeshRenderPath renderPath)
//		{
//		}
//	}
//	public enum MeshRenderPath
//	{
//		Deferred,
//		Forward,
//		Shadows,
//		Selection
//	}

//	public struct MeshRenderInstance
//	{
//		public MeshRenderBase RenderMesh;

//		public Matrix Transform;
//	}

//	public enum LightRenderType
//	{
//		Sphere
//	}

//	public struct LightRenderInstance
//	{
//		public LightRenderType Type;

//		public Matrix Transform;

//		public Vector3 Color;

//		public float Intensity;

//		public float AttenuationRadius;

//		public float SphereRadius;

//		public int LightId;
//	}

//	public class MeshRenderShape : MeshRenderBase, IDisposable
//	{
//		private struct ShapeVertex
//		{
//			public Vector3 Pos;

//			public Vector3 Normal;

//			public Vector2 TexCoord;

//			public ShapeVertex(Vector3 p, Vector3 n, Vector2 t)
//			{
//				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
//				//IL_0002: Unknown result type (might be due to invalid IL or missing references)
//				//IL_0008: Unknown result type (might be due to invalid IL or missing references)
//				//IL_0009: Unknown result type (might be due to invalid IL or missing references)
//				//IL_000f: Unknown result type (might be due to invalid IL or missing references)
//				//IL_0010: Unknown result type (might be due to invalid IL or missing references)
//				Pos = p;
//				Normal = n;
//				TexCoord = t;
//			}
//		}

//		private Buffer vertexBuffer;

//		private Buffer indexBuffer;

//		private Buffer pixelParameters;

//		private List<ShaderResourceView> pixelTextures = new List<ShaderResourceView>();

//		private ShaderPermutation permutation;

//		private int indexCount;

//		private string name;

//		public override string DebugName => name;

//		public static MeshRenderShape CreateSphere(RenderCreateState state, string inName, string shaderName, float radius, int tessellation)
//		{
//			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
//			List<ShapeVertex> list = new List<ShapeVertex>();
//			int num = tessellation * 2;
//			Vector3 val = default(Vector3);
//			for (int i = 0; i <= tessellation; i++)
//			{
//				_ = (float)i / (float)tessellation;
//				float value = (float)((double)i * Math.PI / (double)tessellation - Math.PI / 2.0);
//				float Sin = 0f;
//				float Cos = 0f;
//				DirectXMathUtils.XMScalarSinCos(ref Sin, ref Cos, value);
//				for (int j = 0; j <= num; j++)
//				{
//					_ = (float)j / (float)num;
//					float value2 = (float)((double)j * (Math.PI * 2.0) / (double)num);
//					float Sin2 = 0f;
//					float Cos2 = 0f;
//					DirectXMathUtils.XMScalarSinCos(ref Sin2, ref Cos2, value2);
//					Sin2 *= Cos;
//					Cos2 *= Cos;
//					((Vector3)(ref val))._002Ector(Sin2, Sin, Cos2);
//					Vector3 p = val * radius;
//					list.Add(new ShapeVertex(p, Vector3.TransformCoordinate(val, Matrix.Scaling(-1f, 1f, -1f)), Vector2.Zero));
//				}
//			}
//			int num2 = num + 1;
//			List<ushort> list2 = new List<ushort>();
//			for (int k = 0; k < tessellation; k++)
//			{
//				for (int l = 0; l <= num; l++)
//				{
//					int num3 = k + 1;
//					int num4 = (l + 1) % num2;
//					list2.Add((ushort)(k * num2 + num4));
//					list2.Add((ushort)(k * num2 + l));
//					list2.Add((ushort)(num3 * num2 + l));
//					list2.Add((ushort)(num3 * num2 + num4));
//					list2.Add((ushort)(k * num2 + num4));
//					list2.Add((ushort)(num3 * num2 + l));
//				}
//			}
//			return new MeshRenderShape(state, inName, shaderName, list, list2);
//		}

//		public static MeshRenderShape CreateCube(RenderCreateState state, string inName, string shaderName, int width, int height, int depth)
//		{
//			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
//			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
//			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
//			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
//			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0102: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
//			//IL_011d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0122: Unknown result type (might be due to invalid IL or missing references)
//			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0140: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0147: Unknown result type (might be due to invalid IL or missing references)
//			//IL_014e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0153: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0155: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0157: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0159: Unknown result type (might be due to invalid IL or missing references)
//			//IL_015e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0160: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0162: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0164: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0169: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
//			//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
//			//IL_020c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_020e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0210: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0215: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0217: Unknown result type (might be due to invalid IL or missing references)
//			//IL_021c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_021d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0222: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0226: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0236: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0238: Unknown result type (might be due to invalid IL or missing references)
//			//IL_023a: Unknown result type (might be due to invalid IL or missing references)
//			//IL_023f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0241: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0246: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0247: Unknown result type (might be due to invalid IL or missing references)
//			//IL_024c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0250: Unknown result type (might be due to invalid IL or missing references)
//			Vector3[] array = (Vector3[])(object)new Vector3[6]
//			{
//				new Vector3(0f, 0f, 1f),
//				new Vector3(0f, 0f, -1f),
//				new Vector3(1f, 0f, 0f),
//				new Vector3(-1f, 0f, 0f),
//				new Vector3(0f, 1f, 0f),
//				new Vector3(0f, -1f, 0f)
//			};
//			Vector2[] array2 = (Vector2[])(object)new Vector2[4]
//			{
//				new Vector2(1f, 0f),
//				new Vector2(1f, 1f),
//				new Vector2(0f, 1f),
//				new Vector2(0f, 0f)
//			};
//			Vector3 val = new Vector3((float)width, (float)height, (float)depth);
//			val /= 2f;
//			List<ShapeVertex> list = new List<ShapeVertex>();
//			List<ushort> list2 = new List<ushort>();
//			for (int i = 0; i < 6; i++)
//			{
//				Vector3 val2 = array[i];
//				Vector3 val3 = (i >= 4) ? Vector3.UnitZ : Vector3.UnitY;
//				Vector3 val4 = Vector3.Cross(val2, val3);
//				Vector3 val5 = Vector3.Cross(val2, val4);
//				int count = list.Count;
//				list2.Add((ushort)(count + 2));
//				list2.Add((ushort)(count + 1));
//				list2.Add((ushort)count);
//				list2.Add((ushort)(count + 3));
//				list2.Add((ushort)(count + 2));
//				list2.Add((ushort)count);
//				list.Add(new ShapeVertex((val2 - val4 - val5) * val, val2, array2[0]));
//				list.Add(new ShapeVertex((val2 - val4 + val5) * val, val2, array2[1]));
//				list.Add(new ShapeVertex((val2 + val4 + val5) * val, val2, array2[2]));
//				list.Add(new ShapeVertex((val2 + val4 - val5) * val, val2, array2[3]));
//			}
//			return new MeshRenderShape(state, inName, shaderName, list, list2);
//		}

//		public static MeshRenderShape CreatePlane(RenderCreateState state, string inName, string shaderName, int width, int depth)
//		{
//			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
//			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
//			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
//			List<ShapeVertex> vertices = new List<ShapeVertex>
//			{
//				new ShapeVertex(new Vector3((float)(-width), 0f, (float)depth), new Vector3(0f, 1f, 0f), new Vector2(0f, 0f)),
//				new ShapeVertex(new Vector3((float)width, 0f, (float)depth), new Vector3(0f, 1f, 0f), new Vector2(1f, 0f)),
//				new ShapeVertex(new Vector3((float)width, 0f, (float)(-depth)), new Vector3(0f, 1f, 0f), new Vector2(1f, 1f)),
//				new ShapeVertex(new Vector3((float)(-width), 0f, (float)(-depth)), new Vector3(0f, 1f, 0f), new Vector2(0f, 1f))
//			};
//			List<ushort> indices = new List<ushort>
//			{
//				0,
//				1,
//				2,
//				0,
//				2,
//				3
//			};
//			return new MeshRenderShape(state, inName, shaderName, vertices, indices);
//		}

//		private MeshRenderShape(RenderCreateState state, string inName, string shaderName, List<ShapeVertex> vertices, List<ushort> indices)
//		{
//			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0022: Expected O, but got Unknown
//			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0057: Expected O, but got Unknown
//			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
//			//IL_0075: Expected O, but got Unknown
//			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
//			//IL_00ac: Expected O, but got Unknown
//			DataStream val = (DataStream)(object)new DataStream(indices.Count * 2, false, true);
//			try
//			{
//				val.WriteRange<ushort>(indices.ToArray());
//				((Stream)(object)val).Position = 0L;
//				indexBuffer = (Buffer)(object)new Buffer(state.Device, val, indices.Count * 2, (ResourceUsage)0, (BindFlags)2, (CpuAccessFlags)0, (ResourceOptionFlags)0, 2);
//			}
//			finally
//			{
//				((IDisposable)val)?.Dispose();
//			}
//			DataStream val2 = (DataStream)(object)new DataStream(vertices.Count * 32, false, true);
//			try
//			{
//				val2.WriteRange<ShapeVertex>(vertices.ToArray());
//				((Stream)(object)val2).Position = 0L;
//				vertexBuffer = (Buffer)(object)new Buffer(state.Device, val2, vertices.Count * 32, (ResourceUsage)0, (BindFlags)1, (CpuAccessFlags)0, (ResourceOptionFlags)0, 32);
//			}
//			finally
//			{
//				((IDisposable)val2)?.Dispose();
//			}
//			GeometryDeclarationDesc.Element[] array = new GeometryDeclarationDesc.Element[3];
//			GeometryDeclarationDesc.Element element = array[0] = new GeometryDeclarationDesc.Element
//			{
//				Usage = VertexElementUsage.Pos,
//				Format = VertexElementFormat.Float3
//			};
//			element = (array[1] = new GeometryDeclarationDesc.Element
//			{
//				Usage = VertexElementUsage.Normal,
//				Format = VertexElementFormat.Float3
//			});
//			element = (array[2] = new GeometryDeclarationDesc.Element
//			{
//				Usage = VertexElementUsage.TexCoord0,
//				Format = VertexElementFormat.Float2
//			});
//			GeometryDeclarationDesc geomDecl = GeometryDeclarationDesc.Create(array);
//			permutation = state.ShaderLibrary.GetUserShader(shaderName, geomDecl);
//			permutation.LoadShaders(state.Device);
//			permutation.AssignParameters(state, ref pixelParameters, ref pixelTextures);
//			indexCount = indices.Count;
//			name = inName;
//		}

//		public override void Render(DeviceContext context, MeshRenderPath renderPath)
//		{
//			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
//			if (renderPath != MeshRenderPath.Shadows && renderPath != MeshRenderPath.Selection)
//			{
//				context.get_InputAssembler().SetIndexBuffer(indexBuffer, (Format)57, 0);
//				context.get_InputAssembler().set_PrimitiveTopology((PrimitiveTopology)4);
//				context.get_InputAssembler().SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, 32, 0));
//				permutation.SetState(context, renderPath);
//				((CommonShaderStage)context.get_PixelShader()).SetConstantBuffer(2, pixelParameters);
//				((CommonShaderStage)context.get_PixelShader()).SetShaderResources(1, pixelTextures.ToArray());
//				context.DrawIndexed(indexCount, 0, 0);
//			}
//		}

//		public void Dispose()
//		{
//			if (pixelParameters != null)
//			{
//				((DisposeBase)pixelParameters).Dispose();
//			}
//			((DisposeBase)indexBuffer).Dispose();
//			((DisposeBase)vertexBuffer).Dispose();
//			pixelTextures.Clear();
//		}
//	}
//}


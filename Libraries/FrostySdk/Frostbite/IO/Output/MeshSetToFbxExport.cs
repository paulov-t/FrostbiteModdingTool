using FMT.FileTools;
using FrostbiteSdk;
using FrostbiteSdk.FbxExporter;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
//using SharpDX;

namespace FrostySdk.Frostbite.IO.Output
{
    public class MeshSetToFbxExport
    {
        private int totalExportCount;

        private int currentProgress;

        private int boneCount;

        private bool flattenHierarchy;

        private FbxGeometryConverter geomConverter;

        private int lodIndex;

        public void Export(AssetManager assetManager, dynamic meshAsset, string filename, string fbxVersion, string units, bool flattenHierarchy, string skeleton, string fileType, params MeshSet[] meshSets)
        {
            //if (assetManager == null)
            //{
            //	throw new ArgumentNullException("assetManager");
            //}
            if (meshAsset == null)
            {
                throw new ArgumentNullException("meshAsset");
            }
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }
            if (fbxVersion == null)
            {
                throw new ArgumentNullException("fbxVersion");
            }
            if (units == null)
            {
                throw new ArgumentNullException("units");
            }
            if (fileType == null)
            {
                throw new ArgumentNullException("fileType");
            }
            this.flattenHierarchy = flattenHierarchy;
            using FbxManager fbxManager = new FbxManager();
            FbxIOSettings fbxIOSettings = new FbxIOSettings(fbxManager, "IOSRoot");
            fbxIOSettings.SetBoolProp("Export|AdvOptGrp|Fbx|Material", pValue: false);
            fbxIOSettings.SetBoolProp("Export|AdvOptGrp|Fbx|Texture", pValue: false);
            fbxIOSettings.SetBoolProp("Export|AdvOptGrp|Fbx|Global_Settings", pValue: true);
            fbxIOSettings.SetBoolProp("Export|AdvOptGrp|Fbx|Shape", pValue: true);
            fbxManager.SetIOSettings(fbxIOSettings);
            FbxScene fbxScene = new FbxScene(fbxManager, "")
            {
                SceneInfo = new FbxDocumentInfo(fbxManager, "SceneInfo")
                {
                    Title = "FBX Exporter",
                    Subject = "Export FBX meshes from",
                    OriginalApplicationVendor = string.Empty,
                    OriginalApplicationName = string.Empty,
                    OriginalApplicationVersion = string.Empty,
                    LastSavedApplicationVendor = string.Empty,
                    LastSavedApplicationName = string.Empty,
                    LastSavedApplicationVersion = string.Empty,
                }
            };
            geomConverter = new FbxGeometryConverter(fbxManager);
            switch (units)
            {
                case "Millimeters":
                    fbxScene.GlobalSettings.SetSystemUnit(FbxSystemUnit.Millimeters);
                    break;
                case "Centimeters":
                    fbxScene.GlobalSettings.SetSystemUnit(FbxSystemUnit.Centimeters);
                    break;
                case "Meters":
                    fbxScene.GlobalSettings.SetSystemUnit(FbxSystemUnit.Meters);
                    break;
                case "Kilometers":
                    fbxScene.GlobalSettings.SetSystemUnit(FbxSystemUnit.Kilometers);
                    break;
            }
            totalExportCount++;
            MeshSet[] array = meshSets;
            for (int k = 0; k < array.Length; k++)
            {
                foreach (MeshSetLod lod in array[k].Lods)
                {
                    foreach (MeshSetSection section in lod.Sections)
                    {
                        totalExportCount += ((section.Name != "") ? 1 : 0);
                    }
                }
            }
            List<FbxNode> boneNodes = new List<FbxNode>();
            if (meshSets[0].Lods[0].Type == MeshType.MeshType_Skinned && !string.IsNullOrEmpty(skeleton))
            {
                FbxNode pNode = this.FBXCreateSkeleton(assetManager, fbxScene, meshAsset, skeleton, ref boneNodes);
                fbxScene.RootNode.AddChild(pNode);
            }
            currentProgress++;
            array = meshSets;
            foreach (MeshSet meshSet in array)
            {
                for (int i = 0; i < meshSet.Lods.Count; i++)
                {
                    FBXCreateMesh(fbxScene, meshSet.Lods[i], boneNodes);
                }
            }
            using FbxExporter fbxExporter = new FbxExporter(fbxManager, "");
            fbxExporter.SetFileExportVersion("FBX" + fbxVersion + "00");
            int writerFormatCount = fbxManager.IOPluginRegistry.WriterFormatCount;
            for (int j = 0; j < writerFormatCount; j++)
            {
                if (fbxManager.IOPluginRegistry.GetWriterFormatDescription(j).Contains(fileType))
                {
                    if (fbxExporter.Initialize(filename, j, fbxIOSettings))
                    {
                        fbxExporter.Export(fbxScene);
                    }
                    break;
                }
            }
            geomConverter.Dispose();
        }

        private FbxNode FBXCreateCompositeSkeleton(FbxScene scene, List<LinearTransform> partTransforms, ref List<FbxNode> boneNodes)
        {
            int num = 0;
            FbxSkeleton fbxSkeleton = new FbxSkeleton(scene, "ROOT");
            fbxSkeleton.SetSkeletonType(FbxSkeleton.EType.eRoot);
            fbxSkeleton.Size = 1.0;
            FbxNode fbxNode = new FbxNode(scene, "ROOT");
            fbxNode.SetNodeAttribute(fbxSkeleton);
            Matrix4x4 identity = Matrix4x4.Identity;
            Vector3 translationVector = identity.Translation;
            Vector3 val = SharpDXUtils.ExtractEulerAngles(identity);
            fbxNode.LclTranslation = new Vector3(translationVector.X, translationVector.Y, translationVector.Z);
            fbxNode.LclRotation = new Vector3(val.X, val.Y, val.Z);
            foreach (LinearTransform partTransform in partTransforms)
            {
                _ = partTransform;
                string pName = "PART_" + num;
                fbxSkeleton = new FbxSkeleton(scene, pName);
                fbxSkeleton.SetSkeletonType(FbxSkeleton.EType.eLimbNode);
                fbxSkeleton.Size = 1.0;
                FbxNode fbxNode2 = new FbxNode(scene, pName);
                fbxNode2.SetNodeAttribute(fbxSkeleton);
                fbxNode2.LclTranslation = new Vector3(translationVector.X, translationVector.Y, translationVector.Z);
                fbxNode2.LclRotation = new Vector3(val.X, val.Y, val.Z);
                boneNodes.Add(fbxNode2);
                fbxNode.AddChild(fbxNode2);
                num++;
            }
            return fbxNode;
        }

        private FbxNode FBXCreateSkeleton(AssetManager assetManager, FbxScene scene, dynamic meshAsset, string skeleton, ref List<FbxNode> boneNodes)
        {
            var skeletonEntry = assetManager.GetEbxEntry(skeleton);
            dynamic rootObject = assetManager.GetEbx(skeletonEntry).RootObject;
            boneCount = rootObject.BoneNames.Count;
            for (int i = 0; i < boneCount; i++)
            {
                dynamic val = rootObject.LocalPose;
                Matrix4x4 j = new Matrix4x4(val[i].right.x, val[i].right.y, val[i].right.z, 0f, val[i].up.x, val[i].up.y, val[i].up.z, 0f, val[i].forward.x, val[i].forward.y, val[i].forward.z, 0f, val[i].trans.x, val[i].trans.y, val[i].trans.z, 1f);
                Vector3 translationVector = j.Translation;
                Vector3 val2 = SharpDXUtils.ExtractEulerAngles(j);
                FbxSkeleton fbxSkeleton = new FbxSkeleton(scene, rootObject.BoneNames[i]);
                fbxSkeleton.SetSkeletonType((i != 0) ? FbxSkeleton.EType.eLimbNode : FbxSkeleton.EType.eRoot);
                fbxSkeleton.Size = 1.0;
                FbxNode fbxNode = new FbxNode(scene, rootObject.BoneNames[i]);
                fbxNode.SetNodeAttribute(fbxSkeleton);
                fbxNode.LclTranslation = new Vector3(translationVector.X, translationVector.Y, translationVector.Z);
                fbxNode.LclRotation = new Vector3(val2.X, val2.Y, val2.Z);
                if (i != 0)
                {
                    int index = rootObject.Hierarchy[i];
                    boneNodes[index].AddChild(fbxNode);
                }
                boneNodes.Add(fbxNode);
            }
            return boneNodes[0];
        }

        private void FBXCreateMesh(FbxScene scene, MeshSetLod lod, List<FbxNode> boneNodes)
        {
            int indexSize = lod.IndexUnitSize / 8;
            FbxNode fbxNode = (flattenHierarchy ? scene.RootNode : new FbxNode(scene, lod.String03));
            foreach (MeshSetSection section in lod.Sections)
            {
                if (section.Name == "")
                {
                    continue;
                }
                NativeReader reader = null;
                if (AssetManager.Instance != null && lod.ChunkId != Guid.Empty)
                    reader = new NativeReader(AssetManager.Instance.GetChunk(AssetManager.Instance.GetChunkEntry(lod.ChunkId)));
                else if (lod.InlineData != null && lod.InlineData.Length > 0)
                    reader = new NativeReader(new MemoryStream(lod.InlineData));
                else if (FIFAMod.CurrentFIFAModInstance != null)
                {
                    var modResource = FIFAMod.CurrentFIFAModInstance.Resources.FirstOrDefault(x => x.Type == ModResourceType.Chunk && Guid.Parse(x.Name) == lod.ChunkId);
                    if (modResource != null)
                    {
                        var modResourceData = new CasReader(new MemoryStream(FIFAMod.CurrentFIFAModInstance.GetResourceData(modResource))).Read();
                        reader = new NativeReader(new MemoryStream(modResourceData));
                    }

                }

                if (reader == null)
                    continue;

                reader.Position = 0;
                var debugMeshesChunkDataPath = "_MeshesChunkData_" + ProfileManager.ProfileName + ".dat";
                if (File.Exists(debugMeshesChunkDataPath))
                    File.Delete(debugMeshesChunkDataPath);

                File.WriteAllBytes(debugMeshesChunkDataPath, reader.ReadToEnd());
                reader.Position = 0;


                FbxNode fbxNode2 = FBXExportSubObject(scene, section, lod.VertexBufferSize, indexSize, reader);
                if (flattenHierarchy)
                {
                    fbxNode2.Name = lod.String03 + ":" + section.Name;
                    //fbxNode2.Name = lod.MeshName + ":" + section.materialName;
                }
                fbxNode.AddChild(fbxNode2);
                if ((lod.Type != MeshType.MeshType_Skinned && lod.Type != MeshType.MeshType_Composite) || boneNodes.Count <= 0)
                {
                    continue;
                }
                List<ushort> boneList = section.BoneList;
                if (lod.Type == MeshType.MeshType_Composite && lod.PartTransforms.Count != 0)
                {
                    boneList = new List<ushort>();
                    for (ushort num = 0; num < lod.PartTransforms.Count; num = (ushort)(num + 1))
                    {
                        boneList.Add(num);
                    }
                }

                // Find Out Why? : For some reason, the bone indexes require repacking?
                //if (ProfileManager.DataVersion == 20160927
                //	|| ProfileManager.DataVersion == 20170929
                //	|| ProfileManager.DataVersion == 20180807
                //	|| ProfileManager.DataVersion == 20180914
                //	|| ProfileManager.DataVersion == 20180628
                //	|| ProfileManager.IsFIFA20DataVersion()
                //	|| ProfileManager.IsFIFA21DataVersion()
                //	|| ProfileManager.IsFIFA22DataVersion()
                //	|| ProfileManager.IsFIFA23DataVersion()
                //                )
                {
                    boneList.Clear();
                    for (ushort boneIndex = 0; boneIndex < boneNodes.Count; boneIndex = (ushort)(boneIndex + 1))
                    {
                        boneList.Add(boneIndex);
                    }
                }
                FBXCreateSkin(scene, section, fbxNode2, boneNodes, boneList, lod.Type, reader);
                FBXCreateBindPose(scene, section, fbxNode2);
            }
            if (!flattenHierarchy)
            {
                scene.RootNode.AddChild(fbxNode);
            }
        }

        private void FBXCreateBindPose(FbxScene scene, MeshSetSection section, FbxNode actor)
        {
            List<FbxNode> list = new List<FbxNode>();
            FbxMesh fbxMesh = new FbxMesh(actor.GetNodeAttribute(FbxNodeAttribute.EType.eMesh));
            int deformerCount = fbxMesh.GetDeformerCount(FbxDeformer.EDeformerType.eSkin);
            int num = 0;
            for (int i = 0; i < deformerCount; i++)
            {
                FbxSkin fbxSkin = new FbxSkin(fbxMesh.GetDeformer(i, FbxDeformer.EDeformerType.eSkin));
                num += fbxSkin.ClusterCount;
            }
            if (num > 0)
            {
                for (int j = 0; j < deformerCount; j++)
                {
                    FbxSkin fbxSkin2 = new FbxSkin(fbxMesh.GetDeformer(j, FbxDeformer.EDeformerType.eSkin));
                    for (int k = 0; k < fbxSkin2.ClusterCount; k++)
                    {
                        FbxNode link = fbxSkin2.GetCluster(k).GetLink();
                        FBXAddNodeRecursively(list, link);
                    }
                }
                list.Add(actor);
            }
            if (list.Count > 0)
            {
                FbxPose fbxPose = new FbxPose(scene, section.Name)
                {
                    IsBindPose = true
                };
                for (int l = 0; l < list.Count; l++)
                {
                    FbxNode fbxNode = list[l];
                    FbxMatrix matrix = new FbxMatrix(fbxNode.EvaluateGlobalTransform());
                    fbxPose.Add(fbxNode, matrix);
                }
                scene.AddPose(fbxPose);
            }
        }

        private void FBXAddNodeRecursively(List<FbxNode> nodes, FbxNode node)
        {
            if (node != null)
            {
                FBXAddNodeRecursively(nodes, node.GetParent());
                if (!nodes.Contains(node))
                {
                    nodes.Add(node);
                }
            }
        }

        private void FBXCreateSkin(FbxScene scene, MeshSetSection section, FbxNode actor, List<FbxNode> boneNodes, List<ushort> boneList, MeshType meshType, NativeReader reader)
        {
            FbxMesh fbxMesh = new FbxMesh(actor.GetNodeAttribute(FbxNodeAttribute.EType.eMesh));
            FbxSkin fbxSkin = new FbxSkin(scene, "");
            List<FbxCluster> list = new List<FbxCluster>();
            for (int i = 0; i < section.VertexCount; i++)
            {
                ushort[] boneIndicy = new ushort[8];
                float[] boneWeight = new float[8];
                int totalStride = 0;
                GeometryDeclarationDesc.Stream[] streams = section.GeometryDeclDesc[0].Streams;
                for (int j = 0; j < streams.Length; j++)
                {
                    GeometryDeclarationDesc.Stream stream = streams[j];
                    if (stream.VertexStride == 0)
                    {
                        continue;
                    }
                    int currentStride = 0;
                    GeometryDeclarationDesc.Element[] elements = section.GeometryDeclDesc[0].Elements;
                    for (int k = 0; k < elements.Length; k++)
                    {
                        GeometryDeclarationDesc.Element element = elements[k];
                        if (currentStride >= totalStride && currentStride < totalStride + stream.VertexStride && (element.Usage == VertexElementUsage.BoneIndices || element.Usage == VertexElementUsage.BoneIndices2 || element.Usage == VertexElementUsage.BoneWeights || element.Usage == VertexElementUsage.BoneWeights2))
                        {
                            reader.Position = section.VertexOffset + totalStride * section.VertexCount + i * stream.VertexStride + (currentStride - totalStride);
                            if (element.Usage == VertexElementUsage.BoneIndices)
                            {
                                if (element.Format == VertexElementFormat.Byte4 || element.Format == VertexElementFormat.Byte4N || element.Format == VertexElementFormat.UByte4 || element.Format == VertexElementFormat.UByte4N)
                                {
                                    boneIndicy[3] = reader.ReadByte();
                                    boneIndicy[2] = reader.ReadByte();
                                    boneIndicy[1] = reader.ReadByte();
                                    boneIndicy[0] = reader.ReadByte();
                                }
                                else if (element.Format == VertexElementFormat.UShort4 || element.Format == VertexElementFormat.UShort4N)
                                {
                                    boneIndicy[3] = reader.ReadUShort();
                                    boneIndicy[2] = reader.ReadUShort();
                                    boneIndicy[1] = reader.ReadUShort();
                                    boneIndicy[0] = reader.ReadUShort();
                                }
                            }
                            else if (element.Usage == VertexElementUsage.BoneIndices2)
                            {
                                if (element.Format == VertexElementFormat.Byte4 || element.Format == VertexElementFormat.Byte4N || element.Format == VertexElementFormat.UByte4 || element.Format == VertexElementFormat.UByte4N)
                                {
                                    boneIndicy[7] = reader.ReadByte();
                                    boneIndicy[6] = reader.ReadByte();
                                    boneIndicy[5] = reader.ReadByte();
                                    boneIndicy[4] = reader.ReadByte();
                                }
                                else if (element.Format == VertexElementFormat.UShort4 || element.Format == VertexElementFormat.UShort4N)
                                {
                                    boneIndicy[7] = reader.ReadUShort();
                                    boneIndicy[6] = reader.ReadUShort();
                                    boneIndicy[5] = reader.ReadUShort();
                                    boneIndicy[4] = reader.ReadUShort();
                                }
                            }
                            else if (element.Usage == VertexElementUsage.BoneWeights)
                            {
                                boneWeight[3] = reader.ReadByte() / 255f;
                                boneWeight[2] = reader.ReadByte() / 255f;
                                boneWeight[1] = reader.ReadByte() / 255f;
                                boneWeight[0] = reader.ReadByte() / 255f;
                            }
                            else if (element.Usage == VertexElementUsage.BoneWeights2)
                            {
                                boneWeight[7] = reader.ReadByte() / 255f;
                                boneWeight[6] = reader.ReadByte() / 255f;
                                boneWeight[5] = reader.ReadByte() / 255f;
                                boneWeight[4] = reader.ReadByte() / 255f;
                            }
                        }
                        currentStride += element.Size;
                    }
                    totalStride += stream.VertexStride;
                }
                if (meshType == MeshType.MeshType_Composite)
                {
                    boneWeight[0] = 1f;
                }
                for (int l = 0; l < 8; l++)
                {
                    if (boneWeight[l] > 0f)
                    {
                        int indIndex = boneIndicy[l];
                        
                        indIndex = boneList[indIndex];
                        if (((uint)indIndex & 0x8000u) != 0)
                        {
                            indIndex = indIndex - 32768 + boneCount;
                        }
                        while (indIndex >= list.Count)
                        {
                            list.Add(null);
                        }
                        if (list[indIndex] == null)
                        {
                            list[indIndex] = new FbxCluster(scene, "");
                            list[indIndex].SetLink(boneNodes[indIndex]);
                            list[indIndex].SetLinkMode(FbxCluster.ELinkMode.eTotalOne);
                            FbxAMatrix transformLinkMatrix = boneNodes[indIndex].EvaluateGlobalTransform();
                            list[indIndex].SetTransformLinkMatrix(transformLinkMatrix);
                            fbxSkin.AddCluster(list[indIndex]);
                        }
                        list[indIndex].AddControlPointIndex(i, boneWeight[l]);
                    }
                }
            }
            fbxMesh.AddDeformer(fbxSkin);
        }

        private unsafe FbxNode FBXExportSubObject(FbxScene scene, MeshSetSection section, long indicesOffset, int indexSize, NativeReader reader)
        {
            FbxNode fbxNode = new FbxNode(scene, section.Name);
            FbxMesh fbxMesh = new FbxMesh(scene, section.Name);
            FbxLayerElementNormal fbxLayerElementNormal = null;
            FbxLayerElementTangent fbxLayerElementTangent = null;
            FbxLayerElementBinormal fbxLayerElementBinormal = null;
            FbxLayerElementVertexColor[] array = new FbxLayerElementVertexColor[15];
            FbxLayerElementUV[] array2 = new FbxLayerElementUV[15];
            Dictionary<VertexElementUsage, int> dictionary = new Dictionary<VertexElementUsage, int>();
            Dictionary<VertexElementUsage, int> dictionary2 = new Dictionary<VertexElementUsage, int>();
            reader.Position = section.VertexOffset;
            fbxMesh.InitControlPoints((int)section.VertexCount);
            IntPtr controlPoints = fbxMesh.GetControlPoints();
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            bool flag = false;
            Vector3 val = default(Vector3);
            Vector3 val2 = default(Vector3);
            Vector3 val3 = default(Vector3);
            List<float> list = new List<float>();
            List<object> list2 = new List<object>();
            GeometryDeclarationDesc.Stream[] streams = section.GeometryDeclDesc[0].Streams;
            for (int i = 0; i < streams.Length; i++)
            {
                GeometryDeclarationDesc.Stream stream = streams[i];
                if (stream.VertexStride == 0)
                {
                    continue;
                }
                for (int j = 0; j < section.VertexCount; j++)
                {
                    int num4 = 0;
                    GeometryDeclarationDesc.Element[] elements = section.GeometryDeclDesc[0].Elements;
                    for (int k = 0; k < elements.Length; k++)
                    {
                        GeometryDeclarationDesc.Element element = elements[k];
                        if (element.Usage == VertexElementUsage.Unknown)
                        {
                            continue;
                        }
                        if (num4 >= num3 && num4 < num3 + stream.VertexStride)
                        {
                            if (element.Usage == VertexElementUsage.Pos)
                            {
                                double* ptr = (double*)(void*)controlPoints;
                                if (element.Format == VertexElementFormat.Float3)
                                {
                                    ptr[j * 4] = reader.ReadFloat();
                                    ptr[j * 4 + 1] = reader.ReadFloat();
                                    ptr[j * 4 + 2] = reader.ReadFloat();
                                }
                                else if (element.Format == VertexElementFormat.Half3 || element.Format == VertexElementFormat.Half4)
                                {
                                    ptr[j * 4] = HalfUtils.Unpack(reader.ReadUShort());
                                    ptr[j * 4 + 1] = HalfUtils.Unpack(reader.ReadUShort());
                                    ptr[j * 4 + 2] = HalfUtils.Unpack(reader.ReadUShort());
                                    if (element.Format == VertexElementFormat.Half4)
                                    {
                                        reader.ReadUShort();
                                    }
                                }
                            }
                            else if (element.Usage == VertexElementUsage.BinormalSign)
                            {
                                if (element.Format == VertexElementFormat.Half4 || element.Format == VertexElementFormat.Float4)
                                {
                                    if (fbxLayerElementTangent == null)
                                    {
                                        fbxLayerElementTangent = new FbxLayerElementTangent(fbxMesh, "");
                                        fbxLayerElementTangent.MappingMode = EMappingMode.eByControlPoint;
                                        fbxLayerElementTangent.ReferenceMode = EReferenceMode.eDirect;
                                    }
                                    if (element.Format == VertexElementFormat.Half4)
                                    {
                                        val2.X = HalfUtils.Unpack(reader.ReadUShort());
                                        val2.Y = HalfUtils.Unpack(reader.ReadUShort());
                                        val2.Z = HalfUtils.Unpack(reader.ReadUShort());
                                        list.Add(HalfUtils.Unpack(reader.ReadUShort()));
                                    }
                                    else
                                    {
                                        val2.X = reader.ReadFloat();
                                        val2.Y = reader.ReadFloat();
                                        val2.Z = reader.ReadFloat();
                                        list.Add(reader.ReadFloat());
                                    }
                                }
                                else if (element.Format == VertexElementFormat.Float)
                                {
                                    list.Add(reader.ReadFloat());
                                }
                                else
                                {
                                    list.Add(HalfUtils.Unpack(reader.ReadUShort()));
                                }
                                flag = true;
                            }
                            else if (element.Usage == VertexElementUsage.Normal)
                            {
                                if (fbxLayerElementNormal == null)
                                {
                                    fbxLayerElementNormal = new FbxLayerElementNormal(fbxMesh, "");
                                    fbxLayerElementNormal.MappingMode = EMappingMode.eByControlPoint;
                                    fbxLayerElementNormal.ReferenceMode = EReferenceMode.eDirect;
                                }
                                val.X = HalfUtils.Unpack(reader.ReadUShort());
                                val.Y = HalfUtils.Unpack(reader.ReadUShort());
                                val.Z = HalfUtils.Unpack(reader.ReadUShort());
                                fbxLayerElementNormal.DirectArray.Add(val.X, val.Y, val.Z);
                                val3.Y = HalfUtils.Unpack(reader.ReadUShort());
                            }
                            else if (element.Usage == VertexElementUsage.Binormal)
                            {
                                if (fbxLayerElementBinormal == null)
                                {
                                    fbxLayerElementBinormal = new FbxLayerElementBinormal(fbxMesh, "");
                                    fbxLayerElementBinormal.MappingMode = EMappingMode.eByControlPoint;
                                    fbxLayerElementBinormal.ReferenceMode = EReferenceMode.eDirect;
                                }
                                val3.X = HalfUtils.Unpack(reader.ReadUShort());
                                val3.Y = HalfUtils.Unpack(reader.ReadUShort());
                                val3.Z = HalfUtils.Unpack(reader.ReadUShort());
                                fbxLayerElementBinormal.DirectArray.Add(val3.X, val3.Y, val3.Z);
                                reader.ReadUShort();
                            }
                            else if (element.Usage == VertexElementUsage.Tangent)
                            {
                                if (fbxLayerElementTangent == null)
                                {
                                    fbxLayerElementTangent = new FbxLayerElementTangent(fbxMesh, "");
                                    fbxLayerElementTangent.MappingMode = EMappingMode.eByControlPoint;
                                    fbxLayerElementTangent.ReferenceMode = EReferenceMode.eDirect;
                                }
                                val2.X = HalfUtils.Unpack(reader.ReadUShort());
                                val2.Y = HalfUtils.Unpack(reader.ReadUShort());
                                val2.Z = HalfUtils.Unpack(reader.ReadUShort());
                                fbxLayerElementTangent.DirectArray.Add(val2.X, val2.Y, val2.Z);
                                val3.Z = HalfUtils.Unpack(reader.ReadUShort());
                            }
                            else if (element.Usage == VertexElementUsage.TangentSpace)
                            {
                                if (fbxLayerElementTangent == null)
                                {
                                    fbxLayerElementNormal = new FbxLayerElementNormal(fbxMesh, "");
                                    fbxLayerElementNormal.MappingMode = EMappingMode.eByControlPoint;
                                    fbxLayerElementNormal.ReferenceMode = EReferenceMode.eDirect;
                                    fbxLayerElementTangent = new FbxLayerElementTangent(fbxMesh, "");
                                    fbxLayerElementTangent.MappingMode = EMappingMode.eByControlPoint;
                                    fbxLayerElementTangent.ReferenceMode = EReferenceMode.eDirect;
                                    fbxLayerElementBinormal = new FbxLayerElementBinormal(fbxMesh, "");
                                    fbxLayerElementBinormal.MappingMode = EMappingMode.eByControlPoint;
                                    fbxLayerElementBinormal.ReferenceMode = EReferenceMode.eDirect;
                                }
                                if (element.Format == VertexElementFormat.UByte4N)
                                {
                                    list2.Add(new Vector4(reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f, reader.ReadByte() / 255f));
                                }
                                else if (element.Format == VertexElementFormat.UShort4N)
                                {
                                    list2.Add(new Vector4(reader.ReadUShort() / 65535f, reader.ReadUShort() / 65535f, reader.ReadUShort() / 65535f, reader.ReadUShort() / 65535f));
                                }
                                else if (element.Format == VertexElementFormat.UInt)
                                {
                                    list2.Add(reader.ReadUInt());
                                }
                            }
                            else if ((int)element.Usage >= 33 && (int)element.Usage <= 40)
                            {
                                if (!dictionary.ContainsKey(element.Usage))
                                {
                                    array2[num] = new FbxLayerElementUV(fbxMesh, "UVChannel" + num);
                                    array2[num].MappingMode = EMappingMode.eByControlPoint;
                                    array2[num].ReferenceMode = EReferenceMode.eDirect;
                                    dictionary.Add(element.Usage, num);
                                    num++;
                                }
                                float num5;
                                float num6;
                                if (element.Format == VertexElementFormat.Float2)
                                {
                                    num5 = reader.ReadFloat();
                                    num6 = 1f - reader.ReadFloat();
                                }
                                else
                                {
                                    num5 = HalfUtils.Unpack(reader.ReadUShort());
                                    num6 = 1f - HalfUtils.Unpack(reader.ReadUShort());
                                }
                                array2[dictionary[element.Usage]].DirectArray.Add(num5, num6);
                            }
                            else if ((int)element.Usage >= 30 && (int)element.Usage <= 31)
                            {
                                if (!dictionary2.ContainsKey(element.Usage))
                                {
                                    array[num2] = new FbxLayerElementVertexColor(fbxMesh, "VColor" + (num2 + 1));
                                    array[num2].MappingMode = EMappingMode.eByControlPoint;
                                    array[num2].ReferenceMode = EReferenceMode.eDirect;
                                    dictionary2.Add(element.Usage, num2);
                                    num2++;
                                }
                                array[dictionary2[element.Usage]].DirectArray.Add(reader.ReadByte() / 255.0, reader.ReadByte() / 255.0, reader.ReadByte() / 255.0, reader.ReadByte() / 255.0);
                            }
                            else if (element.Usage == VertexElementUsage.RadiosityTexCoord)
                            {
                                reader.Position += element.Size;
                            }
                            else if (element.Usage == VertexElementUsage.DisplacementMapTexCoord)
                            {
                                reader.Position += element.Size;
                            }
                            else
                            {
                                VertexElementUsage usage = element.Usage;
                                string pName = usage.ToString();
                                if (element.Format == VertexElementFormat.Half2 || element.Format == VertexElementFormat.Float2 || element.Format == VertexElementFormat.Short2N)
                                {
                                    if (!dictionary.ContainsKey(element.Usage))
                                    {
                                        array2[num] = new FbxLayerElementUV(fbxMesh, pName);
                                        array2[num].MappingMode = EMappingMode.eByControlPoint;
                                        array2[num].ReferenceMode = EReferenceMode.eDirect;
                                        dictionary.Add(element.Usage, num);
                                        num++;
                                    }
                                    float num7 = 0f;
                                    float num8 = 0f;
                                    if (element.Format == VertexElementFormat.Half2)
                                    {
                                        num7 = HalfUtils.Unpack(reader.ReadUShort());
                                        num8 = HalfUtils.Unpack(reader.ReadUShort());
                                    }
                                    else if (element.Format == VertexElementFormat.Float2)
                                    {
                                        num7 = reader.ReadFloat();
                                        num8 = reader.ReadFloat();
                                    }
                                    else
                                    {
                                        num7 = reader.ReadShort() / 32767f * 0.5f + 0.5f;
                                        num8 = reader.ReadShort() / 32767f * 0.5f + 0.5f;
                                    }
                                    array2[dictionary[element.Usage]].DirectArray.Add(num7, num8);
                                }
                                else if (element.Format == VertexElementFormat.Half4 || element.Format == VertexElementFormat.Float4)
                                {
                                    if (!dictionary2.ContainsKey(element.Usage))
                                    {
                                        array[num2] = new FbxLayerElementVertexColor(fbxMesh, pName);
                                        array[num2].MappingMode = EMappingMode.eByControlPoint;
                                        array[num2].ReferenceMode = EReferenceMode.eDirect;
                                        dictionary2.Add(element.Usage, num2);
                                        num2++;
                                    }
                                    float num9 = 0f;
                                    float num10 = 0f;
                                    float num11 = 0f;
                                    float num12 = 0f;
                                    if (element.Format == VertexElementFormat.Half4)
                                    {
                                        num9 = HalfUtils.Unpack(reader.ReadUShort());
                                        num10 = HalfUtils.Unpack(reader.ReadUShort());
                                        num11 = HalfUtils.Unpack(reader.ReadUShort());
                                        num12 = HalfUtils.Unpack(reader.ReadUShort());
                                    }
                                    else
                                    {
                                        num9 = reader.ReadFloat();
                                        num10 = reader.ReadFloat();
                                        num11 = reader.ReadFloat();
                                        num12 = reader.ReadFloat();
                                    }
                                    array[dictionary2[element.Usage]].DirectArray.Add(num9, num10, num11, num12);
                                }
                                else if (element.Format == VertexElementFormat.Float)
                                {
                                    if (!dictionary2.ContainsKey(element.Usage))
                                    {
                                        array[num2] = new FbxLayerElementVertexColor(fbxMesh, pName);
                                        array[num2].MappingMode = EMappingMode.eByControlPoint;
                                        array[num2].ReferenceMode = EReferenceMode.eDirect;
                                        dictionary2.Add(element.Usage, num2);
                                        num2++;
                                    }
                                    float num13 = reader.ReadFloat();
                                    array[dictionary2[element.Usage]].DirectArray.Add(num13, 0.0, 0.0);
                                }
                                else if (element.Format == VertexElementFormat.Int)
                                {
                                    if (!dictionary2.ContainsKey(element.Usage))
                                    {
                                        array[num2] = new FbxLayerElementVertexColor(fbxMesh, pName);
                                        array[num2].MappingMode = EMappingMode.eByControlPoint;
                                        array[num2].ReferenceMode = EReferenceMode.eDirect;
                                        dictionary2.Add(element.Usage, num2);
                                        num2++;
                                    }
                                    int num14 = reader.ReadInt();
                                    array[dictionary2[element.Usage]].DirectArray.Add(num14 / 255f, 0.0, 0.0);
                                }
                                else
                                {
                                    reader.Position += element.Size;
                                }
                            }
                        }
                        num4 += element.Size;
                    }
                }
                num3 += stream.VertexStride;
            }
            if (flag)
            {
                fbxLayerElementBinormal = new FbxLayerElementBinormal(fbxMesh, "");
                fbxLayerElementBinormal.MappingMode = EMappingMode.eByControlPoint;
                fbxLayerElementBinormal.ReferenceMode = EReferenceMode.eDirect;
                for (int l = 0; l < section.VertexCount; l++)
                {
                    fbxLayerElementNormal.DirectArray.GetAt(l, out Vector4 outValue);
                    fbxLayerElementTangent.DirectArray.GetAt(l, out Vector4 outValue2);
                    val = new Vector3(outValue.X, outValue.Y, outValue.Z);
                    val2 = new Vector3(outValue2.X, outValue2.Y, outValue2.Z);
                    val3 = Vector3.Cross(val2, val) * list[l];
                    fbxLayerElementBinormal.DirectArray.Add(val3.X, val3.Y, val3.Z);
                }
            }
            reader.Position = indicesOffset + section.StartIndex * indexSize;
            for (int m = 0; m < section.PrimitiveCount; m++)
            {
                fbxMesh.BeginPolygon(0);
                for (int n = 0; n < 3; n++)
                {
                    int index = (int)((indexSize == 2) ? reader.ReadUShort() : reader.ReadUInt32LittleEndian());
                    fbxMesh.AddPolygon(index);
                }
                fbxMesh.EndPolygon();
            }
            for (int num6 = 0; num6 < 15; num6++)
            {
                FbxLayer layer = fbxMesh.GetLayer(num6);
                if (layer == null)
                {
                    fbxMesh.CreateLayer();
                    layer = fbxMesh.GetLayer(num6);
                }
                if (num6 == 0)
                {
                    if (fbxLayerElementNormal != null)
                    {
                        layer.SetNormals(fbxLayerElementNormal);
                    }
                    if (fbxLayerElementTangent != null)
                    {
                        layer.SetTangents(fbxLayerElementTangent);
                    }
                    if (fbxLayerElementBinormal != null)
                    {
                        layer.SetBinormals(fbxLayerElementBinormal);
                    }
                }
                if (array[num6] != null)
                {
                    layer.SetVertexColors(array[num6]);
                }
                if (array2[num6] != null)
                {
                    layer.SetUVs(array2[num6]);
                }
            }
            fbxMesh.BuildMeshEdgeArray();
            geomConverter.ComputeEdgeSmoothingFromNormals(fbxMesh);
            fbxNode.SetNodeAttribute(fbxMesh);
            return fbxNode;
        }

        //public MeshSet LoadMeshSet(AssetManager assetManager, object rootObject)
        //{
        //	MeshSet meshSet = new MeshSet(assetManager.GetRes(assetManager.GetResEntry(((dynamic)rootObject).Name)));
        //	return meshSet;
        //}
        public MeshSet LoadMeshSet(AssetManager assetManager, string resName)
        {
            MeshSet meshSet = new MeshSet(assetManager.GetRes(assetManager.GetResEntry(resName)));
            //MeshSet meshSet = new MeshSet(assetManager.GetRes(assetManager.GetResEntry(resName)), ProfileManager.Game, assetManager.GetResEntry(resName));
            return meshSet;
        }

        public MeshSet LoadMeshSet(EbxAssetEntry ebx)
        {
            //EbxAsset ebxAsset = AssetManager.Instance.GetEbx(ebx);
            //return LoadMeshSet(AssetManager.Instance, ebxAsset.RootObject);
            return LoadMeshSet(AssetManager.Instance, ebx.Name);
        }

    }
}

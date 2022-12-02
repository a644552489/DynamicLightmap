using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using YLib.Lightmap;
using static CustomRenderer.DrawMeshNode;

namespace CustomRenderer
{
    public class DrawMeshRenderMgr
    {
        #region 单例
        private static DrawMeshRenderMgr g_Inst = null;
        public static DrawMeshRenderMgr Inst
        {
            get
            {
                if (g_Inst == null) g_Inst = new DrawMeshRenderMgr();
                return g_Inst;
            }
        }
        #endregion


        public DrawMeshLayerMgr[] drawMeshLayers_opaque = null;
        public DrawMeshLayerMgr[] drawMeshLayers_transparent = null;

        public DrawMeshRenderMgr()
        {
            drawMeshLayers_opaque = new DrawMeshLayerMgr[32];
            drawMeshLayers_transparent = new DrawMeshLayerMgr[32];
        }

        public void Reset()
        {
            drawMeshLayers_opaque = new DrawMeshLayerMgr[32];
            drawMeshLayers_transparent = new DrawMeshLayerMgr[32];
        }

        public DrawMeshLayerMgr GetDrawMeshLayerMgr(RenderQueueType renderQueueType, int layer)
        {
            if (renderQueueType == RenderQueueType.Opaque)
            {
                return drawMeshLayers_opaque[layer];
            }
            else
            {
                return drawMeshLayers_transparent[layer];
            }
        }

        public void Add(DrawMeshNode node)
        {
            Profiler.BeginSample("DrawMeshRenderMgr.Add");
            int count = node.infos.Length;
            for (int i = 0; i < count; i++)
            {
                var info = node.infos[i];

                if (info.renderQueue >= (int)RenderQueue.Transparent)
                {
                    if (drawMeshLayers_transparent[info.layer] == null)
                    {
                        drawMeshLayers_transparent[info.layer] = new DrawMeshLayerMgr();
                    }
                    drawMeshLayers_transparent[info.layer].Add(info);
                }
                else
                {
                    if (drawMeshLayers_opaque[info.layer] == null)
                    {
                        drawMeshLayers_opaque[info.layer] = new DrawMeshLayerMgr();
                    }
                    drawMeshLayers_opaque[info.layer].Add(info);
                }
            }
            Profiler.EndSample();
        }

        public void Remove(DrawMeshNode node)
        {
            int count = node.infos.Length;
            for (int i = 0; i < count; i++)
            {
                node.infos[i].RemoveFromBatchNode();
            }
        }

        //DrawMeshLayerMgr-----------
        public class DrawMeshLayerMgr
        {
            public LinkedList<DrawMeshBatchNode> renderQueueList = null;

            //索引：一维=gpu inctance id，二维=lightmapType ,三维=lightmapIndex
            DrawMeshBatchNode[][][] onlyIdList = null;

            public DrawMeshLayerMgr()
            {
                renderQueueList = new LinkedList<DrawMeshBatchNode>();

                onlyIdList = new DrawMeshBatchNode[50][][];
                for (int i = 0; i < onlyIdList.Length; i++)
                {
                    onlyIdList[i] = new DrawMeshBatchNode[1][];
                    onlyIdList[i][0] = new DrawMeshBatchNode[1];
                }
            }

            public void Add(DrawMeshInfo info)
            {
                var gpu_inctancing_id = info.gpu_inctancing_id;
                //没有光照贴图的是-1，所有这里要+1
                var lightmapType = info.lightmapType + 1;
      
                var lightmapIndex = info.lightmapProp[0].lightmapIndex + 1;
                if (onlyIdList.Length <= gpu_inctancing_id)
                {
                    var temp = new DrawMeshBatchNode[gpu_inctancing_id * 2][][];
                    System.Array.Copy(onlyIdList, temp, onlyIdList.Length);
                    onlyIdList = temp;
                }

                if (onlyIdList[gpu_inctancing_id]== null)
                {
                    onlyIdList[gpu_inctancing_id] = new DrawMeshBatchNode[1][];
                }
                if (onlyIdList[gpu_inctancing_id].Length <= lightmapType)
                {
                    var temp = new DrawMeshBatchNode[lightmapType * 2][];
                    System.Array.Copy(onlyIdList[gpu_inctancing_id], temp, onlyIdList[gpu_inctancing_id].Length);
                    onlyIdList[gpu_inctancing_id] = temp;
                }

                if(onlyIdList[gpu_inctancing_id][lightmapType] == null)
                {
                    onlyIdList[gpu_inctancing_id][lightmapType] = new DrawMeshBatchNode[1];
                }
                if (onlyIdList[gpu_inctancing_id][lightmapType].Length <= lightmapIndex)
                {
                    var temp = new DrawMeshBatchNode[lightmapIndex * 2];
                    System.Array.Copy(onlyIdList[gpu_inctancing_id][lightmapType], temp, onlyIdList[gpu_inctancing_id][lightmapType].Length);
                    onlyIdList[gpu_inctancing_id][lightmapType] = temp;
                }


                DrawMeshBatchNode batchNode = null;
                if (onlyIdList[gpu_inctancing_id][lightmapType][lightmapIndex] == null)
                {
                    batchNode = new DrawMeshBatchNode(info);
                    onlyIdList[gpu_inctancing_id][lightmapType][lightmapIndex] = batchNode;

                    var cur = renderQueueList.First;
                    if (cur == null)
                    {
                        renderQueueList.AddFirst(batchNode);
                    }
                    else
                    {
                        if (DrawMeshBatchNode.Compare(batchNode,cur.Value)==-1)
                        {
                            renderQueueList.AddBefore(cur, batchNode);
                        }
                        else
                        {
                            while (cur.Next != null && DrawMeshBatchNode.Compare(batchNode, cur.Next.Value) == 1)
                            {
                                cur = cur.Next;
                            }
                            renderQueueList.AddAfter(cur, batchNode);
                        }
                    }
                }
                else
                {
                    batchNode = onlyIdList[gpu_inctancing_id][lightmapType][lightmapIndex];
                }

                batchNode.Add(info);
            }
        }

        //DrawMeshBatchNode-----------
        public class DrawMeshBatchNode
        {
            static List<Vector4> tempV4List = new List<Vector4>(1023);
            static List<float> tempFList_1 = new List<float>(1023);
            static List<float> tempFList_2 = new List<float>(1023);

            public Mesh mesh = null;
            public Material material = null;
            public Shader shader = null;
            //public MaterialPropertyBlock propertyBlock = null;
            public int renderQueue = -1;
            public List<int> lightmapIndex = new List<int>();
            public int lightmapType = -1;

            public List<MaterialPropertyBlock> propertyBlockList = new List<MaterialPropertyBlock>();
            public List<Matrix4x4[]> matrix4X4sList = new List<Matrix4x4[]>();
            //public Matrix4x4[] matrix4X4s = null;



            public DrawMeshInfo[] infos = null;

            public int showNodeCount = 0;
            public int nodeCount = 0;

            public DrawMeshBatchNode(DrawMeshInfo info)
            {
                mesh = info.mesh;
                material = info.material;
                shader = material.shader;
                renderQueue = info.renderQueue;

                for (int i = 0; i < info.lightmapProp.Count; i++)
                {
                    lightmapIndex.Add(info.lightmapProp[i].lightmapIndex);
                }  

                lightmapType = info.lightmapType;

                if(lightmapType != -1)
                {
                    if (!Application.isPlaying)
                    {
                        material = GameObject.Instantiate<Material>(info.material);
                    }
                    material.EnableKeyword("_CUSTOM_LIGHT_MAP_BATCH_ON");
                    material.enableInstancing = true;
                }

                if (material.enableInstancing)
                {
                    infos = new DrawMeshInfo[512];
                }
                else
                {
                    infos = new DrawMeshInfo[32];
                }

                Add(info);
            }

            public void Add(DrawMeshInfo info)
            {
                if (info.batchNodeIndex != -1)
                {
                    return;
                }

                //切换场景会导致material变null，编辑器没paly下特殊处理
                if (lightmapType != -1 && material == null)
                {
                    if (!Application.isPlaying)
                    {
                        material = GameObject.Instantiate<Material>(info.material);
                    }
                    material.EnableKeyword("_CUSTOM_LIGHT_MAP_BATCH_ON");
                    material.enableInstancing = true;
                }

                if (nodeCount + 1 >= infos.Length)
                {
                    var temp = new DrawMeshInfo[nodeCount * 2];
                    System.Array.Copy(infos, temp, infos.Length);
                    infos = temp;
                }

                info.batchNode = this;
                info.batchNodeIndex = nodeCount;

                infos[nodeCount] = info;
                SwapPos(nodeCount, showNodeCount);

                showNodeCount++;
                nodeCount++;
            }

            public void Remove(DrawMeshInfo info)
            {
                if (info.batchNodeIndex == -1) return;

                if (info.isShow)
                {
                    SetShow(info.batchNodeIndex, false);
                }

                SwapPos(nodeCount - 1, info.batchNodeIndex);
                infos[nodeCount - 1] = null;
                nodeCount--;

                info.isShow = true;
                info.batchNode = null;
                info.batchNodeIndex = -1;
            }

            public void SetShow(int batchNodeIndex, bool isShow)
            {
                if (isShow)
                {
                    SwapPos(showNodeCount, batchNodeIndex);
                    showNodeCount++;
                }
                else
                {
                    SwapPos(showNodeCount - 1, batchNodeIndex);
                    showNodeCount--;
                }
            }

            private void SwapPos(int i, int j)
            {
                if (i == j) return;

                var temp = infos[i];
                infos[i] = infos[j];
                infos[j] = temp;

                if (infos[i] != null)
                {
                    infos[i].batchNodeIndex = i;
                }

                if (infos[j] != null)
                {
                    infos[j].batchNodeIndex = j;
                }
            }

            public void RefreshGpuInctancingData()
            {
                //大于1023个mesh就增加一个批
                var batchCount = (showNodeCount / 1023) + 1;
                if (matrix4X4sList.Count < batchCount)
                {
                    for (int i = matrix4X4sList.Count; i < batchCount; i++)
                    {
                        matrix4X4sList.Add(new Matrix4x4[1023]);
                        propertyBlockList.Add(new MaterialPropertyBlock());
                    }
                }

                for (int i = 0; i < batchCount; i++)
                {
                    var index = i * 1023;
                    var count = (i == batchCount - 1) ? (showNodeCount % 1023) : 1023;
                    ImportData(index, count, matrix4X4sList[i], propertyBlockList[i]);
                }
            }


            public void ImportData(int index, int count, Matrix4x4[] matrix4X4s, MaterialPropertyBlock propertyBlock)
            {
                Profiler.BeginSample("Refresh Matrix");
                for (int i = 0; i < count; i++)
                {
                    matrix4X4s[i] = infos[index + i].realMmatrix4X4;
                }
                Profiler.EndSample();

                Profiler.BeginSample("Refresh Property");
                propertyBlock.Clear();

                if (material.HasProperty(CustomRenderer.CustomRendererExtend.OffsetUV_ID))
                {
                    var defalutValue = material.GetVector(CustomRenderer.CustomRendererExtend.OffsetUV_ID);
                    var list = tempV4List;
                    tempV4List.Clear();
                    for (int i = 0; i < count; i++)
                    {
                        var info = infos[index + i];
                        if (info.isOffsetUV)
                        {
                            list.Add(info.offsetUV);
                        }
                        else
                        {
                            list.Add(defalutValue);
                        }
                    }
                    propertyBlock.SetVectorArray(CustomRenderer.CustomRendererExtend.OffsetUV_ID, list);
                }

                if (material.HasProperty(CustomRenderer.CustomRendererExtend.VertexSpeed_ID)
                    && material.HasProperty(CustomRenderer.CustomRendererExtend.VertexScale_ID))
                {
                    var defalutValue_1 = material.GetFloat(CustomRenderer.CustomRendererExtend.VertexSpeed_ID);
                    var defalutValue_2 = material.GetFloat(CustomRenderer.CustomRendererExtend.VertexScale_ID);
                    var list_1 = tempFList_1;
                    var list_2 = tempFList_2;
                    tempFList_1.Clear();
                    tempFList_2.Clear();
                    for (int i = 0; i < count; i++)
                    {
                        var info = infos[index + i];
                        if (!info.combineMesh_isVertexOffset)
                        {
                            list_1.Add(0);
                            list_2.Add(0);
                        }
                        else
                        {
                            list_1.Add(defalutValue_1);
                            list_2.Add(defalutValue_2);
                        }
                    }
                    propertyBlock.SetFloatArray(CustomRenderer.CustomRendererExtend.VertexSpeed_ID, list_1);
                    propertyBlock.SetFloatArray(CustomRenderer.CustomRendererExtend.VertexScale_ID, list_2);
                }

                if (lightmapIndex[(int)LightmapMgr.Inst.LightType] != -1 && material.HasProperty(LightmapNode.Custom_LightmapST_ID))
                {
                    
                    var texturePackage = LightmapMgr.Inst.GetTexturePackageByInfo(lightmapType, lightmapIndex[(int)LightmapMgr.Inst.LightType]);
                    if (texturePackage != null )
                    {
                        List<Vector4> listmapST = tempV4List;
                        tempV4List.Clear();
                        for (int i = 0; i < count; i++)
                        {
                            DrawMeshInfo info = infos[index + i];
                            listmapST.Add(info.lightmapProp[(int)LightmapMgr.Inst.LightType].lightmapST);
                        }

                        if (texturePackage.lightmapColor != null)
                            propertyBlock.SetTexture(LightmapNode.Unity_Lightmap_ID, texturePackage.lightmapColor);

                        if (texturePackage.lightmapDir != null)
                            propertyBlock.SetTexture(LightmapNode.Unity_LightmapInd_ID, texturePackage.lightmapDir);

                        if (texturePackage.shadowMask != null)
                            propertyBlock.SetTexture(LightmapNode.Unity_ShadowMask_ID, texturePackage.shadowMask);


                        propertyBlock.SetVectorArray(LightmapNode.Custom_LightmapST_ID, listmapST);
                    }
                }
                Profiler.EndSample();
            }

            public static int Compare(DrawMeshBatchNode a, DrawMeshBatchNode b)
            {
                if(a.renderQueue < b.renderQueue)
                {
                    return -1;
                }
                else if (a.renderQueue > b.renderQueue)
                {
                    return 1;
                }
                else
                {
                    if (a.lightmapType == b.lightmapType)
                    {
                        return System.Math.Sign(a.lightmapIndex[0] - b.lightmapIndex[0]);
                    }
                    else if (a.lightmapType < b.lightmapType)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
            }

        }

    }
}

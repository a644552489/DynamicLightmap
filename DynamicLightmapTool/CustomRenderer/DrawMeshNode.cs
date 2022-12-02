//#define YXX_DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using static CustomRenderer.DrawMeshRenderMgr;
using YLib.Lightmap;
using static YLib.Lightmap.LightmapMgr;
using Sirenix.Serialization; 

namespace CustomRenderer
{
    [ExecuteInEditMode]
    public class DrawMeshNode : MonoBehaviour
    {
        [Serializable]

        public class DrawMeshInfo 
        {
            private static DrawMeshGpuIncetancingOnlyIdData _onlyIdData;
            public static DrawMeshGpuIncetancingOnlyIdData OnlyIdData
            {
                get
                {
#if UNITY_EDITOR
                    if (_onlyIdData == null)
                    {
                        var list = UnityEditor.AssetDatabase.FindAssets("t:DrawMeshGpuIncetancingOnlyIdData");
                        if (list.Length > 0)
                        {
                            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(list[0]);
                            _onlyIdData = UnityEditor.AssetDatabase.LoadAssetAtPath<DrawMeshGpuIncetancingOnlyIdData>(path);
                        }
                    }

                    return _onlyIdData;
#else
                    return null;
#endif
                }
            }

            [SerializeField, HideInInspector]
            private SerializationData serializationData;

            [Title("Common")]
#if YXX_DEBUG
            public string name;
#endif
            public int gpu_inctancing_id;
            public Mesh mesh;
            [HideInInspector]
            public Matrix4x4 matrix4X4;
            public Material material;
            public int renderQueue;
            [CustomValueDrawer("DrawLayer")]
            public int layer;
            public int DrawLayer(int layer)
            {
#if UNITY_EDITOR
                this.layer = UnityEditor.EditorGUILayout.LayerField("Layer", layer);
#endif
                return this.layer;
            }

            //MaterialProperty
            [HideInInspector]
            public MaterialPropertyBlock propertyBlock;
            [Title("MaterialPropertyBlock")]
            public bool isOffsetUV = false;
            public Vector4 offsetUV;
            public bool combineMesh_isVertexOffset = true;

            //Lightmap
            [Title("Lightmap")]
            [ValueDropdown("GetTypeName")]
            public int lightmapType;
            public IEnumerable GetTypeName()
            {
                if (LightmapTypeData.Inst != null)
                    return LightmapTypeData.Inst.Propertys;
                return null;
            }
            //public int lightmapIndex = -1;
            //public Vector4 lightmapST;
            //public LightmapsMode lightmapsMode;
            //public MixedLightingMode mixedLightingMode;


            public List<LightProp> lightmapProp = new List<LightProp>();

            //Runtime Data
            [HideInInspector]
            public Matrix4x4 realMmatrix4X4;
            [Title("Runtime Data")]
            [NonSerialized]
            [ShowInInspector]
            public Vector3 position;
            [NonSerialized]
            [ShowInInspector]
            public Vector3 boundsMax;
            [NonSerialized]
            [ShowInInspector]
            public Vector3 boundsMin;

            [NonSerialized]
            [ShowInInspector]
            public bool isShow = true;
            [NonSerialized]
            public DrawMeshBatchNode batchNode = null;
            [NonSerialized]
            [ShowInInspector]
            public int batchNodeIndex = -1;

            public DrawMeshInfo(MeshRenderer meshRenderer, Transform transform)
            {
                var rendererTF = meshRenderer.transform;

#if YXX_DEBUG
                name = meshRenderer.gameObject.name;
#endif
                mesh = meshRenderer.GetComponent<MeshFilter>().sharedMesh;
                matrix4X4 = transform.worldToLocalMatrix * rendererTF.localToWorldMatrix;
                realMmatrix4X4 = matrix4X4;
                material = meshRenderer.sharedMaterial;
                renderQueue = material.renderQueue == -1 ? material.shader.renderQueue : material.renderQueue;
                layer = meshRenderer.gameObject.layer;

                rendererTF.TryGetComponent<LightmapNode>(out var lightmapNode);
                if (lightmapNode != null)
                {
                    lightmapType = lightmapNode.type;

                    for (int j = 0; j < lightmapNode.LightmapProp.Keys.Count; j++)
                    {
                        LightProp prop = null;

                        lightmapNode.LightmapProp.TryGetValue( (LightmapType)j, out prop);
                        if (prop != null)
                        {
                            lightmapProp.Add(prop);
                        }
                    }

                }

                if (meshRenderer.TryGetComponent<CustomRenderer.CustomRendererExtend>(out var customRendererExtend))
                {
                    if (customRendererExtend.isOffsetUV)
                    {
                        isOffsetUV = customRendererExtend.isOffsetUV;
                        offsetUV = customRendererExtend.offsetUV;
                    }

                    if (!customRendererExtend.combineMesh_isVertexOffset)
                    {
                        combineMesh_isVertexOffset = customRendererExtend.combineMesh_isVertexOffset;
                    }
                }

                if (OnlyIdData)
                {
                    gpu_inctancing_id = OnlyIdData.GetID(layer, material, mesh);
                }
            }
         

            public void GenPropertyData()
            {
                bool isChangeBlock = isOffsetUV || !combineMesh_isVertexOffset;
                if (isChangeBlock)
                {
                    propertyBlock = new MaterialPropertyBlock();

                    if (isOffsetUV)
                    {
                        propertyBlock.SetVector(CustomRenderer.CustomRendererExtend.OffsetUV_ID, offsetUV);
                    }

                    if (!combineMesh_isVertexOffset)
                    {
                        propertyBlock.SetFloat(CustomRenderer.CustomRendererExtend.VertexSpeed_ID, 0);
                        propertyBlock.SetFloat(CustomRenderer.CustomRendererExtend.VertexScale_ID, 0);
                    }
                }
            }

            public void RemoveFromBatchNode()
            {
                if (batchNode != null)
                {
                    batchNode.Remove(this);
                }
            }

           
        }

        [Searchable]
        [ListDrawerSettings(ShowIndexLabels = true)]
        public DrawMeshInfo[] infos;

        private Matrix4x4 matrix4X4;

        public Camera camera;

        private void Awake()
        {
            if (infos == null) infos = new DrawMeshInfo[0];
            if (infos.Length == 0) return;


            for (int i = 0; i < infos.Length; i++)
            {
                infos[i].GenPropertyData();
                infos[i].realMmatrix4X4 = transform.localToWorldMatrix * infos[i].matrix4X4;
                infos[i].position = infos[i].realMmatrix4X4 * new Vector4(0, 0, 0, 1);

                var max = infos[i].mesh.bounds.max;
                var min = infos[i].mesh.bounds.min;
                infos[i].boundsMax = infos[i].realMmatrix4X4 * new Vector4(max.x, max.y, max.z, 1);
                infos[i].boundsMin = infos[i].realMmatrix4X4 * new Vector4(min.x, min.y, min.z, 1);
                infos[i].isShow = true;
                infos[i].batchNodeIndex = -1;
            }

        }

        void OnEnable()
        {
#if UNITY_EDITOR
            if (infos == null) return;
            for (int i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                info.GenPropertyData();
                info.realMmatrix4X4 = transform.localToWorldMatrix * info.matrix4X4;
                info.position = info.realMmatrix4X4 * new Vector4(0, 0, 0, 1);

                var max = info.mesh.bounds.max;
                var min = info.mesh.bounds.min;
                info.boundsMax = info.realMmatrix4X4 * new Vector4(max.x, max.y, max.z, 1);
                info.boundsMin = info.realMmatrix4X4 * new Vector4(min.x, min.y, min.z, 1);
                info.isShow = true;
                info.batchNodeIndex = -1;

                if (info.lightmapProp[(int)LightmapMgr.Inst.LightType].lightmapIndex != -1 )
                {
                    if (Application.isPlaying)
                    {
                        LightmapNode.SetMaterial(info.material, info.lightmapProp[(int)LightmapMgr.Inst.LightType].lightmapsMode, info.lightmapProp[(int)LightmapMgr.Inst.LightType].mixedLightingMode);
                    }
                }
            }
#endif


            if (infos.Length == 0) return;
            DrawMeshRenderMgr.Inst.Add(this);

        }

        private void OnDisable()
        {
            if (infos.Length == 0) return;
            DrawMeshRenderMgr.Inst.Remove(this);
        }
        private void OnDestroy()
        {
            if (infos.Length == 0) return;
            DrawMeshRenderMgr.Inst.Remove(this);
        }



        //private void OnDrawGizmos()
        //{
        //    for (int i = 0; i < infos.Count; i++)
        //    {
        //        var center = (infos[i].boundsMax - infos[i].boundsMin) / 2 + infos[i].boundsMin;
        //        var size = (infos[i].boundsMax - infos[i].boundsMin);

        //        Gizmos.DrawCube(center, size);
        //    }
        //}

        //void Update()
        //{
        //    //if (matrix4X4 != transform.localToWorldMatrix)
        //    //{
        //    //    for (int i = 0; i < infos.Count; i++)
        //    //    {
        //    //        infos[i].realMmatrix4X4 = transform.localToWorldMatrix * infos[i].matrix4X4;
        //    //    }
        //    //    matrix4X4 = transform.localToWorldMatrix;
        //    //}

        //    for (int i = 0; i < infos.Length; i++)
        //    {
        //        var info = infos[i];

        //        //info.isShow = IsInViewport(info);
        //        //if (info.isShow)
        //        //{
        //        Graphics.DrawMesh(info.mesh, info.realMmatrix4X4, info.material, info.layer, null, 0, info.propertyBlock);
        //        //}
        //    }
        //}

        [Button("GenData(Button)")]
        public void GenData()
        {
            var list = GetComponentsInChildren<MeshRenderer>(true);

            infos = new DrawMeshInfo[list.Length];

            for (int i = 0; i < list.Length; i++)
            {
                infos[i] = new DrawMeshInfo(list[i], this.transform);
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                // DestroyImmediate(transform.GetChild(i).gameObject);
                transform.GetChild(i).gameObject.SetActive(false);
            }
            if (infos.Length == 0) return;
            DrawMeshRenderMgr.Inst.Add(this);

        }

        public void GenData(List<MeshRenderer> list, List<DrawMeshNode> drawMeshNodelist)
        {
            //infos = new DrawMeshInfo[list.Count];

            //for (int i = 0; i < list.Count; i++)
            //{
            //    infos[i] = new DrawMeshInfo(list[i], this.transform);
            //}

            var tempList = new List<DrawMeshInfo>();

            for (int i = 0; i < list.Count; i++)
            {
                tempList.Add(new DrawMeshInfo(list[i], this.transform));
            }

            for (int i = 0; i < drawMeshNodelist.Count; i++)
            {
                for (int j = 0; j < drawMeshNodelist[i].infos.Length; j++)
                {
                    var info = drawMeshNodelist[i].infos[j];
                    var tempTF = drawMeshNodelist[i].transform;
                    info.matrix4X4 = transform.worldToLocalMatrix * tempTF.localToWorldMatrix * info.matrix4X4;
                    info.realMmatrix4X4 = info.matrix4X4;
                    tempList.Add(info);
                }

                //tempList.AddRange(drawMeshNodelist[i].infos);
            }

            infos = tempList.ToArray();

        }

        public bool IsInViewport(DrawMeshInfo drawMeshInfo)
        {
            if (camera == null)
            {
                if (Camera.main == null) return true;
                camera = Camera.main;
            };

            if (Vector3.Dot(camera.transform.forward, (drawMeshInfo.position - camera.transform.position)) <= 0) return false;

            Vector3 minViewprot = camera.WorldToViewportPoint(drawMeshInfo.boundsMin);
            Vector3 maxViewprot = camera.WorldToViewportPoint(drawMeshInfo.boundsMax);

            Vector4 test = camera.projectionMatrix * camera.worldToCameraMatrix * new Vector4(drawMeshInfo.boundsMin.x, drawMeshInfo.boundsMin.y, drawMeshInfo.boundsMin.z, 1);
            Vector3 test2 = camera.previousViewProjectionMatrix * drawMeshInfo.boundsMin;
            Vector3 test3 = camera.previousViewProjectionMatrix * camera.worldToCameraMatrix * drawMeshInfo.boundsMin;

            Vector3 test4 = new Vector3(0.5f + 0.5f * test.x / test.w, 0.5f + 0.5f * test.y / test.w, test.w);


            //overlaps
            float left, right, bottom, top;
            if (minViewprot.x < maxViewprot.x)
            {
                left = minViewprot.x;
                right = maxViewprot.x;
            }
            else
            {
                left = maxViewprot.x;
                right = minViewprot.x;
            }
            if (minViewprot.y < maxViewprot.y)
            {
                bottom = minViewprot.y;
                top = maxViewprot.y;
            }
            else
            {
                bottom = maxViewprot.y;
                top = minViewprot.y;
            }
            if (left > 1 || right < 0 || top < 0 || bottom > 1) return false;


            Vector3 posViewport = camera.WorldToViewportPoint(drawMeshInfo.position);
            if (posViewport.z >= camera.nearClipPlane && posViewport.z <= camera.farClipPlane) return true;

            if (minViewprot.z >= camera.nearClipPlane && minViewprot.z <= camera.farClipPlane) return true;

            if (maxViewprot.z >= camera.nearClipPlane && maxViewprot.z <= camera.farClipPlane) return true;

            return false;
        }
    }

}



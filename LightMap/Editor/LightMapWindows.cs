using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using static OuY.Lightmap.LightmapMgr;

namespace OuY.Lightmap
{

    public class LightMapWindows : EditorWindow
    {
        [MenuItem("MyTools/������ͼ")]
        static void Init()
        {
            var type = typeof(LightMapWindows);
            var mywindow = EditorWindow.GetWindow(type, false, type.Name, true);
            mywindow.Show();

        }

 
        public LightmapType type;

        private void OnGUI()
        {

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            {
               
         
             type =   (LightmapType)EditorGUILayout.EnumPopup( "������ͼ����",type ,GUILayout.Width(240 ),GUILayout.Height(20)) ;
                
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Space(10);

     
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.Space(20);
                    if (GUILayout.Button("����ǰ���õƹ�", GUILayout.Width(120), GUILayout.Height(40)))
                    {
                        SetLightProp();
                    }
                    EditorGUILayout.Space(20);
                }
                EditorGUILayout.EndHorizontal();


            EditorGUILayout.Space(10);

          
        
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.Space(20);
                    if (GUILayout.Button("���ɹ���ͼ����", GUILayout.Width(120), GUILayout.Height(40)))
                    {
                        if (Lightmapping.lightingDataAsset != null)
                        {
                             GenLightmapData();
                            Debug.Log("������ͼ--�����������");
                        }
                        else
                        {
                            Debug.LogError("û���ҵ�������ͼ");
                         }
                    }
                    EditorGUILayout.Space(20);
                }
                EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.Space(20);
                if (GUILayout.Button("��������еĹ���ͼ", GUILayout.Width(120), GUILayout.Height(40)))
                {

                        ClearLightData();
                        Debug.Log("������ͼ--�������������");
                
                }
                EditorGUILayout.Space(20);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            {
             
                if (GUILayout.Button("��������Ԥ����", GUILayout.Width(120), GUILayout.Height(40)))
                {

                    SaveAllPrefab();
                    Debug.Log("��������Ԥ�������");

                }
                EditorGUILayout.Space();
                if (GUILayout.Button("ֻ��������Ԥ�Ƽ�", GUILayout.Width(120), GUILayout.Height(40)))
                {
                    SaveContainerPrefab();
                    Debug.Log("������ͼ����ֻ��������Ԥ�Ƽ����");
                }

            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("��������lightmapNode\n(ֻ������ʾ�Ľڵ�)", GUILayout.Width(220), GUILayout.Height(40)))
                {
                    ClearAllLightmapNode();
                    Debug.Log("������ͼ������������lightmapNode���");
                }

                EditorGUILayout.Space();
            }
            EditorGUILayout.EndHorizontal();

        }

        void ClearAllLightmapNode()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            foreach (var go in roots)
            {
                var node = go.gameObject.GetComponentsInChildren<LightmapNode>();
                foreach (var item in node)
                {
                    DestroyImmediate(item);
                }
            }
        }

        void SaveContainerPrefab()
        {
            LightmapMgr lightmapMgr = FindLightmapMgr();
            if (lightmapMgr != null)
            {
                SavePrefab(lightmapMgr.gameObject);
            }
        }

        void ClearLightData()
        {
            Lightmapping.lightingDataAsset = null;
            SetLightNode(false);
            ClearRenderer();
        }
        void SaveAllPrefab()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            foreach (var go in roots)
            {
                SavePrefab(go);
            }
        }
   
        private void SetLightProp()
        {
            SetLightNode(true);
            LightmapMgr lightmapMgr = FindLightmapMgr();
            lightmapMgr?.gameObject.SetActive(false);
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            foreach (var item in roots)
            {
                var lightmapNodes = item.GetComponentsInChildren<LightmapNode>();
                foreach (var lightmapNode in lightmapNodes)
                {
                    lightmapNode.enabled = false;
                    lightmapNode.ClearBlockProp();
                }
            }

            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.OpenScene(scene.path);

        }

        private void GenLightmapData()
        {
            GenLightmapMgr();

            GenLightMapNode();
        }

        #region ����
        void GenLightMapNode()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            foreach (var item in roots)
            {
                var renderers = item.GetComponentsInChildren<MeshRenderer>();
                foreach (var render in renderers)
                {
                    if (render.lightmapIndex != -1)
                    {
                        LightmapNode node = null;
                        if (!render.gameObject.TryGetComponent<LightmapNode>(out node))
                        {
                            node = render.gameObject.AddComponent<LightmapNode>();
                        }
                        node.enabled = true;
                     
                        LightmapProp prop = new LightmapProp();
                        prop.lightmapIndex = render.lightmapIndex;
                        prop.lightmapST = render.lightmapScaleOffset;
                        prop.lightmapsMode = Lightmapping.lightingSettings.directionalityMode;
                        prop.mixedLightingMode = Lightmapping.lightingSettings.mixedBakeMode;
                        
                        
                  
                        node.Register(type, prop);
                    }

                }
            }
        }

        void GenLightmapMgr()
        {
            LightmapMgr lightmapMgr = FindLightmapMgr();
            if (lightmapMgr == null)
            {
                GameObject go = new GameObject(); 
                go.name = $"lightmapMgr_{type}";
                lightmapMgr = go.AddComponent<LightmapMgr>();

            }
            lightmapMgr.gameObject.SetActive(true);
            lightmapMgr.SetType(this.type);


            //lightmapMgr.Register(type, GenTexturePackage());
            GenTexturePackage(ref lightmapMgr);


        }

       void  GenTexturePackage(ref LightmapMgr lightmapMgr)
        {
    
            var lightmap = LightmapSettings.lightmaps;
          //  var lightingDataAsset = Lightmapping.lightingDataAsset;
            

            foreach (var item in lightmap)
            {

                TexturePackage tp = new TexturePackage() {
                    lightmapColor = item.lightmapColor,
                    lightmapDir = item.lightmapDir,
                    shadowMask = item.shadowMask
                };
           

                lightmapMgr.Register(type, tp);

            }
          
            
        }


        //���صƹ�



            private void SetLightNode(bool active)
        {
            var go = GameObject.Find("lightNode");
            if (go == null)
            {
                Debug.Log("û���ҵ�LightNode �ڵ�");
                return;
            }
            for (int i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(0);
                if (child.name == "baked")
                {
                    child.gameObject.SetActive(active);
                }
            }

        }

        private LightmapMgr FindLightmapMgr()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            foreach (var go in roots)
            {
                var LightmapMgr = go.GetComponentsInChildren<LightmapMgr>(true);
                foreach (var item in LightmapMgr)
                {
                    
                    return item;
                }
            }
            return null;
        }

        void ClearRenderer()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            foreach (var go in roots)
            {
                var meshRender = go.GetComponentsInChildren<MeshRenderer>();

                foreach (var item in meshRender)
                {
                    item.lightmapIndex = -1;
                    item.lightmapScaleOffset = Vector4.zero;

                }
            }
        }
        void SavePrefab(GameObject go)
        {
            if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
            {
                PrefabUtility.ApplyPrefabInstance(go, InteractionMode.AutomatedAction);
         
            }
            else
            {
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    SavePrefab(go.transform.GetChild(i).gameObject);
                }
            }
        }
        #endregion

    }
}
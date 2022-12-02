using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static YLib.Lightmap.LightmapMgr;

namespace YLib.Lightmap
{

    public class LightmapGenWindow : CustomEditorWindowBase
    {
        [MenuItem("�ҵĹ���/������ͼ/���������ͼ���£�", false, 15000)]
        static void Init()
        {
            var type = typeof(LightmapGenWindow);
            var myWindow = EditorWindow.GetWindow(type, false, type.Name, true);
            myWindow.Show();
        }

        [ValueDropdown("GetTypeName")]
        public int type;

        public LightmapType LightType;



        private IEnumerable GetTypeName()
        {
            if (LightmapTypeData.Inst != null)
            {
                return LightmapTypeData.Inst.Propertys;
            }
            return null;
        }



        protected override IEnumerable<object> GetTargets()
        {
            yield return LightmapTypeData.Inst;

            yield return this;

        }



        [Sirenix.OdinInspector.PropertySpace(SpaceAfter = 100)]
        [Sirenix.OdinInspector.OnInspectorGUI]
        public void OneClickBtn()
        {
#if UNITY_EDITOR
            using (new YLib.EditorTool.Drawer.Horizontal())
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("һ������", GUILayout.Width(120), GUILayout.Height(40)))
                {
                    var scene = EditorSceneManager.GetActiveScene();
                    if (EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new Scene[] { scene }))
                    {
                        string debugInfo = "";
                        try
                        {
                            var curInfo = LightmapTypeData.Inst.typeMap[type];

                            debugInfo = "�л�����";
                            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(curInfo.scene), OpenSceneMode.Single);

                            debugInfo = "�決ǰ���õƹ�";
                            AfterBakedSetLight();

                            debugInfo = "�決";
                            //if (!Lightmapping.Bake())
                            //{
                            //    throw new ArgumentNullException("Baked Error");
                            //}
                            Lightmapping.Bake();

                            RenameBakeLightmap();


                            debugInfo = "��������";
                            if (Lightmapping.lightingDataAsset != null)
                            {
                                GenLightmapData();
                                Debug.Log("������ͼ���������������");
                            }
                            else
                            {
                                throw new ArgumentNullException("Baked Error");
                            }

                            debugInfo = "����unity����ϵͳ";
                            ClearUnityLighting();

                            debugInfo = "����Ԥ�Ƽ�";
                            switch (curInfo.type)
                            {
                                case LightmapTypeData.SavePrefabType.None:
                                    break;
                                case LightmapTypeData.SavePrefabType.SaveContainer:
                                    SaveContainerPrefab();
                                    break;
                                case LightmapTypeData.SavePrefabType.SaveAll:
                                    SaveAllPrefab();
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"{debugInfo}---ʧ��");
                            Debug.LogError(ex);
                        }
                    }
                }
                EditorGUILayout.Space();
            }
#endif
        }

        [FoldoutGroup("������")]
        [Sirenix.OdinInspector.OnInspectorGUI]
        public void DebugButtons()
        {
            EditorGUILayout.Space(10);

       

            using (new YLib.EditorTool.Drawer.Vertical())
            {
                using (new YLib.EditorTool.Drawer.Horizontal())
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("�決ǰ���õƹ�", GUILayout.Width(120), GUILayout.Height(40)))
                    {
                   //     RenameBakeLightmap();
                        AfterBakedSetLight();
                    }
                    EditorGUILayout.Space();
                }

                EditorGUILayout.Space(20);


                using (new YLib.EditorTool.Drawer.Horizontal())
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("�決", GUILayout.Width(120), GUILayout.Height(40)))
                    {
                        if (Lightmapping.Bake())
                        {
                            RenameBakeLightmap();
                                Debug.Log("������ͼ�����決���");
                        }
                        else
                        {
                            Debug.LogError("������ͼ�����決ʧ��");
                        }
                    }
                    EditorGUILayout.Space();
                }

                EditorGUILayout.Space(20);

                using (new YLib.EditorTool.Drawer.Horizontal())
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("��������", GUILayout.Width(120), GUILayout.Height(40)))
                    {
                        if (Lightmapping.lightingDataAsset != null)
                        {
                            GenLightmapData();
                            Debug.Log("������ͼ���������������");
                        }
                        else
                        {
                            Debug.LogError("������ͼ����û�й�����ͼ");
                        }

                    }
                    EditorGUILayout.Space();
                }


                EditorGUILayout.Space(20);

                using (new YLib.EditorTool.Drawer.Horizontal())
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("����unity����ϵͳ", GUILayout.Width(120), GUILayout.Height(40)))
                    {
                        ClearUnityLighting();
                        Debug.Log("������ͼ��������unity����ϵͳ���");
                    }
                    EditorGUILayout.Space();
                }

                EditorGUILayout.Space(20);

                using (new YLib.EditorTool.Drawer.Horizontal())
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("��������Ԥ�Ƽ�", GUILayout.Width(120), GUILayout.Height(40)))
                    {
                        SaveAllPrefab();
                        Debug.Log("������ͼ������������Ԥ�Ƽ����");
                    }
                    EditorGUILayout.Space();
                    if (GUILayout.Button("ֻ��������Ԥ�Ƽ�", GUILayout.Width(120), GUILayout.Height(40)))
                    {
                        SaveContainerPrefab();
                        Debug.Log("������ͼ����ֻ��������Ԥ�Ƽ����");
                    }
                    EditorGUILayout.Space();
                }

                EditorGUILayout.Space(20);

                using (new YLib.EditorTool.Drawer.Horizontal())
                {
                    EditorGUILayout.Space();

                    if (GUILayout.Button("��������lightmapNode\n(ֻ������ʾ�Ľڵ�)", GUILayout.Width(220), GUILayout.Height(40)))
                    {
                        ClearAllLightmapNode();
                        Debug.Log("������ͼ������������lightmapNode���");
                    }

                    EditorGUILayout.Space();
                }

                EditorGUILayout.Space(20);
            }
        }

        private void RenameBakeLightmap()
        {
            string name = "_" + LightType.ToString();
            LightmapData[] lightmaps = LightmapSettings.lightmaps;
            foreach (var item in lightmaps)
            {
                if (item.lightmapColor != null)
                {
                    string path = AssetDatabase.GetAssetPath(item.lightmapColor);
                    //��ͬ������ͼ�Ƿ��Ѵ���
                    SetBakeTexNameReplace(path, name);
                    AssetDatabase.RenameAsset(path,item.lightmapColor.name + name);
             
                }
                if (item.lightmapDir != null)
                {
                    string path = AssetDatabase.GetAssetPath(item.lightmapDir);
                    SetBakeTexNameReplace(path, name);
                    AssetDatabase.RenameAsset(path, item.lightmapDir.name + "_" + LightType.ToString());
       
                }
                if (item.shadowMask != null)
                {
                    string path = AssetDatabase.GetAssetPath(item.shadowMask);
                    SetBakeTexNameReplace(path, name);
                    AssetDatabase.RenameAsset(path, item.shadowMask.name + "_" + LightType.ToString());
                }
            }
        }
        private void SetBakeTexNameReplace(string path , string name)
        {
            string trail = path.Substring(path.Length - 4);
            string newpath = path.Substring(0, path.Length - 4);
            string newp = newpath + name + trail;
            Debug.Log(newp);
            UnityEngine.Object t = AssetDatabase.LoadAssetAtPath(newp, typeof(Texture2D));
            if (t != null)
            {
                AssetDatabase.DeleteAsset(newpath + name + trail);
                AssetDatabase.Refresh();
            }
        }


        protected override void DrawEditorEnd()
        {

        }

        private void AfterBakedSetLight()
        {
            SetLightNode(true);

            LightmapContainer lightmapContainer = FindContainer();
            lightmapContainer?.gameObject.SetActive(false);

            var scene = EditorSceneManager.GetActiveScene();
            var rootGOs = scene.GetRootGameObjects();

            foreach (var item in rootGOs)
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
            //�������ͼ���ݵ�Container
            GenLightmapContainer();
            GenLightmapNode();
        }

        private void GenLightmapContainer()
        {
            LightmapContainer lightmapContainer = FindContainer();

            if (lightmapContainer == null)
            {
                GameObject go = new GameObject();
                go.name = $"lightmapContainer_{LightmapTypeData.Inst.typeMap[type].name}";
                lightmapContainer = go.AddComponent<LightmapContainer>();
            }

            lightmapContainer.gameObject.SetActive(true);
            lightmapContainer.SetType(type);

            lightmapContainer.Register(LightType, GenTexturePackages());
//            lightmapContainer.TexturePackages = GenTexturePackages();
        }

        private LightmapContainer FindContainer()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var rootGOs = scene.GetRootGameObjects();
            foreach (var rootGO in rootGOs)
            {
                var lightmapContainers = rootGO.GetComponentsInChildren<LightmapContainer>(true);
                foreach (var item in lightmapContainers)
                {
                    if (item.type == type)
                    {
                        return item;
                    }
                }
            }

            return null;
        }

        private void GenLightmapNode()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var rootGOs = scene.GetRootGameObjects();

            foreach (var item in rootGOs)
            {
                var renderers = item.GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer.lightmapIndex != -1)
                    {
                        LightmapNode node = null;
                        if (!renderer.gameObject.TryGetComponent<LightmapNode>(out node))
                        {
                            node = renderer.gameObject.AddComponent<LightmapNode>();
                        }

                        node.enabled = true;
                        node.type = type;
                       // node.LightmapType = LightType;
                        LightProp prop = new LightProp();


                        prop.lightmapIndex = renderer.lightmapIndex;
                        prop.lightmapST = renderer.lightmapScaleOffset;
                        prop.lightmapsMode = Lightmapping.lightingSettings.directionalityMode;
                        prop.mixedLightingMode = Lightmapping.lightingSettings.mixedBakeMode;
                        node.Register(LightType, prop);
                    }
                }
            }
        }

        private List<TexturePackage> GenTexturePackages()
        {
            var texturePackage = new List<TexturePackage>();

            var lightmaps = LightmapSettings.lightmaps;
            var lightingDataAsset = Lightmapping.lightingDataAsset;
            var dependencie = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(lightingDataAsset));
            Cubemap specCube = null;
            foreach (var item in dependencie)
            {
                if (item.EndsWith("-0.exr"))
                {
                    specCube = AssetDatabase.LoadAssetAtPath<Cubemap>(item);
                    break;
                }
            }

            foreach (var item in lightmaps)
            {
            
                texturePackage.Add(new TexturePackage()
                {
                    lightmapColor = item.lightmapColor,
                    lightmapDir = item.lightmapDir,
                    shadowMask = item.shadowMask,
                    //specCube = specCube,
                });
            }

            return texturePackage;
        }

        private void ClearUnityLighting()
        {
            Lightmapping.lightingDataAsset = null;

            SetLightNode(false);

            ClearRenderer();

       //     ClearOldComponete();

        }

        private void SetLightNode(bool active)
        {
            var go = GameObject.Find("lightNode");
            if (go == null)
            {
                Debug.LogError("û���ҵ��ڵ� lightNdoe");
                return;
            }
            for (int i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i);
                if (child.name == "baked")
                {
                    child.gameObject.SetActive(active);
                }
                else if (child.name == "realtime")
                {
                    child.gameObject.SetActive(!active);
                }
            }
        }

        private void ClearRenderer()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var rootGOs = scene.GetRootGameObjects();
            foreach (var rootGO in rootGOs)
            {
                var meshRenderers = rootGO.GetComponentsInChildren<MeshRenderer>();
                foreach (var item in meshRenderers)
                {
                    item.lightmapIndex = -1;
                    item.lightmapScaleOffset = Vector4.zero;
                }
            }
        }

        //private void ClearOldComponete()
        //{
        //    var scene = EditorSceneManager.GetActiveScene();
        //    var rootGOs = scene.GetRootGameObjects();
        //    foreach (var rootGO in rootGOs)
        //    {
        //        var items = rootGO.GetComponentsInChildren<Utility.DynamicLightMapItem>(true);
        //        for (int i = items.Length - 1; i >= 0; i--)
        //        {
        //            UnityEngine.Object.DestroyImmediate(items[i]);
        //        }
        //    }
        //}

        private void SaveAllPrefab()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var rootGOs = scene.GetRootGameObjects();
            foreach (var rootGO in rootGOs)
            {
                SavePrefab(rootGO);
            }
        }

        private void SaveContainerPrefab()
        {
            LightmapContainer lightmapContainer = FindContainer();

            if (lightmapContainer != null)
            {
                SavePrefab(lightmapContainer.gameObject);
            }
        }

        private void SavePrefab(GameObject go)
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

        private void ClearAllLightmapNode()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var rootGOs = scene.GetRootGameObjects();
            foreach (var rootGO in rootGOs)
            {
                var list = rootGO.gameObject.GetComponentsInChildren<LightmapNode>();
                foreach (var item in list)
                {
                    DestroyImmediate(item);
                }
            }
        }
    }
}

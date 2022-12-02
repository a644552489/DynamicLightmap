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
        [MenuItem("我的工具/光照贴图/打包光照贴图（新）", false, 15000)]
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
                if (GUILayout.Button("一键操作", GUILayout.Width(120), GUILayout.Height(40)))
                {
                    var scene = EditorSceneManager.GetActiveScene();
                    if (EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new Scene[] { scene }))
                    {
                        string debugInfo = "";
                        try
                        {
                            var curInfo = LightmapTypeData.Inst.typeMap[type];

                            debugInfo = "切换场景";
                            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(curInfo.scene), OpenSceneMode.Single);

                            debugInfo = "烘焙前设置灯光";
                            AfterBakedSetLight();

                            debugInfo = "烘焙";
                            //if (!Lightmapping.Bake())
                            //{
                            //    throw new ArgumentNullException("Baked Error");
                            //}
                            Lightmapping.Bake();

                            RenameBakeLightmap();


                            debugInfo = "生成数据";
                            if (Lightmapping.lightingDataAsset != null)
                            {
                                GenLightmapData();
                                Debug.Log("光照贴图——生成数据完成");
                            }
                            else
                            {
                                throw new ArgumentNullException("Baked Error");
                            }

                            debugInfo = "清理unity照明系统";
                            ClearUnityLighting();

                            debugInfo = "保存预制件";
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
                            Debug.LogError($"{debugInfo}---失败");
                            Debug.LogError(ex);
                        }
                    }
                }
                EditorGUILayout.Space();
            }
#endif
        }

        [FoldoutGroup("测试用")]
        [Sirenix.OdinInspector.OnInspectorGUI]
        public void DebugButtons()
        {
            EditorGUILayout.Space(10);

       

            using (new YLib.EditorTool.Drawer.Vertical())
            {
                using (new YLib.EditorTool.Drawer.Horizontal())
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("烘焙前设置灯光", GUILayout.Width(120), GUILayout.Height(40)))
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
                    if (GUILayout.Button("烘焙", GUILayout.Width(120), GUILayout.Height(40)))
                    {
                        if (Lightmapping.Bake())
                        {
                            RenameBakeLightmap();
                                Debug.Log("光照贴图——烘焙完成");
                        }
                        else
                        {
                            Debug.LogError("光照贴图——烘焙失败");
                        }
                    }
                    EditorGUILayout.Space();
                }

                EditorGUILayout.Space(20);

                using (new YLib.EditorTool.Drawer.Horizontal())
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("生成数据", GUILayout.Width(120), GUILayout.Height(40)))
                    {
                        if (Lightmapping.lightingDataAsset != null)
                        {
                            GenLightmapData();
                            Debug.Log("光照贴图——生成数据完成");
                        }
                        else
                        {
                            Debug.LogError("光照贴图——没有光照贴图");
                        }

                    }
                    EditorGUILayout.Space();
                }


                EditorGUILayout.Space(20);

                using (new YLib.EditorTool.Drawer.Horizontal())
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("清理unity照明系统", GUILayout.Width(120), GUILayout.Height(40)))
                    {
                        ClearUnityLighting();
                        Debug.Log("光照贴图——清理unity照明系统完成");
                    }
                    EditorGUILayout.Space();
                }

                EditorGUILayout.Space(20);

                using (new YLib.EditorTool.Drawer.Horizontal())
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("保存所有预制件", GUILayout.Width(120), GUILayout.Height(40)))
                    {
                        SaveAllPrefab();
                        Debug.Log("光照贴图——保存所有预制件完成");
                    }
                    EditorGUILayout.Space();
                    if (GUILayout.Button("只保存容器预制件", GUILayout.Width(120), GUILayout.Height(40)))
                    {
                        SaveContainerPrefab();
                        Debug.Log("光照贴图——只保存容器预制件完成");
                    }
                    EditorGUILayout.Space();
                }

                EditorGUILayout.Space(20);

                using (new YLib.EditorTool.Drawer.Horizontal())
                {
                    EditorGUILayout.Space();

                    if (GUILayout.Button("清理所有lightmapNode\n(只清理显示的节点)", GUILayout.Width(220), GUILayout.Height(40)))
                    {
                        ClearAllLightmapNode();
                        Debug.Log("光照贴图——清理所有lightmapNode完成");
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
                    //相同类型贴图是否已存在
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
            //传入光照图数据到Container
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
                Debug.LogError("没有找到节点 lightNdoe");
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

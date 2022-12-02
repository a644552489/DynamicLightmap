using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace YLib.Lightmap
{
    public class LightmapMgr
    {

        #region 单例
        private static LightmapMgr g_Inst = null;
        public static LightmapMgr Inst
        {
            get
            {
                if (g_Inst == null)
                {
                    g_Inst = new LightmapMgr();
                }
                return g_Inst;
            }
        }

        #endregion

        #region  枚举
        [System.Serializable]
        public class TexturePackage
        {
            public Texture2D lightmapDir;
            public Texture2D shadowMask;
            public Texture2D lightmapColor;

            private static TexturePackage _temp = new TexturePackage();
            public static TexturePackage NULL { get { return _temp; } }
        }
        [System.Serializable]
        public class LightProp
        {
            public int lightmapIndex = -1;
            public Vector4 lightmapST = Vector4.zero;
            public LightmapsMode lightmapsMode = LightmapsMode.NonDirectional;
            public MixedLightingMode mixedLightingMode = MixedLightingMode.Subtractive;
        }


        public enum LightmapType
        {
            Morning,
            Evening
        }


        #endregion

        #region 属性
        public Dictionary<int, List<LightmapContainer>> map = new Dictionary<int, List<LightmapContainer>>();

        //   public Dictionary<int, Dictionary<LightmapType, List<LightmapContainer>>> maps = new Dictionary<int, Dictionary<LightmapType, List<LightmapContainer>>>();

        private LightmapType lightType;
        public LightmapType LightType
        {
            get { return lightType; }

            set {
                if (lightType != value)
                {
                    lightType = value;
                    if (SetLightmapType != null)
                        SetLightmapType();
                }
            }
        }

        public static Action SetLightmapType;

        #endregion

        private LightmapMgr()
        {

        }

        public TexturePackage GetTexturePackageByInfo(int type , int index)
        {
            if (map.ContainsKey(type) && map[type].Count > 0)
            {
                return map[type][0].GetTexturePackageByIndex(LightType, index);
            }
            return null;
        }

        public void SetLightMapType(LightmapType t)
        {
            LightType = t;
        }

        public void Register(LightmapContainer container)
        {
            var type = container.type;
            if (!map.ContainsKey(type))
            {
                map.Add(type, new List<LightmapContainer>());
            }

            if (!map[type].Contains(container))
            {
                map[type].Add(container);
            }

#if IS_ART
            UpdateKeyWorld();
#endif
        }

        public void Logout(LightmapContainer container)
        {
            var type = container.type;
            if (map.ContainsKey(type))
            {
                map[type].Remove(container);

                if (map[type].Count == 0)
                {
                    map.Remove(type);
                }

#if IS_ART
                UpdateKeyWorld();
#endif
            }
        }

        private void UpdateKeyWorld()
        {
            if (map.Count == 0)
            {
                CloseAllKeyword();
                return;
            }

            bool isDirMap = CheckDirMap();
            bool isShadowmask = CheckShadowmask();

            if (!Application.isPlaying)
            {
                if (isDirMap)
                {
                    Shader.EnableKeyword("CUSTOM_DIRLIGHTMAP_COMBINED");
                }
                else
                {
                    Shader.DisableKeyword("CUSTOM_DIRLIGHTMAP_COMBINED");
                }

                if (isShadowmask)
                {
                    Shader.EnableKeyword("CUSTOM_SHADOWS_SHADOWMASK");
                }
                else
                {
                    Shader.DisableKeyword("CUSTOM_SHADOWS_SHADOWMASK");
                }

                Shader.EnableKeyword("CUSTOM_LIGHTMAP_SHADOW_MIXING");
                Shader.EnableKeyword("CUSTOM_LIGHTMAP_ON");

                return;
            }
            else
            {
                CloseAllKeyword();
            }
        }

        private bool CheckDirMap()
        {
            int hasDir = -1;
            foreach (var kv in map)
            {
                foreach (var container in kv.Value)
                {
                    foreach (var itemList in container.TexturePackages.Values)
                    {
                        foreach (var item in itemList)
                        {
                            if (item.lightmapDir != null)
                            {
                                if (hasDir != -1 && hasDir != 1)
                                {
                                    Debug.Log("不是所有的LightmapContainer都有Dirmap");
                                    return false;
                                }

                                hasDir = 1;
                            }
                            else
                            {
                                if (hasDir != -1 && hasDir != 0)
                                {
                                    Debug.Log("不是所有的LightmapContainer都有Dirmap");
                                    return false;
                                }

                                hasDir = 0;
                            }
                        }
                    }
                }
            }
            return hasDir == 1;
        }

        private bool CheckShadowmask()
        {
            foreach (var kv in map)
            {
                foreach (var container in kv.Value)
                {
                    foreach (var itemList in container.TexturePackages.Values)
                    {
                        foreach (var item in itemList)
                        {
                            if (item.shadowMask != null)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private void CloseAllKeyword()
        {
            Shader.DisableKeyword("CUSTOM_DIRLIGHTMAP_COMBINED");
            Shader.DisableKeyword("CUSTOM_SHADOWS_SHADOWMASK");
            Shader.DisableKeyword("CUSTOM_LIGHTMAP_SHADOW_MIXING");
            Shader.DisableKeyword("CUSTOM_LIGHTMAP_ON");
        }
    }
}



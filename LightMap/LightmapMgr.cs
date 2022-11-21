using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;

namespace OuY.Lightmap
{
    [ExecuteInEditMode]
    public class LightmapMgr :SerializedMonoBehaviour
    {
        public  delegate void SetLightmapNodeType();
        public static event SetLightmapNodeType SetTypeValue;
        public static LightmapMgr Inst = null;


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
        public class LightmapProp
        {
            public int lightmapIndex = -1;
            public Vector4 lightmapST = Vector4.zero;
            public LightmapsMode lightmapsMode = LightmapsMode.NonDirectional;
            public MixedLightingMode mixedLightingMode = MixedLightingMode.Subtractive;

        }
         [SerializeField]
        public LightmapType Type
        {
            get
            {
                return type;
            }

            set
            {
                if (type != value)
                {
                    type = value;
                    LightmapNode.SetType(type);

                   if(SetTypeValue  != null)
                        SetTypeValue();

                }
            }
        }

        private LightmapType type;

        [SerializeField]
        public Dictionary<LightmapType, List<TexturePackage>> map = new Dictionary<LightmapType, List<TexturePackage>>();

   


        public void SetType(LightmapType t)
        {
            Type = t;
     
        }

        public void Register(LightmapType type, TexturePackage texturePackage)
        {
            if (!map.ContainsKey(type))
            {
                map.Add(type, new List<TexturePackage>());
            }
           
     
            for (int i = 0; i < map[type].Count; i++)
            {
                
                if (map[type][i].lightmapColor == texturePackage.lightmapColor)
                {
                    map[type].Remove(map[type][i]);
                }

            }
            map[type].Add(texturePackage);

            UpdateKeyword();

        }



        private LightmapMgr()
        { }
        public  TexturePackage GetTexturePackageByInfo( LightmapType type, int index)
        {
            if (map.ContainsKey(type) && map[type].Count > 0)
            {
                return map[type][index];


            }
            else
            {
                Debug.Log($"LightmapMgr:don't find tex, type = {type} , index = {index} ");
            }
            return null;
        }

        private void Awake()
        {
            
            Inst = this;
            UpdateKeyword();
        }
        private void OnDestroy()
        {
            UpdateKeyword();   
        }

        private void UpdateKeyword()
        {
            if (map.Count == 0)
            {
                CloseAllKeyword();
                    return;
            }

            bool isDirMap = CheckDirMap();
            bool isShadowmap = CheckShadowMask();

            if (!Application.isPlaying)
            {
                if (isDirMap)
                {
                    Shader.EnableKeyword("DIRLIGHTMAP_COMBINED");
                }
                else
                {
                    Shader.DisableKeyword("DIRLIGHTMAP_COMBINED");
                }

                if (isShadowmap)
                {
                    Shader.EnableKeyword("SHADOWS_SHADOWMASK");
                }
                else
                {
                    Shader.DisableKeyword("SHADOWS_SHADOWMASK");
                }

                Shader.EnableKeyword("LIGHTMAP_SHADOW_MIXING");
                Shader.EnableKeyword("LIGHTMAP_ON");
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
                if (kv.Value == null) return false;
                foreach (var item in kv.Value)
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
            return hasDir == 1;
        }

        private bool CheckShadowMask()
        {
            foreach (var contain in map)
            {
                foreach (var item in contain.Value)
                {
                    if (item.shadowMask != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void CloseAllKeyword()
        {
            Shader.DisableKeyword("DIRLIGHTMAP_COMBINED");
            Shader.DisableKeyword("SHADOWS_SHADOWMASK");
            Shader.DisableKeyword("LIGHTMAP_SHADOW_MIXING");
            Shader.DisableKeyword("LIGHTMAP_ON");
        }



     public   void SetTypeMorning()
        {
            Type = LightmapType.Morning;
    
        }

        public void SetTypeAfternoon()
        {
            Type = LightmapType.Afternoon;
   
        }
        public void SetTypeEvening()
        {
            Type = LightmapType.Evening;

        }


    }

    public enum LightmapType
    {
        Morning,
        Afternoon,
        Evening
    }


}
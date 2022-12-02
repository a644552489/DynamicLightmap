using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static YLib.Lightmap.LightmapMgr;
using Sirenix.Serialization;

namespace YLib.Lightmap
{

    [ExecuteInEditMode]
    public class LightmapNode : SerializedMonoBehaviour
    {
        public static int Unity_Lightmap_ID = Shader.PropertyToID("unity_Lightmap");
        public static int Unity_LightmapInd_ID = Shader.PropertyToID("unity_LightmapInd");
        public static int Unity_ShadowMask_ID = Shader.PropertyToID("unity_ShadowMask");
        public static int Unity_LightmapST_ID = Shader.PropertyToID("unity_LightmapST");
        public static int Unity_SpecCube0_ID = Shader.PropertyToID("unity_SpecCube0");
        public static int Custom_LightmapST_ID = Shader.PropertyToID("_Custom_LightmapST");

     
        public static MaterialPropertyBlock block = null;
         
        [ValueDropdown("GetTypeName")]
        public int type;
    
        private IEnumerable GetTypeName()
        {
            if (LightmapTypeData.Inst != null)
            {
                return LightmapTypeData.Inst.Propertys;
            }
            return null;
        }
        [SerializeField]
        public Dictionary<LightmapType, LightProp> LightmapProp = new Dictionary<LightmapType, LightProp>();



        private void Awake()
        {
            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }
        }

        private void Start()
        {
#if !IS_ART
            LightmapMgr.SetLightmapType += SetProperty;
            SetProperty();
#endif
        }

        private void OnEnable()
        {
#if !IS_ART
            LightmapMgr.SetLightmapType += SetProperty;
            SetProperty();
#endif
        }
        private void OnDestroy()
        {
            LightmapMgr.SetLightmapType -= SetProperty;
        }
        private void OnDisable()
        {
            LightmapMgr.SetLightmapType -= SetProperty;
        }
       
        private void Update()
        {
#if IS_ART
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null) return;

            if (renderer.lightmapIndex != -1) return;

            LightProp prop = null;
            if (LightmapProp.TryGetValue(LightmapMgr.Inst.LightType, out prop))
            {

                var texturePackage = LightmapMgr.Inst.GetTexturePackageByInfo(type, prop.lightmapIndex);
                if (texturePackage == null)
                {
                    SetBlockProp(TexturePackage.NULL, renderer, prop.lightmapST);
                    return;
                }

                if (!Application.isPlaying)
                {
                    SetBlockProp(texturePackage, renderer, prop.lightmapST);
                    return;
                }

                var material = renderer.material;
                if (material == null) return;
                SetMaterial(material, prop.lightmapsMode, prop.mixedLightingMode);
                SetBlockProp(texturePackage, renderer, prop.lightmapST);
            }
#endif
        }

        private void SetProperty()
        {
            LightProp prop = null;
            LightmapProp.TryGetValue(LightmapMgr.Inst.LightType, out prop);
            if (prop == null) return;

            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null) return;

            var material = renderer.sharedMaterial;
            if (material == null) return;

            var texturePackage = LightmapMgr.Inst.GetTexturePackageByInfo(type, prop.lightmapIndex);
            if (texturePackage == null) return;

  
            SetMaterial(material, prop.lightmapsMode, prop.mixedLightingMode);
            SetBlockProp(texturePackage, renderer, prop.lightmapST);
            
        }



        /*
                public void SetData(SLG_LightingMapData lightingMapData)
                {
                    type = lightingMapData.type;
                    lightmapIndex = lightingMapData.lightmapIndex;
                    lightmapST = lightingMapData.lightmapST;
                    lightmapsMode = lightingMapData.lightmapsMode;
                    mixedLightingMode = lightingMapData.mixedLightingMode;
                }

                public static void SetData(MeshRenderer renderer, SLG_LightingMapData lightingMapData)
                {
                    var type = lightingMapData.type;
                    var lightmapIndex = lightingMapData.lightmapIndex;
                    var lightmapST = lightingMapData.lightmapST;
                    var lightmapsMode = lightingMapData.lightmapsMode;
                    var mixedLightingMode = lightingMapData.mixedLightingMode;

                    var material = renderer.material;
                    if (material == null) return;

                    var texturePackage = LightmapMgr.Inst.GetTexturePackageByInfo(type, lightmapIndex);
                    if (texturePackage == null) return;

                    SetMaterial(material, lightmapsMode, mixedLightingMode);
                    SetBlockProp(texturePackage, renderer, lightmapST);
                }


                */
        public void Register(LightmapType t, LightProp prop)
        {
            if (!LightmapProp.ContainsKey(t))
            {
                LightmapProp.Add(t, new LightProp());
            }
            LightmapProp[t] = prop;
        }


        public static void SetMaterial(Material material, LightmapsMode lightmapsMode, MixedLightingMode mixedLightingMode)
        {
            if (lightmapsMode == LightmapsMode.CombinedDirectional)
            {
                material.EnableKeyword("DIRLIGHTMAP_COMBINED");
            }

            if (mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                material.EnableKeyword("SHADOWS_SHADOWMASK");
                material.EnableKeyword("LIGHTMAP_SHADOW_MIXING");
            }

            if (mixedLightingMode == MixedLightingMode.Subtractive)
            {
                material.EnableKeyword("LIGHTMAP_SHADOW_MIXING");
            }

            material.EnableKeyword("LIGHTMAP_ON");
        }

        private static void SetBlockProp(TexturePackage texturePackage, MeshRenderer renderer, Vector4 lightmapST)
        {
            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }

            block.Clear();

            renderer.GetPropertyBlock(block);

            block.SetVector(Unity_LightmapST_ID, lightmapST);

            if (texturePackage.lightmapColor != null)
            {
                block.SetTexture(Unity_Lightmap_ID, texturePackage.lightmapColor);
            }
            else
            {
                block.SetTexture(Unity_Lightmap_ID, Texture2D.blackTexture);
            }

            if (texturePackage.lightmapDir != null)
            {
                block.SetTexture(Unity_LightmapInd_ID, texturePackage.lightmapDir);
            }
            else
            {
                block.SetTexture(Unity_LightmapInd_ID, Texture2D.blackTexture);
            }

            if (texturePackage.shadowMask != null)
            {
                block.SetTexture(Unity_ShadowMask_ID, texturePackage.shadowMask);
            }
            else
            {
                block.SetTexture(Unity_ShadowMask_ID, Texture2D.blackTexture);
            }

            renderer.SetPropertyBlock(block);
        }

        public void ClearBlockProp()
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null) return;

            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }

            block.Clear();

            renderer.SetPropertyBlock(block);
        }



        /*
                public void CopyTo(SLG_LightingMapData lightingMapData)
                {
                    lightingMapData.type = type;
                    lightingMapData.lightmapIndex = lightmapIndex;
                    lightingMapData.lightmapST = lightmapST;
                    lightingMapData.lightmapsMode = lightmapsMode;
                    lightingMapData.mixedLightingMode = mixedLightingMode;
                }
        */
    }
}

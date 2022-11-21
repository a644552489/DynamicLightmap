using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OuY.Lightmap.LightmapMgr;
using Sirenix.OdinInspector;

namespace OuY.Lightmap
{
    [ExecuteInEditMode]
    public class LightmapNode : SerializedMonoBehaviour
    {
        public static int Unity_Lightmap_ID = Shader.PropertyToID("unity_Lightmap");
        public static int Unity_LightmapInd_ID = Shader.PropertyToID("unity_LightmapInd");
        public static int Unity_ShadowMask_ID = Shader.PropertyToID("unity_ShadowMask");
        public static int Unity_LightmapST_ID = Shader.PropertyToID("unity_LightmapST");
        public static int Unity_SpecCube0_ID = Shader.PropertyToID("unity_SpecCube0");

        public static MaterialPropertyBlock block = null;
    
        public static  LightmapType type;

        [SerializeField]
        public Dictionary<LightmapType, LightmapProp> lightmap = new Dictionary<LightmapType, LightmapProp>();

            
        private void Awake()
        {
            LightmapMgr.SetTypeValue += SetMeshRenderLightmap;
            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }
        }
        private void OnDestroy()
        {
            LightmapMgr.SetTypeValue -= SetMeshRenderLightmap;
        }




        private void Start()
        {
            SetMeshRenderLightmap();

        }

        public void SetMeshRenderLightmap()
        {
            if (LightmapMgr.Inst == null) return;
             
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null) return;

            if (!Application.isPlaying)
            {
                SetBlockProp(TexturePackage.NULL, renderer);
                return;
            }

            var material = renderer.material;
            if (material == null) return; 

         

            var texturePackage = LightmapMgr.Inst.GetTexturePackageByInfo(type, lightmap[type].lightmapIndex);
            if (texturePackage == null) return;

            SetMaterial(material);
            SetBlockProp(texturePackage, renderer);
        }

        public static void SetType(LightmapType t )
        {
            type = t;
        }

        public void Register(LightmapType t , LightmapProp prop)
        {
            if (!lightmap.ContainsKey(t))
            {
                lightmap.Add(t, new LightmapProp());
            }

            lightmap[t] = prop;
            
         
        }




        //private void Update()
        //{

            //var renderer = GetComponent<MeshRenderer>();
            //if (renderer == null) return;

            //if (renderer.lightmapIndex != -1) return;



            //var texturePackage = LightmapMgr.Inst.GetTexturePackageByInfo( type,lightmapIndex);
            //if (texturePackage == null)
            //{
            //    SetBlockProp(TexturePackage.NULL, renderer);
            //    return;
            //}
            //if (!Application.isPlaying)
            //{
            //    SetBlockProp(texturePackage, renderer);
            //    return;
            //}
            //var material = renderer.material;
            //if (material == null) return;

            //SetMaterial(material);
            //SetBlockProp(texturePackage, renderer);
      //  }



        private void SetMaterial(Material mat)
        {
            if (lightmap[type].lightmapsMode == LightmapsMode.CombinedDirectional)
            {
                mat.EnableKeyword("DIRLIGHTMAP_COMBINED");
            }

            if (lightmap[type].mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                mat.EnableKeyword("SHADOWS_SHADOWMASK");
                mat.EnableKeyword("LIGHTMAP_SHADOW_MIXING");
            }

            if (lightmap[type].mixedLightingMode == MixedLightingMode.Subtractive)
            {
                mat.EnableKeyword("LIGHTMAP_SHADOW_MIXING");
            }

            mat.EnableKeyword("LIGHTMAP_ON");
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

        private void SetBlockProp(TexturePackage texturePackage, MeshRenderer render)
        {
            if (block == null)
            {
                block = new MaterialPropertyBlock();

            }
            block.Clear();
            render.GetPropertyBlock(block);
            block.SetVector(Unity_LightmapST_ID, lightmap[type].lightmapST);


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

            render.SetPropertyBlock(block);


        }


    }
}
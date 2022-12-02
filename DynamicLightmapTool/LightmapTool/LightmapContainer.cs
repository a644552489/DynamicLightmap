using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static YLib.Lightmap.LightmapMgr;

using System;

namespace YLib.Lightmap
{
    [ExecuteInEditMode]
    public class LightmapContainer :SerializedMonoBehaviour
    {
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


       

        [SerializeField ]
        public  Dictionary<LightmapType, List<TexturePackage>> TexturePackages = new Dictionary<LightmapType, List<TexturePackage>>();

     
         void Awake()
        {
            LightmapMgr.Inst.Register(this);
        }


        private void OnEnable()
        {
#if IS_ART
            LightmapMgr.Inst.Register(this);
#endif
        }

        private void OnDisable()
        {
#if IS_ART
            LightmapMgr.Inst.Logout(this);
#endif
        }
     
        private void OnDestroy()
        {
            LightmapMgr.Inst.Logout(this);
        }
         
        public void SetType(int t)
        {
            LightmapMgr.Inst.Logout(this);
            type = t;
            LightmapMgr.Inst.Register(this);
        }

        public void Register(LightmapType t , List<TexturePackage> list)
        {
            if (!TexturePackages.ContainsKey(t))
            {
                TexturePackages.Add(t, new List<TexturePackage>());
            }

            TexturePackages[t] = list;
        }

        public void SetLightMapMorning()
        {
            LightmapMgr.Inst.LightType = LightmapType.Morning;
        }
        public void SetLightMapEvening()
        {
            LightmapMgr.Inst.LightType = LightmapType.Evening;
        }

        public TexturePackage GetTexturePackageByIndex( LightmapType t,int index)
        {
            if (TexturePackages[t].Count > index)
            {
                return TexturePackages[t][index];
            }
            else
            {
                Debug.LogError($"LightmapContainer:don't find tex, type = {type} ,light = {t} , index = {index} ");
                return null;
            }
        }
          

    }
}

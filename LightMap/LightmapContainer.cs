using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OuY.Lightmap.LightmapMgr;

namespace OuY.Lightmap
{
    
    

    [CreateAssetMenu(fileName = "LightmapData" , menuName = "ScriptableObject/LightmapData" , order =1)]
    public class LightmapContainer : ScriptableObject
    {
        
        public LightmapType type;

        [SerializeField]
        public List<TexturePackage> TexturePackages = new List<TexturePackage>();
 

    }

   
}
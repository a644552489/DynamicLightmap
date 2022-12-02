using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CustomRenderer
{
    [Serializable]
    [CreateAssetMenu(fileName = "DrawMeshGpuIncetancingOnlyIdData", menuName = "我的脚本对象/Draw Mesh Id Data")]
    public class DrawMeshGpuIncetancingOnlyIdData : SerializedScriptableObject
    {
        [HideReferenceObjectPicker]
        [Serializable]
        public class OnlyIdMaker 
        {
            [HideInInspector]
            public int cur_onlyId = 0;

            [SerializeField]
            [Searchable]
            [DictionaryDrawerSettings(KeyColumnWidth = 250)]
            public Dictionary<Material, Dictionary<Mesh, int>> data;

            public OnlyIdMaker()
            {
                data = new Dictionary<Material, Dictionary<Mesh, int>>();
            }

            public int GetID(Material material, Mesh mesh,out bool isNewAdd)
            {
                isNewAdd = false;
                if (!data.ContainsKey(material))
                {
                    data.Add(material, new Dictionary<Mesh, int>());
                    isNewAdd = true;
                }

                if (!data[material].ContainsKey(mesh))
                {
                    data[material].Add(mesh, cur_onlyId);
                    cur_onlyId++;
                    isNewAdd = true;
                }

                return data[material][mesh];
            }
        }

        [SerializeField]
        Dictionary<int, OnlyIdMaker> map;

        DrawMeshGpuIncetancingOnlyIdData()
        {
            map = new Dictionary<int, OnlyIdMaker>();
        }


        public int GetID(int layer, Material material, Mesh mesh)
        {
            var isNewAdd = false;

            if (!map.ContainsKey(layer))
            {
                map.Add(layer, new OnlyIdMaker());
            }
            var id = map[layer].GetID(material, mesh,out isNewAdd);

            if (isNewAdd)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }

            return id;
        }

        [ShowInInspector]
        public void Find()
        {
            var a = map[24];

            foreach (var item in a.data)
            {
                if (item.Key.name == "t_dsj_zb_s_02_lod")
                {

                    foreach (var b in item.Value)
                    {
                        if (b.Key.name == "p_dsj_zb_04_zh_lod_shu3_t_dsj_zb_s_01_lod")
                        {
                            Debug.Log($"{b.Value}");
                            return;
                        }
                    }
                }
            }
        }

        [Button("检查id正确性")]
        public void Check()
        {
            foreach (var keyValuePair in map)
            {
                var maker = keyValuePair.Value;
                var curId = maker.cur_onlyId;
                var checkMap = new Dictionary<int, KeyValuePair<Material,Mesh>>();

                foreach (var item in maker.data)
                {
                    foreach (var kv in item.Value)
                    {
                        if(curId == kv.Value)
                        {
                            Debug.LogError($"layer = {keyValuePair.Key},mat = {item.Key.name},mesh = {kv.Key}:与当前id相同");
                        }
                        if (checkMap.ContainsKey(kv.Value))
                        {
                            Debug.LogError(
                                $"有重复的：\n" +
                                $"layer = {keyValuePair.Key},mat = {item.Key.name},mesh = {kv.Key.name} \n " +
                                $"layer = {keyValuePair.Key},mat = {checkMap[kv.Value].Key.name},mesh = {checkMap[kv.Value].Value.name}"
                                );
                        }
                        else
                        {
                            checkMap.Add(kv.Value, new KeyValuePair<Material, Mesh>(item.Key, kv.Key));
                        }
                    }
                }
            }

            Debug.Log("检查完成");
        }
    }
}

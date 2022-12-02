using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomRenderer
{
    [ExecuteInEditMode]
    public class CustomRendererExtend : MonoBehaviour
    {
        static MaterialPropertyBlock block = null;
        public static int OffsetUV_ID = Shader.PropertyToID("_OffsetUV");
        public static int VertexSpeed_ID = Shader.PropertyToID("_VertexSpeed");
        public static int VertexScale_ID = Shader.PropertyToID("_VertexScale");

        private Renderer m_renderer;

        //--------------------------------

        [OnValueChanged("RefreshBlock")]
        public bool isOffsetUV = false;
        [EnableIf("isOffsetUV")]
        [OnValueChanged("RefreshBlock")]
        public Vector4 offsetUV;

        [PropertySpace(10)]
        [OnValueChanged("RefreshBlock")]
        public bool isRendererPriority = false;
        [EnableIf("isRendererPriority")]
        [OnValueChanged("RefreshBlock")]
        public int rendererPriority = 0;

        [PropertySpace(10)]
        [OnValueChanged("RefreshBlock")]
        [LabelText("[CombineMesh] Is Vertex Offset ")]
        [OnValueChanged("RefreshBlock")]
        public bool combineMesh_isVertexOffset = true;

        //--------------------------------

        void Start()
        {
            RefreshBlock();
        }

        public void RefreshBlock()
        {
            if (block == null)
                block = new MaterialPropertyBlock();

            if (m_renderer == null)
            {
                if (!TryGetComponent<Renderer>(out m_renderer))
                {
                    return;
                }
            }

            if (isRendererPriority)
            {
                m_renderer.rendererPriority = rendererPriority;
            }

            bool isChangeBlock = isOffsetUV || !combineMesh_isVertexOffset;
            if (isChangeBlock)
            {
                m_renderer.GetPropertyBlock(block);

                if (isOffsetUV)
                {
                    block.SetVector(OffsetUV_ID, offsetUV);
                }

                if (!combineMesh_isVertexOffset)
                {
                    block.SetFloat(VertexSpeed_ID, 0);
                    block.SetFloat(VertexScale_ID, 0);
                }

                m_renderer.SetPropertyBlock(block);
            }
        }
    }

}


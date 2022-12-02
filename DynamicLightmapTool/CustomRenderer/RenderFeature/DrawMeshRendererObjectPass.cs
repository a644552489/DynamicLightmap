using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomRendererFeature
{
    public class DrawMeshRendererObjectPass : RenderObjectsPass
    {
        RenderQueueType renderQueueType;
        //FilteringSettings m_FilteringSettings;
        RenderObjects.CustomCameraSettings m_CameraSettings;
        string m_ProfilerTag;
        ProfilingSampler m_ProfilingSampler;

        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        List<string> m_PassNameList = new List<string>();
        List<int> m_renderPassIndexs = new List<int>();

        List<int> m_renderLayer = new List<int>();

        static ShaderTagId tagId = new ShaderTagId("LightMode");

        RenderStateBlock m_RenderStateBlock;

        public DrawMeshRendererObjectPass(string profilerTag, RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, RenderObjects.CustomCameraSettings cameraSettings) : base(profilerTag, renderPassEvent, shaderTags, renderQueueType, layerMask, cameraSettings)
        {
            base.profilingSampler = new ProfilingSampler(nameof(RenderObjectsPass));

            m_ProfilerTag = profilerTag;
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            this.renderPassEvent = renderPassEvent;
            this.renderQueueType = renderQueueType;
            this.overrideMaterial = null;
            this.overrideMaterialPassIndex = 0;
            //RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
            //    ? RenderQueueRange.transparent
            //    : RenderQueueRange.opaque;
            //m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);

            for (int i = 0; i < 32; i++)
            {
                if (((layerMask >> i) & 1) == 1)
                {
                    m_renderLayer.Add(i);
                }
            }


            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var passName in shaderTags)
                {
                    m_ShaderTagIdList.Add(new ShaderTagId(passName));
                    m_PassNameList.Add(passName);
                }
            }
            else
            {
                m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
                m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));

                m_PassNameList.Add("SRPDefaultUnlit");
                m_PassNameList.Add("UniversalForward");
                m_PassNameList.Add("UniversalForwardOnly");
                m_PassNameList.Add("LightweightForward");
            }

            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            m_CameraSettings = cameraSettings;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;

            // In case of camera stacking we need to take the viewport rect from base camera
            Rect pixelRect = renderingData.cameraData.camera.pixelRect;
            float cameraAspect = (float)pixelRect.width / (float)pixelRect.height;

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                if (m_CameraSettings.overrideCamera)
                {
                    Matrix4x4 projectionMatrix = Matrix4x4.Perspective(m_CameraSettings.cameraFieldOfView, cameraAspect,
                        camera.nearClipPlane, camera.farClipPlane);
                    projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, cameraData.IsCameraProjectionMatrixFlipped());

                    Matrix4x4 viewMatrix = cameraData.GetViewMatrix();
                    Vector4 cameraTranslation = viewMatrix.GetColumn(3);
                    viewMatrix.SetColumn(3, cameraTranslation + m_CameraSettings.offset);

                    RenderingUtils.SetViewAndProjectionMatrices(cmd, viewMatrix, projectionMatrix, false);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();


                var tagIdCount = m_ShaderTagIdList.Count;

                foreach (var layer in m_renderLayer)
                {
                    var drawMeshLayerMgr = CustomRenderer.DrawMeshRenderMgr.Inst.GetDrawMeshLayerMgr(renderQueueType, layer);

                    if (drawMeshLayerMgr == null) continue;

                    var linkList = drawMeshLayerMgr.renderQueueList;
                    var cur = linkList.First;
                    while (cur != null)
                    {
                        var value = cur.Value;
                        var showNodeCount = value.showNodeCount;
                        if (showNodeCount > 0)
                        {
                            var shader = value.shader;
                            var passCount = shader.passCount;

                            if (value.material.enableInstancing)
                            {
                                value.RefreshGpuInctancingData();
                                var batchCount = (showNodeCount / 1023) + 1;

                                for (int i = 0; i < tagIdCount; i++)
                                {
                                    var renderTag = m_ShaderTagIdList[i];
                                    var passName = m_PassNameList[i];
                                    if (value.material.GetShaderPassEnabled(passName))
                                    {
                                        for (int j = 0; j < passCount; j++)
                                        {

                                            var curTag = shader.FindPassTagValue(j, tagId);
                                            if (renderTag == curTag)
                                            {
                                                for (int k = 0; k < batchCount; k++)
                                                {
                                                    var meshCount = (k == batchCount - 1) ? (showNodeCount % 1023) : 1023;
                                                    cmd.DrawMeshInstanced(value.mesh, 0, value.material, j, value.matrix4X4sList[k], meshCount, value.propertyBlockList[k]);
                                                }

                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                m_renderPassIndexs.Clear();
                                for (int i = 0; i < tagIdCount; i++)
                                {
                                    var renderTag = m_ShaderTagIdList[i];
                                    var passName = m_PassNameList[i];
                                    if (value.material.GetShaderPassEnabled(passName))
                                    {
                                        for (int j = 0; j < passCount; j++)
                                        {
                                            var curTag = shader.FindPassTagValue(j, tagId);
                                            if (renderTag == curTag)
                                            {
                                                m_renderPassIndexs.Add(j);
                                                break;
                                            }
                                        }
                                    }
                                }

                                for (int k = 0; k < showNodeCount; k++)
                                {
                                    var count = m_renderPassIndexs.Count;
                                    for (int i = 0; i < count; i++)
                                    {
                                        var info = value.infos[k];
                                        cmd.DrawMesh(value.mesh, info.realMmatrix4X4, value.material, 0, m_renderPassIndexs[i], info.propertyBlock);
                                    }
                                }
                            }
                        }

                        cur = cur.Next;
                    }
                }
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();


                //context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings,
                //    ref m_RenderStateBlock);

                if (m_CameraSettings.overrideCamera && m_CameraSettings.restoreCamera)
                {
                    RenderingUtils.SetViewAndProjectionMatrices(cmd, cameraData.GetViewMatrix(), cameraData.GetGPUProjectionMatrix(), false);
                }
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}


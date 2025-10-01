// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace ResoDynamix.Runtime.Scripts.RenderPipeline
{
    /// <summary>
    ///     Pass that renders overlay camera rendering to intermediate texture
    /// </summary>
    /// <remarks>
    ///     This pass is executed when the rendering scale of the overlay camera's rendering result in ResoDynamixController is changed.
    /// </remarks>
    public class DrawOverlayCameraImageToResultTexturePass : ScriptableRenderPass
    {
        private static readonly int ScreenParams = Shader.PropertyToID("_ScreenParams");

        private Camera _camera;
        private ResoDynamixController _resoDynamixController;

        private readonly List<ShaderTagId> _shaderTagIds = new();

        public DrawOverlayCameraImageToResultTexturePass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            _shaderTagIds.Add(new ShaderTagId("SRPDefaultUnlit"));
            _shaderTagIds.Add(new ShaderTagId("UniversalForward"));
            _shaderTagIds.Add(new ShaderTagId("UniversalForwardOnly"));
        }

        public void Setup(ResoDynamixController resoDynamixController)
        {
            _resoDynamixController = resoDynamixController;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var resourceData = frameData.Get<UniversalResourceData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var lightData = frameData.Get<UniversalLightData>();
            var camera = cameraData.camera;
            camera.cullingMask = _resoDynamixController.GetOverlayCameraCullingMask(camera);
            
            if (!camera.TryGetCullingParameters(out var cullingParameters)) return;
            
            _resoDynamixController.ScriptableRenderContext.SetupCameraProperties(camera);
            var cullResults = _resoDynamixController.ScriptableRenderContext.Cull(ref cullingParameters);
            
            var opaqueRenderList = CreateOpaqueRendererList(renderGraph, cullResults, cameraData, renderingData,
                lightData);
            var transparentRenderList = CreateTransparentRendererList(renderGraph, cullResults, cameraData, renderingData,
                lightData);
            
            var resultRtTexture = renderGraph.ImportTexture(_resoDynamixController.ResultRTHandle);
            
            using (var builder =
                   renderGraph.AddRasterRenderPass<PassData>("DrawOverlayCameraImageToResultTexturePass",
                       out var passData))
            {
                builder.SetRenderAttachment(resultRtTexture, 0);
                if (_resoDynamixController.UseDepthTextureWithOverlayCamera)
                {
                    var depthTexture = renderGraph.ImportTexture(_resoDynamixController.ResultDepthRTHandle);
                    builder.SetRenderAttachmentDepth(depthTexture);
                }
                builder.AllowGlobalStateModification(true);
                builder.UseRendererList(opaqueRenderList);
                builder.UseRendererList(transparentRenderList);
                passData.OpaqueRendererList = opaqueRenderList;
                passData.TransparentRendererList = transparentRenderList;
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    var width = Screen.width * _resoDynamixController.ResultRenderScale;
                    var height = Screen.height * _resoDynamixController.ResultRenderScale;
                    cmd.SetGlobalVector(ScreenParams,
                        new Vector4(width, height, 1.0f + 1.0f / width, 1.0f + 1.0f / height));
                    cmd.DrawRendererList(data.OpaqueRendererList);
                    cmd.DrawRendererList(data.TransparentRendererList);
                });
            }

            // Since we're using an intermediate texture for result rendering, blit to the final rendering destination.
            renderGraph.AddBlitPass(resultRtTexture, resourceData.activeColorTexture, Vector2.one,
                Vector2.zero);
        }
        
        private RendererListHandle CreateTransparentRendererList(RenderGraph renderGraph, CullingResults cullResults, UniversalCameraData cameraData, UniversalRenderingData renderingData, UniversalLightData lightData)
        {
            var transparentFilteringSettings =
                new FilteringSettings(RenderQueueRange.transparent, LayerMask.GetMask("UI"));
            var transparentDrawSettings = CreateDrawingSettings(_shaderTagIds, renderingData, cameraData,
                lightData, SortingCriteria.CommonTransparent);
            return renderGraph.CreateRendererList(new RendererListParams(cullResults,
                transparentDrawSettings, transparentFilteringSettings));
        }
        
        private RendererListHandle CreateOpaqueRendererList(RenderGraph renderGraph, CullingResults cullResults, UniversalCameraData cameraData, UniversalRenderingData renderingData, UniversalLightData lightData)
        {
            var opaqueFilteringSettings = new FilteringSettings(RenderQueueRange.opaque, LayerMask.GetMask("UI"));
            var opaqueDrawSettings = CreateDrawingSettings(_shaderTagIds, renderingData, cameraData,
                lightData, SortingCriteria.CommonOpaque);
            return renderGraph.CreateRendererList(new RendererListParams(cullResults,
                opaqueDrawSettings, opaqueFilteringSettings));
        }

        private class PassData
        {
            public RendererListHandle OpaqueRendererList;
            public RendererListHandle TransparentRendererList;
        }
    }
}
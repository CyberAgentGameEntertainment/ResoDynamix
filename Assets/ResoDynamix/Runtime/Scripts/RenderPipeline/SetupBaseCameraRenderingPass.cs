// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace ResoDynamix.Runtime.Scripts.RenderPipeline
{
    /// <summary>
    ///     Pass that blits the result rendered by the base camera to ResoDynamix's result texture
    /// </summary>
    public class SetupBaseCameraRenderingPass : ScriptableRenderPass
    {
        private class PassData
        {
        }
        private TextureHandle _baseCameraColorTexture;
        private TextureHandle _baseCameraDepthTexture;
        private ResoDynamixController _resoDynamixController;
        private static readonly int ScaledScreenParams = Shader.PropertyToID("_ScaledScreenParams");
        public SetupBaseCameraRenderingPass(RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingOpaques)
        {
            this.renderPassEvent = renderPassEvent;
        }

        public void Setup(ResoDynamixController resoDynamixController)
        {
            _resoDynamixController = resoDynamixController;
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var contextItem = frameData.GetOrCreate<BaseCameraContextItem>();
            contextItem.BaseCameraColorTexture = resourceData.cameraColor;
            contextItem.BaseCameraDepthTexture = resourceData.cameraDepth;
            _baseCameraColorTexture = renderGraph.ImportTexture(_resoDynamixController.BaseCameraColorRTHandle);
            _baseCameraDepthTexture = renderGraph.ImportTexture(_resoDynamixController.BaseCameraDepthRTHandle);
            using var builder = renderGraph.AddRasterRenderPass<PassData>("SetupBaseCameraRenderingPass", out var _);
            builder.AllowPassCulling(false);
            builder.SetRenderAttachment(_baseCameraColorTexture, 0);
            builder.SetRenderAttachmentDepth(_baseCameraDepthTexture);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc((PassData _, RasterGraphContext context) =>
            {
                var cmd = context.cmd;
                cmd.ClearRenderTarget(RTClearFlags.All, Color.clear, 1.0f, 0);
                var width = Screen.width * _resoDynamixController.BaseCameraRenderScale;
                var height = Screen.height * _resoDynamixController.BaseCameraRenderScale;
                cmd.SetGlobalVector(ScaledScreenParams,
                    new Vector4(width, height, 1.0f + 1.0f / width, 1.0f + 1.0f / height));
            });
            resourceData.cameraColor = _baseCameraColorTexture;
            resourceData.cameraDepth = _baseCameraDepthTexture;
            
        }
    }
}

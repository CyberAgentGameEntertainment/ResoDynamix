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
    public class BlitBaseCameraImageToResultTexturePass : ScriptableRenderPass
    {
        private ResoDynamixController _resoDynamixController;
        public BlitBaseCameraImageToResultTexturePass()
        {
            renderPassEvent = RenderPassEvent.AfterRendering;
        }

        public void Setup(ResoDynamixController resoDynamixController)
        {
            _resoDynamixController = resoDynamixController;
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var contextItem= frameData.Get<BaseCameraContextItem>();
            if (_resoDynamixController.UseResultRTHandle)
            {
                // Preparing the texture for the result rendering destination
                var resultRtTexture = renderGraph.ImportTexture(_resoDynamixController.ResultRTHandle);
                renderGraph.AddBlitPass(resourceData.activeColorTexture, resultRtTexture,
                    Vector2.one, Vector2.zero);
            }
            else
            {
                // Otherwise, blit the base camera's rendering result to the original write destination
                renderGraph.AddBlitPass(resourceData.activeColorTexture, contextItem.BaseCameraColorTexture,
                    Vector2.one, Vector2.zero);
            }
            resourceData.cameraColor = contextItem.BaseCameraColorTexture;
            resourceData.cameraDepth = contextItem.BaseCameraDepthTexture;
            
        }
    }
}

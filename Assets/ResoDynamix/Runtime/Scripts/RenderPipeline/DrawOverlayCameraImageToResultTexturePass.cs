using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ResoDynamix.Runtime.Scripts.RenderPipeline
{
    /// <summary>
    ///     Executes overlay camera rendering to ResoDynamix's result texture
    /// </summary>
    public class DrawOverlayCameraImageToResultTexturePass : ScriptableRenderPass
    {
        private static readonly int ScreenParams = Shader.PropertyToID("_ScreenParams");
        private readonly ProfilingSampler _profilingSampler = new(nameof(DrawOverlayCameraImageToResultTexturePass));
        private Camera _camera;
        private RTHandle _depthHandle;
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

#if UNITY_2023_3_OR_NEWER
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                context.SetupCameraProperties(camera);
                cmd.SetRenderTarget(_resoDynamixController.ResultTexture);
                cmd.ClearRenderTarget(true, false, Color.black);
                var width = Screen.width * _resoDynamixController.ResultRenderScale;
                var height = Screen.height * _resoDynamixController.ResultRenderScale;
                cmd.SetGlobalVector(ScreenParams,
                    new Vector4(width, height, 1.0f + 1.0f / width, 1.0f + 1.0f / height));
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var drawingSettings = RenderingUtils.CreateDrawingSettings(_shaderTagIds, ref renderingData,
                    SortingCriteria.CommonTransparent);

                var opaqueFilteringSettings = new FilteringSettings(RenderQueueRange.opaque);
                var transparentFilteringSettings =
                    new FilteringSettings(RenderQueueRange.transparent);

                camera.cullingMask = _resoDynamixController.GetOverlayCameraCullingMask(camera);
                if (camera.TryGetCullingParameters(out var cullingParameters))
                {
                    var cullingResults = context.Cull(ref cullingParameters);
                    // Draw opaque objects
                    context.DrawRenderers(cullingResults, ref drawingSettings, ref opaqueFilteringSettings);
                    // Draw transparent objects
                    context.DrawRenderers(cullingResults, ref drawingSettings, ref transparentFilteringSettings);
                }
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
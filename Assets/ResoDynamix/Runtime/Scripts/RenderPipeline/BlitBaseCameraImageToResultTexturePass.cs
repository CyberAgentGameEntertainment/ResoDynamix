using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ResoDynamix.Runtime.Scripts.RenderPipeline
{
    /// <summary>
    ///     Pass that blits the result rendered by the base camera to ResoDynamix's result texture
    /// </summary>
    public class BlitBaseCameraImageToResultTexturePass : ScriptableRenderPass
    {
        private readonly ProfilingSampler _profilingSampler = new(nameof(BlitBaseCameraImageToResultTexturePass));
        private ResoDynamixController _resoDynamixController;
        public BlitBaseCameraImageToResultTexturePass()
        {
            renderPassEvent = RenderPassEvent.AfterRendering;
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
            // Execute if we need to blit to ResultTexture manually
            if (_resoDynamixController.NeedBlitToResultTextureManually)
            {
                var cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, _profilingSampler))
                {
#if UNITY_2023_3_OR_NEWER
                    Blit(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle,
                         _resoDynamixController.ResultTextureHandle);
#else
                    Blit(cmd, renderingData.cameraData.renderer.cameraColorTarget,
                         _resoDynamixController.ResultTexture);
#endif
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
    }
}

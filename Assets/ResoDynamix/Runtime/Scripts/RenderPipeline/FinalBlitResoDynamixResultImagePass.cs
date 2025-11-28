using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ResoDynamix.Runtime.Scripts.RenderPipeline
{
    /// <summary>
    ///     Blits ResoDynamix's result image to the frame buffer.
    /// </summary>
    public class FinalBlitResoDynamixResultImagePass : ScriptableRenderPass
    {
        private readonly ProfilingSampler _profilingSampler = new(nameof(FinalBlitResoDynamixResultImagePass));
        private ResoDynamixController _resoDynamixController;

        public FinalBlitResoDynamixResultImagePass()
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
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
#if UNITY_2023_3_OR_NEWER
                Blit(cmd, _resoDynamixController.ResultTextureHandle,
                    renderingData.cameraData.renderer.cameraColorTargetHandle);
#else
                Blit(cmd, _resoDynamixController.ResultTexture,
                    renderingData.cameraData.renderer.cameraColorTarget);
#endif
            }
            //
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}

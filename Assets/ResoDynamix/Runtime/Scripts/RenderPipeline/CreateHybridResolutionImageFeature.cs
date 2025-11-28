using UnityEngine.Rendering.Universal;

namespace ResoDynamix.Runtime.Scripts.RenderPipeline
{
    /// <summary>
    ///     Feature to create hybrid resolution images
    ///     The base camera is rendered at dynamic resolution from 0.0x to 1.0x, and overlay cameras are rendered at fixed resolution of 1.0x.
    ///     This feature is used to achieve dynamic resolution (base camera) and fixed resolution (overlay cameras).
    ///     The images created by this feature are blitted to the frame buffer by FinalBlitDynamicResolutionImageFeature.
    /// </summary>
    public class CreateHybridResolutionImageFeature : ScriptableRendererFeature
    {
        private BlitBaseCameraImageToResultTexturePass _blitBaseCameraImageToResultTexturePass;
        private DrawOverlayCameraImageToResultTexturePass _drawOverlayCameraImageToResultTexturePass;
        public override void Create()
        {
            _blitBaseCameraImageToResultTexturePass = new BlitBaseCameraImageToResultTexturePass();
            _drawOverlayCameraImageToResultTexturePass = new DrawOverlayCameraImageToResultTexturePass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var resoController = ResoDynamix.FindController(renderingData.cameraData.camera);
            if (resoController == null
                || !resoController.IsEnable) return;

            if (resoController.IsBaseCamera(renderingData.cameraData.camera))
            {
                _blitBaseCameraImageToResultTexturePass.Setup(resoController);
                renderer.EnqueuePass(_blitBaseCameraImageToResultTexturePass);
            }
            else if (resoController.IsOverlayCamera(renderingData.cameraData.camera))
            {
                _drawOverlayCameraImageToResultTexturePass.Setup(resoController);
                renderer.EnqueuePass(_drawOverlayCameraImageToResultTexturePass);
            }
        }
    }
}
// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ResoDynamix.Runtime.Scripts.RenderPipeline
{
    /// <summary>
    /// Feature for creating hybrid resolution images
    /// The base camera is rendered at 0.0x to 1.0x dynamic resolution, and the overlay camera is rendered at 1.0x fixed resolution.
    /// This feature is designed to achieve dynamic resolution (base camera) and fixed resolution (overlay camera).
    /// The images created by this feature's functionality are blitted to the frame buffer by FinalBlitDynamicResolutionImageFeature.
    /// </summary>
    public class CreateHybridResolutionImageFeature : ScriptableRendererFeature
    {
        [Header("Setup Base Camera Rendering Pass Settings")]
        [Tooltip("Sets the execution timing of SetupBaseCameraRenderingPass")]
        [SerializeField] private RenderPassEvent setupBaseCameraRenderingPassEvent = RenderPassEvent.BeforeRenderingOpaques;

        private SetupBaseCameraRenderingPass _setupBaseCameraRenderingPass;
        private BlitBaseCameraImageToResultTexturePass _blitBaseCameraImageToResultTexturePass;
        private DrawOverlayCameraImageToResultTexturePass _drawOverlayCameraImageToResultTexturePass;

        public override void Create()
        {
            _setupBaseCameraRenderingPass = new (setupBaseCameraRenderingPassEvent);
            _blitBaseCameraImageToResultTexturePass = new ();
            _drawOverlayCameraImageToResultTexturePass = new ();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var resoController = ResoDynamix.FindController(renderingData.cameraData.camera);
            if (resoController == null
                || !resoController.IsEnable) return;

            if (resoController.IsBaseCamera(renderingData.cameraData.camera))
            {
                _setupBaseCameraRenderingPass.Setup(resoController);
                _blitBaseCameraImageToResultTexturePass.Setup(resoController);
                renderer.EnqueuePass(_setupBaseCameraRenderingPass);
                renderer.EnqueuePass(_blitBaseCameraImageToResultTexturePass);
            }else if (resoController.IsOverlayCamera(renderingData.cameraData.camera)
                      && resoController.UseResultRTHandle)
            {
                // When rendering overlay camera objects to intermediate texture
                _drawOverlayCameraImageToResultTexturePass.Setup(resoController);
                renderer.EnqueuePass(_drawOverlayCameraImageToResultTexturePass);
            }
        }
    }
}
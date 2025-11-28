using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ResoDynamix.Runtime.Scripts.RenderPipeline
{
    /// <summary>
    /// Feature that final blits the dynamic resolution image created by CreateHybridResolutionImageFeature to the frame buffer.
    /// </summary>
    public class FinalBlitResoDynamixResultImageFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering;
        [SerializeField]
        private int renderPassEventOffset;  // For controlling rendering order when events overlap

        private FinalBlitResoDynamixResultImagePass _pass;

        public override void Create()
        {
            _pass = new FinalBlitResoDynamixResultImagePass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var resoController = ResoDynamix.FindController(renderingData.cameraData.camera);
            if (resoController == null
                || !resoController.IsEnable) return;

            _pass.Setup(resoController);
            _pass.renderPassEvent = renderPassEvent + renderPassEventOffset;
            renderer.EnqueuePass(_pass);
        }
    }
}

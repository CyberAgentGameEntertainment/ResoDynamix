using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace ResoDynamix.Runtime.Scripts
{
    public class ResoDynamixController : MonoBehaviour
    {
        [FormerlySerializedAs("renderScale")] [SerializeField] [Range(0.1f, 1.0f)] private float baseCameraRenderScale = 1.0f;
        [SerializeField] [Range(0.1f, 1.0f)] private float resultRenderScale = 1.0f;
        [SerializeField] private Camera _baseCamera;
        [SerializeField] private Camera _finalBlitCamera;
        [FormerlySerializedAs("useDepthTextureWithOverrayCamera")] [SerializeField] private bool useDepthTextureWithOverlayCamera;
        [SerializeField] private bool setRenderTextureToBaseCameraOutput;   // Whether to set ResultTexture as the base camera output destination
        private const int maxOverlayCameraCount = 32; // Maximum number of overlay cameras.
        private List<Camera> _overlayCameras = new(maxOverlayCameraCount);
        private List<LayerMask> _overlayCameraCullingMasks = new(maxOverlayCameraCount);
        /// <summary>
        ///     URPアセットのRender Scale
        /// </summary>
        internal float UrpRenderScale { get; private set; }
        /// <summary>
        ///     RenderScale
        /// </summary>
        [Obsolete("Use BaseCameraRenderScale instead.")]
        public float RenderScale
        {
            get => baseCameraRenderScale;
            set => baseCameraRenderScale = value;
        }
        /// <summary>
        ///     BaseCameraのRenderScale
        /// </summary>
        public float BaseCameraRenderScale
        {
            get => baseCameraRenderScale;
            set => baseCameraRenderScale = value;
        }
        /// <summary>
        ///     Rendering scale of the final rendering result
        /// </summary>
        public float ResultRenderScale
        {
            get => resultRenderScale;
            set => resultRenderScale = value;
        }
        /// <summary>
        /// Determine if dynamic resolution control is enabled
        /// </summary>
        public bool IsEnable
        {
            get
            {
                return isActiveAndEnabled // Whether the controller is enabled
                       && BaseCamera != null // Base camera is not null
                       && BaseCamera.isActiveAndEnabled
                       && ( BaseCameraRenderScale < 1f || ResultRenderScale < 1f); // Either camera's rendering scale is less than 1.0
            }
        }

        public Camera BaseCamera
        {
            get => _baseCamera;
            set => _baseCamera = value;
        }

        internal RenderTexture ResultTexture { get; set; }
#if UNITY_2023_3_OR_NEWER
        internal RTHandle ResultTextureHandle { get; set; }
#endif
        public Camera FinalBlitCamera => _finalBlitCamera;

        public bool UsingCameraStack => BaseCamera?.GetUniversalAdditionalCameraData()?.cameraStack?.Count > 0;
        public bool NeedBlitToResultTextureManually => !setRenderTextureToBaseCameraOutput || UsingCameraStack;

#if UNITY_2023_3_OR_NEWER
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
#endif
        static GraphicsFormat MakeRenderTextureGraphicsFormat(bool isHdrEnabled, bool needsAlpha)
        {
            if (isHdrEnabled)
            {
                if (!needsAlpha && RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.B10G11R11_UFloatPack32,
                        FormatUsage.Linear | FormatUsage.Render))
                    return GraphicsFormat.B10G11R11_UFloatPack32;
                if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R16G16B16A16_SFloat,
                        FormatUsage.Linear | FormatUsage.Render))
                    return GraphicsFormat.R16G16B16A16_SFloat;
                return
                    SystemInfo.GetGraphicsFormat(DefaultFormat
                        .HDR); // This might actually be a LDR format on old devices.
            }

            return SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
        }
        /// <summary>
        /// Determine if the specified camera is a base camera
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        internal bool IsBaseCamera(Camera camera) => BaseCamera == camera;
        /// <summary>
        /// Determine if the specified camera is an overlay camera
        /// </summary>
        internal bool IsOverlayCamera(Camera camera)
        {
            return _overlayCameras.Contains(camera);
        }

        /// <summary>
        ///     Get the culling mask of the specified overlay camera.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        internal LayerMask GetOverlayCameraCullingMask(Camera camera)
        {
            for (int i = 0; i < _overlayCameraCullingMasks.Count; i++)
            {
                if (_overlayCameras[i] == camera)
                {
                    return _overlayCameraCullingMasks[i];
                }
            }

            return 0;
        }

        /// <summary>
        /// Determine if overlay cameras are empty
        /// </summary>
        /// <returns></returns>
        internal bool IsOverlayCamerasEmpty()
        {
            return _overlayCameras.Count == 0;
        }

        private void ReleaseTemporaryTextures()
        {
            if (ResultTexture != null)
            {
                RenderTexture.ReleaseTemporary(ResultTexture);
                ResultTexture = null;
#if UNITY_2023_3_OR_NEWER
                ResultTextureHandle?.Release();
                ResultTextureHandle = null;
#endif
            }

            for (int i = 0; i < _overlayCameras.Count; i++)
            {
                if (_overlayCameras[i] && _overlayCameras[i] != null)
                {
                    _overlayCameras[i].targetTexture = null;
                }
            }

            if (_finalBlitCamera)
            {
                _finalBlitCamera.targetTexture = null;
            }
        }
        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            Disable();
        }
        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (BaseCamera == camera)
            {
                // Rendering of the camera controlled by this controller begins
                var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                // Store the URP asset's rendering scale
                UrpRenderScale = urpAsset.renderScale;
                // Override with the rendering scale specified in ResoDynamixController.
                urpAsset.renderScale = BaseCameraRenderScale;
            }
        }
        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (UrpRenderScale != 0.0f
                && _overlayCameras.Count > 0
                && camera == _overlayCameras[^1])
            {
                // Restore the render scale.
                var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                urpAsset.renderScale = UrpRenderScale;
            }
        }
        private void Disable()
        {
            ReleaseTemporaryTextures();
            if (UrpRenderScale != 0.0f)
            {
                // Restore URP's render scale
                var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                urpAsset.renderScale = UrpRenderScale;
            }
            if(_finalBlitCamera && _finalBlitCamera != null )
                _finalBlitCamera.enabled = false;

            // Restore overlay camera culling masks
            for (int i = 0; i < _overlayCameras.Count; ++i)
            {
                if (_overlayCameras[i])
                {
                    _overlayCameras[i].cullingMask = _overlayCameraCullingMasks[i];
                }
            }
            _overlayCameras.Clear();
            _overlayCameraCullingMasks.Clear();
        }

        void Update()
        {
            // Disable once
            Disable();

            if (_baseCamera == null || _overlayCameras == null) return;

            UniversalAdditionalCameraData additionalCameraData = _baseCamera.GetUniversalAdditionalCameraData();
            if (additionalCameraData.renderType == CameraRenderType.Overlay )// Base camera is set as overlay camera.
            {
                // Dynamic resolution control is not needed
                return;
            }

            if (resultRenderScale >= 1f && BaseCameraRenderScale >= 1f)
            {
                // Dynamic resolution control is not needed
                return;
            }
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            // Store the URP asset's rendering scale
            UrpRenderScale = urpAsset.renderScale;
            
            // Create a texture to write the rendering result to.
            float resultScale = resultRenderScale;
            var resultTextrueDesc = new RenderTextureDescriptor(
                Mathf.FloorToInt(Screen.width * resultScale),
                Mathf.FloorToInt(Screen.height * resultScale));

            var isHdrEnabled = urpAsset.supportsHDR && _baseCamera.allowHDR; // Whether HDR is enabled is determined by both settings
            resultTextrueDesc.graphicsFormat = MakeRenderTextureGraphicsFormat(isHdrEnabled, false);
            resultTextrueDesc.depthBufferBits = useDepthTextureWithOverlayCamera ? 32 : 0;
            resultTextrueDesc.msaaSamples = urpAsset.msaaSampleCount;
            resultTextrueDesc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            resultTextrueDesc.useMipMap = false;
            ResultTexture = RenderTexture.GetTemporary(resultTextrueDesc);
#if UNITY_2023_3_OR_NEWER
            ResultTextureHandle = RTHandles.Alloc(ResultTexture);
#endif
            // Add overlay cameras to the list
            int cameraStackCount = additionalCameraData.cameraStack.Count;
            for (int i = 0; i < cameraStackCount; i++)
            {
                Camera stackedCamera = additionalCameraData.cameraStack[i];
                if (!stackedCamera.isActiveAndEnabled) continue;
                stackedCamera.targetTexture = BaseCamera.targetTexture;
                _overlayCameras.Add(stackedCamera);
                _overlayCameraCullingMasks.Add(stackedCamera.cullingMask);
                stackedCamera.cullingMask = 0;
            }

            // Final Blit camera inherits the base camera's state
            _finalBlitCamera.enabled = _baseCamera.enabled;
            _finalBlitCamera.depth = _baseCamera.depth + 1;
            _finalBlitCamera.targetTexture = BaseCamera.targetTexture; 
            // Set the base camera's RenderTarget to ResultTexture to avoid unnecessary Blit operations
            if (!NeedBlitToResultTextureManually)
            {
                BaseCamera.targetTexture = ResultTexture;
            }
        }
    }
}

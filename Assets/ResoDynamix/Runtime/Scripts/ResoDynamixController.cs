// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

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
        private const int maxOverlayCameraCount = 32; // Maximum number of overlay cameras.

        [FormerlySerializedAs("renderScale")] [SerializeField] [Range(0.1f, 1.0f)]
        private float baseCameraRenderScale = 1.0f;

        [SerializeField] [Range(0.1f, 1.0f)] private float resultRenderScale = 1.0f;
        [SerializeField] private Camera _baseCamera;

        [FormerlySerializedAs("useDepthTextureWithOverrayCamera")] [SerializeField]
        private bool useDepthTextureWithOverlayCamera;
        
        private RenderTexture _baseCameraColorTexture;
        private RenderTexture _baseCameraDepthTexture;
        private readonly List<LayerMask> _overlayCameraCullingMasks = new(maxOverlayCameraCount);
        private readonly List<Camera> _overlayCameras = new(maxOverlayCameraCount);

        private RenderTexture _resultTexture;
        private RenderTexture _resultDepthTexture;
        public ScriptableRenderContext ScriptableRenderContext { get; private set; }
        public bool UseDepthTextureWithOverlayCamera => useDepthTextureWithOverlayCamera;
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
        ///     Determines whether dynamic resolution control is enabled
        /// </summary>
        public bool IsEnable =>
            isActiveAndEnabled // Whether the controller itself is enabled
            && BaseCamera != null // Base camera is not null
            && BaseCamera.isActiveAndEnabled
            && (BaseCameraRenderScale < 1f || ResultRenderScale < 1f); // Either camera's rendering scale is less than 1.0
        public bool UseResultRTHandle => ResultRenderScale < 1f;
        public Camera BaseCamera
        {
            get => _baseCamera;
            set => _baseCamera = value;
        }
        internal RTHandle BaseCameraColorRTHandle { get; private set; }
        internal RTHandle BaseCameraDepthRTHandle { get; private set; }
        internal RTHandle ResultRTHandle { get; private set; }
        internal RTHandle ResultDepthRTHandle { get; private set; }
        public bool UsingCameraStack => BaseCamera?.GetUniversalAdditionalCameraData()?.cameraStack?.Count > 0;
        
        private void Update()
        {
            // Disable temporarily.
            Disable();

            if (_baseCamera == null || _overlayCameras == null) return;

            var additionalCameraData = _baseCamera.GetUniversalAdditionalCameraData();
            if (additionalCameraData.renderType == CameraRenderType.Overlay) // Base camera has become an overlay camera.
                // Dynamic resolution control is not needed
                return;

            if (resultRenderScale >= 1f && BaseCameraRenderScale >= 1f)
                // Dynamic resolution control is not needed
                return;

            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var isHdrEnabled = urpAsset.supportsHDR && _baseCamera.allowHDR; // Whether HDR is enabled is determined by both settings

            CreateBaseCameraRTHandles(urpAsset, isHdrEnabled);
            CreateResultRTHandles(urpAsset, isHdrEnabled);

            // Add overlay cameras to the list
            var cameraStackCount = additionalCameraData.cameraStack.Count;
            for (var i = 0; i < cameraStackCount; i++)
            {
                var stackedCamera = additionalCameraData.cameraStack[i];
                if (!stackedCamera.isActiveAndEnabled) continue;
                stackedCamera.targetTexture = BaseCamera.targetTexture;
                _overlayCameras.Add(stackedCamera);
                _overlayCameraCullingMasks.Add(stackedCamera.cullingMask);
                
                stackedCamera.cullingMask = UseResultRTHandle ? 0 : stackedCamera.cullingMask;
            }
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            Disable();
        }

        private static GraphicsFormat MakeRenderTextureGraphicsFormat(bool isHdrEnabled, bool needsAlpha)
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
        ///     Determines whether the specified camera is a base camera
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        internal bool IsBaseCamera(Camera camera)
        {
            return BaseCamera == camera;
        }

        /// <summary>
        ///     Determines whether the specified camera is an overlay camera
        /// </summary>
        internal bool IsOverlayCamera(Camera camera)
        {
            return _overlayCameras.Contains(camera);
        }

        /// <summary>
        ///     Gets the culling mask of the specified overlay camera.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        internal LayerMask GetOverlayCameraCullingMask(Camera camera)
        {
            for (var i = 0; i < _overlayCameraCullingMasks.Count; i++)
                if (_overlayCameras[i] == camera)
                    return _overlayCameraCullingMasks[i];

            return 0;
        }

        /// <summary>
        ///     Determines whether overlay cameras are empty
        /// </summary>
        /// <returns></returns>
        internal bool IsOverlayCamerasEmpty()
        {
            return _overlayCameras.Count == 0;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            ScriptableRenderContext = context;
        }

        private void ReleaseTemporaryTextures()
        {
            if (_baseCameraColorTexture != null)
            {
                RenderTexture.ReleaseTemporary(_baseCameraColorTexture);
                _baseCameraColorTexture = null;
            }

            if (_baseCameraDepthTexture != null)
            {
                RenderTexture.ReleaseTemporary(_baseCameraDepthTexture);
                _baseCameraDepthTexture = null;
            }

            if (_resultTexture != null)
            {
                RenderTexture.ReleaseTemporary(_resultTexture);
                _resultTexture = null;
            }
            if(_resultDepthTexture != null)
            {
                RenderTexture.ReleaseTemporary(_resultDepthTexture);
                _resultDepthTexture = null;
            }
            ResultRTHandle?.Release();
            ResultDepthRTHandle?.Release();
            BaseCameraColorRTHandle?.Release();
            BaseCameraDepthRTHandle?.Release();
            for (var i = 0; i < _overlayCameras.Count; i++)
                if (_overlayCameras[i] && _overlayCameras[i] != null)
                    _overlayCameras[i].targetTexture = null;
        }

        private void Disable()
        {
            ReleaseTemporaryTextures();

            // Restore overlay camera culling masks
            for (var i = 0; i < _overlayCameras.Count; ++i)
                if (_overlayCameras[i])
                    _overlayCameras[i].cullingMask = _overlayCameraCullingMasks[i];
            _overlayCameras.Clear();
            _overlayCameraCullingMasks.Clear();
        }

        private void CreateBaseCameraRTHandles(UniversalRenderPipelineAsset urpAsset, bool isHdrEnabled)
        {
            var baseCameraColorDesc = new RenderTextureDescriptor(
                Mathf.FloorToInt(Screen.width * baseCameraRenderScale),
                Mathf.FloorToInt(Screen.height * baseCameraRenderScale));
            baseCameraColorDesc.graphicsFormat = MakeRenderTextureGraphicsFormat(isHdrEnabled, false);
            baseCameraColorDesc.depthBufferBits = 0; //useDepthTextureWithOverlayCamera ? 32 : 0;
            baseCameraColorDesc.msaaSamples = urpAsset.msaaSampleCount;
            baseCameraColorDesc.sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear;
            baseCameraColorDesc.useMipMap = false;
            _baseCameraColorTexture = RenderTexture.GetTemporary(baseCameraColorDesc);

            var baseCameraDepthDesc = baseCameraColorDesc;
            baseCameraDepthDesc.graphicsFormat = GraphicsFormat.None;
            
        #if !UNITY_EDITOR && UNITY_ANDROID
            baseCameraDepthDesc.depthBufferBits = 24;
        #else
            baseCameraDepthDesc.depthBufferBits = 32;
        #endif
            _baseCameraDepthTexture = RenderTexture.GetTemporary(baseCameraDepthDesc);
            BaseCameraColorRTHandle = RTHandles.Alloc(_baseCameraColorTexture);
            BaseCameraDepthRTHandle = RTHandles.Alloc(_baseCameraDepthTexture);
        }

        private void CreateResultRTHandles(UniversalRenderPipelineAsset urpAsset, bool isHdrEnabled)
        {
            // If the result rendering scale is 1.0 or higher, write directly to the final rendering destination, so do nothing
            if(resultRenderScale >= 1f) return;
            
            // If the result rendering scale is less than 1.0, create an intermediate texture
            var resultScale = resultRenderScale;
            var colorTextrueDesc = new RenderTextureDescriptor(
                Mathf.FloorToInt(Screen.width * resultScale),
                Mathf.FloorToInt(Screen.height * resultScale));
            
            colorTextrueDesc.graphicsFormat = MakeRenderTextureGraphicsFormat(isHdrEnabled, false);
            colorTextrueDesc.depthBufferBits = 0;
            colorTextrueDesc.msaaSamples = urpAsset.msaaSampleCount;
            colorTextrueDesc.sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear;
            colorTextrueDesc.useMipMap = false;
            _resultTexture = RenderTexture.GetTemporary(colorTextrueDesc);
            ResultRTHandle = RTHandles.Alloc(_resultTexture);

            if (useDepthTextureWithOverlayCamera)
            {
                var depthTextureDesc = colorTextrueDesc;
                depthTextureDesc.graphicsFormat = GraphicsFormat.None;
            #if !UNITY_EDITOR && UNITY_ANDROID
                depthTextureDesc.depthBufferBits = 24;
            #else
                depthTextureDesc.depthBufferBits = 32;
            #endif
                _resultDepthTexture = RenderTexture.GetTemporary(depthTextureDesc);
                ResultDepthRTHandle = RTHandles.Alloc(_resultDepthTexture);
            }
        }
    }
}
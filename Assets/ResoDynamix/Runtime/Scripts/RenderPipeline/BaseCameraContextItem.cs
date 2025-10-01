// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ResoDynamix.Runtime.Scripts.RenderPipeline
{
    public class BaseCameraContextItem : ContextItem
    {
        public TextureHandle BaseCameraColorTexture;
        public TextureHandle BaseCameraDepthTexture;
        public override void Reset()
        {
            BaseCameraColorTexture = TextureHandle.nullHandle;
            BaseCameraDepthTexture = TextureHandle.nullHandle;
        }
    }
}
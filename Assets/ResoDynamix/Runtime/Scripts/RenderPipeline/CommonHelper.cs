using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace ResoDynamix.Runtime.Scripts.RenderPipeline
{
    public static class CommonHelper
    {
        private class PassData
        {
            public TextureHandle Source;
            public Vector4 ScaleBias;
            public int MipLevel;
            public bool Bilinear;
        }

        public static IRasterRenderGraphBuilder AddBlitPassCustom(this RenderGraph renderGraph, 
            in TextureHandle source, in TextureHandle destination, in Vector2 scale, in Vector2 bias, 
            in int mipLevel = 0, in bool bilinear = true, in bool returnBuilder = false,
            in string passName = "CustomBlitPass")
        {
            var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData);
            passData.Source = source;
            passData.ScaleBias.Set(scale.x, scale.y, bias.x, bias.y);
            passData.MipLevel = mipLevel;
            passData.Bilinear = bilinear;
            builder.SetRenderAttachment(destination, 0, AccessFlags.WriteAll);
            builder.UseTexture(passData.Source);
            builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) =>
            {
                Blitter.BlitTexture(ctx.cmd, data.Source, data.ScaleBias, data.MipLevel, data.Bilinear);
            });
            
            if (returnBuilder)
            {
                return builder;
            }
            
            builder.Dispose();
            return null;
        }
    }
}

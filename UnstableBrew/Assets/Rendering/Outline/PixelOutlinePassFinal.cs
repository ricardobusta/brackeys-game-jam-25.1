using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Rendering.Outline
{
    public class PixelOutlinePassFinal : ScriptableRenderPass
    {
        private class PassData
        {
            internal TextureHandle BlitTextureHandle;
            internal Material Material;
        }

        private static readonly int BlitTexture = Shader.PropertyToID("_BlitTexture");
        private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int FillColor = Shader.PropertyToID("_FillColor");
        private static readonly int OutlineThickness = Shader.PropertyToID("_OutlineThickness");

        private readonly Material blitMaterial;

        public PixelOutlinePassFinal(PixelOutlineRenderFeature.Settings settings, PixelOutlineRenderFeature.OutlineSettings outlineSettings)
        {
            renderPassEvent = settings.RenderPassEvent;

            if (settings.BlitShader == null)
            {
                return;
            }

            blitMaterial = new Material(settings.BlitShader);
            blitMaterial.SetColor(OutlineColor, outlineSettings.OutlineColor);
            blitMaterial.SetColor(FillColor, outlineSettings.FillColor);
            blitMaterial.SetFloat(OutlineThickness, outlineSettings.OutlineThickness);
        }

        private static void ExecutePass(PassData passData, RasterGraphContext context)
        {
            if (passData.Material != null)
            {
                passData.Material.SetTexture(BlitTexture, passData.BlitTextureHandle);
            }

            Blitter.BlitTexture(context.cmd, passData.BlitTextureHandle, new Vector4(1, 1, 0, 0), passData.Material, 0);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var outlineData = frameData.Get<PixelOutlineRenderFeature.OutlineData>();

            using var builder = renderGraph.AddRasterRenderPass<PassData>("PixelOutlinePass_Final", out var passData, new ProfilingSampler("PixelOutlinePass_Final"));

            if (!outlineData.FilterTextureHandle.IsValid())
                return;

            if (blitMaterial == null)
                return;

            passData.Material = blitMaterial;
            passData.BlitTextureHandle = outlineData.FilterTextureHandle;

            builder.AllowPassCulling(false);
            builder.UseTexture(passData.BlitTextureHandle);
            builder.SetRenderAttachment(resourceData.cameraColor, index: 0);
            builder.SetRenderFunc<PassData>(ExecutePass);
        }
    }
}
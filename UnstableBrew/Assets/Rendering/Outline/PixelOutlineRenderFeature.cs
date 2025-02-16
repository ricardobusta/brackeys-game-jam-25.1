using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Rendering.Outline
{
    public class PixelOutlineRenderFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public RenderPassEvent RenderPassEvent = RenderPassEvent.AfterRenderingTransparents;

            public LayerMask LayerMask = 0;

            public RenderingLayerMask RenderingLayerMask = 0;

            public Shader BlitShader;

            public bool ClearDepth;
        }

        [Serializable]
        public class OutlineSettings
        {
            public Color OutlineColor = Color.white;
            public Color FillColor = Color.white;
            public float OutlineThickness = 1.0f;
        }

        public class OutlineData : ContextItem
        {
            public TextureHandle FilterTextureHandle;

            public override void Reset()
            {
                FilterTextureHandle = TextureHandle.nullHandle;
            }
        }

        public Settings FeatureSettings;
        public OutlineSettings MaterialSettings;

        private PixelOutlinePassFilter _pixelOutlinePassFilter;
        private PixelOutlinePassFinal _pixelOutlinePassFinal;

        public override void Create()
        {
            _pixelOutlinePassFilter = new PixelOutlinePassFilter(FeatureSettings);
            _pixelOutlinePassFinal = new PixelOutlinePassFinal(FeatureSettings, MaterialSettings);
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_pixelOutlinePassFilter);
            renderer.EnqueuePass(_pixelOutlinePassFinal);
        }
    }
}
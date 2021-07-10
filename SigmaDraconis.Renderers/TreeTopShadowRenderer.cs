namespace SigmaDraconis.Renderers
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.Shared;
    using Shadows;
    using WorldControllers;
    using WorldInterfaces;

    public class TreeTopShadowRenderer : ShadowMeshRenderer
    {
        public TreeTopShadowRenderer() : base(ShadowMeshType.TreeTop, 10000, 30000, 15000)
        {
        }

        public override void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.mainTexture = contentManager.Load<Texture2D>("Textures\\Shadows\\TreeTopShadow");

            this.effect = contentManager.Load<Effect>("Effects\\TreeTopShadowEffect");
            this.effect.Parameters["xTexture"].SetValue(this.mainTexture);
            this.effect.Parameters["xWarpPhase"].SetValue(DateTime.UtcNow.Millisecond / 159f);
            this.effect.Parameters["xWarpAmplitude"].SetValue(WeatherController.TreetopSwayAmplitude * 0.016f);

            base.LoadContent(graphicsDevice, contentManager);
        }

        public override void Update(Vector2f scrollPos, float zoom, Vector3 sunVector, int shadowDetail, bool isPaused = false)
        {
            if (!isPaused && shadowDetail > 0)
            {
                // Move in the wind
                this.effect.Parameters["xWarpPhase"].SetValue(DateTime.UtcNow.Millisecond / 159f);
                this.effect.Parameters["xWarpAmplitude"].SetValue(WeatherController.TreetopSwayAmplitude * 0.016f);
                WorldRenderer.Instance.InvalidateShadows();
            }

            base.Update(scrollPos, zoom, sunVector, shadowDetail, isPaused);
        }

        protected override Color GetVertexColour(IThing thing, float alpha)
        {
            if (!(thing is ITree tree)) return base.GetVertexColour(thing, alpha);
            return new Color(1f, tree.TreeTopWarpPhase, 1f, alpha);
        }
    }
}

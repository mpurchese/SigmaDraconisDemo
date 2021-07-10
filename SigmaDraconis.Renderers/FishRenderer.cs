namespace SigmaDraconis.Renderers
{
    using System;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Config;
    using Shared;
    using World;
    using WorldInterfaces;

    public class FishRenderer : SinglePassAnimalRendererBase, IRenderer, IDisposable
    {
        public FishRenderer() : base("Fish")
        {
        }

        public override void UpdateBuffers()
        {
            if (thingTypes == null) thingTypes = Enum.GetValues(typeof(ThingType)).Cast<ThingType>().Where(t => ThingTypeManager.IsRendererType(t, RendererType.Fish)).ToArray();

            var things = World.GetThings(thingTypes).ToList();
            var size = things.Count * 4;
            if (size == 0)
            {
                this.usedBufferSize = 0;
                return;
            }

            VertexPositionColorTexture[] vertexArray = new VertexPositionColorTexture[size];
            int i = 0;

            foreach (IWaterAnimal thing in things)
            {
                var textureName = thing.GetTextureName();
                var frame = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.WaterAnimals, thing.ThingType, (thing as IAnimatedThing)?.AnimationFrame ?? 1);
                var scale = 0.25f;

                var tile = thing.MainTile;
                var x = thing.RenderPos.X - (0.25f * frame.CX);
                var y = thing.RenderPos.Y - (0.25f * frame.CY);

                float width = frame.Width * scale;
                float height = frame.Height * scale;

                float tx = frame.X / this.textureSize.X;
                float ty = frame.Y / this.textureSize.Y;
                float tw = frame.Width / this.textureSize.X;
                float th = frame.Height / this.textureSize.Y;

                if (WorldRenderer.Instance.TextureRes == 1)
                {
                    tx *= 2;
                    ty *= 2;
                    tw *= 2;
                    th *= 2;
                }

                var c = GetVertexColor(thing);
                vertexArray[i] = new VertexPositionColorTexture(new Vector3(x, y, 0), c, new Vector2(tx, ty));
                vertexArray[i + 1] = new VertexPositionColorTexture(new Vector3(x + width, y, 0), c, new Vector2(tx + tw, ty));
                vertexArray[i + 2] = new VertexPositionColorTexture(new Vector3(x, y + height, 0), c, new Vector2(tx, ty + th));
                vertexArray[i + 3] = new VertexPositionColorTexture(new Vector3(x + width, y + height, 0), c, new Vector2(tx + tw, ty + th));

                i += 4;
            }

            this.UpdateIndexBuffer(vertexArray, size);
        }

        public override void Draw()
        {
            if (this.zoom < 2) return;   // Don't draw when zoomed  all the way out, can hardly see them anyway

            if (vertexBuffer == null || vertexBuffer.IsContentLost || vertexBuffer.IsDisposed || vertexBuffer.VertexCount == 0 || usedBufferSize == 0)
            {
                return;
            }

            if (indexBuffer == null || indexBuffer.IsContentLost || indexBuffer.IsDisposed)
            {
                return;
            }

            this.effect.CurrentTechnique = this.effect.Techniques["MainTechnique"];

            var light = World.WorldLight;
            var darkFactor = 0.5f * (1f - (light.LightFactorT + light.LightFactorS + light.LightFactorN + light.LightFactorE + light.LightFactorW));
            this.effect.Parameters["xLightingDirectionFactors"].SetValue(new Vector4(light.LightFactorW + darkFactor, light.LightFactorT + light.LightFactorS, light.LightFactorE + darkFactor, 1));
            this.effect.Parameters["xLightingFactorBlueness"].SetValue(light.LightFactorBlueness);
            this.effect.Parameters["xAlpha"].SetValue(1f);

            this.graphics.SetVertexBuffer(vertexBuffer);
            this.graphics.Indices = indexBuffer;
            var prevBlendState = this.graphics.BlendState;
            this.graphics.BlendState = BlendState.AlphaBlend;
            this.effect.CurrentTechnique.Passes[0].Apply();
            this.graphics.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, this.usedBufferSize / 2);
            this.graphics.BlendState = prevBlendState;
        }

        private static Color GetVertexColor(IThing thing)
        {
            return new Color(0f, 0f, 1f, thing.RenderAlpha);
        }
    }
}

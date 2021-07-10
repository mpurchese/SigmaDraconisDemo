namespace SigmaDraconis.Renderers
{
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Config;
    using Shared;
    using World;
    using WorldInterfaces;

    public class FlyingInsectsRenderer : RendererBase, IRenderer
    {
        public override void LoadContent()
        {
            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Animals\\LoRes\\FlyingInsects_1" : "Animals\\FlyingInsects_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Animals\\LoRes\\FlyingInsects_2" : "Animals\\FlyingInsects_2";

            base.LoadContent();
            this.LoadGeneralEffectNew(WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath1), WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath2));
        }

        public override void ReloadContent()
        {
            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Animals\\LoRes\\FlyingInsects_1" : "Animals\\FlyingInsects_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Animals\\LoRes\\FlyingInsects_2" : "Animals\\FlyingInsects_2";

            this.texture1 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath1);
            this.texture2 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath2);
            this.textureSize = new Vector2(texture1.Width, texture1.Height);
            this.effect.Parameters["xTexture1"].SetValue(texture1);
            this.effect.Parameters["xTexture2"].SetValue(texture2);

            base.ReloadContent();
        }

        public override void Update(WorldTime time, Vector2f scrollPos, float zoom, bool isPaused = false)
        {
            foreach (var id in EventManager.MovedFlyingInsects)
            {
                if (World.GetThing(id) is IFlyingInsect insect && insect.RenderRow.HasValue)
                {
                    this.InvalidateRow(insect.RenderRow.Value);
                    if (insect.PrevRenderRow.HasValue && insect.PrevRenderRow != insect.RenderRow)
                    {
                        this.InvalidateRow(insect.PrevRenderRow.Value);
                        insect.PrevRenderRow = insect.RenderRow;
                    }
                }
            }

            var light = World.WorldLight;
            this.effect.Parameters["xLightingDirectionFactors"].SetValue(new Vector4(light.LightFactorW, light.LightFactorT + light.LightFactorS, light.LightFactorE, 1));
            this.effect.Parameters["xLightingFactorBlueness"].SetValue(light.LightFactorBlueness);
            this.effect.Parameters["xAlpha"].SetValue(1f);

            base.Update(time, scrollPos, zoom, isPaused);
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var insects = World.GetThingsByRow(rowNum, ThingType.Bee).ToList();

            var size = insects.Count * 4;
            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[size];
            int i = 0;
            float textureScale = WorldRenderer.Instance.TextureRes == 1 ? 2f : 1f;

            foreach (IFlyingInsect insect in insects)
            {
                var r = ((int)(((insect.Angle * 32f) + 0.5f) / (Mathf.PI * 2f))).Clamp(0, 31);
                var frame = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.FlyingInsects, ThingType.Bee, (r * 3) + insect.AnimationFrame);

                var x = insect.RenderPos.X - (0.25f * frame.CX);
                var y = insect.RenderPos.Y - 8 - (0.25f * frame.CY) - (insect.Height * 0.02f);
                float width = frame.Width * 0.25f;
                float height = frame.Height * 0.25f;

                float tx = frame.X * textureScale / this.textureSize.X;
                float ty = frame.Y * textureScale / this.textureSize.Y;
                float tw = frame.Width * textureScale / this.textureSize.X;
                float th = frame.Height * textureScale / this.textureSize.Y;

                var c = new Color(0f, 0f, 1f, insect.RenderAlpha);
                vertex[i] = new VertexPositionColorTexture(new Vector3(x, y, 0), c, new Vector2(tx, ty));
                vertex[i + 1] = new VertexPositionColorTexture(new Vector3(x + width, y, 0), c, new Vector2(tx + tw, ty));
                vertex[i + 2] = new VertexPositionColorTexture(new Vector3(x, y + height, 0), c, new Vector2(tx, ty + th));
                vertex[i + 3] = new VertexPositionColorTexture(new Vector3(x + width, y + height, 0), c, new Vector2(tx + tw, ty + th));

                i += 4;
            }

            this.SetBufferData(rowNum, vertex, size);
        }
    }
}

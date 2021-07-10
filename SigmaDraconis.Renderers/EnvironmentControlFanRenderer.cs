namespace SigmaDraconis.Renderers
{
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Config;
    using Shared;
    using World;
    using World.Blueprints;
    using WorldInterfaces;

    public class EnvironmentControlFanRenderer : RendererBase, IRenderer
    {
        public bool IsRoofVisible { get; set; } = true;
        public int RoofAlphaPercent { get; set; } = 100;

        public override void LoadContent()
        {
            base.LoadContent();

            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\EnvironmentControlFan_1" : "Buildings\\EnvironmentControlFan_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\EnvironmentControlFan_2" : "Buildings\\EnvironmentControlFan_2";
            this.LoadGeneralEffectNew(WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath1), WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath2));
        }

        public override void ReloadContent()
        {
            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\EnvironmentControlFan_1" : "Buildings\\EnvironmentControlFan_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\EnvironmentControlFan_2" : "Buildings\\EnvironmentControlFan_2";

            this.texture1 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath1);
            this.texture2 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath2);
            this.textureSize = new Vector2(texture1.Width, texture1.Height);
            this.effect.Parameters["xTexture1"].SetValue(texture1);
            this.effect.Parameters["xTexture2"].SetValue(texture2);

            base.ReloadContent();
        }

        public override void Update(WorldTime time, Vector2f scrollPos, float zoom, bool isPaused = false)
        {
            var light = World.WorldLight;
            this.effect.CurrentTechnique = this.effect.Techniques["MainTechnique"];
            this.effect.Parameters["xLightingDirectionFactors"].SetValue(new Vector4(light.LightFactorW, light.LightFactorT + light.LightFactorS, light.LightFactorE, 1));
            this.effect.Parameters["xLightingFactorBlueness"].SetValue(light.LightFactorBlueness);
            this.effect.Parameters["xAlpha"].SetValue(this.RoofAlphaPercent * 0.01f);

            base.Update(time, scrollPos, zoom, isPaused);
        }

        public override void Draw(int row)
        {
            if (this.RoofAlphaPercent == 0) return;
            base.Draw(row);
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var row = World.GetSmallTilesByRow(rowNum);

            var things = row.SelectMany(r => r.ThingsPrimary).Where(t => ThingTypeManager.IsRendererType(t.ThingType, RendererType.EnvironmentControlFan))
                    .Union(World.VirtualBlueprint.Where(b => b.MainTile.Row == rowNum && ThingTypeManager.IsRendererType(b.ThingType, RendererType.EnvironmentControlFan))).ToList();

            var size = things.Count * 4;
            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[size];
            int i = 0;
            var textureScale = (WorldRenderer.Instance.TextureRes == 1) ? 2f : 1f;
            var scale = 0.25f;

            foreach (var thing in things)
            {
                var frame = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.EnvironmentControlFan, ThingType.EnvironmentControl, (thing as IEnvironmentControl)?.FanAnimationFrame ?? 1);

                var tile = thing.MainTile;
                var x = tile.CentrePosition.X - (scale * frame.CX);
                var y = tile.CentrePosition.Y - (scale * frame.CY);

                float width = frame.Width * scale;
                float height = frame.Height * scale;

                float tx = frame.X * textureScale / this.textureSize.X;
                float ty = frame.Y * textureScale / this.textureSize.Y;
                float tw = frame.Width * textureScale / this.textureSize.X;
                float th = frame.Height * textureScale / this.textureSize.Y;

                var alpha = thing is Blueprint ? (thing as Blueprint).ColourA : thing.RenderAlpha;
                var c = new Color(0f, 0f, 1f, (float)System.Math.Sqrt(alpha));

                vertex[i] = new VertexPositionColorTexture(new Vector3(x, y, 0), c, new Vector2(tx, ty));
                vertex[i + 1] = new VertexPositionColorTexture(new Vector3(x + width, y, 0), c, new Vector2(tx + tw, ty));
                vertex[i + 2] = new VertexPositionColorTexture(new Vector3(x, y + height, 0), c, new Vector2(tx, ty + th));
                vertex[i + 3] = new VertexPositionColorTexture(new Vector3(x + width, y + height, 0), c, new Vector2(tx + tw, ty + th));

                i += 4;
            }

            this.SetBufferData(rowNum, vertex, size);
        }

        private Color GetVertexColor(IThing thing)
        {
            var alpha = thing is Blueprint ? (thing as Blueprint).ColourA : thing.RenderAlpha;
            alpha = (float)Mathf.Sqrt(alpha);
            var light = thing.AllTiles.Select(t => t.LightSources.Values.Any() ? t.LightSources.Values.Select(v => v.IsOn ? v.Amount : 0).Max() : 0f).Min();
            return new Color(light, 0f, 1f, alpha);
        }
    }
}

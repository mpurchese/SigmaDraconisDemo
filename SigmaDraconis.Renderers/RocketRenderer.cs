namespace SigmaDraconis.Renderers
{
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Shared;
    using World;
    using World.Blueprints;
    using World.Buildings;

    public class RocketRenderer : RendererBase, IRenderer
    {
        public override void LoadContent()
        {
            base.LoadContent();

            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\Rocket_1" : "Buildings\\Rocket_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\Rocket_2" : "Buildings\\Rocket_2";
            this.LoadGeneralEffectNew(WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath1), WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath2));
        }

        public override void ReloadContent()
        {
            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\Rocket_1" : "Buildings\\Rocket_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\Rocket_2" : "Buildings\\Rocket_2";

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
            this.effect.Parameters["xAlpha"].SetValue(1f);

            base.Update(time, scrollPos, zoom, isPaused);
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var row = World.GetSmallTilesByRow(rowNum);

            var rockets = row.SelectMany(r => r.ThingsPrimary).Where(t => t.ThingType == ThingType.Rocket)
                    .Union(World.VirtualBlueprint.Where(b => b.MainTile.Row == rowNum && b.ThingType == ThingType.Rocket)).ToList();

            var size = rockets.Count * 4;
            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[size];
            int i = 0;

            foreach (Thing rocket in rockets)
            {
                var scale = 0.25f;
                var altitude = (rocket as Rocket)?.Altitude ?? 0;
                var renderOffset = new Vector2f(0 - (35f * scale), 0 - 52f - (181f * scale));

                var tile = rocket.MainTile;
                var x = tile.CentrePosition.X + (renderOffset.X * 0.5f);
                var y = tile.CentrePosition.Y + (renderOffset.Y * 0.5f) - altitude;

                float width = 35f * scale;
                float height = 181f * scale;

                var c = rocket is Blueprint ? new Color(0, 0, 1, (rocket as Blueprint).ColourA) : new Color(0, 0, 1, rocket.RenderAlpha);

                vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(0, 0), Color = c };
                vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(1, 0), Color = c };
                vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(0, 1), Color = c };
                vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(1, 1), Color = c };

                i += 4;
            }

            this.SetBufferData(rowNum, vertex, size);
        }
    }
}

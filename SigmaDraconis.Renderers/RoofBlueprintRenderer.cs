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

    public class RoofBlueprintRenderer : RendererBase, IRenderer
    {
        public bool IsRoofVisible { get; set; } = true;
        public int RoofAlphaPercent { get; set; } = 100;

        public override void LoadContent()
        {
            base.LoadContent();
            var texture = Content.Load<Texture2D>("Textures\\Buildings\\LoRes\\Roofs_1");
            this.LoadBuildingEffectForBlueprints(texture);

            EventManager.Subscribe(EventType.Blueprint, delegate (object obj) { this.OnBlueprintUpdated(obj); });
            EventManager.Subscribe(EventType.RecycleBlueprint, delegate (object obj) { this.OnBlueprintUpdated(obj); });
        }

        public override void Update(WorldTime time, Vector2f scrollPos, float zoom, bool isPaused = false)
        {
            // Fade in and out
            if (this.IsRoofVisible && this.RoofAlphaPercent < 100) this.RoofAlphaPercent += 5;
            else if (!this.IsRoofVisible && this.RoofAlphaPercent > 0) this.RoofAlphaPercent -= 5;

            this.effect.CurrentTechnique = this.effect.Techniques["BlueprintTechnique"];
            this.effect.Parameters["xLightingFactors"].SetValue(new Vector4(1, 1, 1, this.RoofAlphaPercent * 0.01f));

            base.Update(time, scrollPos, zoom, isPaused);
        }

        public override void Draw(int row)
        {
            if (this.RoofAlphaPercent > 0) base.Draw(row);
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var buildings = World.ConfirmedBlueprints.Values
                    .Union(World.RecycleBlueprints.Values)
                    .Where(b => b.MainTile.Row == rowNum && ThingTypeManager.IsRendererType(b.ThingType, RendererType.Roof))
                    .ToList();

            var size = buildings.Count * 4;
            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[size];
            int i = 0;

            foreach (var building in buildings)
            {
                var renderOffset = new Vector2f();
                var frame = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.Roof, building.ThingType, building.AnimationFrame);
                var tile = building.MainTile;
                float scale = 0.25f;
                var x = tile.CentrePosition.X - (scale * frame.CX);
                var y = tile.CentrePosition.Y - (scale * frame.CY);

                float width = frame.Width * scale;
                float height = frame.Height * scale;

                float tx = frame.X / this.textureSize.X;
                float ty = frame.Y / this.textureSize.Y;
                float tw = frame.Width / this.textureSize.X;
                float th = frame.Height / this.textureSize.Y;

                var c = new Color(building.ColourR, building.ColourG, building.ColourB, building.ColourA);

                vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(tx, ty), Color = c };
                vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(tx + tw, ty), Color = c };
                vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(tx, ty + th), Color = c };
                vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(tx + tw, ty + th), Color = c };

                i += 4;
            }

            this.SetBufferData(rowNum, vertex, size);
        }

        private void OnBlueprintUpdated(object sender)
        {
            if (sender is Blueprint b && ThingTypeManager.IsRendererType(b.ThingType, RendererType.Roof))
            {
                this.InvalidateRow(b.MainTile.Row);
            }
        }
    }
}

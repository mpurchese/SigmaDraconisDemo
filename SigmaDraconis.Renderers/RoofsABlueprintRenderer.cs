namespace SigmaDraconis.Renderers
{
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Config;
    using Shared;
    using World;
    using World.Blueprints;
    using Draconis.Shared;

    public class EnvironmentControlFanBlueprintRenderer : RendererBase, IRenderer
    {
        public override void LoadContent()
        {
            base.LoadContent();
            var texture = Content.Load<Texture2D>("Textures\\Buildings\\EnvironmentControlFan_1");
            this.LoadBuildingEffectForBlueprints(texture);

            EventManager.Subscribe(EventType.Blueprint, delegate (object obj) { this.OnBlueprintUpdated(obj); });
            EventManager.Subscribe(EventType.RecycleBlueprint, delegate (object obj) { this.OnBlueprintUpdated(obj); });
        }

        public override void Update(WorldTime time, Vector2f scrollPos, float zoom, bool isPaused = false)
        {
            this.effect.CurrentTechnique = this.effect.Techniques["BlueprintTechnique"];
            this.effect.Parameters["xLightingFactors"].SetValue(new Vector4(1, 1, 1, 1));
            base.Update(time, scrollPos, zoom, isPaused);
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var buildings = World.ConfirmedBlueprints.Values
                    .Union(World.RecycleBlueprints.Values)
                    .Where(b => b.MainTile.Row == rowNum && ThingTypeManager.IsRendererType(b.ThingType, RendererType.EnvironmentControlFan))
                    .ToList();

            var size = buildings.Count * 4;
            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[size];
            int i = 0;

            foreach (var building in buildings)
            {
                var frame = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.EnvironmentControlFan, ThingType.EnvironmentControl, 1);
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
            if (sender is Blueprint b && ThingTypeManager.IsRendererType(b.ThingType, RendererType.EnvironmentControlFan))
            {
                this.InvalidateRow(b.MainTile.Row);
            }
        }
    }
}

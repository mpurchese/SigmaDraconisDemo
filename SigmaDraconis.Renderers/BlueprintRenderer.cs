namespace SigmaDraconis.Renderers
{
    using System;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Config;
    using Shared;
    using World;
    using World.Blueprints;
    using World.Flora;
    using WorldInterfaces;
    using Draconis.Shared;

    public class BlueprintRenderer : RendererBase, IRenderer
    {
        public override void LoadContent()
        {
            var texture = Content.Load<Texture2D>("Textures\\Buildings\\Blueprints");
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
            var blueprints = World.ConfirmedBlueprints.Values
                    .Union(World.RecycleBlueprints.Values)
                    .Where(b => b.ThingType != ThingType.Tree
                        && b.MainTile.Row == rowNum
                        && (b.Definition.RendererTypes.Any(r => r.In(
                            RendererType.Rocket, RendererType.LaunchPad, RendererType.Wall, RendererType.GeneralA, RendererType.GeneralB, RendererType.GeneralC, RendererType.PlantsAndRocks, RendererType.Colonist))))
                    .ToList();

            var trees = World.RecycleBlueprints.Values.Where(b => b.ThingType == ThingType.Tree && b.MainTile.Row == rowNum).ToList();
            var grassSpikes = World.RecycleBlueprints.Values.Where(b => b.ThingType == ThingType.Grass && b.MainTile.Row == rowNum && b.ThingId.HasValue && World.GetThing(b.ThingId.Value) is Swordleaf s && s.FlowerFrame.HasValue).ToList();

            var size = (blueprints.Count + trees.Count + grassSpikes.Count) * 4;
            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[size];
            int i = 0;

            foreach (var blueprint in blueprints)
            {
                var textureName = blueprint.ThingType.ToString();
                if (!blueprint.ThingType.In(ThingType.MushFactory, ThingType.SleepPod, ThingType.Colonist, ThingType.KekDispenser, ThingType.WaterDispenser, ThingType.FoodDispenser, ThingType.ResourceProcessor)) textureName = blueprint.GetTextureName();
                var frame = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.Blueprint, textureName);
                if (frame == null)
                {
                    i += 4;
                    continue;
                }

                var tile = blueprint.MainTile;
                float scale = 0.25f;
                var x = tile.CentrePosition.X - (scale * frame.CX);
                var y = tile.CentrePosition.Y - (scale * frame.CY);

                if (blueprint.RenderPositionOffset != null)
                {
                    x += blueprint.RenderPositionOffset.X;
                    y += blueprint.RenderPositionOffset.Y;
                }

                float tx = frame.X / this.textureSize.X;
                float ty = frame.Y / this.textureSize.Y;
                float tw = frame.Width / this.textureSize.X;
                float th = frame.Height / this.textureSize.Y;
                float width = Math.Abs(frame.Width) * scale;
                float height = frame.Height * scale;
                var c = new Color(blueprint.ColourR, blueprint.ColourG, blueprint.ColourB, blueprint.ColourA);

                var definition = blueprint.ThingType.GetDefinition();
                if (definition.IsPlant && blueprint.ThingType != ThingType.Tree && blueprint.ThingId.HasValue)
                {
                    if (definition.Size.X == 2) x += 10.67f;
                }
                else if (definition.Size.X > 1 && blueprint.ThingType != ThingType.RocketGantry && blueprint.ThingType != ThingType.Rocket)
                {
                    if (definition.RendererTypes.Any(r => r.In(RendererType.GeneralA, RendererType.GeneralB, RendererType.GeneralC, RendererType.PlantsAndRocks))) x += (definition.Size.X - 1) * 10.667f;
                    else if (definition.Size.X == 2) y += 5.333f;
                }

                vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(tx, ty), Color = c };
                vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(tx + tw, ty), Color = c };
                vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(tx, ty + th), Color = c };
                vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(tx + tw, ty + th), Color = c };

                i += 4;
            }

            var treeFrame = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.Blueprint, "TreeTrunk_1");
            foreach (var blueprint in trees)
            {
                var tile = blueprint.MainTile;
                if (!(tile.ThingsAll.SingleOrDefault(t => t.ThingType == ThingType.Tree) is Tree tree)) continue;

                float x = tile.CentrePosition.X + tree.RenderPositionOffset.X;
                float y = tile.CentrePosition.Y + tree.RenderPositionOffset.Y;

                float width = tree.RenderSize.X;
                float height = tree.Height;

                float tx = treeFrame.X / this.textureSize.X;
                float ty = treeFrame.Y / this.textureSize.Y;
                float tw = treeFrame.Width / this.textureSize.X;
                float th = treeFrame.Height / this.textureSize.Y;

                var c = new Color(blueprint.ColourR, blueprint.ColourG, blueprint.ColourB, blueprint.ColourA);

                vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(tx, ty), Color = c };
                vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(tx + tw, ty), Color = c };
                vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(tx, ty + th), Color = c };
                vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(tx + tw, ty + th), Color = c };
                i += 4;
            }

            foreach (var blueprint in grassSpikes)
            {
                var grass = World.GetThing(blueprint.ThingId.Value) as Swordleaf;
                var scale = 0.25f;

                var tile = grass.MainTile;
                var x = tile.CentrePosition.X + grass.RenderPositionOffset.X - (scale * 7.5f);
                var y = tile.CentrePosition.Y + grass.RenderPositionOffset.Y - (scale * 291f);

                float width = 15f * scale;
                float height = 293f * scale;

                var frame = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.Blueprint, "GrassSpike_All");

                float tx = (frame.X + ((grass.FlowerFrame.Value - 1) * 15f))/ this.textureSize.X;
                float ty = frame.Y / this.textureSize.X;
                float tw = 15f / this.textureSize.X;
                float th = 293f / this.textureSize.Y;

                var c = new Color(blueprint.ColourR, blueprint.ColourG, blueprint.ColourB, blueprint.ColourA);

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
            var building = (sender as Blueprint);
            if (building is IResourceStack || building.Definition.RendererTypes.Any(r => r.In(
                RendererType.Rocket, RendererType.LaunchPad, RendererType.Wall, RendererType.GeneralA, RendererType.GeneralB, RendererType.GeneralC, RendererType.PlantsAndRocks)))
            {
                this.InvalidateRow(building.MainTile.Row);
            }
        }
    }
}

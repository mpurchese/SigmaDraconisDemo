namespace SigmaDraconis.Renderers
{
    using System;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Shared;
    using World;
    using World.Blueprints;
    using World.Flora;
    using WorldControllers;

    public class TreeTopBlueprintRenderer : RendererBase, IRenderer
    {
        public override void LoadContent()
        {
            base.LoadContent();
            var texturePath = WorldRenderer.Instance.TextureRes == 0 ? "PlantsAndRocks\\LoRes\\PalmTop_1" : "PlantsAndRocks\\PalmTop_1";
            this.mainTexture = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath);
            this.LoadBuildingEffectForBlueprints(this.mainTexture);

            EventManager.Subscribe(EventType.Blueprint, delegate (object obj) { this.OnBlueprintUpdated(obj); });
            EventManager.Subscribe(EventType.RecycleBlueprint, delegate (object obj) { this.OnBlueprintUpdated(obj); });
        }

        public override void ReloadContent()
        {
            var texturePath = WorldRenderer.Instance.TextureRes == 0 ? "PlantsAndRocks\\LoRes\\PalmTop_1" : "PlantsAndRocks\\PalmTop_1";
            this.mainTexture = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath);
            this.effect.Parameters["xNightTexture"].SetValue(this.mainTexture);
            this.textureSize = new Vector2(this.mainTexture.Width, this.mainTexture.Height);
            base.ReloadContent();
        }

        public override void Update(WorldTime time, Vector2f scrollPos, float zoom, bool isPaused = false)
        {
            if (!isPaused)
            {
                // Move in the wind
                this.effect.Parameters["xWarpPhase"].SetValue(DateTime.UtcNow.Millisecond / 159f);
                this.effect.Parameters["xWarpAmplitude"].SetValue(WeatherController.TreetopSwayAmplitude * 0.016f);
            }

            this.effect.CurrentTechnique = this.effect.Techniques["BlueprintWarpTechnique"];
            this.effect.Parameters["xLightingFactors"].SetValue(new Vector4(1, 1, 1, 1));

            base.Update(time, scrollPos, zoom, isPaused);
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var trees = World.RecycleBlueprints.Values.Where(b => b.ThingType == ThingType.Tree && b.MainTile.Row == rowNum).ToList();

            var size = trees.Count * 4;
            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[size];
            int i = 0;

            foreach (var blueprint in trees)
            {
                var tile = blueprint.MainTile;
                if (!(tile.ThingsAll.SingleOrDefault(t => t.ThingType == ThingType.Tree) is Tree tree)) continue;

                var x = tree.MainTile.CentrePosition.X;
                var y = tree.MainTile.CentrePosition.Y + tree.RenderPositionOffset.Y - 26;

                float width = 0.8f * Math.Min((int)tree.Height + 32, 128);
                float height = 0.5f * width;
                x = tile.CentrePosition.X + tree.RenderPositionOffset.X + 0.25f + (0.5f * tree.RenderSize.X) - (width / 2);
                y = tree.MainTile.CentrePosition.Y + tree.RenderPositionOffset.Y + (0.5f * (60 - height)) - 26;

                var warpPhase = tree.TreeTopWarpPhase;

                var c = new Color(blueprint.ColourR, warpPhase, blueprint.ColourB, blueprint.ColourA);
                vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(0, 0), Color = c };
                vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(1, 0), Color = c };
                vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(0, 1), Color = c };
                vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(1, 1), Color = c };
                i += 4;
            }

            this.SetBufferData(rowNum, vertex, size);
        }

        private void OnBlueprintUpdated(object sender)
        {
            var building = (sender as Blueprint);
            if (building.ThingType == ThingType.Tree)
            {
                this.InvalidateRow(building.MainTile.Row);
            }
        }
    }
}

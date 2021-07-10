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
    using World.Rooms; 
    using WorldInterfaces;

    public class WallRenderer : RendererBase, IRenderer
    {
        public WallRenderer()
        {
        }

        public override void LoadContent()
        {
            base.LoadContent();

            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\Walls_1" : "Buildings\\Walls_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\Walls_2" : "Buildings\\Walls_2";
            this.LoadGeneralEffectNew(WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath1), WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath2));
        }

        public override void ReloadContent()
        {
            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\Walls_1" : "Buildings\\Walls_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\Walls_2" : "Buildings\\Walls_2";

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

            var walls = row.SelectMany(r => r.ThingsPrimary).Where(t => ThingTypeManager.IsRendererType(t.ThingType, RendererType.Wall)).OfType<IRotatableThing>().ToList();
            walls.AddRange(World.VirtualBlueprint.Where(b => b.MainTile.Row == rowNum && ThingTypeManager.IsRendererType(b.ThingType, RendererType.Wall)).OfType<IRotatableThing>());

            var size = walls.Count * 4;
            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[size];
            int i = 0;
            var textureScale = (WorldRenderer.Instance.TextureRes == 1) ? 2f : 1f;
            var th = 0.5f;

            foreach (var wall in walls)
            {
                var tile = World.GetSmallTile(wall.MainTileIndex);
                var x = tile.CentrePosition.X - (wall.Direction == Direction.SW ? 11.25f : 0.5f);
                var y = tile.CentrePosition.Y - 12.75f;

                var hasRoof = wall.AllTiles.Any(t => t.ThingsPrimary.Any(u => u.ThingType == ThingType.Roof && !u.IsDesignatedForRecycling));

                float tx = 0f;
                float ty = hasRoof ? 0.5f : 0f;
                float tw = (wall.ThingType == ThingType.Door ? 45f : 47f) * textureScale / this.textureSize.X;
                
                if (wall.ThingType == ThingType.Door) tx = ((wall.Direction == Direction.SW ? 94f : 544f) + ((wall as IAnimatedThing).AnimationFrame * 45f)) * textureScale / this.textureSize.X;
                else if (wall.Direction == Direction.SE) tx = 47f * textureScale / this.textureSize.X;

                var c = GetVertexColor(wall);

                vertex[i] = new VertexPositionColorTexture(new Vector3(x, y, 0), c, new Vector2(tx, ty));
                vertex[i + 1] = new VertexPositionColorTexture(new Vector3(x + 11.75f, y, 0), c, new Vector2(tx + tw, ty));
                vertex[i + 2] = new VertexPositionColorTexture(new Vector3(x, y + 18.5f, 0), c, new Vector2(tx, ty + th));
                vertex[i + 3] = new VertexPositionColorTexture(new Vector3(x + 11.75f, y + 18.5f, 0), c, new Vector2(tx + tw, ty + th));

                i += 4;
            }

            this.SetBufferData(rowNum, vertex, size);
        }

        private static Color GetVertexColor(IThing thing)
        {
            var alpha = thing is Blueprint ? (thing as Blueprint).ColourA : thing.RenderAlpha;

            var room = RoomManager.GetRoom(thing.MainTile.GetTileToDirection((thing as IRotatableThing).Direction).Index);
            var light = thing.AllTiles.Select(t => t.LightSources.Values.Any() ? t.LightSources.Values.Select(v => v.IsOn ? v.Amount : 0).Max() : 0f).Min();
            var artificialLight = Mathf.Max(room?.RenderTopLight ?? 0f, light);
            artificialLight = Mathf.Min(artificialLight, World.WorldLight.NightLightFactor);

            return new Color(artificialLight, 0f, 1f, alpha);
        }
    }
}

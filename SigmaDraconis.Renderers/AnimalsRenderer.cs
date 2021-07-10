namespace SigmaDraconis.Renderers
{
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Config;
    using Shared;
    using World;
    using World.Rooms;
    using WorldInterfaces;

    public class AnimalsRenderer : RendererBase, IRenderer
    {
        private TextureAtlasFrame[] tortoiseStrips;

        public override void LoadContent()
        {
            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Animals\\LoRes\\Animals_1" : "Animals\\Animals_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Animals\\LoRes\\Animals_2" : "Animals\\Animals_2";

            base.LoadContent();
            this.LoadGeneralEffectNew(WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath1), WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath2));

            // Cache animation strips
            this.tortoiseStrips = new TextureAtlasFrame[4];
            for (int i = 1; i <= 4; i++) this.tortoiseStrips[i - 1] = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.Animals, ThingType.Tortoise, i);
        }

        public override void ReloadContent()
        {
            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Animals\\LoRes\\Animals_1" : "Animals\\Animals_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Animals\\LoRes\\Animals_2" : "Animals\\Animals_2";

            this.texture1 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath1);
            this.texture2 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath2);
            this.textureSize = new Vector2(texture1.Width, texture1.Height);
            this.effect.Parameters["xTexture1"].SetValue(texture1);
            this.effect.Parameters["xTexture2"].SetValue(texture2);

            base.ReloadContent();
        }

        public override void Update(WorldTime time, Vector2f scrollPos, float zoom, bool isPaused = false)
        {
            foreach (var id in EventManager.MovedAnimals)
            {
                if (World.GetThing(id) is IAnimal animal && animal.RenderRow.HasValue)
                {
                    this.InvalidateRow(animal.RenderRow.Value);
                    if (animal.PrevRenderRow.HasValue && animal.PrevRenderRow != animal.RenderRow)
                    {
                        this.InvalidateRow(animal.PrevRenderRow.Value);
                        animal.PrevRenderRow = animal.RenderRow;
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
            var animals = World.GetThingsByRow(rowNum, ThingType.Tortoise).ToList();

            var size = animals.Count * 4;
            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[size];
            int i = 0;

            foreach (IAnimal animal in animals)
            {
                var frameIndex = animal.AnimationFrame / 32;
                var frame = this.tortoiseStrips[frameIndex];
                var tile = animal.MainTile;

                float w = frame.Width / 32;
                float width = w * 0.25f;
                float height = frame.Height * 0.25f;

                var x = tile.CentrePosition.X + animal.PositionOffset.X - (width / 2f);
                var y = tile.CentrePosition.Y + animal.PositionOffset.Y - (height / 2f);

                float textureScale = WorldRenderer.Instance.TextureRes == 1 ? 2f : 1f;
                float tx = (frame.X + (w * (animal.AnimationFrame % 32))) * textureScale / this.textureSize.X;
                float ty = frame.Y * textureScale / this.textureSize.Y;
                float tw = w * textureScale / this.textureSize.X;
                float th = frame.Height * textureScale / this.textureSize.Y;

                var posTile = World.GetSmallTile((int)(animal.Position.X + 0.5f), (int)(animal.Position.Y + 0.5f));
                var artificialLight = posTile != null ? RoomManager.GetTileArtificialLightLevel(posTile.Index) : 0;
                var c = new Color(artificialLight, 0f, 1f, animal.RenderAlpha);

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

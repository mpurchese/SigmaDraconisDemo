namespace SigmaDraconis.Renderers
{
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Shared;
    using World;
    using WorldInterfaces;

    public class ColonistHeadRenderer : ColonistRendererBase, IRenderer
    {
        private readonly int layer;
        private static int colonistBodyColourTextureWidth;
        private static int colonistHairColourTextureWidth;

        public ColonistHeadRenderer(int layer)
        {
            this.layer = layer;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Colonists\\LoRes\\ColonistHead_1" : "Colonists\\ColonistHead_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Colonists\\LoRes\\ColonistHead_2" : "Colonists\\ColonistHead_2";

            this.LoadGeneralEffectNew(WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath1), WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath2), "Effects\\ColonistEffect");

            var colonistBodyColoursTexture = Content.Load<Texture2D>("Textures\\Colonists\\ColonistColours");
            var colonistHairColoursTexture = Content.Load<Texture2D>("Textures\\Colonists\\ColonistHairColours");

            this.effect.Parameters["xTextureColonistColour"].SetValue(colonistBodyColoursTexture);
            this.effect.Parameters["xTexturePackContentColour"].SetValue(colonistHairColoursTexture);

            colonistBodyColourTextureWidth = colonistBodyColoursTexture.Width;
            colonistHairColourTextureWidth = colonistHairColoursTexture.Width;

            EventManager.Subscribe(EventType.Colonist, EventSubType.Added, delegate (object obj) { this.OnColonistUpdated(obj); });
            EventManager.Subscribe(EventType.Colonist, EventSubType.Removed, delegate (object obj) { this.OnColonistUpdated(obj); });
        }

        public override void ReloadContent()
        {
            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Colonists\\LoRes\\ColonistHead_1" : "Colonists\\ColonistHead_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Colonists\\LoRes\\ColonistHead_2" : "Colonists\\ColonistHead_2";

            this.texture1 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath1);
            this.texture2 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath2);

            base.ReloadContent();
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var colonists = World.GetThings<IColonist>(ThingType.Colonist)
                .Where(a => a.RenderRow == rowNum && !a.IsDead && ((a.IsRenderLayer1 && layer == 1) || (!a.IsRenderLayer1 && layer == 2)) && (!a.IsDead || a.MainTile.ThingsPrimary.All(t => t.ThingType != ThingType.SleepPod || (t as IBuildableThing)?.IsRecycling == true))
                    && a.MainTile.ThingsPrimary.All(t => t.ThingType != ThingType.LandingPod || (t as ILandingPod).Altitude <= 0f))
                .OrderBy(c => c.RenderPos.Y).ToList();  // Dead colonists invisible in pod

            var size = colonists.Count * 4;
            ColonistVertex[] vertex = new ColonistVertex[size];
            int i = 0;

            var renderOffset = new Vector2f(-2.875f, -11.25f);

            foreach (var colonist in colonists)
            {
                var tile = colonist.MainTile;

                var x = colonist.RenderPos.X + renderOffset.X;
                var y = colonist.RenderPos.Y + renderOffset.Y;

                float frame = colonist.AnimationFrame;
                float width = 5.75f;
                float height = 6f;

                float tx = 23f * frame / this.textureSize.X;
                float ty = 0;
                float tw = 23f / this.textureSize.X;
                float th = 24f / this.textureSize.Y;

                if (colonist.SleepFrame > 0)
                {
                    var f = colonist.SleepFrame >= 2 ? (colonist.SleepFrame / 2) - 1 : 0;
                    if (colonist.FacingDirection == Direction.SE) f += 8;
                    else if (colonist.FacingDirection == Direction.NE) f += 16;
                    else if (colonist.FacingDirection == Direction.NW) f += 24;
                    tx = 23f * f / this.textureSize.X;
                    ty = 24f / this.textureSize.Y;
                    th = 27f / this.textureSize.Y;
                }

                if (WorldRenderer.Instance.TextureRes == 1)
                {
                    tx *= 2;
                    ty *= 2;
                    tw *= 2;
                    th *= 2;
                }

                var c = GetVertexColor(colonist);
                var colonistColourCoord = new Vector2((colonist.ColourCode / (float)colonistBodyColourTextureWidth) + (0.5f / colonistBodyColourTextureWidth), colonist.IsDead ? 0.75f : 0.25f);
                var colonistHairColourCoord = new Vector2((colonist.HairColourCode / (float)colonistHairColourTextureWidth) + (0.5f / colonistHairColourTextureWidth), 0.5f);

                vertex[i] = new ColonistVertex(new Vector3(x, y, 0), new Vector2(tx, ty), colonistColourCoord, colonistHairColourCoord, c);
                vertex[i + 1] = new ColonistVertex(new Vector3(x + width, y, 0), new Vector2(tx + tw, ty), colonistColourCoord, colonistHairColourCoord, c);
                vertex[i + 2] = new ColonistVertex(new Vector3(x, y + height, 0), new Vector2(tx, ty + th), colonistColourCoord, colonistHairColourCoord, c);
                vertex[i + 3] = new ColonistVertex(new Vector3(x + width, y + height, 0), new Vector2(tx + tw, ty + th), colonistColourCoord, colonistHairColourCoord, c);

                i += 4;
            }

            this.SetBufferData(rowNum, vertex, size);
        }

        public override void Update(WorldTime time, Vector2f scrollPos, float zoom, bool isPaused = false)
        {
            foreach (var id in EventManager.MovedColonists)
            {
                if (World.GetThing(id) is IColonist colonist)
                {
                    this.InvalidateRow(colonist.RenderRow.GetValueOrDefault());
                    if (colonist.PrevRenderRow.HasValue && colonist.PrevRenderRow != colonist.RenderRow) this.InvalidateRow(colonist.PrevRenderRow.GetValueOrDefault());
                }
            }

            var light = World.WorldLight;
            this.effect.CurrentTechnique = this.effect.Techniques["MainTechnique"];
            this.effect.Parameters["xLightingDirectionFactors"].SetValue(new Vector4(light.LightFactorW, light.LightFactorT + light.LightFactorS, light.LightFactorE, 1));
            this.effect.Parameters["xLightingFactorBlueness"].SetValue(light.LightFactorBlueness);
            this.effect.Parameters["xAlpha"].SetValue(1f);

            base.Update(time, scrollPos, zoom, isPaused);
        }

        private void OnColonistUpdated(object sender)
        {
            var colonist = (sender as IColonist);
            if (colonist == null) return;

            this.InvalidateRow(colonist.RenderRow.GetValueOrDefault());
            if (colonist.PrevRenderRow.HasValue && colonist.PrevRenderRow != colonist.RenderRow) this.InvalidateRow(colonist.PrevRenderRow.GetValueOrDefault());
        }
    }
}

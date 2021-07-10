namespace SigmaDraconis.Renderers
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Shared;
    using World;
    using WorldInterfaces;

    public class ColonistBodyRenderer : ColonistRendererBase, IRenderer
    {
        private readonly int layer;
        private static Texture2D colonistColoursTexture;
        private static Texture2D packContentColoursTexture;
        private static readonly Dictionary<ItemType, int> itemPackContentColourCoords = new Dictionary<ItemType, int> {
            { ItemType.None, 0 },
            { ItemType.Metal, 1 },
            { ItemType.Biomass, 2 },
            { ItemType.IronOre, 3 },
            { ItemType.Coal, 4 },
            { ItemType.Stone, 5 },
            { ItemType.Compost, 6 }};
        private static readonly Dictionary<int, int> cropPackContentColourCoords = new Dictionary<int, int> { { 1, 8 }, { 100, 9 }, { 101, 10 }, { 102, 11 }, { 2, 12 }, { 3, 13 }, { 4, 14 }, { 5, 15 }, { 6, 16 }, { 110, 17 }, { 111, 18 } };

        public ColonistBodyRenderer(int layer)
        {
            this.layer = layer;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Colonists\\LoRes\\ColonistBody_1" : "Colonists\\ColonistBody_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Colonists\\LoRes\\ColonistBody_2" : "Colonists\\ColonistBody_2";

            this.LoadGeneralEffectNew(WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath1), WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath2), "Effects\\ColonistEffect");

            if (colonistColoursTexture == null) colonistColoursTexture = Content.Load<Texture2D>("Textures\\Colonists\\ColonistColours");
            if (packContentColoursTexture == null) packContentColoursTexture = Content.Load<Texture2D>("Textures\\Colonists\\PackContentColours");

            this.effect.Parameters["xTextureColonistColour"].SetValue(colonistColoursTexture);
            this.effect.Parameters["xTexturePackContentColour"].SetValue(packContentColoursTexture);

            EventManager.Subscribe(EventType.Colonist, EventSubType.Added, delegate (object obj) { this.OnColonistUpdated(obj); });
            EventManager.Subscribe(EventType.Colonist, EventSubType.Removed, delegate (object obj) { this.OnColonistUpdated(obj); });
        }

        public override void ReloadContent()
        {
            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Colonists\\LoRes\\ColonistBody_1" : "Colonists\\ColonistBody_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Colonists\\LoRes\\ColonistBody_2" : "Colonists\\ColonistBody_2";

            this.texture1 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath1);
            this.texture2 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath2);

            base.ReloadContent();
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var colonists = World.GetThings<IColonist>(ThingType.Colonist)
                .Where(a => a.RenderRow == rowNum && ((a.IsRenderLayer1 && layer == 1) || (!a.IsRenderLayer1 && layer == 2)) && (!a.IsDead || a.MainTile.ThingsPrimary.All(t => t.ThingType != ThingType.SleepPod || (t as IBuildableThing)?.IsRecycling == true))
                    && a.MainTile.ThingsPrimary.All(t => t.ThingType != ThingType.LandingPod || (t as ILandingPod).Altitude <= 0f))
                .OrderBy(c => c.RenderPos.Y).ToList();  // Dead colonists invisible in pod

            var size = colonists.Count * 4;
            ColonistVertex[] vertex = new ColonistVertex[size];
            int i = 0;

            var renderOffset = new Vector2f(-3.875f, -8.25f);

            var colonistColourTextureWidth = colonistColoursTexture.Width;
            var packContentColourTextureWidth = packContentColoursTexture.Width;

            foreach (var colonist in colonists)
            {
                var tile = colonist.MainTile;

                var x = colonist.RenderPos.X + renderOffset.X;
                var y = colonist.RenderPos.Y + renderOffset.Y;

                float frame = colonist.AnimationFrame;
                float width = (frame >= 32 ? 46f : 31f) * 0.25f;
                float height = (frame >= 32 ? 32f : 38f) * 0.25f;

                float tx = 31f * frame / this.textureSize.X;
                float ty = 0;
                float tw = 31f / this.textureSize.X;
                float th = 38f / this.textureSize.Y;

                if (frame >= 32)
                {
                    // Dead frames
                    tx = 992f / this.textureSize.X;
                    tw = 46f / this.textureSize.X;
                    th = 32f / this.textureSize.Y;
                    x -= 2f;
                    y += 3.75f;
                }
                else if (colonist.RaisedArmsFrame == 18 && colonist.ActivityType == ColonistActivityType.Geology)
                {
                    if (colonist.FacingDirection == Direction.NE)
                    {
                        tx = 992f / this.textureSize.X;
                        tw = 34f / this.textureSize.X;
                        ty = 36f / this.textureSize.Y;
                        th = 40f / this.textureSize.Y;
                        y -= 0.5f;
                        x += 0.375f;
                        width = 8.5f;
                        height = 10f;
                    }
                    else if (colonist.FacingDirection == Direction.NW)
                    {
                        tx = 1026f / this.textureSize.X;
                        tw = 34f / this.textureSize.X;
                        ty = 36f / this.textureSize.Y;
                        th = 40f / this.textureSize.Y;
                        y -= 0.5f;
                        x -= 0.9375f;
                        width = 8.5f;
                        height = 10f;
                    }
                    else if (colonist.FacingDirection == Direction.SE)
                    {
                        tx = 1060f / this.textureSize.X;
                        tw = 33f / this.textureSize.X;
                        ty = 38f / this.textureSize.Y;
                        x += 0.25f;
                        width = 8.25f;
                    }
                    else// if (colonist.FacingDirection == Direction.SW)
                    {
                        tx = 1093f / this.textureSize.X;
                        tw = 34f / this.textureSize.X;
                        ty = 38f / this.textureSize.Y;
                        x -= 1f;
                        width = 8.5f;
                    }
                }
                else if (colonist.CarriedItemTypeArms == ItemType.Kek)
                {
                    tx = 35f * frame / this.textureSize.X;
                    ty = 152f / this.textureSize.Y;
                    tw = 35f / this.textureSize.X;
                    th = 40f / this.textureSize.Y;
                    x -= 0.5f;
                    width = 8.75f;
                    height = 10f;
                }
                else if (colonist.RaisedArmsFrame > 0)
                {
                    var f = colonist.RaisedArmsFrame >= 2 ? (colonist.RaisedArmsFrame / 2) - 1 : 0;
                    if (colonist.FacingDirection == Direction.N)
                    {
                        tx = (26f * f) / this.textureSize.X;
                        tw = 26f / this.textureSize.X;
                        ty += 76f / this.textureSize.Y;
                        x += 0.625f;
                        width = 6.5f;
                    }
                    else if (colonist.FacingDirection == Direction.NE)
                    {
                        tx = (234 + (32f * f)) / this.textureSize.X;
                        tw = 32f / this.textureSize.X;
                        ty += 76f / this.textureSize.Y;
                        x += 0.625f;
                        width = 8;
                    }
                    else if (colonist.FacingDirection == Direction.E)
                    {
                        tx = (522 + (39f * f)) / this.textureSize.X;
                        tw = 39f / this.textureSize.X;
                        ty += 76f / this.textureSize.Y;
                        width = 10.375f;
                        x -= 0.3125f;
                    }
                    else if (colonist.FacingDirection == Direction.SE)
                    {
                        tx = (873 + (31f * f)) / this.textureSize.X;
                        tw = 31f / this.textureSize.X;
                        ty += 76f / this.textureSize.Y;
                        width = 8.375f;
                        x += 0.5f; 
                    }
                    else if (colonist.FacingDirection == Direction.S)
                    {
                        tx = (26f * f) / this.textureSize.X;
                        tw = 26f / this.textureSize.X;
                        ty += 114f / this.textureSize.Y;
                        width = 7.125f;
                        x += 0.5f;
                    }
                    else if (colonist.FacingDirection == Direction.SW)
                    {
                        tx = (234 + (32f * f)) / this.textureSize.X;
                        tw = 32f / this.textureSize.X;
                        ty += 114f / this.textureSize.Y;
                        width = 8f;
                        x -= 0.75f;
                    }
                    else if (colonist.FacingDirection == Direction.W)
                    {
                        tx = (522 + (39f * f)) / this.textureSize.X;
                        tw = 39f / this.textureSize.X;
                        ty += 114f / this.textureSize.Y;
                        width = 9.75f;
                        x -= 1.875f;
                    }
                    else // if (colonist.FacingDirection == Direction.NW)
                    {
                        tx = (873 + (32f * f)) / this.textureSize.X;
                        tw = 32f / this.textureSize.X;
                        ty += 114f / this.textureSize.Y;
                        width = 8f;
                        x -= 0.6875f;
                    }
                }
                else if (colonist.SleepFrame > 0)
                {
                    var f = colonist.SleepFrame >= 2 ? (colonist.SleepFrame / 2) - 1 : 0;
                    if (colonist.FacingDirection == Direction.SE) f += 8;
                    else if (colonist.FacingDirection == Direction.NE) f += 16;
                    else if (colonist.FacingDirection == Direction.NW) f += 24;
                    tx = 31f * f / this.textureSize.X;
                    ty += 38f / this.textureSize.Y;
                }

                if (WorldRenderer.Instance.TextureRes == 1)
                {
                    tx *= 2;
                    ty *= 2;
                    tw *= 2;
                    th *= 2;
                }

                var c = GetVertexColor(colonist);
                var colonistColourCoord = new Vector2((colonist.ColourCode / (float)colonistColourTextureWidth) + (0.5f / colonistColourTextureWidth), colonist.IsDead ? 0.75f: 0.25f);
                var colourCoord = 0;
                if (itemPackContentColourCoords.ContainsKey(colonist.CarriedItemTypeBack)) colourCoord = itemPackContentColourCoords[colonist.CarriedItemTypeBack];
                else if (colonist.CarriedItemTypeBack == ItemType.Crop && colonist.CarriedCropType.HasValue && cropPackContentColourCoords.ContainsKey(colonist.CarriedCropType.Value))
                {
                    // Crop colours
                    colourCoord = cropPackContentColourCoords[colonist.CarriedCropType.Value];
                }

                var packContentColourCoord = new Vector2((colourCoord / (float)packContentColourTextureWidth) + (0.5f / packContentColourTextureWidth), 0.5f);

                vertex[i] = new ColonistVertex(new Vector3(x, y, 0), new Vector2(tx, ty), colonistColourCoord, packContentColourCoord, c);
                vertex[i + 1] = new ColonistVertex(new Vector3(x + width, y, 0), new Vector2(tx + tw, ty), colonistColourCoord, packContentColourCoord, c);
                vertex[i + 2] = new ColonistVertex(new Vector3(x, y + height, 0), new Vector2(tx, ty + th), colonistColourCoord, packContentColourCoord, c);
                vertex[i + 3] = new ColonistVertex(new Vector3(x + width, y + height, 0), new Vector2(tx + tw, ty + th), colonistColourCoord, packContentColourCoord, c);

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
                    if (layer == 1) colonist.UpdateRenderRow();
                    this.InvalidateRow(colonist.RenderRow.GetValueOrDefault());
                    if (colonist.PrevRenderRow.HasValue && colonist.PrevRenderRow != colonist.RenderRow) this.InvalidateRow(colonist.PrevRenderRow.GetValueOrDefault());
                }
            }

            this.effect.CurrentTechnique = this.effect.Techniques["MainTechnique"];
            var light = World.WorldLight;
            this.effect.Parameters["xLightingDirectionFactors"].SetValue(new Vector4(light.LightFactorW, light.LightFactorT + light.LightFactorS, light.LightFactorE, 1));
            this.effect.Parameters["xLightingFactorBlueness"].SetValue(light.LightFactorBlueness);
            this.effect.Parameters["xAlpha"].SetValue(1f);

            base.Update(time, scrollPos, zoom, isPaused);
        }

        private void OnColonistUpdated(object sender)
        {
            var colonist = (sender as IColonist);
            if (colonist == null) return;

            colonist.UpdateRenderRow();
            this.InvalidateRow(colonist.RenderRow.GetValueOrDefault());
            if (colonist.PrevRenderRow.HasValue && colonist.PrevRenderRow != colonist.RenderRow) this.InvalidateRow(colonist.PrevRenderRow.GetValueOrDefault());
        }
    }
}

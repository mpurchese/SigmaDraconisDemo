namespace SigmaDraconis.Renderers
{
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Shared;
    using World;
    using WorldInterfaces;

    public class ColonistCarryItemRenderer : ColonistRendererBase, IRenderer
    {
        private readonly int layer;

        public ColonistCarryItemRenderer(int layer)
        {
            this.layer = layer;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Colonists\\LoRes\\ColonistCarryItems_1" : "Colonists\\ColonistCarryItems_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Colonists\\LoRes\\ColonistCarryItems_2" : "Colonists\\ColonistCarryItems_2";

            this.LoadGeneralEffectNew(WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath1), WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath2));

            EventManager.Subscribe(EventType.Colonist, EventSubType.Added, delegate (object obj) { this.OnColonistUpdated(obj); });
            EventManager.Subscribe(EventType.Colonist, EventSubType.Removed, delegate (object obj) { this.OnColonistUpdated(obj); });
        }

        public override void ReloadContent()
        {
            var texturePath1 = WorldRenderer.Instance.TextureRes == 0 ? "Colonists\\LoRes\\ColonistCarryItems_1" : "Colonists\\ColonistCarryItems_1";
            var texturePath2 = WorldRenderer.Instance.TextureRes == 0 ? "Colonists\\LoRes\\ColonistCarryItems_2" : "Colonists\\ColonistCarryItems_2";

            this.texture1 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath1);
            this.texture2 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath2);

            base.ReloadContent();
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var colonists = World.GetThings<IColonist>(ThingType.Colonist)
                .Where(a => a.CarriedItemTypeArms == ItemType.Kek && a.RenderRow == rowNum
                    && ((a.IsRenderLayer1 && layer == 1) || (!a.IsRenderLayer1 && layer == 2)) && (!a.IsDead || a.MainTile.ThingsPrimary.All(t => t.ThingType != ThingType.SleepPod || (t as IBuildableThing)?.IsRecycling == true)))
                .OrderBy(c => c.RenderPos.Y).ToList();

            var size = colonists.Count * 4;
            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[size];
            int i = 0;

            var renderOffset = new Vector2f(-4.375f, -8.25f);
            float tw = (WorldRenderer.Instance.TextureRes == 1 ? 70f : 35f) / this.textureSize.X;
            float w = 8.75f;
            float h = 6.5f;
            foreach (var colonist in colonists)
            {
                var x = colonist.RenderPos.X + renderOffset.X;
                var y = colonist.RenderPos.Y + renderOffset.Y;
                var tx = colonist.AnimationFrame * tw;
                var c = GetVertexColor(colonist);

                vertex[i] = new VertexPositionColorTexture(new Vector3(x, y, 0), c, new Vector2(tx, 0));
                vertex[i + 1] = new VertexPositionColorTexture(new Vector3(x + w, y, 0), c, new Vector2(tx + tw, 0));
                vertex[i + 2] = new VertexPositionColorTexture(new Vector3(x, y + h, 0), c, new Vector2(tx, 1f));
                vertex[i + 3] = new VertexPositionColorTexture(new Vector3(x + w, y + h, 0), c, new Vector2(tx + tw, 1f));

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

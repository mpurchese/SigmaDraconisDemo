namespace SigmaDraconis.Renderers
{
    using System;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Config;
    using Shared;
    using World;
    using WorldInterfaces;

    public class BirdsRenderer : SinglePassAnimalRendererBase
    {
        public BirdsRenderer() : base("Birds")
        {
        }

        public override void ReloadContent()
        {
            this.textureNameRoot = "Birds";
            base.ReloadContent();
        }

        public override void UpdateBuffers()
        {
            if (thingTypes == null) thingTypes = Enum.GetValues(typeof(ThingType)).Cast<ThingType>().Where(t => ThingTypeManager.IsRendererType(t, RendererType.Birds)).ToArray();

            var things = World.GetThings(thingTypes).ToList();
            var size = things.Count * 4;
            if (size == 0)
            {
                this.usedBufferSize = 0;
                return;
            }

            VertexPositionColorTexture[] vertexArray = new VertexPositionColorTexture[size];
            int i = 0;
            float textureScale = WorldRenderer.Instance.TextureRes == 1 ? 2f : 1f;

            foreach (IBird thing in things)
            {
                var textureName = thing.GetTextureName();
                var frame = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.Birds, thing.ThingType, (thing as IAnimatedThing)?.AnimationFrame ?? 1);
                var scale = 0.25f;

                float h = thing.Height;
                if (thing.ThingType == ThingType.Bird1 && this.zoom >= 16 && thing.AnimationFrame >= 9 && thing.AnimationFrame <= 168) h += Mathf.Sin(Mathf.PI * 0.05f * ((thing.AnimationFrame - 9) % 20));
                else if (thing.ThingType == ThingType.Bird2 && this.zoom >= 16 && thing.AnimationFrame >= 2) h += Mathf.Sin(Mathf.PI * 0.05f * ((thing.AnimationFrame - 2) % 20));

                var tile = thing.MainTile;
                var x = thing.RenderPos.X - (0.25f * frame.CX);
                var y = thing.RenderPos.Y - (0.25f * frame.CY) - (h * 0.2f);

                float width = frame.Width * scale;
                float height = frame.Height * scale;

                float tx = frame.X * textureScale / this.textureSize.X;
                float ty = frame.Y * textureScale / this.textureSize.Y;
                float tw = frame.Width * textureScale / this.textureSize.X;
                float th = frame.Height * textureScale / this.textureSize.Y;

                var c = new Color(0f, 0f, 1f, thing.RenderAlpha);
                vertexArray[i] = new VertexPositionColorTexture(new Vector3(x, y, 0), c, new Vector2(tx, ty));
                vertexArray[i + 1] = new VertexPositionColorTexture(new Vector3(x + width, y, 0), c, new Vector2(tx + tw, ty));
                vertexArray[i + 2] = new VertexPositionColorTexture(new Vector3(x, y + height, 0), c, new Vector2(tx, ty + th));
                vertexArray[i + 3] = new VertexPositionColorTexture(new Vector3(x + width, y + height, 0), c, new Vector2(tx + tw, ty + th));

                i += 4;
            }

            this.UpdateIndexBuffer(vertexArray, size);
        }
    }
}

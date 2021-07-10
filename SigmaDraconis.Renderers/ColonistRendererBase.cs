namespace SigmaDraconis.Renderers
{
    using Microsoft.Xna.Framework;
    using World;
    using World.Rooms;
    using WorldInterfaces;

    public class ColonistRendererBase : RendererBase, IRenderer
    {
        protected static Color GetVertexColor(IColonist colonist)
        {
            var posTile = World.GetSmallTile((int)(colonist.Position.X + 0.5f), (int)(colonist.Position.Y + 0.5f));
            var artificialLight = RoomManager.GetTileArtificialLightLevel(posTile.Index);
            return new Color(artificialLight, 0f, 1f, colonist.RenderAlpha);
        }

        public override void ReloadContent()
        {
            this.textureSize = new Vector2(this.texture1.Width, this.texture1.Height);

            this.effect.Parameters["xTexture1"].SetValue(this.texture1);
            this.effect.Parameters["xTexture2"].SetValue(this.texture2);
            this.effect.Parameters["xAlpha"].SetValue(1f);

            base.ReloadContent();
        }
    }
}

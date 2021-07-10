namespace SigmaDraconis.Renderers
{
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;
    using Shared;
    using World.Flora;
    using WorldInterfaces;

    public class TreeTrunkShadowRenderer : ShadowRendererOld, IRenderer
    {
        public override void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.mainTexture = contentManager.Load<Texture2D>("Textures\\Shadows\\TreeTrunkShadow");
            this.textureSize = new Vector2(10, 20);
            base.LoadContent(graphicsDevice, contentManager);
        }

        protected override void LoadBasicEffect()
        {
            base.LoadBasicEffect();
            this.effect.CurrentTechnique = this.effect.Techniques["TreeTrunkShadowTechnique"];
        }

        protected override bool IsThingTypeIncluded(ThingType thingType)
        {
            return thingType == ThingType.Tree;
        }

        protected override void GenerateVertexBufferForThing(IThingWithShadow thing, List<Vector3> shadowModel, List<int> bufferPositions)
        {
            var tree = thing as Tree;
            var array = this.vertexArray as VertexPositionColorTexture[];
            var points = this.GetShadowQuad(tree);
            var offset = bufferPositions[0] * 4;

            var w = tree.RenderSize.X * 1.0f;  // Trunk width, passed in as red component on colour
            for (int j = 0; j < 4; ++j)
            {
                var r = (j == 0 || j == 3) ? 0 - w : w;
                var c = new Color((r + 50f) / 100f, 1, 1, tree.ShadowAlpha);
                array[offset + j] = new VertexPositionColorTexture { Position = points[j], TextureCoordinate = new Vector2(j == 1 || j == 2 ? 1 : 0, j == 2 || j == 3 ? 1 : 0), Color = c };
            }

            WorldRenderer.Instance.InvalidateShadows();
        }

        private List<Vector3> GetShadowQuad(Tree tree)
        {
            var tile = tree.MainTile;
            float cx = tile.CentrePosition.X;
            float cy = tile.CentrePosition.Y + (4 - ((98 - tree.Height) * 0.025f)) - (tree.RenderSize.X / 2f);
            float h = tree.Height / 2;

            List<Vector3> points = new List<Vector3>(4)
            {
                new Vector3(cx, cy, 0),
                new Vector3(cx, cy, 0),
                new Vector3(cx, cy, h),
                new Vector3(cx, cy, h)
            };

            return points;
        }
    }
}

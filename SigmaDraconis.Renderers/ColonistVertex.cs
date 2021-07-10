namespace SigmaDraconis.Renderers
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    public struct ColonistVertex : IVertexType
    {
        public Vector3 Position;
        public Color Color0;
        public Vector2 TextureCoordinate0;
        public Vector2 TextureCoordinate1;
        public Vector2 TextureCoordinate2;

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get { return VertexDeclaration; }
        }

        public readonly static VertexDeclaration VertexDeclaration =
             new VertexDeclaration(
             new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
             new VertexElement(sizeof(float) * 3, VertexElementFormat.Color, VertexElementUsage.Color, 0),
             new VertexElement((sizeof(float) * 3) + 4, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
             new VertexElement((sizeof(float) * 3) + 12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
             new VertexElement((sizeof(float) * 3) + 20, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 2)
        );

        public ColonistVertex(Vector3 p, Vector2 t0, Vector2 t1, Vector2 t2, Color c0)
        {
            Position = p;
            Color0 = c0;
            TextureCoordinate0 = t0;
            TextureCoordinate1 = t1;
            TextureCoordinate2 = t2;
        }
    }
}

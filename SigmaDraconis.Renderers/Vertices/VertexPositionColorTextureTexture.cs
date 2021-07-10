namespace SigmaDraconis.Renderers.Vertices
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexPositionColorTextureTexture : IVertexType
    {
        public Vector3 Position;
        public Color Color;
        public Vector2 TextureCoordinate1;
        public Vector2 TextureCoordinate2;
        public static readonly VertexDeclaration VertexDeclaration;

        public VertexPositionColorTextureTexture(Vector3 position, Color color, Vector2 textureCoordinate1, Vector2 textureCoordinate2)
        {
            Position = position;
            Color = color;
            TextureCoordinate1 = textureCoordinate1;
            TextureCoordinate2 = textureCoordinate2;
        }

        VertexDeclaration IVertexType.VertexDeclaration
        {
            get
            {
                return VertexDeclaration;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ Color.GetHashCode();
                hashCode = (hashCode * 397) ^ TextureCoordinate1.GetHashCode();
                hashCode = (hashCode * 397) ^ TextureCoordinate2.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return "{{Position:" + this.Position + " Color:" + this.Color + " TextureCoordinate1:" + this.TextureCoordinate1 + " TextureCoordinate2:" + this.TextureCoordinate2 + "}}";
        }

        public static bool operator ==(VertexPositionColorTextureTexture left, VertexPositionColorTextureTexture right)
        {
            return (((left.Position == right.Position) && (left.Color == right.Color)) && (left.TextureCoordinate1 == right.TextureCoordinate1) && (left.TextureCoordinate2 == right.TextureCoordinate2));
        }

        public static bool operator !=(VertexPositionColorTextureTexture left, VertexPositionColorTextureTexture right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj.GetType() != base.GetType())
                return false;

            return (this == ((VertexPositionColorTextureTexture)obj));
        }

        static VertexPositionColorTextureTexture()
        {
            var elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1)
            };
            VertexDeclaration = new VertexDeclaration(elements);
        }
    }
}

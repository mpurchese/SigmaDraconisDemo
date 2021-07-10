namespace SigmaDraconis.Shadows
{
    using Microsoft.Xna.Framework;

    public class ShadowTransformScale : IShadowTransform
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public ShadowTransformScale(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Vector3 Apply(Vector3 source)
        {
            return new Vector3(source.X * this.X, source.Y * this.Y, source.Z * this.Z);
        }
    }
}

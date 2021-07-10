namespace SigmaDraconis.Shadows
{
    using Draconis.Shared;
    using Microsoft.Xna.Framework;

    public class ShadowTransformRotate : IShadowTransform
    {
        public float Angle { get; private set; }

        private readonly float sinAngle;
        private readonly float cosAngle;

        public ShadowTransformRotate(float angle)
        {
            this.Angle = angle;
            var radians = angle * Mathf.PI / 180f;
            this.sinAngle = Mathf.Sin(radians);
            this.cosAngle = Mathf.Cos(radians);
        }

        public Vector3 Apply(Vector3 source)
        {
            return new Vector3(this.cosAngle * source.X - this.sinAngle * source.Y, this.sinAngle * source.X + this.cosAngle * source.Y, source.Z);
        }
    }
}

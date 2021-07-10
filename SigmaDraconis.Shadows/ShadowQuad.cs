namespace SigmaDraconis.Shadows
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Shared;

    public class ShadowQuad
    {
        public Vector3 V1 { get; set; }
        public Vector3 V2 { get; set; }
        public Vector3 V3 { get; set; }
        public Vector3 V4 { get; set; }
        public float Alpha { get; set; } = 1f;
        public Direction? Direction { get; set; }
        public List<int> Frames { get; set; }
        public string Texture { get; set; }

        public ShadowQuad Clone()
        {
            return new ShadowQuad
            {
                V1 = new Vector3(this.V1.X, this.V1.Y, this.V1.Z),
                V2 = new Vector3(this.V2.X, this.V2.Y, this.V2.Z),
                V3 = new Vector3(this.V3.X, this.V3.Y, this.V3.Z),
                V4 = new Vector3(this.V4.X, this.V4.Y, this.V4.Z),
                Alpha = this.Alpha,
                Direction = this.Direction,
                Frames = this.Frames.ToList(),
                Texture = this.Texture
            };
        }
    }
}

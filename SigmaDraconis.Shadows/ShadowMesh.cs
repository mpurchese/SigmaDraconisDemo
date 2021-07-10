namespace SigmaDraconis.Shadows
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Draconis.Shared;
    using Shared;

    public class ShadowMesh
    {
        public string Id { get; private set; }
        public Dictionary<int, Vector3> Points { get; set; } = new Dictionary<int, Vector3>();
        public Dictionary<int, Vector2> TexCoords { get; set; } = new Dictionary<int, Vector2>();
        public List<Vector3i> Triangles { get; set; } = new List<Vector3i>();
        public List<int> Frames { get; private set; }
        public Direction? Direction { get; set; }
        public ShadowMeshType Type { get; set; }
        public int DetailLevel { get; set; }

        public ShadowMesh(string id)
        {
            this.Id = id;
        }

        public ShadowMesh(List<int> frames, ShadowMesh source, List<IShadowTransform> transforms, Direction? direction, ShadowMeshType type, int detailLevel)
        {
            this.Frames = frames.ToList();
            this.Direction = direction;
            this.Type = type;
            this.DetailLevel = detailLevel;

            foreach (var point in source.Points)
            {
                var p = new Vector3(point.Value.X, point.Value.Y, point.Value.Z);
                foreach (var transform in transforms) p = transform.Apply(p);

                this.Points.Add(point.Key, p);
            }

            foreach (var tc in source.TexCoords)
            {
                this.TexCoords.Add(tc.Key, tc.Value);
            }

            foreach (var tri in source.Triangles)
            {
                this.Triangles.Add(tri.Clone());
            }
        }

        public void TransformToWorld()
        {
            foreach (var k in this.Points.Keys.ToList())
            {
                var p = this.Points[k];
                var p2 = new Vector3(7.111f * (p.X + p.Y), 3.5555f * (p.X - p.Y), 10f * p.Z);
                this.Points[k] = p2;
            }
        }
    }
}

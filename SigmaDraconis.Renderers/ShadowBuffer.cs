namespace SigmaDraconis.Renderers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Shadows;

    internal class ShadowBuffer : IDisposable
    {
        private readonly GraphicsDevice graphics;

        private VertexPositionColorTexture[] vertexArray = null;
        private int[] indexArray = null;

        private readonly Queue<int> vertexBufferGaps = new Queue<int>();
        private readonly Queue<int> indexBufferGaps = new Queue<int>();

        private readonly Dictionary<int, List<int>> thingVertexBufferPositions = new Dictionary<int, List<int>>();
        private readonly Dictionary<int, List<int>> thingIndexBufferPositions = new Dictionary<int, List<int>>();

        private bool isVertexBufferInvalidated;
        private bool isIndexBufferInvalidated;

        private readonly int vertexCapacity;
        private readonly int initialIndexCapacity;
        private readonly int indexCapacityGrowStep;

        public DynamicVertexBuffer VertexBuffer { get; }
        public IndexBuffer IndexBuffer { get; private set; }

        public int FreeSpace => vertexBufferGaps.Count;
        public bool IsInvalidated => this.isIndexBufferInvalidated || this.isVertexBufferInvalidated;

        public int TriangleCount { get; private set; } = 0;

        public ShadowBuffer(GraphicsDevice graphics, int vertexCapacity, int initialIndexCapacity, int indexCapacityGrowStep)
        {
            this.graphics = graphics;
            this.vertexCapacity = vertexCapacity;
            this.initialIndexCapacity = initialIndexCapacity;
            this.indexCapacityGrowStep = indexCapacityGrowStep;

            this.IndexBuffer = new IndexBuffer(graphics, typeof(int), this.initialIndexCapacity, BufferUsage.WriteOnly);
            this.VertexBuffer = new DynamicVertexBuffer(graphics, VertexPositionColorTexture.VertexDeclaration, vertexCapacity, BufferUsage.WriteOnly);

            this.Clear();
        }

        public void Clear()
        {
            this.indexArray = new int[this.initialIndexCapacity];
            this.vertexArray = new VertexPositionColorTexture[this.vertexCapacity];
            this.vertexArray[0] = new VertexPositionColorTexture { Position = Vector3.Zero, TextureCoordinate = Vector2.Zero, Color = new Color(0, 0, 0, 0) };

            this.vertexBufferGaps.Clear();
            this.indexBufferGaps.Clear();
            for (int i = 0; i < this.initialIndexCapacity - 2; i += 3) this.indexBufferGaps.Enqueue(i);
            for (int i = 1; i < vertexCapacity; i++) this.vertexBufferGaps.Enqueue(i);

            if (this.IndexBuffer?.IsDisposed == false)
            {
                this.IndexBuffer.Dispose();
            }

            this.IndexBuffer = new IndexBuffer(this.graphics, typeof(int), this.initialIndexCapacity, BufferUsage.WriteOnly);

            this.thingVertexBufferPositions.Clear();
            this.thingIndexBufferPositions.Clear();

            this.TriangleCount = 0;
            this.isIndexBufferInvalidated = true;
        }

        public bool CanDraw()
        {
            if (this.VertexBuffer == null || this.VertexBuffer.IsDisposed || this.VertexBuffer.VertexCount < 1 || this.TriangleCount == 0) return false;
            if (this.IndexBuffer == null || this.IndexBuffer.IsDisposed) return false;
            return true;
        }

        public void AddMesh(int thingId, ShadowMesh mesh, float scale, Vector3 translate, Color vertexColour)
        {
            if (!this.thingVertexBufferPositions.ContainsKey(thingId)) this.thingVertexBufferPositions.Add(thingId, new List<int>());
            if (!this.thingIndexBufferPositions.ContainsKey(thingId)) this.thingIndexBufferPositions.Add(thingId, new List<int>());

            var vertexPositionIndex = new Dictionary<int, int>();
            foreach (var vertex in mesh.Points)
            {
                var i = this.vertexBufferGaps.Dequeue();

                var texCoord = mesh.TexCoords.ContainsKey(vertex.Key) ? mesh.TexCoords[vertex.Key] : new Vector2(0.5f, 0.5f);

                vertexPositionIndex.Add(vertex.Key, i);
                this.thingVertexBufferPositions[thingId].Add(i);

                this.vertexArray[i] = new VertexPositionColorTexture { Position = (vertex.Value * scale) + translate, TextureCoordinate = texCoord, Color = vertexColour };
            }

            foreach (var triangle in mesh.Triangles)
            {
                if (!this.indexBufferGaps.Any()) this.EnlargeIndexBuffer();

                var i = this.indexBufferGaps.Dequeue();
                this.indexArray[i] = vertexPositionIndex[triangle.X];
                this.indexArray[i + 1] = vertexPositionIndex[triangle.Y];
                this.indexArray[i + 2] = vertexPositionIndex[triangle.Z];
                this.thingIndexBufferPositions[thingId].Add(i);
                this.TriangleCount = Math.Max(TriangleCount, (i / 3) + 1);
            }

            this.isVertexBufferInvalidated = true;
            this.isIndexBufferInvalidated = true;
        }

        public void RemoveThing(int thingId)
        {
            if (this.thingVertexBufferPositions.ContainsKey(thingId))
            {
                foreach (var pos in this.thingVertexBufferPositions[thingId]) this.vertexBufferGaps.Enqueue(pos);
            }

            if (this.thingIndexBufferPositions.ContainsKey(thingId))
            {
                foreach (var pos in this.thingIndexBufferPositions[thingId])
                {
                    this.indexArray[pos] = 0;
                    this.indexArray[pos + 1] = 0;
                    this.indexArray[pos + 2] = 0;
                    this.indexBufferGaps.Enqueue(pos);
                    while (this.TriangleCount > 0 && this.indexArray[(this.TriangleCount * 3) - 1] == 0) this.TriangleCount--;
                }
            }

            this.thingIndexBufferPositions.Remove(thingId);
            this.thingVertexBufferPositions.Remove(thingId);
            this.isIndexBufferInvalidated = true;
        }

        public void UpdateThingAlpha(int thingId, float alpha)
        {
            if (!this.thingVertexBufferPositions.ContainsKey(thingId)) return;

            foreach (var pos in this.thingVertexBufferPositions[thingId]) this.vertexArray[pos].Color = new Color(Color.White, alpha);
            this.isVertexBufferInvalidated = true;
        }

        public void Invalidate()
        {
            this.isVertexBufferInvalidated = true;
            this.isIndexBufferInvalidated = true;
        }

        public void Validate()
        {
            if (this.isIndexBufferInvalidated) this.IndexBuffer.SetData(this.indexArray);
            if (this.isVertexBufferInvalidated) this.VertexBuffer.SetData(this.vertexArray);
            this.isIndexBufferInvalidated = false;
            this.isVertexBufferInvalidated = false;
        }

        private void EnlargeIndexBuffer()
        {
            var oldCapacity = this.IndexBuffer.IndexCount;
            var newCapacity = oldCapacity + this.indexCapacityGrowStep;

            if (this.IndexBuffer?.IsDisposed == false)
            {
                this.IndexBuffer.Dispose();
            }

            this.IndexBuffer = new IndexBuffer(this.graphics, typeof(int), newCapacity, BufferUsage.WriteOnly);

            Array.Resize(ref this.indexArray, newCapacity);

            for (int i = oldCapacity; i < this.indexArray.Length; i += 3)
            {
                this.indexArray[i] = 0;
                this.indexArray[i + 1] = 0;
                this.indexArray[i + 2] = 0;
                this.indexBufferGaps.Enqueue(i);
            }
        }

        #region IDisposable implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.IndexBuffer != null) this.IndexBuffer.Dispose();
                if (this.VertexBuffer != null) this.VertexBuffer.Dispose();
            }
        }

        #endregion IDisposable implementation
    }
}

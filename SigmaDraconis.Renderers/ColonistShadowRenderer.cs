namespace SigmaDraconis.Renderers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;

    public class ColonistShadowRenderer : IRenderer, IDisposable
    {
        protected VertexBuffer vertexBuffer;
        protected IndexBuffer indexBuffer;
        protected GraphicsDevice graphics;
        protected ContentManager content;
        protected Vector2f scrollPosition = new Vector2i(0, 0);
        protected float zoom;
        protected int viewWidth;
        protected int viewHeight;
        protected int initialBufferCapacity = 64;
        protected float vertexGrowthFactor = 1.5f;
        protected Effect effect;
        protected Texture2D mainTexture;
        protected Stack<int> bufferGaps = new Stack<int>();
        protected Dictionary<int, List<int>> thingBufferPositions = new Dictionary<int, List<int>>();
        protected Dictionary<int, IThing> thingRegister = new Dictionary<int, IThing>();
        protected HashSet<int> thingsInvalidated = new HashSet<int>();
        protected int? highestUsedBufferIndex;
        protected WorldTime WorldTime;
        protected Vector3 sunVector;
        protected VertexPositionColorTexture[] vertexArray = null;
        protected bool isVertexBufferInvalidated = true;
        protected int shadowDetail = 2;

        public ColonistShadowRenderer()
        {
            EventManager.Subscribe(EventType.Colonist, EventSubType.Added, delegate (object obj) { this.OnThingAdded(obj); });
            EventManager.Subscribe(EventType.Colonist, EventSubType.Removed, delegate (object obj) { this.OnThingRemoved(obj); });
            EventManager.Subscribe(EventType.GameExit, delegate (object obj) { this.OnGameExit(obj); });
        }

        private void OnGameExit(object obj)
        {
            this.thingBufferPositions.Clear();
            this.thingRegister.Clear();
            this.highestUsedBufferIndex = null;
            this.bufferGaps.Clear();
        }

        public virtual void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.graphics = graphicsDevice;
            this.content = contentManager;

            if (this.effect == null)
            {
                this.mainTexture = contentManager.Load<Texture2D>("Textures\\Shadows\\ColonistShadow");
                this.LoadBasicEffect();
            }

            this.InitBuffers(this.initialBufferCapacity, true);

            foreach (var thing in this.thingRegister.Keys.ToList())
            {
                this.UpdateThing(thing);
            }
        }

        public virtual void Update(WorldTime time, Vector2f scrollPos, float zoom, Vector3 sunVector, int shadowDetail)
        {
            this.shadowDetail = shadowDetail;
            if (shadowDetail == 0) return;

            this.WorldTime = time.Clone();

            this.sunVector = sunVector.Y > 0 ? sunVector : new Vector3(0 - sunVector.X, 0 - sunVector.Y, 0 - sunVector.Z);

            var isProjectionInvalidated = this.scrollPosition == null
               || scrollPos.X != this.scrollPosition.X
               || scrollPos.Y != this.scrollPosition.Y
               || this.graphics.Viewport.Width != this.viewWidth
               || this.graphics.Viewport.Height != this.viewHeight
               || this.zoom != zoom;

            if (isProjectionInvalidated)
            {
                this.viewWidth = this.graphics.Viewport.Width;
                this.viewHeight = this.graphics.Viewport.Height;

                var sx = scrollPos.X / 2f;
                var sy = scrollPos.Y / 2f;
                Matrix projection = Matrix.CreateOrthographicOffCenter(sx, sx + this.graphics.Viewport.Width, sy + this.graphics.Viewport.Height, sy, 0, 1) * Matrix.CreateScale(zoom);

                if (this.effect is BasicEffect)
                {
                    (this.effect as BasicEffect).Projection = projection;
                }
                else
                {
                    this.effect.Parameters["xViewProjection"].SetValue(projection);
                }
            }

            this.scrollPosition.X = scrollPos.X;
            this.scrollPosition.Y = scrollPos.Y;
            this.zoom = zoom;

            this.effect.Parameters["xSunVector"].SetValue(this.sunVector);
            if (this.isVertexBufferInvalidated)
            {
                this.vertexBuffer.SetData(this.vertexArray);
                this.isVertexBufferInvalidated = false;
                WorldRenderer.Instance.InvalidateShadows();
            }

            foreach (var id in EventManager.MovedColonists)
            {
                this.thingsInvalidated.AddIfNew(id);
            }

            foreach (var thing in this.thingsInvalidated.ToList()) this.UpdateThing(thing);
            thingsInvalidated.Clear();
        }

        public void InvalidateThing(int thingId)
        {
            if (this.thingRegister.ContainsKey(thingId)) this.thingsInvalidated.AddIfNew(thingId);
        }

        private void UpdateThing(int thingId)
        {
            if (!this.thingRegister.ContainsKey(thingId)) return;

            var thing = this.thingRegister[thingId];
            var colonist = thing as IColonist;
            if (colonist == null || colonist.IsDead)
            {
                this.RemoveThing(thingId);
                return;
            }

            var bufferPositions = this.thingBufferPositions.ContainsKey(thingId) ? this.thingBufferPositions[thingId] : new List<int>();
            if (!bufferPositions.Any())
            {
                this.AddThing(colonist);
                if (this.thingBufferPositions.ContainsKey(thingId))
                {
                    bufferPositions = this.thingBufferPositions[thingId];
                }
            }

            this.GenerateVertexBufferForColonist(colonist, bufferPositions);

            this.isVertexBufferInvalidated = true;
        }

        protected virtual void AddThing(IThing thing)
        {
            try
            {
                if (this.thingBufferPositions.ContainsKey(thing.Id)) this.RemoveThing(thing.Id);

                var positions = new List<int>();
                if (this.vertexBuffer?.VertexCount > 0)
                {
                    if (this.bufferGaps.Count == 0)
                    {
                        this.InitBuffers((int)(this.vertexGrowthFactor * this.vertexBuffer.VertexCount / 4));
                    }

                    positions.Add(this.bufferGaps.Pop());

                    if (this.thingBufferPositions.ContainsKey(thing.Id)) this.thingBufferPositions[thing.Id] = positions;
                    else this.thingBufferPositions.Add(thing.Id, positions);
                }

                if (!this.thingRegister.ContainsKey(thing.Id)) this.thingRegister.Add(thing.Id, thing);
                else this.thingRegister[thing.Id] = thing;

                foreach (var p in positions)
                {
                    var offsetInBytes = p * VertexPositionColorTexture.VertexDeclaration.VertexStride * 4;
                    this.vertexBuffer.SetData(offsetInBytes, this.vertexArray, p * 4, 4, VertexPositionColorTexture.VertexDeclaration.VertexStride);
                    this.highestUsedBufferIndex = Math.Max(this.highestUsedBufferIndex.GetValueOrDefault(), p);
                }

                this.InvalidateThing(thing.Id);
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot add thing of type {thing.GetType()} to renderer", ex);
            }
        }

        public void Clear()
        {
            this.thingBufferPositions.Clear();
            this.thingRegister.Clear();
            this.highestUsedBufferIndex = null;
            this.bufferGaps.Clear();
            this.vertexArray = null;
            if (this.graphics != null) this.InitBuffers(this.initialBufferCapacity, true);
        }

        protected virtual void RemoveThing(int thingId)
        {
            if (!this.thingBufferPositions.TryGetValue(thingId, out List<int> indexes))
            {
                if (this.thingRegister.ContainsKey(thingId)) this.thingRegister[thingId] = null;
                return;
            }

            if (indexes != null)
            {
                foreach (var index in indexes)
                {
                    var i = index * 4;
                    var texCoord = Vector2.Zero;
                    this.vertexArray[i] = new VertexPositionColorTexture { Position = Vector3.Zero, TextureCoordinate = texCoord };
                    this.vertexArray[i + 1] = new VertexPositionColorTexture { Position = Vector3.Zero, TextureCoordinate = texCoord };
                    this.vertexArray[i + 2] = new VertexPositionColorTexture { Position = Vector3.Zero, TextureCoordinate = texCoord };
                    this.vertexArray[i + 3] = new VertexPositionColorTexture { Position = Vector3.Zero, TextureCoordinate = texCoord };
                    this.vertexBuffer.SetData(i * VertexPositionColorTexture.VertexDeclaration.VertexStride, this.vertexArray, i, 4, VertexPositionColorTexture.VertexDeclaration.VertexStride);

                    this.bufferGaps.Push(index);
                    if (index == this.highestUsedBufferIndex)
                    {
                        this.highestUsedBufferIndex = index > 0 ? index - 1 : (int?)null;
                    }
                }
            }

            this.thingsInvalidated.RemoveIfExists(thingId);
            this.thingBufferPositions[thingId] = null;
            this.thingRegister[thingId] = null;
        }

        protected virtual void GenerateVertexBufferForColonist(IColonist colonist, List<int> bufferPositions)
        {
            var tile = colonist.MainTile;
            float cx = colonist.RenderPos.X;
            float cy = colonist.RenderPos.Y;

            var model = new List<Vector3>(4)
            {
                new Vector3(cx, cy, -1),
                new Vector3(cx, cy, -1),
                new Vector3(cx, cy, 13),
                new Vector3(cx, cy, 13)
            };

            var widthFactors = new List<float>(4)
            {
                -5f, 5f, 5f, -5f
            };

            var offset = bufferPositions[0] * 4;
            var array = this.vertexArray as VertexPositionColorTexture[];

            array[offset] = new VertexPositionColorTexture { Position = model[0], TextureCoordinate = new Vector2(0, 0), Color = new Color((widthFactors[0] + 50f) / 100f, 1, 1, colonist.RenderAlpha) };
            array[offset + 1] = new VertexPositionColorTexture { Position = model[1], TextureCoordinate = new Vector2(1, 0), Color = new Color((widthFactors[1] + 50f) / 100f, 1, 1, colonist.RenderAlpha) };
            array[offset + 2] = new VertexPositionColorTexture { Position = model[2], TextureCoordinate = new Vector2(1, 1), Color = new Color((widthFactors[2] + 50f) / 100f, 1, 1, colonist.RenderAlpha) };
            array[offset + 3] = new VertexPositionColorTexture { Position = model[3], TextureCoordinate = new Vector2(0, 1), Color = new Color((widthFactors[3] + 50f) / 100f, 1, 1, colonist.RenderAlpha) };

            WorldRenderer.Instance.InvalidateShadows();
        }

        public void InvalidateBuffers()
        {
            this.isVertexBufferInvalidated = true;
        }

        protected void InitBuffers(int capacity, bool clear = false)
        {
            var oldCapacity = 0;

            if (this.vertexBuffer?.IsDisposed == false)
            {
                this.vertexBuffer.Dispose();
            }

            if (this.indexBuffer?.IsDisposed == false)
            {
                this.indexBuffer.Dispose();
            }

            this.vertexBuffer = new VertexBuffer(this.graphics, VertexPositionColorTexture.VertexDeclaration, capacity * 4, BufferUsage.WriteOnly);
            this.indexBuffer = new IndexBuffer(this.graphics, typeof(int), capacity * 6, BufferUsage.WriteOnly);

            if (this.vertexArray == null)
            {
                this.vertexArray = new VertexPositionColorTexture[capacity * 4];
            }
            else
            {
                oldCapacity = this.vertexArray.Length / 4;
                Array.Resize(ref this.vertexArray, capacity * 4);

                for (int i = clear ? 0 : oldCapacity * 4; i < this.vertexArray.Length; i++)
                {
                    var texCoord = Vector2.Zero;
                    this.vertexArray[i] = new VertexPositionColorTexture { Position = Vector3.Zero, TextureCoordinate = texCoord };
                }

                this.isVertexBufferInvalidated = true;
            }

            int[] index = new int[capacity * 6];

            for (int i = 0, j = 0; j < capacity * 4; i += 6, j += 4)
            {
                index[i] = j;
                index[i + 1] = j + 1;
                index[i + 2] = j + 2;
                index[i + 3] = j + 2;
                index[i + 4] = j;
                index[i + 5] = j + 3;
            }

            this.indexBuffer.SetData(index);

            this.bufferGaps.Clear();
            for (int i = capacity - 1; i >= oldCapacity; --i)
            {
                this.bufferGaps.Push(i);
            }
        }

        public virtual void Draw()
        {
            if (this.vertexBuffer == null || this.vertexBuffer.IsDisposed || this.vertexBuffer.VertexCount < 4 || this.highestUsedBufferIndex == null || this.shadowDetail == 0)
            {
                return;
            }

            if (this.indexBuffer == null || this.indexBuffer.IsDisposed)
            {
                return;
            }

            this.graphics.SetVertexBuffer(vertexBuffer);
            this.graphics.Indices = indexBuffer;
            this.effect.CurrentTechnique.Passes[0].Apply();
            this.graphics.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, (1 + this.highestUsedBufferIndex.GetValueOrDefault()) * 2);
        }

        protected virtual void OnThingRemoved(object sender)
        {
            var thing = (sender as IColonist);
            if (thing is null) return;

            this.RemoveThing(thing.Id);
        }

        protected virtual void OnThingAdded(object sender)
        {
            var thing = (sender as IColonist);
            if (thing is null) return;

            if (!this.thingRegister.ContainsKey(thing.Id)) this.thingRegister.Add(thing.Id, thing);
            else this.thingRegister[thing.Id] = thing;

            if (!this.thingBufferPositions.ContainsKey(thing.Id))
            {
                this.AddThing(thing);
            }

            this.InvalidateThing(thing.Id);
        }

        protected virtual void LoadBasicEffect()
        {
            this.effect = content.Load<Effect>("Effects\\ShadowEffect").Clone();
            if (this.mainTexture != null) this.effect.Parameters["xTexture"].SetValue(this.mainTexture);
            this.effect.CurrentTechnique = this.effect.Techniques["TreeTrunkShadowTechnique"];
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
                if (this.indexBuffer != null) this.indexBuffer.Dispose();
                if (this.vertexBuffer != null) this.vertexBuffer.Dispose();
            }
        }

        #endregion IDisposable implementation
    }
}

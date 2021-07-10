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

    public class ShadowRendererOld : IRenderer, IDisposable
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
        protected Vector2 textureSize;
        protected Stack<int> bufferGaps = new Stack<int>();
        protected Dictionary<int, List<int>> thingBufferPositions = new Dictionary<int, List<int>>();
        protected Dictionary<int, IThingWithShadow> thingRegister = new Dictionary<int, IThingWithShadow>();
        protected HashSet<int> thingsRemoved = new HashSet<int>();
        protected int? highestUsedBufferIndex;
        protected WorldTime WorldTime;
        protected Vector3 sunVector;
        protected VertexPositionColorTexture[] vertexArray = null;
        protected bool isVertexBufferInvalidated = true;
        protected bool validateShadows = true;
        protected int shadowDetail = 2;

        public ShadowRendererOld()
        {
            EventManager.Subscribe(EventType.Shadow, EventSubType.Added, delegate (object obj) { this.OnShadowUpdated(obj); });
            EventManager.Subscribe(EventType.Shadow, EventSubType.Updated, delegate (object obj) { this.OnShadowUpdated(obj); });
            EventManager.Subscribe(EventType.Shadow, EventSubType.Removed, delegate (object obj) { this.OnShadowRemoved(obj); });
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
                this.LoadBasicEffect();
            }

            this.InitBuffers(this.initialBufferCapacity, true);

            foreach(var thing in this.thingRegister.Keys.ToList())
            {
                this.UpdateThing(thing);
            }
        }

        public virtual void Update(WorldTime time, Vector2f scrollPos, float zoom, Vector3 sunVector, int shadowDetail, bool isPaused = false)
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
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.effect != null) this.effect.Dispose();
                if (this.mainTexture != null) this.mainTexture.Dispose();
                if (this.indexBuffer != null) this.indexBuffer.Dispose();
                if (this.vertexBuffer != null) this.vertexBuffer.Dispose();
            }
        }

        private void UpdateThing(int thingId)
        {
            if (!this.thingRegister.ContainsKey(thingId)) return;

            var thing = this.thingRegister[thingId];
            if (thing == null) return;

            if (thing.ShadowModel.IsShadowInvalidated || !this.validateShadows)
            {
                var shadowModel = thing.ShadowModel.GetModel();
                var bufferPositions = this.thingBufferPositions.ContainsKey(thingId) ? this.thingBufferPositions[thingId] : new List<int>();
                if (bufferPositions != null)
                {
                    if (bufferPositions != null && ((shadowModel.Count > 0 && shadowModel.Count != bufferPositions.Count * 4)
                        || (thing.ThingType == ThingType.Tree && bufferPositions.Count == 0)
                        || (thing.ThingType == ThingType.Bush && bufferPositions.Count == 0)
                        || (this is PoleShadowRenderer && bufferPositions.Count == 0 && thing.ThingType.In(ThingType.WindTurbine, ThingType.RocketGantry))))
                    {
                        this.AddThing(thing);
                        if (this.thingBufferPositions.ContainsKey(thingId))
                        {
                            bufferPositions = this.thingBufferPositions[thingId];
                        }
                    }

                    if ((shadowModel.Count > 0 && shadowModel.Count == bufferPositions.Count * 4)
                        || (thing.ThingType == ThingType.Tree && bufferPositions.Count == 1)
                        || (thing.ThingType == ThingType.Bush && bufferPositions.Count == 6)
                        || (this is PoleShadowRenderer && thing.ThingType.In(ThingType.WindTurbine, ThingType.RocketGantry)))
                    {
                        this.GenerateVertexBufferForThing(thing, shadowModel, bufferPositions);
                    }

                    if (this.validateShadows && this.vertexBuffer?.VertexCount > 0) thing.ShadowModel.IsShadowInvalidated = false;

                    this.isVertexBufferInvalidated = true;
                }
            }
        }

        public virtual void AddThing(IThingWithShadow thing)
        {
            try
            {
                if (this.thingBufferPositions.ContainsKey(thing.Id)) this.RemoveThing(thing);

                var positions = new List<int>();
                if (this.vertexBuffer?.VertexCount > 0)
                {
                    var shadowModelCount = 1;
                    if (thing.ThingType == ThingType.Bush) shadowModelCount = 6;
                    else if (thing.ThingType == ThingType.RocketGantry && this is PoleShadowRenderer) shadowModelCount = 8;
                    else if (thing.ThingType != ThingType.Tree) shadowModelCount = thing.ShadowModel.GetModel().Count / 4;

                    for (int i = 0; i < shadowModelCount; ++i)
                    {
                        if (this.bufferGaps.Count == 0)
                        {
                            this.InitBuffers((int)(this.vertexGrowthFactor * this.vertexBuffer.VertexCount / 4));
                        }

                        positions.Add(this.bufferGaps.Pop());
                    }

                    if (this.thingBufferPositions.ContainsKey(thing.Id)) this.thingBufferPositions[thing.Id] = positions;
                    else this.thingBufferPositions.Add(thing.Id, positions);
                }

                if (!this.thingRegister.ContainsKey(thing.Id)) this.thingRegister.Add(thing.Id, thing);
                else this.thingRegister[thing.Id] = thing;

                thing.ShadowModel.IsShadowInvalidated = true;

                foreach (var p in positions)
                {
                    var offsetInBytes = p * VertexPositionColorTexture.VertexDeclaration.VertexStride * 4;
                    this.vertexBuffer.SetData(offsetInBytes, this.vertexArray, p * 4, 4, VertexPositionColorTexture.VertexDeclaration.VertexStride);
                    this.highestUsedBufferIndex = Math.Max(this.highestUsedBufferIndex.GetValueOrDefault(), p);
                }
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
            if (this.graphics != null) this.InitBuffers(this.initialBufferCapacity, true);
        }

        public virtual void RemoveThing(IThingWithShadow thing)
        {
            if (!this.IsThingTypeIncluded(thing.ThingType)) return;

            this.thingsRemoved.Add(thing.Id);

            if (!this.thingBufferPositions.TryGetValue(thing.Id, out List<int> indexes))
            {
                if (this.thingRegister.ContainsKey(thing.Id)) this.thingRegister[thing.Id] = null;
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

            thing.ShadowModel.IsShadowInvalidated = false;
            this.thingBufferPositions[thing.Id] = null;
            this.thingRegister[thing.Id] = null;
        }

        protected virtual void GenerateVertexBufferForThing(IThingWithShadow thing, List<Vector3> shadowModel, List<int> bufferPositions)
        {
            var array = this.vertexArray as VertexPositionColorTexture[];
            for (int i = 0; i < shadowModel.Count; i += 4)
            {
                var offset = bufferPositions[i / 4] * 4;
                var points = new List<Vector3>(4) { shadowModel[i], shadowModel[i + 1], shadowModel[i + 2], shadowModel[i + 3] };
                for (int j = 0; j < 4; ++j)
                {
                    array[offset + j] = new VertexPositionColorTexture { Position = points[j], TextureCoordinate = new Vector2(j == 1 || j == 2 ? 1 : 0, j == 2 || j == 3 ? 1 : 0), Color = new Color(Color.White, thing.ShadowAlpha) };
                }
            }

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

        protected virtual bool IsThingTypeIncluded(ThingType thingType)
        {
            return thingType != ThingType.Tree && thingType != ThingType.Bush && thingType != ThingType.Colonist && thingType != ThingType.WindTurbine;
        }

        protected virtual void OnShadowRemoved(object sender)
        {
            var thing = (sender as IThingWithShadow);
            if (this.IsThingTypeIncluded(thing.ThingType)) this.RemoveThing(thing);
        }

        protected virtual void OnShadowUpdated(object sender)
        {
            var thing = (sender as IThingWithShadow);
            if (thing is null || (thing.ThingType != ThingType.Tree && thing.ThingType != ThingType.Bush
                && !(this is PoleShadowRenderer) && thing.ShadowModel.GetModel()?.Count == 0) || !this.IsThingTypeIncluded(thing.ThingType)) return;

            thing.ShadowModel.IsShadowInvalidated = true;

            if (!this.thingRegister.ContainsKey(thing.Id)) this.thingRegister.Add(thing.Id, thing);
            else this.thingRegister[thing.Id] = thing;

            if (!this.thingBufferPositions.ContainsKey(thing.Id))
            {
                this.AddThing(thing);
            }

            this.UpdateThing(thing.Id);
        }

        protected void LoadCustomEffect(string effectPath, string texturePath)
        {
            this.mainTexture = content.Load<Texture2D>(texturePath);

            this.effect = content.Load<Effect>(effectPath).Clone();
            this.textureSize = new Vector2(mainTexture.Width, mainTexture.Height);

            this.effect.Parameters["xTexture"].SetValue(this.mainTexture);
        }

        protected virtual void LoadBasicEffect()
        {
            this.effect = content.Load<Effect>("Effects\\ShadowEffect").Clone();
            if (this.mainTexture != null)
            {
                this.effect.Parameters["xTexture"].SetValue(this.mainTexture);
                this.effect.CurrentTechnique = this.effect.Techniques["TexturedShadowTechnique"];
            }
            else
            {
                this.effect.CurrentTechnique = this.effect.Techniques["SimpleShadowTechnique"];
            }
        }
    }
}

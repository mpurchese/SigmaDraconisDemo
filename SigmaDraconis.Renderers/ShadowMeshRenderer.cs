namespace SigmaDraconis.Renderers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.Shared;
    using Shadows;
    using Shared;
    using World;
    using WorldInterfaces;

    public class ShadowMeshRenderer : IRenderer, IDisposable
    {
        private readonly Dictionary<int, IThing> thingRegister = new Dictionary<int, IThing>();
        private readonly List<ShadowBuffer> buffers = new List<ShadowBuffer>();
        private readonly Dictionary<int, ShadowBuffer> buffersByThing = new Dictionary<int, ShadowBuffer>();

        protected GraphicsDevice graphics;
        protected ContentManager content;
        protected Vector2f scrollPosition = new Vector2i(0, 0);
        protected float zoom;
        protected int viewWidth;
        protected int viewHeight;
        protected Effect effect;
        protected Texture2D mainTexture;
        protected HashSet<int> thingsInvalidated = new HashSet<int>();
        protected HashSet<int> thingsAlphaChanged = new HashSet<int>();
        
        protected Vector3 sunVector;
        protected int frame = 0;
        protected int framesPerUpdate;
        protected readonly ShadowMeshType meshType;
        protected int shadowDetail = 2;

        private readonly int bufferVertexCapacity;
        private readonly int bufferInitialIndexCapacity;
        private readonly int bufferIndexCapacityGrowStep;

        public int TriCount { get; private set; }

        public ShadowMeshRenderer(ShadowMeshType meshType, int bufferVertexCapacity, int bufferInitialIndexCapacity, int bufferIndexCapacityGrowStep, int framesPerUpdate = 1)
        {
            this.bufferVertexCapacity = bufferVertexCapacity;
            this.bufferInitialIndexCapacity = bufferInitialIndexCapacity;
            this.bufferIndexCapacityGrowStep = bufferIndexCapacityGrowStep;
            this.framesPerUpdate = framesPerUpdate;
            this.meshType = meshType;
            EventManager.Subscribe(EventType.Thing, EventSubType.Added, delegate (object obj) { this.OnThingAdded(obj); });
            EventManager.Subscribe(EventType.Thing, EventSubType.Removed, delegate (object obj) { this.OnThingRemoved(obj); });
            EventManager.Subscribe(EventType.GameExit, delegate (object obj) { this.OnGameExit(); });
        }

        protected bool IsThingRegistered(int thingId)
        {
            return this.thingRegister.ContainsKey(thingId);
        }

        private void OnGameExit()
        {
            this.Clear();
        }

        public virtual void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.graphics = graphicsDevice;
            this.content = contentManager;

            if (this.effect == null)
            {
                this.mainTexture = new Texture2D(graphicsDevice, 1, 1);
                Color[] color = new Color[1] { Color.Black };
                this.mainTexture.SetData(color);

                this.LoadBasicEffect();
            }

            if (!buffers.Any()) this.AddBuffer();

            foreach (var thing in this.thingRegister.Keys.ToList())
            {
                this.RemoveThing(thing);
                this.AddThing(World.GetThing(thing));
            }
        }

        public virtual void Update(Vector2f scrollPos, float zoom, Vector3 sunVector, int shadowDetail, bool isPaused = false)
        {
            if (shadowDetail != this.shadowDetail)
            {
                if (this.shadowDetail == 0)
                {
                    foreach (var kv in this.thingRegister)
                    {
                        this.thingsInvalidated.Add(kv.Key);
                    }
                }
                else if (shadowDetail != 0)
                {
                    foreach (var kv in this.thingRegister.Where(kv => ShadowManager.ThingsWithMultiDetailMeshes.Contains(kv.Value.ThingType)))
                    {
                        this.thingsInvalidated.Add(kv.Key);
                    }
                }

                this.shadowDetail = shadowDetail;
            }

            if (shadowDetail == 0) return;

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

            // Optimisation - framerate setting
            this.frame++;
            if ((frame - 1) % this.framesPerUpdate != 0) return;

            foreach (var thing in this.thingsInvalidated.ToList())
            {
                this.RemoveThing(thing);
                this.AddThing(World.GetThing(thing));
            }

            foreach (var thing in this.thingsAlphaChanged.ToList())
            {
                this.UpdateThingAlpha(World.GetThing(thing));
            }

            var shadowsInvalidated = false;
            foreach (var buffer in this.buffers.Where(b => b.IsInvalidated).ToList())
            {
                buffer.Validate();
                shadowsInvalidated = true;
            }

            if (shadowsInvalidated) WorldRenderer.Instance.InvalidateShadows();

            this.thingsInvalidated.Clear();
            this.thingsAlphaChanged.Clear();
        }

        public void InvalidateThing(int thingId)
        {
            if (this.thingRegister.ContainsKey(thingId)) this.thingsInvalidated.AddIfNew(thingId);
        }

        public void InvalidateAlpha(int thingId)
        {
            if (this.thingRegister.ContainsKey(thingId)) this.thingsAlphaChanged.AddIfNew(thingId);
        }

        protected virtual void AddThing(IThing thing)
        {
            if (thing == null) return;

            if (!this.thingRegister.ContainsKey(thing.Id)) this.thingRegister.Add(thing.Id, thing);
            else this.thingRegister[thing.Id] = thing;

            if (thing.ShadowAlpha < 0.01) return;

            try
            {
                var z = 0f;
                var scale = 1f;
                var alpha = thing.ShadowAlpha;

                if (thing is ILandingPod pod) z = pod.Altitude;
                else if (thing is ITree tree)
                {
                    z = tree.Height * 0.5f;
                    scale = 0.0078125f * Math.Min((int)tree.Height + 32, 128);
                }

                var translate = thing is IRenderOffsettable ro 
                    ? new Vector3(thing.MainTile.CentrePosition.X + ro.RenderPositionOffset.X, thing.MainTile.CentrePosition.Y + ro.RenderPositionOffset.Y, z)
                    : new Vector3(thing.MainTile.CentrePosition.X, thing.MainTile.CentrePosition.Y, z);

                if (this.buffers.Any() && this.buffers[0].VertexBuffer.VertexCount > 0)
                {
                    var frame = 0;
                    Direction? direction = null;
                    if (thing is IAnimatedThing ia) frame = ia.AnimationFrame;
                    if (thing is IResourceStack rs) frame = rs.ItemCount;
                    if (thing is IRotatableThing rt) direction = rt.Direction;

                    var meshes = ShadowManager.GetMeshes(thing.ThingType, frame, direction, this.meshType, this.shadowDetail);
                    var spaceNeeded = meshes.SelectMany(m => m.Points).Count();
                    if (spaceNeeded > this.bufferVertexCapacity) return;

                    var buffer = this.buffers.FirstOrDefault(b => b.FreeSpace >= spaceNeeded);
                    if (buffer == null) buffer = this.AddBuffer();

                    var vertexColour = this.GetVertexColour(thing, alpha);
                    this.buffersByThing.Add(thing.Id, buffer);
                    
                    foreach (var mesh in meshes) buffer.AddMesh(thing.Id, mesh, scale, translate, vertexColour);
                }
                else
                {
                    // Build later, when the buffer is ready
                    this.InvalidateThing(thing.Id);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot add thing of type {thing.GetType()} to renderer", ex);
            }
        }

        protected virtual Color GetVertexColour(IThing thing, float alpha)
        {
            return new Color(Color.White, alpha);
        }

        public void Clear()
        {
            this.thingRegister.Clear();
            this.buffersByThing.Clear();
            foreach (var buffer in this.buffers) buffer.Clear();
            if (this.graphics != null && !this.buffers.Any()) this.AddBuffer();
        }

        protected virtual void RemoveThing(int thingId)
        {
            if (this.buffersByThing.ContainsKey(thingId))
            {
                this.buffersByThing[thingId].RemoveThing(thingId);
                this.buffersByThing.Remove(thingId);
            }

            this.thingRegister.Remove(thingId);
        }

        protected virtual void UpdateThingAlpha(IThing thing)
        {
            if (thing == null) return;

            if (this.buffersByThing.ContainsKey(thing.Id))
            {
                if (thing.ShadowAlpha > 0.01) this.buffersByThing[thing.Id].UpdateThingAlpha(thing.Id, thing.ShadowAlpha);
                else this.RemoveThing(thing.Id);
            }
            else if (thing.ShadowAlpha > 0.01) this.AddThing(thing);
        }

        public void InvalidateBuffers()
        {
            this.buffers.ForEach(b => b.Invalidate());
        }

        private ShadowBuffer AddBuffer()
        {
            var newBuffer = new ShadowBuffer(this.graphics, this.bufferVertexCapacity, this.bufferInitialIndexCapacity, this.bufferIndexCapacityGrowStep);
            this.buffers.Add(newBuffer);
            return newBuffer;
        }

        public virtual void Draw()
        {
            this.TriCount = 0;
            if (this.shadowDetail == 0) return;

            foreach (var buffer in this.buffers.Where(b => b.CanDraw()))
            {
                this.graphics.Indices = buffer.IndexBuffer;
                this.graphics.SetVertexBuffer(buffer.VertexBuffer);
                this.effect.CurrentTechnique.Passes[0].Apply();
                this.graphics.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, buffer.TriangleCount);
                this.TriCount += buffer.TriangleCount;
            }
        }

        protected virtual void OnThingRemoved(object sender)
        {
            var thing = (sender as IThing);
            if (thing is null || !ShadowManager.ThingTypesByMeshType[this.meshType].Contains(thing.ThingType)) return;

            this.RemoveThing(thing.Id);
        }

        protected virtual void OnThingAdded(object sender)
        {
            var thing = (sender as IThing);
            if (thing is null || !ShadowManager.ThingTypesByMeshType[this.meshType].Contains(thing.ThingType)) return;

            if (!this.thingRegister.ContainsKey(thing.Id)) this.thingRegister.Add(thing.Id, thing);
            else this.thingRegister[thing.Id] = thing;

            if (!this.buffersByThing.ContainsKey(thing.Id)) this.AddThing(thing);
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
                this.buffers.ForEach(b => b.Dispose());
                if (this.mainTexture != null) this.mainTexture.Dispose();
            }
        }

        #endregion IDisposable implementation
    }
}

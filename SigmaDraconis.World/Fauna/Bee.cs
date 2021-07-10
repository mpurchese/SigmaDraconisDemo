namespace SigmaDraconis.World.Fauna
{
    using System;
    using System.Linq;
    using Draconis.Shared;
    using Shared;
    using Flora;
    using WorldInterfaces;
    using ProtoBuf;

    [ProtoContract]
    public class Bee : FlyingInsect
    {
        private int frame;
        private float angleOffset; // For making flying direction inaccurate and unpredictable

        [ProtoMember(1)]
        public int FlowerID { get; set; }

        public Bee() : base(ThingType.Bee)
        {
        }

        public Bee(ISmallTile tile) : base(ThingType.Bee, tile)
        {
        }

        public void Init()
        {
            this.RenderRow = this.MainTile.Row;
            this.Position = this.MainTile.TerrainPosition;
            this.UpdateRenderPos();
            this.PrevRenderRow = this.RenderRow;
            this.renderAlpha = 0;
            this.IsFadingIn = true;
            this.AnimationFrame = 1;
            this.affectsPathFinders = false;
            this.RaiseRendererUpdateEvent();
        }

        public override void AfterDeserialization()
        {
            this.affectsPathFinders = false;
            base.AfterDeserialization();
        }

        public override void AfterAddedToWorld()
        {
            this.UpdateRenderPos();
            this.PrevRenderRow = this.RenderRow;
            EventManager.EnqueueSoundAddEvent(this.id, "Bee");
            base.AfterAddedToWorld();
        }

        public override void BeforeRemoveFromWorld()
        {
            EventManager.EnqueueSoundRemoveEvent(this.id);
            base.BeforeRemoveFromWorld();
        }

        public override void Update()
        {
            base.Update();

            this.frame++;
            if (this.frame % 3 != 0) return;   // Performance optimisation

            var updateRenderer = false;
            var animFrame = this.AnimationFrame;
            var prevHeight = this.Height;
            var prevAngle = this.Angle;

            if (this.IsFadingIn)
            {
                this.renderAlpha += 0.03f;
                updateRenderer = true;
                if (this.renderAlpha >= 1.0f)
                {
                    this.renderAlpha = 1.0f;
                    this.IsFadingIn = false;
                }
            }
            else if (this.IsFadingOut)
            {
                this.renderAlpha -= 0.03f;
                updateRenderer = true;
                if (this.renderAlpha <= 0.0f)
                {
                    this.renderAlpha = 0.0f;
                    this.IsFadingOut = false;
                    if (this.FlowerID > 0)
                    {
                        if (World.GetThing(this.FlowerID) is SmallPlant3 plant) plant.BeeID = 0;
                    }

                    World.RemoveThing(this);
                }
            }

            if (this.Height > 0)// && this.frame % 3 == 0)
            {
                animFrame = animFrame == 1 ? 2 : 1;
                EventManager.EnqueueSoundUpdateEvent(this.id, false, 0.03f, 0, true);
            }
            else
            {
                animFrame = 3;
                EventManager.EnqueueSoundUpdateEvent(this.id, true, 0);
            }

            if (animFrame != this.AnimationFrame)
            {
                this.AnimationFrame = animFrame;
                updateRenderer = true;
            }

            if (this.Height > 0)  // I.e. we're flying
            {
                if (World.GetThing(this.FlowerID) is IPlant target)
                {
                    var distance = this.GetDistanceToPlant(target);
                    if (distance > 0.1f && this.Height < 100) this.Height += 3;
                    else if (distance < 0.1f && this.Height > 3) this.Height -= 3;
                    else if (distance < 0.03f) this.Height -= 3;

                    if (this.Height > 0 && distance >= 0.03f)
                    {
                        var a = this.GetDirectionToPlant(target);
                        if (distance < 0.5f) this.Angle = a;
                        else
                        {
                            // Turn incrementally
                            var a1 = Mathf.AngleBetween(this.Angle, a);
                            if (distance > 1f) a1 += this.angleOffset;
                            var sharpness = distance > 2f ? 0.0234375f * Mathf.PI : 0.046875f * Mathf.PI;
                            if (a1 >= sharpness)
                            {
                                this.Angle += sharpness;
                                if (this.Angle > 2f * Mathf.PI) this.Angle -= 2f * Mathf.PI;
                            }
                            else if (a1 <= -sharpness)
                            {
                                this.Angle -= sharpness;
                                if (this.Angle < 0f) this.Angle += 2f * Mathf.PI;
                            }
                        }

                        this.Speed = ((int)(distance * 1000)).Clamp(1, Math.Min(this.Speed + 1, 20)) * 3;
                        var vec = new Vector2f(0, -this.Speed / 1000f).Rotate(this.Angle);
                        this.Position += vec;
                        this.UpdateRenderPos();
                        updateRenderer = true;
                    }
                    else if (this.Height < 0) this.Height = 0;
                }
            }

            if (this.frame % 60 == 0 && !this.IsFadingIn && !this.IsFadingOut)
            {
                if (World.GetThing(this.FlowerID) == null || Rand.Next(10) == 0)
                {
                    var flowers = World.GetThings(ThingType.SmallPlant3)
                        .OfType<SmallPlant3>()
                        .Where(p => p is IAnimatedThing at && at.AnimationFrame >= 40 && !p.IsDead && p.Id != this.FlowerID && p.BeeID == 0 && this.GetDistanceToPlant(p) < 20f)
                        .ToList();

                    if (flowers.Any())
                    {
                        if (this.FlowerID > 0)
                        {
                            if (World.GetThing(this.FlowerID) is SmallPlant3 prevFlower) prevFlower.BeeID = 0;
                        }

                        var flower = flowers[Rand.Next(flowers.Count)];
                        this.FlowerID = flower.Id;
                        flower.BeeID = this.Id;
                        if (this.Height == 0) this.Height = 1;
                    }
                }

                this.angleOffset = (float)(Rand.NextDouble() - 0.5);
            }

            if (!updateRenderer && (this.Height != prevHeight || this.Angle != prevAngle)) updateRenderer = true;
            if (updateRenderer) this.RaiseRendererUpdateEvent();
        }

        private float GetDistanceToPlant(IPlant target)
        {
            return (target.MainTile.TerrainPosition - this.Position).Length();
        }

        private float GetDirectionToPlant(IPlant target)
        {
            return (target.MainTile.TerrainPosition - this.Position).Angle();
        }
    }
}

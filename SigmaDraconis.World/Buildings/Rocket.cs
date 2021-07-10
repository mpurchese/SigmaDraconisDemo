namespace SigmaDraconis.World.Buildings
{
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using Particles;
    using WorldInterfaces;

    [ProtoContract]
    public class Rocket : Building
    {
        private float altitude;

        [ProtoMember(1)]
        public bool IsLaunching { get; set; }

        [ProtoMember(2)]
        public float Altitude
        {
            get
            {
                return this.altitude;
            }
            set
            {
                if (this.altitude != value)
                {
                    if (this.mainTile != null) EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.Altitude), this.altitude, value, this.mainTile.Row, this.ThingType);
                    this.altitude = value;
                }
            }
        }

        [ProtoMember(3)]
        public float VerticalSpeed { get; set; }

        [ProtoMember(4)]
        public bool FiredLaunchEvent { get; set; }

        [ProtoMember(5)]
        public int LaunchCountdown { get; set; }

        public Rocket() : base(ThingType.Rocket)
        {
        }

        public Rocket(ISmallTile mainTile) : base(ThingType.Rocket, mainTile, 5)
        {
            this.Altitude = 0;
        }

        public override void BeforeRemoveFromWorld()
        {
            EventManager.EnqueueSoundRemoveEvent(this.id);
            base.BeforeRemoveFromWorld();
        }

        public void Launch()
        {
            this.IsLaunching = true;
            this.LaunchCountdown = 120;
            EventManager.EnqueueSoundAddEvent(this.id, "Rocket");
        }

        public override bool IncrementConstructionProgress(double amountPercent)
        {
            EventManager.RaiseEvent(EventType.Shadow, EventSubType.Updated, this);
            return base.IncrementConstructionProgress(amountPercent);
        }

        public override void UpdateShadowModel()
        {
            var shadowModel = new List<Vector3>();
            var x = this.MainTile.CentrePosition.X;
            var y = this.MainTile.CentrePosition.Y - 3f;

            // Fuselage, tapers at top
            this.AddVerticalCylinderShadowQuad(shadowModel, x, y, 2.8f, 1.4f, this.Altitude + 3, this.Altitude + 57, 16, false);
            this.AddVerticalCylinderShadowQuad(shadowModel, x, y, 2.8f, 0.4f, 1.4f, 0.2f, this.Altitude + 57, this.Altitude + 69, 16, false);

            // Tail fins
            this.AddShadowQuad(shadowModel, x, y, -5.7f, -2.9f, this.Altitude + 1.5f, 0, 0, this.Altitude + 4f, 0, 0, this.Altitude + 17f, -5.7f, -2.9f, this.Altitude + 3f);
            this.AddShadowQuad(shadowModel, x, y, 5.7f, -2.9f, this.Altitude + 1.5f, 0, 0, this.Altitude + 4f, 0, 0, this.Altitude + 17f, 5.7f, -2.9f, this.Altitude + 3f);
            this.AddShadowQuad(shadowModel, x, y, -5.7f, 2.9f, this.Altitude + 1.5f, 0, 0, this.Altitude + 4f, 0, 0, this.Altitude + 17f, -5.7f, 2.9f, this.Altitude + 3f);
            this.AddShadowQuad(shadowModel, x, y, 5.7f, 2.9f, this.Altitude + 1.5f, 0, 0, this.Altitude + 4f, 0, 0, this.Altitude + 17f, 5.7f, 2.9f, this.Altitude + 3f);

            this.ShadowModel.SetModel(shadowModel, this);
        }

        public override void Update()
        {
            if (this.IsReady && this.IsLaunching)
            {
                if (this.LaunchCountdown > 0)
                {
                    this.LaunchCountdown--;
                    if (this.LaunchCountdown == 0)
                    {
                        EventManager.RaiseEvent(EventType.RocketLaunchStart, this);
                    }

                    return;
                }

                // Produce fewer particles with higher alpha if framerate is low
                var alphaScale = 1;
                var fps = PerfMonitor.DrawFramesPerSecond;
                if (fps < 56)
                {
                    alphaScale = fps < 32 ? 4 : 2;
                }

                var smokeCount = 4 / alphaScale;
                var alpha = alphaScale * this.RenderAlpha;
                if (this.Altitude < 5f)
                {
                    alpha *= 2f;
                    smokeCount *= 2;
                }

                for (int i = 0; i < smokeCount; i++)
                {
                    RocketExhaustSimulator.AddParticle(this.mainTile, 1f, 0.5f, Altitude + 4f, -2f, alpha);
                    RocketExhaustSimulator.AddParticle(this.mainTile, 1f, -0.5f, Altitude + 4f, -2f, alpha);
                    RocketExhaustSimulator.AddParticle(this.mainTile, -1f, 0.5f, Altitude + 4f, -2f, alpha);
                    RocketExhaustSimulator.AddParticle(this.mainTile, -1f, -0.5f, Altitude + 4f, -2f, alpha);
                }

                this.Altitude += this.VerticalSpeed;

                this.VerticalSpeed += 0.0005f;

                if (this.Altitude > 1000 && this.RenderAlpha > 0)
                {
                    this.RenderAlpha -= 0.005f;
                    if (this.RenderAlpha <= 0f)
                    {
                        this.RenderAlpha = 0f;
                        World.RemoveThing(this);
                    }
                }
                else if (this.Altitude > 300 && !this.FiredLaunchEvent)
                {
                    EventManager.RaiseEvent(EventType.RocketLaunched, this);
                    this.FiredLaunchEvent = true;
                }

                var volume = Mathf.Min(5f, this.VerticalSpeed * 100f) - this.Altitude * 0.01f;
                if (volume < 0.5f) volume = 0.5f;
                volume *= this.renderAlpha;
                EventManager.EnqueueSoundUpdateEvent(this.id, volume < 0.001f, volume, this.Altitude);

                EventManager.RaiseEvent(EventType.Building, EventSubType.Updated, this);
                this.UpdateShadowModel();
            }

            base.Update();
        }
    }
}

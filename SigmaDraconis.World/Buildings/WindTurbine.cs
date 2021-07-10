namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using Draconis.Shared;
    using WorldInterfaces;
    using Shared;

    [ProtoContract]
    public class WindTurbine : Building, IEnergyGenerator
    {
        private float soundVolume;

        [ProtoMember(1)]
        private int timerCount = 0;

        [ProtoMember(2)]
        private int gameFramesPerAnimationFrame = 4;

        [ProtoMember(3)]
        private double windFactor;

        [ProtoMember(4)]
        public Energy EnergyGenRate { get; set; } = 0;

        [ProtoMember(5)]
        public string Status { get; set; }

        [ProtoMember(6)]
        public FactoryStatus FactoryStatus { get; set; }

        [ProtoMember(7)]
        public double FactoryProgress { get; protected set; }

        [ProtoMember(8)]
        private double currentSpeed;

        private bool newFrame = true;
        private bool isWindFactorInvalidated = true;

        public WindTurbine() : base(ThingType.WindTurbine)
        {
        }

        public WindTurbine(ISmallTile mainTile) : base(ThingType.WindTurbine, mainTile, 1)
        {
        }

        public override void Update()
        {
            if (this.currentSpeed > 0 || this.soundVolume > 0)
            {
                this.soundVolume = (float)this.currentSpeed.Clamp(this.soundVolume - this.definitionSoundFade, this.soundVolume + this.definitionSoundFade);
                EventManager.EnqueueSoundUpdateEvent(this.id, this.soundVolume < 0.001, this.soundVolume * this.definitionSoundVolume);
            }

            base.Update();
            this.newFrame = true;
        }

        public override void AfterConstructionComplete()
        {
            this.windFactor = this.CalculateWindFactor();
            EventManager.Subscribe(EventType.Timer1Second, delegate (object obj) { this.OnTimer(obj); });
            EventManager.Subscribe(EventType.Wind, delegate (object obj) { this.OnWindChanged(); });
            base.AfterConstructionComplete();
        }

        public override void AfterAddedToWorld()
        {
            if (this.IsReady)
            {
                this.windFactor = this.CalculateWindFactor();
                EventManager.Subscribe(EventType.Timer1Second, delegate (object obj) { this.OnTimer(obj); });
                EventManager.Subscribe(EventType.Wind, delegate (object obj) { this.OnWindChanged(); });
            }

            base.AfterAddedToWorld();
        }

        public Energy UpdateGenerator()
        {
            if (this.isWindFactorInvalidated) this.windFactor = this.CalculateWindFactor();

            this.currentSpeed = this.GetWindFraction();
            var result = this.IsRecycling ? 0 : Energy.FromKwH(Constants.WindTurbineEnergyProduction * this.currentSpeed);
            this.EnergyGenRate = result;

            return result / 3600;
        }

        private double GetWindFraction()
        {
            var wind = World.Wind * this.windFactor;
            var windFraction = Math.Max(0f, Math.Min(1f, (wind - Constants.WindTurbineMinWind) / (float)(Constants.WindTurbineMaxWind - Constants.WindTurbineMinWind)));
            return windFraction;
        }

        protected override void OnTimer(object sender)
        {
            if (this.IsRecycling) return;

            this.timerCount = this.timerCount + 1 < this.gameFramesPerAnimationFrame ? this.timerCount + 1 : 0;
            if (this.timerCount > 0) return;

            var speed = this.currentSpeed > 0.001 ? (int)(3 - (this.currentSpeed * 2.0)) : 4;
            if (this.gameFramesPerAnimationFrame > speed) this.gameFramesPerAnimationFrame--;
            else if (this.gameFramesPerAnimationFrame < speed) this.gameFramesPerAnimationFrame++;

            if (this.newFrame && this.gameFramesPerAnimationFrame < 4)
            {
                this.AnimationFrame = this.AnimationFrame < 30 ? this.AnimationFrame + 1 : 0;
                this.UpdateShadowModel();
                this.newFrame = false;
            }

            this.Status = gameFramesPerAnimationFrame < 4 ? "" : "Stopped: Not enough wind";
        }

        protected double CalculateWindFactor()
        {
            var result = 0.0;
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2; y <= 2; y++)
                {
                    var tx = this.MainTile.X + x;
                    var ty = this.MainTile.Y + y;
                    var distance = Math.Sqrt((x * x) + (y * y));
                    result += this.GetTileWindFactor(tx, ty, distance);
                }
            }

            if (result > 0) result = Math.Min(1.0, Math.Pow(result, 0.25));

            this.isWindFactorInvalidated = false;
            return Math.Max(0.0, result);
        }

        protected void OnWindChanged()
        {
            this.isWindFactorInvalidated = true;
        }

        protected double GetTileWindFactor(int x, int y, double distance)
        {
            var tile = World.GetSmallTile(x, y);
            if (tile == null) return 0.0;

            var modifier = Math.Min(220.0, tile.WindModifier);
            modifier = (int)(modifier + ((Math.Max(distance, 1.0)) - 1) * 96);
            modifier = Math.Max(0, 256 - modifier);

            return modifier / 2000.0;
        }

        public override string GetTextureName(int layer = 1)
        {
            // Animation frames changed in 0.0.9
            if (this.AnimationFrame < 1) this.AnimationFrame = 1;
            if (this.AnimationFrame > 30) this.AnimationFrame = 30;
            return base.GetTextureName();
        }

        public override void UpdateShadowModel()
        {
            // The support pole is rendered by PoleShadowRenderer.  Here we need to do the rest.
            var cx = this.MainTile.CentrePosition.X;
            var cy = this.MainTile.CentrePosition.Y;
            var x1 = cx - 5f;
            var y1 = cy - 5f;
            var x2 = cx + 5f;
            var y2 = cy + 5f;

            var model = new List<Vector3>
            {
                new Vector3(x1 + 0.33f, y1 - 0.33f, 16),
                new Vector3(x1 - 0.33f, y1 + 0.33f, 16),
                new Vector3(x2 - 0.33f, y2 + 0.33f, 16),
                new Vector3(x2 + 0.33f, y2 - 0.33f, 16),

                new Vector3(x2 - 0.33f, y1 - 0.33f, 16),
                new Vector3(x2 + 0.33f, y1 + 0.33f, 16),
                new Vector3(x1 + 0.33f, y2 + 0.33f, 16),
                new Vector3(x1 - 0.33f, y2 - 0.33f, 16),

                new Vector3(x1 + 0.33f, y1 - 0.33f, 25),
                new Vector3(x1 - 0.33f, y1 + 0.33f, 25),
                new Vector3(x2 - 0.33f, y2 + 0.33f, 25),
                new Vector3(x2 + 0.33f, y2 - 0.33f, 25),

                new Vector3(x2 - 0.33f, y1 - 0.33f, 25),
                new Vector3(x2 + 0.33f, y1 + 0.33f, 25),
                new Vector3(x1 + 0.33f, y2 + 0.33f, 25),
                new Vector3(x1 - 0.33f, y2 - 0.33f, 25),

                new Vector3(x1 + 0.33f, y1 - 0.33f, 15),
                new Vector3(x1 - 0.33f, y1 + 0.33f, 15),
                new Vector3(x1 - 0.33f, y1 + 0.33f, 26),
                new Vector3(x1 + 0.33f, y1 - 0.33f, 26),

                new Vector3(x1 - 0.33f, y2 - 0.33f, 15),
                new Vector3(x1 + 0.33f, y2 + 0.33f, 15),
                new Vector3(x1 + 0.33f, y2 + 0.33f, 26),
                new Vector3(x1 - 0.33f, y2 - 0.33f, 26),

                new Vector3(x2 - 0.33f, y1 - 0.33f, 15),
                new Vector3(x2 + 0.33f, y1 + 0.33f, 15),
                new Vector3(x2 + 0.33f, y1 + 0.33f, 26),
                new Vector3(x2 - 0.33f, y1 - 0.33f, 26),

                new Vector3(x2 + 0.33f, y2 - 0.33f, 15),
                new Vector3(x2 - 0.33f, y2 + 0.33f, 15),
                new Vector3(x2 - 0.33f, y2 + 0.33f, 26),
                new Vector3(x2 + 0.33f, y2 - 0.33f, 26)
            };

            // Now we need to rotate the model.  120 frames = 1 rotation.
            var rot = 360 * (this.AnimationFrame - 1) * 0.0174533f / 120f;

            var rotatedModel = new List<Vector3>();
            foreach (var item in model)
            {
                var p = RotatePoint(item.X, item.Y, cx, cy, -rot);
                rotatedModel.Add(new Vector3(p.X, p.Y, item.Z));
            }

            this.ShadowModel.SetModel(rotatedModel, this);
        }

        public override void UpdatePoleShadowModel()
        {
            var tile = this.MainTile;
            float cx = tile.CentrePosition.X;
            float cy = tile.CentrePosition.Y - 0.25f;
            float h1 = 0;
            float h2 = 26;

            var model = new List<Vector3>(4)
            {
                new Vector3(cx, cy, h1),
                new Vector3(cx, cy, h1),
                new Vector3(cx, cy, h2),
                new Vector3(cx, cy, h2)
            };

            var widthFactors = new List<float>(32)
            {
                -1.4f, 1.4f, 1.4f, -1.4f
            };

            this.SetPoleShadowModel(model, widthFactors);
        }

        static Vector2f RotatePoint(float px, float py, float cx, float cy, float angle)
        {
            float cosTheta = (float)Math.Cos(angle);
            float sinTheta = (float)Math.Sin(angle);
            return new Vector2f((cosTheta * (px - cx) - sinTheta * (py - cy) + cx),
                                (0.5f * (sinTheta * (px - cx) + cosTheta * (py - cy)) + cy));
        }
    }
}

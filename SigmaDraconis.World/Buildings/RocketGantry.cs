namespace SigmaDraconis.World.Buildings
{
    using Microsoft.Xna.Framework;
    using System.Linq;
    using System.Collections.Generic;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class RocketGantry : Building
    {
        public RocketGantry() : base(ThingType.RocketGantry)
        {
        }

        public RocketGantry(ISmallTile mainTile) : base(ThingType.RocketGantry, mainTile, 5)
        {
        }

        public override void Update()
        {
            if (this.AnimationFrame < 17 && this.mainTile.ThingsPrimary.Any(t => t.ThingType == ThingType.Rocket && (!(t as Rocket).IsLaunching || (t as Rocket).LaunchCountdown > 0)))
            {
                this.AnimationFrame++;
                this.UpdateShadowModel();
            }
            else if (this.AnimationFrame > 0 && this.mainTile.ThingsPrimary.All(t => t.ThingType != ThingType.Rocket || ((t as Rocket).IsLaunching && (t as Rocket).LaunchCountdown <= 0)))
            {
                this.AnimationFrame--;
                this.UpdateShadowModel();
            }

            base.Update();
        }

        public override bool IncrementConstructionProgress(double amountPercent)
        {
            EventManager.RaiseEvent(EventType.Shadow, EventSubType.Updated, this);
            return base.IncrementConstructionProgress(amountPercent);
        }

        public override void UpdateShadowModel()
        {
            // The vertical poles are done by PoleShadowRender.  Here we define the model for the horizontal sections.

            var x = this.MainTile.CentrePosition.X;
            var y = this.MainTile.CentrePosition.Y - 3f;

            var model = new List<Vector3>();
            var f1 = this.AnimationFrame * 0.16f;
            var f2 = this.AnimationFrame * 0.08f;

            for (int z = 22; z <= 48; z += 26)
            {
                this.AddHorizontalShadowQuad(model, x, y, -10.3f, 0.15f, -9.7f, -0.15f, 0.3f, 4.85f, -0.3f, 5.15f, z);
                this.AddHorizontalShadowQuad(model, x, y, -10.3f, -0.15f, -9.7f, 0.15f, 0.3f, -4.85f, -0.3f, -5.15f, z);
                this.AddHorizontalShadowQuad(model, x, y, 10.3f, 0.15f, 9.7f, -0.15f, -0.3f, 4.85f, 0.3f, 5.15f, z);
                this.AddHorizontalShadowQuad(model, x, y, 10.3f, -0.15f, 9.7f, 0.15f, -0.3f, -4.85f, 0.3f, -5.15f, z);

                this.AddHorizontalShadowQuad(model, x, y, -10f, -0.25f, -10f, 0.25f, f1 - 7.5f, 0.25f, f1 - 7.5f, -0.25f, z);
                this.AddHorizontalShadowQuad(model, x, y, -0.5f, -5f, 0.5f, -5f, 0.5f, f2 - 3.75f, -0.5f, f2 - 3.75f, z);
                this.AddHorizontalShadowQuad(model, x, y, 10f, -0.25f, 10f, 0.25f, 7.5f - f1, 0.25f, 7.5f - f1, -0.25f, z);
                this.AddHorizontalShadowQuad(model, x, y, -0.5f, 5f, 0.5f, 5f, 0.5f, 3.75f - f2, -0.5f, 3.75f - f2, z);

                // The red bits
                this.AddHorizontalShadowQuad(model, x, y, f1 - 7.5f, -0.5f, f1 - 7.5f, 0.5f, f1 - 5f, 0.5f, f1 - 5f, -0.5f, z);
                this.AddHorizontalShadowQuad(model, x, y, -1f, f2 - 3.75f, 1f, f2 - 3.75f, 1f, f2 - 2.5f, -1f, f2 - 2.5f, z);
                this.AddHorizontalShadowQuad(model, x, y, 7.5f - f1, -0.5f, 7.5f - f1, 0.5f, 5f - f1, 0.5f, 5f - f1, -0.5f, z);
                this.AddHorizontalShadowQuad(model, x, y, -1f, 3.75f - f2, 1f, 3.75f - f2, 1f, 2.5f - f2, -1f, 2.5f - f2, z);
            }

            this.ShadowModel.SetModel(model, this);
        }

        public override void UpdatePoleShadowModel()
        {
            var tile = this.MainTile;
            float cx = tile.CentrePosition.X;
            float cy = tile.CentrePosition.Y - 1f;
            float h1 = 0;
            float h2 = 60;

            var model = new List<Vector3>(16)
            {
                new Vector3(cx - 10, cy - 2, h1),
                new Vector3(cx - 10, cy - 2, h1),
                new Vector3(cx - 10, cy - 2, h2),
                new Vector3(cx - 10, cy - 2, h2),
                new Vector3(cx + 10, cy - 2, h1),
                new Vector3(cx + 10, cy - 2, h1),
                new Vector3(cx + 10, cy - 2, h2),
                new Vector3(cx + 10, cy - 2, h2),
                new Vector3(cx, cy - 7, h1),
                new Vector3(cx, cy - 7, h1),
                new Vector3(cx, cy - 7, h2),
                new Vector3(cx, cy - 7, h2),
                new Vector3(cx, cy + 3, h1),
                new Vector3(cx, cy + 3, h1),
                new Vector3(cx, cy + 3, h2),
                new Vector3(cx, cy + 3, h2),
            };

            var widthFactors = new List<float>(32)
            {
                -3f, 3f, 3f, -3f,
                -3f, 3f, 3f, -3f,
                -3f, 3f, 3f, -3f,
                -3f, 3f, 3f, -3f,
                -3f, 3f, 0.4f, -0.4f,
                -3f, 3f, 0.4f, -0.4f,
                -3f, 3f, 0.4f, -0.4f,
                -3f, 3f, 0.4f, -0.4f,
            };

            this.SetPoleShadowModel(model, widthFactors);
        }
    }
}

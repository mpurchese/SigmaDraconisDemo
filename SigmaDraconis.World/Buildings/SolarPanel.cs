namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System;
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using ProtoBuf;
    using WorldInterfaces;
    using Shared;

    [ProtoContract]
    public class SolarPanel : Building, IEnergyGenerator
    {
        [ProtoMember(2)]
        public Energy EnergyGenRate { get; set; }

        #region IEnergyGenerator implementation

        [ProtoMember(3)]
        public FactoryStatus FactoryStatus { get; set; }

        [ProtoMember(4)]
        public double FactoryProgress { get; protected set; }

        #endregion

        public SolarPanel() : base(ThingType.SolarPanelArray)
        {
        }

        public SolarPanel(ISmallTile mainTile) : base(ThingType.SolarPanelArray, mainTile, 2)
        {
            this.AnimationFrame = 31;
        }

        public Energy UpdateGenerator()
        {
            if (!this.IsReady)
            {
                this.EnergyGenRate = 0;
                return 0;
            }

            var result = this.GetEnergyPerHour();
            this.EnergyGenRate = result;

            // Sun tracking during generation hours
            if (result > 0)
            {
                var prevFrame = this.AnimationFrame;
                var frame = ((int)((1f - World.WorldTime.DayFraction - 0.375f) * 128)).Clamp(1, 31);
                if (frame != prevFrame)
                {
                    this.AnimationFrame = Math.Min(prevFrame + 1, Math.Max(prevFrame - 1, frame));
                    this.UpdateShadowModel();
                }
            }

            return result / 3600;
        }

        public override void UpdateShadowModel()
        {
            var x = this.MainTile.CentrePosition.X + 10.67f;
            var y = this.MainTile.CentrePosition.Y;

            List<Vector3> model;
            if (this.AnimationFrame == 0)
            {
                model = this.GetShadowModelFrame0(x, y);
            }
            else if (this.AnimationFrame == 15)
            {
                model = this.GetShadowModelFrame15(x, y);
            }
            else if (this.AnimationFrame == 31)
            {
                model = this.GetShadowModelFrame31(x, y);
            }
            else if (this.AnimationFrame < 15)
            {
                var model0 = this.GetShadowModelFrame0(x, y);
                var model15 = this.GetShadowModelFrame15(x, y);
                model = new List<Vector3>();
                var f15 = this.AnimationFrame / 15f;
                var f0 = 1 - f15;
                for (int i = 0; i < model0.Count; i++)
                {
                    model.Add(new Vector3((model0[i].X * f0) + (model15[i].X * f15), (model0[i].Y * f0) + (model15[i].Y * f15), (model0[i].Z * f0) + (model15[i].Z * f15)));
                }
            }
            else
            {
                var model15 = this.GetShadowModelFrame15(x, y);
                var model31 = this.GetShadowModelFrame31(x, y);
                model = new List<Vector3>();
                var f31 = (this.AnimationFrame - 15) / 16f;
                var f15 = 1 - f31;
                for (int i = 0; i < model15.Count; i++)
                {
                    model.Add(new Vector3((model31[i].X * f31) + (model15[i].X * f15), (model31[i].Y * f31) + (model15[i].Y * f15), (model31[i].Z * f31) + (model15[i].Z * f15)));
                }
            }

            this.ShadowModel.SetModel(model, this);
        }

        private List<Vector3> GetShadowModelFrame0(float cx, float cy)
        {
            var model = new List<Vector3>();
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    var x1 = cx + (4.2f * j) - (4.2f * i);
                    var y1 = cy - (2.1f * (i + j));
                    model.Add(new Vector3(x1 + 0.13f, y1 + 10.18f, 1f));
                    model.Add(new Vector3(x1 - 3.08f, y1 + 8.57f, 1f));
                    model.Add(new Vector3(x1 - 0.40f, y1 + 7.23f, 2.3f));
                    model.Add(new Vector3(x1 + 2.81f, y1 + 8.84f, 2.3f));
                }
            }

            return model;
        }

        private List<Vector3> GetShadowModelFrame15(float cx, float cy)
        {
            var model = new List<Vector3>();
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    var x1 = cx + (4.2f * j) - (4.2f * i);
                    var y1 = cy - (2.1f * (i + j));
                    model.Add(new Vector3(x1, y1 + 10.1f, 0.3f));
                    model.Add(new Vector3(x1 - 3.21f, y1 + 8.73f, 1.15f));
                    model.Add(new Vector3(x1, y1 + 7.36f, 2.05f));
                    model.Add(new Vector3(x1 + 3.21f, y1 + 8.73f, 1.15f));
                }
            }

            return model;
        }

        private List<Vector3> GetShadowModelFrame31(float cx, float cy)
        {
            var model = new List<Vector3>();
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    var x1 = cx + (4.2f * j) - (4.2f * i);
                    var y1 = cy - (2.1f * (i + j));
                    model.Add(new Vector3(x1 - 0.13f, y1 + 10.18f, 1f));
                    model.Add(new Vector3(x1 - 2.81f, y1 + 8.84f, 2.3f));
                    model.Add(new Vector3(x1 + 0.40f, y1 + 7.23f, 2.3f));
                    model.Add(new Vector3(x1 + 3.08f, y1 + 8.57f, 1f));
                }
            }

            return model;
        }

        private Energy GetEnergyPerHour()
        {
            var light = World.WorldLight;
            var lightFactor = Math.Min(1f, light.EveningLightFactor + light.MorningLightFactor);
            if (lightFactor <= 0) return 0;

            // Sun tracking
            var dayFraction = World.WorldTime.DayFraction;
            if (dayFraction < 0.5f) dayFraction = Math.Min(0.5f, dayFraction + 0.125f);
            else if (dayFraction > 0.5f) dayFraction = Math.Max(0.5f, dayFraction - 0.125f);

            return Energy.FromKwH(Constants.SolarPanelEnergyProduction * Math.Min(lightFactor, Math.Sin((dayFraction - 0.25f) * 2 * Math.PI)));
        }

        //private Energy GetEnergyPerHour()
        //{
        //    var light = World.WorldLight;
        //    var lightFactor = Math.Min(1f, light.EveningLightFactor + light.MorningLightFactor);
        //    if (lightFactor < 1f) lightFactor = Math.Max(0f, lightFactor - (1 - lightFactor) * 2f);
        //    this.Status = lightFactor < 0.05 ? "Not enough light" : "";
        //    return Energy.FromKwH(Constants.SolarPanelEnergyProduction * lightFactor);
        //}
    }
}

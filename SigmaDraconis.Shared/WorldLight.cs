namespace SigmaDraconis.Shared
{
    using Draconis.Shared;
    using Microsoft.Xna.Framework;
    using System;

    public class WorldLight
    {
        public float MorningLightFactor { get; private set; }
        public float EveningLightFactor { get; private set; }
        public float NightLightFactor { get; private set; }
        public float LightFactorN { get; private set; }
        public float LightFactorE { get; private set; }
        public float LightFactorS { get; private set; }
        public float LightFactorW { get; private set; }
        public float LightFactorT { get; private set; }
        public float LightFactorBlueness { get; private set; }
        public float Brightness { get; private set; }

        public Vector3 SunVector { get; private set; }
        public Vector3 AmbientLightColour { get; private set; }

        public void Update(WorldTime time)
        {
            this.MorningLightFactor = GetMorningTextureAlpha(time.DayFraction);
            this.EveningLightFactor = GetEveningTextureAlpha(time.DayFraction);
            this.NightLightFactor = 1f - (this.MorningLightFactor + this.EveningLightFactor);
            this.Brightness = this.MorningLightFactor + this.EveningLightFactor;

            var r = this.MorningLightFactor + this.EveningLightFactor + (0.3f * this.NightLightFactor);
            var g = this.MorningLightFactor + this.EveningLightFactor + (0.39f * this.NightLightFactor);
            var b = this.MorningLightFactor + this.EveningLightFactor + (0.5f * this.NightLightFactor);
            this.AmbientLightColour = new Vector3(r, g, b);

            double dayFraction = time.DayFraction;
            double sunAngle = (dayFraction - 0.25) * 2.0 * Math.PI;
            this.SunVector = new Vector3((float)Math.Cos(sunAngle), (float)Math.Sin(sunAngle) * 0.8f, -(float)Math.Sin(sunAngle) * 0.2f);
            this.SunVector.Normalize();

            // New shader light factors
            double angle = dayFraction * 2.0 * Math.PI;
            this.LightFactorN = 0;
            this.LightFactorE = (float)Math.Sin(angle).Clamp(0, 1) * (1 - this.NightLightFactor);
            this.LightFactorW = (float)(0.0 - Math.Sin(angle)).Clamp(0, 1) * (1 - this.NightLightFactor);
            this.LightFactorS = 0.2f * (1f - (this.LightFactorE + this.LightFactorW)) * (1 - this.NightLightFactor);
            this.LightFactorT = 0.8f * (1f - (this.LightFactorE + this.LightFactorW)) * (1 - this.NightLightFactor);
            this.LightFactorBlueness = this.NightLightFactor;
        }

        public static float GetEffectiveLight(float brightness)
        {
            var f = (brightness - 0.1f) * 1.5f;
            if (brightness > 0.5f) f -= (brightness - 0.5f) * 0.65f;
            return f.Clamp(0f, 1f);
        }

        public static int GetEffectiveLightPercent(float brightness)
        {
            return (int)(GetEffectiveLight(brightness) * 100f);
        }

        private static float GetMorningTextureAlpha(float dayFraction)
        {
            if (dayFraction >= 0.23125f && dayFraction < 0.28125f)
            {
                return (dayFraction - 0.23125f) * 20f;
            }
            else if (dayFraction >= 0.28125f && dayFraction < 0.71875f)
            {
                return 1f - ((dayFraction - 0.28125f) * 2.285714f);
            }
            else
            {
                return 0f;
            }
        }

        private static float GetEveningTextureAlpha(float dayFraction)
        {
            if (dayFraction >= 0.71875f && dayFraction < 0.76875f)
            {
                return 1f - ((dayFraction - 0.71875f) * 20f);
            }
            else if (dayFraction >= 0.28125f && dayFraction < 0.71875f)
            {
                return (dayFraction - 0.28125f) * 2.285714f;
            }
            else
            {
                return 0f;
            }
        }
    }
}

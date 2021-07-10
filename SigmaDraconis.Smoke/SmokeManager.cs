namespace SigmaDraconis.Smoke
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Microsoft.Xna.Framework;

    using Draconis.Shared;
    using Shared;

    public static class SmokeManager
    {
        private static readonly Dictionary<ThingType, List<SmokeModel>> smokeModels = new Dictionary<ThingType, List<SmokeModel>>();

        public static void Load(ThingType thingType, List<string> lines)
        {
            var model = new SmokeModel();
            var origin = new Vector3();
            var scale = 1f;
            var rotationByDirection = new Dictionary<Direction, float> { { Direction.None, 0f } };
            var originByDirection = new Dictionary<Direction, Vector3>();

            foreach (var l in lines)
            {
                var line = l.Contains('#') ? l.Substring(0, l.IndexOf('#')) : l;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length < 3 || fields[0] != "SET") continue;

                switch(fields[1])
                {
                    case "TYPE":
                        model.ParticleType = (SmokeParticleType)Enum.Parse(typeof(SmokeParticleType), fields[2]);
                        break;
                    case "RATE":
                        model.ProductionRate = float.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                        break;
                    case "LAYER":
                        model.Layer = int.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                        break;
                    case "SCALE":
                        scale = float.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                        break;
                    case "ORIGIN":
                        if (fields.Length >= 5)
                        {
                            origin.X = float.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                            origin.Y = float.Parse(fields[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                            origin.Z = float.Parse(fields[4], NumberStyles.Any, CultureInfo.InvariantCulture);
                            if (fields.Length >= 7 && fields[5] == "SCALE") scale = float.Parse(fields[6], NumberStyles.Any, CultureInfo.InvariantCulture);
                        }
                        break;
                    case "DIRECTION":
                        var direction = (Direction)Enum.Parse(typeof(Direction), fields[2]);
                        if (fields.Length >= 5 && fields[3] == "ROTATION")
                        {
                            var rotation = float.Parse(fields[4], NumberStyles.Any, CultureInfo.InvariantCulture);
                            rotationByDirection.Add(direction, rotation);
                        }
                        else if (fields.Length >= 7 && fields[3] == "ORIGIN")
                        {
                            var o = new Vector3
                            {
                                X = float.Parse(fields[4], NumberStyles.Any, CultureInfo.InvariantCulture),
                                Y = float.Parse(fields[5], NumberStyles.Any, CultureInfo.InvariantCulture),
                                Z = float.Parse(fields[6], NumberStyles.Any, CultureInfo.InvariantCulture)
                            };
                            originByDirection.Add(direction, o);
                        }
                        break;
                }
            }

            foreach (var kv in rotationByDirection)
            {
                var rotatedOrigin = Rotate(origin, kv.Value * Mathf.PI / 180f);
                model.OriginsByDirection.Add(kv.Key, TransformToWorld(rotatedOrigin, scale));
            }

            foreach (var kv in originByDirection)
            {
                if (!model.OriginsByDirection.ContainsKey(kv.Key)) model.OriginsByDirection.Add(kv.Key, TransformToWorld(kv.Value, scale));
            }

            if (!smokeModels.ContainsKey(thingType)) smokeModels.Add(thingType, new List<SmokeModel>());
            smokeModels[thingType].Add(model);
        }

        public static List<SmokeModel> Get(ThingType thingType)
        {
            return smokeModels.ContainsKey(thingType) ? smokeModels[thingType] : null;
        }

        private static Vector3 Rotate(Vector3 v, float angle)
        {
            float cosTheta = Mathf.Cos(angle);
            float sinTheta = Mathf.Sin(angle);
            return new Vector3(cosTheta * v.X - sinTheta * v.Y, sinTheta * v.X + cosTheta * v.Y, v.Z);
        }

        private static Vector3 TransformToWorld(Vector3 origin, float scale)
        {
            return new Vector3(7.111f * (origin.X + origin.Y) * scale, 3.5555f * (origin.X - origin.Y) * scale, 10f * origin.Z * scale);
        }
    }
}
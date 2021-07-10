namespace SigmaDraconis.Shadows
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Microsoft.Xna.Framework;

    using Draconis.Shared;

    using Shared;

    public static class ShadowManager
    {
        private static readonly Dictionary<ThingType, ShadowModel> shadowModels = new Dictionary<ThingType, ShadowModel>();
        private static readonly Dictionary<ThingType, List<ShadowMesh>> shadowMeshes = new Dictionary<ThingType, List<ShadowMesh>>();

        public static Dictionary<ShadowMeshType, HashSet<ThingType>> ThingTypesByMeshType { get; } = new Dictionary<ShadowMeshType, HashSet<ThingType>>();
        public static Dictionary<ThingType, HashSet<ShadowMeshType>> MeshTypesByThingType { get; } = new Dictionary<ThingType, HashSet<ShadowMeshType>>();
        public static HashSet<ThingType> ThingsWithMultiDetailMeshes { get; } = new HashSet<ThingType>();

        public static void Load(ThingType thingType, List<string> lines)
        {
            Direction? direction = null;
            List<int> frames = new List<int>();
            float offsetX = 0;
            float offsetY = 0;
            string texture = "Default";
            var offsets = new List<Vector2f>();
            var quads = new List<ShadowQuad>();
            var meshTemplates = new Dictionary<string, ShadowMesh>();
            var currentMeshName = "";
            ShadowMesh currentMesh = null;

            foreach (var l in lines)
            {
                var line = l.Contains('#') ? l.Substring(0, l.IndexOf('#')) : l;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (currentMesh != null)
                {
                    if (fields[0] == "P")
                    {
                        int id = int.Parse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                        float x = float.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                        float y = float.Parse(fields[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                        float z = float.Parse(fields[4], NumberStyles.Any, CultureInfo.InvariantCulture);
                        currentMesh.Points.Add(id, new Vector3(x, y, z));

                        if (fields.Length == 7)
                        {
                            float tx = float.Parse(fields[5], NumberStyles.Any, CultureInfo.InvariantCulture);
                            float ty = float.Parse(fields[6], NumberStyles.Any, CultureInfo.InvariantCulture);
                            currentMesh.TexCoords.Add(id, new Vector2(tx, ty));
                        }
                    }
                    else if (fields[0] == "T")
                    {
                        int x = int.Parse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                        int y = int.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                        int z = int.Parse(fields[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                        currentMesh.Triangles.Add(new Vector3i(x, y, z));
                    }
                    else if (fields[0] == "Q")
                    {
                        int a = int.Parse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                        int b = int.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                        int c = int.Parse(fields[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                        int d = int.Parse(fields[4], NumberStyles.Any, CultureInfo.InvariantCulture);
                        currentMesh.Triangles.Add(new Vector3i(a, b, c));
                        currentMesh.Triangles.Add(new Vector3i(c, d, a));
                    }
                    else if (fields[0] == "END")
                    {
                        meshTemplates.Add(currentMeshName, currentMesh);
                        currentMesh = null;
                    }

                    continue;
                }

                if (fields[0] == "SET")
                {
                    if (fields[1] == "FRAME")
                    {
                        frames.Clear();
                        var f = 0;
                        for (int i = 2; i < fields.Length; i++)
                        {
                            if (fields[i] == "TO")
                            {
                                i++;
                                var next = int.Parse(fields[i], NumberStyles.Any, CultureInfo.InvariantCulture);
                                for (int j = f + 1; j < next; j++)
                                {
                                    frames.Add(j);
                                }
                            }
                            else if (fields[i] == "AND" || fields[i] == "ALL") continue;   // Just use AND for readability

                            f = int.Parse(fields[i], NumberStyles.Any, CultureInfo.InvariantCulture);
                            frames.Add(f);
                        }
                    }
                    else if (fields[1] == "DIRECTION")
                    {
                        if (fields[2] == "ALL") direction = null;
                        else direction = (Direction)Enum.Parse(typeof(Direction), fields[2]);
                    }
                    else if (fields[1] == "OFFSET")
                    {
                        offsetX = float.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                        offsetY = float.Parse(fields[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    else if (fields[1] == "TEXTURE")
                    {
                        texture = fields[2];
                    }
                }
                else if (fields[0] == "Q")
                {
                    var nextFieldIndex = 13;
                    var newQuads = new List<ShadowQuad>();

                    var x1 = float.Parse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var y1 = float.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var z1 = float.Parse(fields[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var x2 = float.Parse(fields[4], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var y2 = float.Parse(fields[5], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var z2 = float.Parse(fields[6], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var x3 = float.Parse(fields[7], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var y3 = float.Parse(fields[8], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var z3 = float.Parse(fields[9], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var x4 = float.Parse(fields[10], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var y4 = float.Parse(fields[11], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var z4 = float.Parse(fields[12], NumberStyles.Any, CultureInfo.InvariantCulture);
                    newQuads.Add(new ShadowQuad
                    {
                        V1 = new Vector3(x1, y1, z1),
                        V2 = new Vector3(x2, y2, z2),
                        V3 = new Vector3(x3, y3, z3),
                        V4 = new Vector3(x4, y4, z4),
                        Direction = direction,
                        Frames = frames.ToList(),
                        Texture = texture
                    });

                    if (newQuads.Any())
                    {
                        foreach (var quad in newQuads)
                        {
                            while (fields.Count() > nextFieldIndex)
                            {
                                if (fields[nextFieldIndex] == "ALPHA") quad.Alpha = float.Parse(fields[nextFieldIndex + 1], NumberStyles.Any, CultureInfo.InvariantCulture);
                                if (fields[nextFieldIndex] == "FRAME") quad.Frames.Add(int.Parse(fields[nextFieldIndex + 1], NumberStyles.Any, CultureInfo.InvariantCulture));
                                if (fields[nextFieldIndex] == "TEXTURE") quad.Texture = fields[nextFieldIndex + 1];
                                nextFieldIndex += 2;
                            }

                            quads.Add(quad);
                            offsets.Add(new Vector2f(offsetX, offsetY));
                        }
                    }
                }
                else if (fields[0] == "QH")
                {
                    var nextFieldIndex = 10;
                    var newQuads = new List<ShadowQuad>();

                    var x1 = float.Parse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var y1 = float.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var x2 = float.Parse(fields[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var y2 = float.Parse(fields[4], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var x3 = float.Parse(fields[5], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var y3 = float.Parse(fields[6], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var x4 = float.Parse(fields[7], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var y4 = float.Parse(fields[8], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var z = float.Parse(fields[9], NumberStyles.Any, CultureInfo.InvariantCulture);
                    newQuads.Add(new ShadowQuad
                    {
                        V1 = new Vector3(x1, y1, z),
                        V2 = new Vector3(x2, y2, z),
                        V3 = new Vector3(x3, y3, z),
                        V4 = new Vector3(x4, y4, z),
                        Direction = direction,
                        Frames = frames.ToList(),
                        Texture = texture
                    });

                    if (newQuads.Any())
                    {
                        foreach (var quad in newQuads)
                        {
                            while (fields.Count() > nextFieldIndex)
                            {
                                if (fields[nextFieldIndex] == "ALPHA") quad.Alpha = float.Parse(fields[nextFieldIndex + 1], NumberStyles.Any, CultureInfo.InvariantCulture);
                                if (fields[nextFieldIndex] == "FRAME") quad.Frames.Add(int.Parse(fields[nextFieldIndex + 1], NumberStyles.Any, CultureInfo.InvariantCulture));
                                if (fields[nextFieldIndex] == "TEXTURE") quad.Texture = fields[nextFieldIndex + 1];
                                nextFieldIndex += 2;
                            }

                            quads.Add(quad);
                            offsets.Add(new Vector2f(offsetX, offsetY));
                        }
                    }
                }
                else if (fields[0] == "QV")
                {
                    var nextFieldIndex = 7;
                    var newQuads = new List<ShadowQuad>();

                    var x1 = float.Parse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var y1 = float.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var x2 = float.Parse(fields[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var y2 = float.Parse(fields[4], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var z1 = float.Parse(fields[5], NumberStyles.Any, CultureInfo.InvariantCulture);
                    var z2 = float.Parse(fields[6], NumberStyles.Any, CultureInfo.InvariantCulture);
                    newQuads.Add(new ShadowQuad
                    {
                        V1 = new Vector3(x1, y1, z1),
                        V2 = new Vector3(x2, y2, z1),
                        V3 = new Vector3(x2, y2, z2),
                        V4 = new Vector3(x1, y1, z2),
                        Direction = direction,
                        Frames = frames.ToList(),
                        Texture = texture
                    });

                    if (newQuads.Any())
                    {
                        foreach (var quad in newQuads)
                        {
                            while (fields.Count() > nextFieldIndex)
                            {
                                if (fields[nextFieldIndex] == "ALPHA") quad.Alpha = float.Parse(fields[nextFieldIndex + 1], NumberStyles.Any, CultureInfo.InvariantCulture);
                                if (fields[nextFieldIndex] == "FRAME") quad.Frames.Add(int.Parse(fields[nextFieldIndex + 1], NumberStyles.Any, CultureInfo.InvariantCulture));
                                if (fields[nextFieldIndex] == "TEXTURE") quad.Texture = fields[nextFieldIndex + 1];
                                nextFieldIndex += 2;
                            }

                            quads.Add(quad);
                            offsets.Add(new Vector2f(offsetX, offsetY));
                        }
                    }
                }
                else if (fields[0] == "ADD")
                {
                    var nextFieldIndex = 0;
                    var newQuads = new List<ShadowQuad>();
                    if (fields[1] == "CYLINDERV")
                    {
                        var points = int.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                        var cx = float.Parse(fields[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                        var cy = float.Parse(fields[4], NumberStyles.Any, CultureInfo.InvariantCulture);
                        var r1 = float.Parse(fields[5], NumberStyles.Any, CultureInfo.InvariantCulture);
                        var r2 = float.Parse(fields[6], NumberStyles.Any, CultureInfo.InvariantCulture);
                        var z1 = float.Parse(fields[7], NumberStyles.Any, CultureInfo.InvariantCulture);
                        var z2 = float.Parse(fields[8], NumberStyles.Any, CultureInfo.InvariantCulture);

                        for (int i = 0; i < points; i++)
                        {
                            var rad1 = Math.PI * 2.0 * (i / (double)points);
                            var rad2 = Math.PI * 2.0 * ((i + 1) / (double)points);
                            var x1 = (float)Math.Sin(rad1) * r1;
                            var y1 = (float)Math.Cos(rad1) * r1;
                            var x2 = (float)Math.Sin(rad2) * r1;
                            var y2 = (float)Math.Cos(rad2) * r1;
                            var x3 = (float)Math.Sin(rad2) * r2;
                            var y3 = (float)Math.Cos(rad2) * r2;
                            var x4 = (float)Math.Sin(rad1) * r2;
                            var y4 = (float)Math.Cos(rad1) * r2;
                            newQuads.Add(new ShadowQuad
                            {
                                V1 = new Vector3(x1, y1, z1),
                                V2 = new Vector3(x2, y2, z1),
                                V3 = new Vector3(x3, y3, z2),
                                V4 = new Vector3(x4, y4, z2),
                                Direction = direction,
                                Frames = frames.ToList(),
                                Texture = texture
                            });
                        }

                        nextFieldIndex = 9;
                    }
                    else if (fields[1] == "MESH")
                    {
                        var id = fields[2];
                        var angle = 0f;
                        var transforms = new List<IShadowTransform>();
                        var i = 3;
                        var type = ShadowMeshType.General;
                        var detailLevel = 0;
                        while (fields.Length > i)
                        {
                            if (fields[i] == "ROTATE")
                            {
                                angle = float.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture);
                                transforms.Add(new ShadowTransformRotate(angle));
                                i += 2;
                            }
                            else if (fields[i] == "SCALE")
                            {
                                var scale = float.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture);
                                transforms.Add(new ShadowTransformScale(scale, scale, scale));
                                i += 2;
                            }
                            else if (fields[i] == "SCALEXY")
                            {
                                var scale = float.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture);
                                transforms.Add(new ShadowTransformScale(scale, scale, 1f));
                                i += 2;
                            }
                            else if (fields[i] == "SCALEZ")
                            {
                                var scale = float.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture);
                                transforms.Add(new ShadowTransformScale(1f, 1f, scale));
                                i += 2;
                            }
                            else if (fields[i] == "TYPE")
                            {
                                type = (ShadowMeshType)Enum.Parse(typeof(ShadowMeshType), fields[i + 1]);
                                i += 2;
                            }
                            else if (fields[i] == "OFFSET")
                            {
                                transforms.Add(new ShadowTransformOffset(Parse(fields[i + 1]), Parse(fields[i + 2]), Parse(fields[i + 3])));
                                i += 4;
                            }
                            else if (fields[i] == "DETAIL")
                            {
                                ThingsWithMultiDetailMeshes.Add(thingType);
                                detailLevel = int.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture);
                                i += 2;
                            }
                            else throw new Exception("Unrecognised field " + fields[i] + " in shadow definition for " + thingType.ToString());
                        }
                        
                        var newMesh = new ShadowMesh(frames, meshTemplates[id], transforms, direction, type, detailLevel);
                        newMesh.TransformToWorld();

                        if (!shadowMeshes.ContainsKey(thingType)) shadowMeshes.Add(thingType, new List<ShadowMesh>());
                        shadowMeshes[thingType].Add(newMesh);

                        if (!ThingTypesByMeshType.ContainsKey(type)) ThingTypesByMeshType.Add(type, new HashSet<ThingType>());
                        ThingTypesByMeshType[type].Add(thingType);

                        if (!MeshTypesByThingType.ContainsKey(thingType)) MeshTypesByThingType.Add(thingType, new HashSet<ShadowMeshType>());
                        MeshTypesByThingType[thingType].Add(type);
                    }

                    if (newQuads.Any())
                    {
                        foreach (var quad in newQuads)
                        {
                            while (fields.Count() > nextFieldIndex)
                            {
                                if (fields[nextFieldIndex] == "ALPHA") quad.Alpha = float.Parse(fields[nextFieldIndex + 1], NumberStyles.Any, CultureInfo.InvariantCulture);
                                if (fields[nextFieldIndex] == "FRAME") quad.Frames.Add(int.Parse(fields[nextFieldIndex + 1], NumberStyles.Any, CultureInfo.InvariantCulture));
                                if (fields[nextFieldIndex] == "TEXTURE") quad.Texture = fields[nextFieldIndex + 1];
                                nextFieldIndex += 2;
                            }

                            quads.Add(quad);
                            offsets.Add(new Vector2f(offsetX, offsetY));
                        }
                    }
                }
                else if (fields[0] == "REPEAT" && fields[1] == "ALL")
                {
                    if (fields[2] == "MIRROR")
                    {
                        var mirrorX = fields[3].Contains('X');
                        var mirrorY = fields[3].Contains('Y');
                        var mirrorZ = fields[3].Contains('Z');
                        var transform = new Vector3(mirrorX ? -1 : 1, mirrorY ? -1 : 1, mirrorZ ? -1 : 1);
                        foreach (var quad in quads.ToList())
                        {
                            offsets.Add(new Vector2f(offsetX, offsetY));
                            quads.Add(new ShadowQuad
                            {
                                Alpha = quad.Alpha,
                                Direction = direction,
                                Frames = frames.ToList(),
                                V1 = quad.V1 * transform,
                                V2 = quad.V2 * transform,
                                V3 = quad.V3 * transform,
                                V4 = quad.V4 * transform,
                                Texture = texture
                            });
                        }
                    }
                    else if (fields[2] == "SWAP" && fields[3] == "XY")
                    {
                        foreach (var quad in quads.ToList())
                        {
                            offsets.Add(new Vector2f(offsetX, offsetY));
                            quads.Add(new ShadowQuad
                            {
                                Alpha = quad.Alpha,
                                Direction = direction,
                                Frames = frames.ToList(),
                                V1 = new Vector3(quad.V1.Y, quad.V1.X, quad.V1.Z),
                                V2 = new Vector3(quad.V2.Y, quad.V2.X, quad.V2.Z),
                                V3 = new Vector3(quad.V3.Y, quad.V3.X, quad.V3.Z),
                                V4 = new Vector3(quad.V4.Y, quad.V4.X, quad.V4.Z)
                            });
                        }
                    }
                }
                else if (fields[0] == "REPEAT" && fields[1] == "FIRST")
                {
                    if (fields[2] == "ROTATE")
                    {
                        var angle = float.Parse(fields[3], NumberStyles.Any, CultureInfo.InvariantCulture) * Mathf.PI / 180f;
                        offsets.Add(new Vector2f(offsetX, offsetY));
                        quads.Add(new ShadowQuad
                        {
                            Alpha = quads[0].Alpha,
                            Direction = direction,
                            Frames = frames.ToList(),
                            Texture = quads[0].Texture,
                            V1 = Rotate(quads[0].V1, angle),
                            V2 = Rotate(quads[0].V2, angle),
                            V3 = Rotate(quads[0].V3, angle),
                            V4 = Rotate(quads[0].V4, angle)
                        });
                    }
                }
                else if (fields[0] == "BEGIN" && fields[1] == "MESH")
                {
                    currentMeshName = fields[2];
                    currentMesh = new ShadowMesh(fields[2]);
                }
            }

            // Transform all quads into world coords
            for (int i = 0; i < quads.Count; i++)
            {
                var quad = quads[i];
                var offset = offsets[i];
                quad.V1 = Transform(quad.V1, offset);
                quad.V2 = Transform(quad.V2, offset);
                quad.V3 = Transform(quad.V3, offset);
                quad.V4 = Transform(quad.V4, offset);
            }

            if (quads.Any())
            {
                if (!shadowModels.ContainsKey(thingType)) shadowModels.Add(thingType, new ShadowModel(quads));
                else shadowModels[thingType].Quads.AddRange(quads.ToList());
            }
        }

        private static Vector3 Transform(Vector3 v, Vector2f offset)
        {
            return new Vector3((offset.X * 10.667f) + (7.111f * (v.X + v.Y)), (offset.Y * 5.333f) - (3.5555f * (v.X - v.Y)), v.Z * 10f);
        }

        private static Vector3 Rotate(Vector3 v, float angle)
        {
            float cosTheta = (float)Math.Cos(angle);
            float sinTheta = (float)Math.Sin(angle);
            return new Vector3(cosTheta * v.X - sinTheta * v.Y, sinTheta * v.X + cosTheta * v.Y, v.Z);
        }

        private static float Parse(string input)
        {
            return float.Parse(input, NumberStyles.Any, CultureInfo.InvariantCulture);
        }

        public static bool Contains(ThingType thingType)
        {
            return shadowModels.ContainsKey(thingType);
        }

        public static bool ContainsMesh(ThingType thingType)
        {
            return shadowMeshes.ContainsKey(thingType);
        }

        public static bool ContainsMeshWithFrames(ThingType thingType)
        {
            return shadowMeshes.ContainsKey(thingType) && shadowMeshes[thingType].Any(m => m.Frames.Any());
        }

        public static bool ContainsMeshWithFrames(ThingType thingType, ShadowMeshType shadowMeshType)
        {
            return shadowMeshes.ContainsKey(thingType) && shadowMeshes[thingType].Any(m => m.Frames.Any() && m.Type == shadowMeshType);
        }

        public static List<ShadowQuad> Get(ThingType thingType, Direction direction, int frame)
        {
            if (!shadowModels.ContainsKey(thingType)) return new List<ShadowQuad>();

            return shadowModels[thingType].Quads.Where(q => (q.Direction == null || q.Direction == direction) && (!q.Frames.Any() || q.Frames.Contains(frame))).ToList();
        }

        public static List<ShadowMesh> GetMeshes(ThingType thingType, int frame, Direction? direction, ShadowMeshType meshType, int detailLevel)
        {
            if (!shadowMeshes.ContainsKey(thingType)) return new List<ShadowMesh>();

            return shadowMeshes[thingType]
                .Where(q => (!q.Frames.Any() || q.Frames.Contains(frame)) && (!q.Direction.HasValue || q.Direction == direction) && q.Type == meshType && (q.DetailLevel == 0 || q.DetailLevel == detailLevel))
                .ToList();
        }

        public static int CountQuads(ThingType thingType, Direction direction, int frame)
        {
            if (!shadowModels.ContainsKey(thingType)) return 0;

            return shadowModels[thingType].Quads.Where(q => (q.Direction == null || q.Direction == direction) && (!q.Frames.Any() || q.Frames.Contains(frame))).Count();
        }
    }
}

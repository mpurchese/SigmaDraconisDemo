namespace SigmaDraconis.Config
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using Shared;

    public static class TextureAtlasManager
    {
        private static readonly Dictionary<TextureAtlasIdentifiers, Dictionary<int, TextureAtlasFrame>> framesByThingType = new Dictionary<TextureAtlasIdentifiers, Dictionary<int, TextureAtlasFrame>>();
        private static readonly Dictionary<TextureAtlasIdentifiers, Dictionary<string, TextureAtlasFrame>> framesByName = new Dictionary<TextureAtlasIdentifiers, Dictionary<string, TextureAtlasFrame>>();
        private const int maxFramesPerThingType = 1024;

        public static TextureAtlasFrame GetFrame(TextureAtlasIdentifiers atlasID, string frameName)
        {
            if (!framesByName.ContainsKey(atlasID)) LoadAtlas(atlasID);
            try
            {
                return framesByName[atlasID][frameName];
            }
            catch { return null; }
        }

        public static TextureAtlasFrame GetFrame(TextureAtlasIdentifiers atlasID, ThingType thingType, int frameNumber = 0)
        {
            if (!framesByThingType.ContainsKey(atlasID)) LoadAtlasForAnimatedThings(atlasID);
            return framesByThingType[atlasID][((int)thingType * maxFramesPerThingType) + frameNumber];
        }

        private static void LoadAtlas(TextureAtlasIdentifiers id)
        {
            var name = Enum.GetName(typeof(TextureAtlasIdentifiers), id);
            var path = Path.Combine("Config", $"{name}Atlas.csv");
            using (var sr = File.OpenText(path))
            {
                var atlas = new Dictionary<string, TextureAtlasFrame>();
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    var fields = line.Split(',');
                    TextureAtlasFrame newFrame = null;
                    if (fields.Length == 5)
                    {
                        newFrame = new TextureAtlasFrame { X = int.Parse(fields[1]), Y = int.Parse(fields[2]), Width = int.Parse(fields[3]), Height = int.Parse(fields[4]) };
                    }
                    else if (fields.Length >= 7)
                    {
                        newFrame = new TextureAtlasFrame { X = int.Parse(fields[1]), Y = int.Parse(fields[2]), Width = int.Parse(fields[3]), Height = int.Parse(fields[4]), CX = float.Parse(fields[5], NumberStyles.Any, CultureInfo.InvariantCulture), CY = float.Parse(fields[6], NumberStyles.Any, CultureInfo.InvariantCulture) };
                        if (fields.Length == 8 && fields[7] == "-1")
                        {
                            newFrame.FlipHorizontal = true;
                        }
                    }

                    if (newFrame != null) atlas.Add(fields[0], newFrame);
                }

                framesByName.Add(id, atlas);
            }
        }

        private static void LoadAtlasForAnimatedThings(TextureAtlasIdentifiers id)
        {
            var name = Enum.GetName(typeof(TextureAtlasIdentifiers), id);
            var path = Path.Combine("Config", $"{name}Atlas.csv");
            using (var sr = File.OpenText(path))
            {
                var atlas = new Dictionary<int, TextureAtlasFrame>();
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    var fields = line.Split(',');
                    if (fields.Length < 2) continue;

                    TextureAtlasFrame newFrame = null;
                    if (fields.Length == 6)
                    {
                        newFrame = new TextureAtlasFrame { X = int.Parse(fields[2]), Y = int.Parse(fields[3]), Width = int.Parse(fields[4]), Height = int.Parse(fields[5]) };
                    }
                    else if (fields.Length >= 8)
                    {
                        newFrame = new TextureAtlasFrame { X = int.Parse(fields[2]), Y = int.Parse(fields[3]), Width = int.Parse(fields[4]), Height = int.Parse(fields[5]), CX = float.Parse(fields[6], NumberStyles.Any, CultureInfo.InvariantCulture), CY = float.Parse(fields[7], NumberStyles.Any, CultureInfo.InvariantCulture) };
                        if (fields.Length == 9 && fields[8] == "-1")
                        {
                            newFrame.FlipHorizontal = true;
                        }
                    }

                    // Each thingtype has space for 1024 frames
                    var number = int.Parse(fields[1]) + ((int)Enum.Parse(typeof(ThingType), fields[0]) * maxFramesPerThingType);
                    if (newFrame != null) atlas.Add(number, newFrame);
                }

                framesByThingType.Add(id, atlas);
            }
        }
    }
}

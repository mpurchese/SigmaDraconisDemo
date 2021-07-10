namespace SigmaDraconis.Renderers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Config;
    using Shared;
    using World;
    using World.Blueprints;
    using World.Rooms;
    using WorldControllers;
    using WorldInterfaces;
    using SigmaDraconis.World.Flora;

    public class GeneralRenderer : RendererBase, IRenderer
    {
        private readonly RendererType rendererType;
        private readonly TextureAtlasIdentifiers atlasIdentifier;

        public GeneralRenderer(RendererType rendererType, TextureAtlasIdentifiers atlasIdentifier)
        {
            this.rendererType = rendererType;
            this.atlasIdentifier = atlasIdentifier;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            var texturePathRoot = this.GetTexturePathRoot();
            this.texture1 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>($"{texturePathRoot}_1");
            this.texture2 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>($"{texturePathRoot}_2");
            this.LoadGeneralEffectNew(this.texture1, this.texture2);

            EventManager.Subscribe(EventType.Thing, EventSubType.Added, delegate (object obj) { this.OnThingAddedOrRemoved(obj); });
            EventManager.Subscribe(EventType.Thing, EventSubType.Removed, delegate (object obj) { this.OnThingAddedOrRemoved(obj); });

            if (this.rendererType == RendererType.TreeTop)
            {
                this.effect.Parameters["xWarpPhase"].SetValue(DateTime.UtcNow.Millisecond / 159f);
                this.effect.Parameters["xWarpAmplitude"].SetValue(WeatherController.TreetopSwayAmplitude * 0.016f);
            }
        }

        public override void ReloadContent()
        {
            var texturePathRoot = this.GetTexturePathRoot();
            this.texture1 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>($"{texturePathRoot}_1");
            this.texture2 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>($"{texturePathRoot}_2");
            this.textureSize = new Vector2(texture1.Width, texture1.Height);
            this.effect.Parameters["xTexture1"].SetValue(texture1);
            this.effect.Parameters["xTexture2"].SetValue(texture2);

            base.ReloadContent();
        }

        public override void Update(WorldTime time, Vector2f scrollPos, float zoom, bool isPaused = false)
        {
            this.effect.CurrentTechnique = this.effect.Techniques[this.rendererType == RendererType.TreeTop ? "WarpTechnique" : "MainTechnique"];

            var light = World.WorldLight;
            this.effect.Parameters["xLightingDirectionFactors"].SetValue(new Vector4(light.LightFactorW, light.LightFactorT + light.LightFactorS, light.LightFactorE, 1));
            this.effect.Parameters["xLightingFactorBlueness"].SetValue(light.LightFactorBlueness);
            this.effect.Parameters["xAlpha"].SetValue(1f);

            if (this.rendererType == RendererType.TreeTop && !isPaused)
            {
                // Move in the wind
                this.effect.Parameters["xWarpPhase"].SetValue(DateTime.UtcNow.Millisecond / 159f);
                this.effect.Parameters["xWarpAmplitude"].SetValue(WeatherController.TreetopSwayAmplitude * 0.016f);
            }

            base.Update(time, scrollPos, zoom);
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var thingsLayer0 = new List<IThing>();
            var thingsLayer1 = new List<IThing>();
            var thingsLayer2 = new List<IThing>();

            float textureScale = (WorldRenderer.Instance.TextureRes == 1) ? 2f : 1f;
            float texScaleX = textureScale / this.textureSize.X;
            float texScaleY = textureScale / this.textureSize.Y;

            foreach (var thing in World.GetThingsByRow(rowNum).Concat(World.VirtualBlueprint.Where(b => b.MainTile?.Row == rowNum)))
            {
                var def = thing.Definition;
                if (def == null) continue;

                for (int a = 0; a < def.RendererTypes.Count; a++)
                {
                    var renderer = def.RendererTypes[a];
                    var layer = thing is IThingWithRenderLayer tr ? tr.RenderLayer: def.RendererLayers[a];

                    if (renderer == this.rendererType)
                    {
                        switch (layer)
                        {
                            case 0: thingsLayer0.Add(thing); break;
                            case 1: thingsLayer1.Add(thing); break;
                            case 2: thingsLayer2.Add(thing); break;
                        }
                    }
                }

                // NE and NW facing lab screens rendered first
                if (this.rendererType == RendererType.GeneralA && thing is ILab lab && !string.IsNullOrEmpty(lab.ScreenFrame) && lab is IRotatableThing ir && ir.Direction.In(Direction.NW, Direction.NE)) thingsLayer0.Add(thing);
                if (this.rendererType == RendererType.PlantsAndRocks && thing is IPlantWithAnimatedFlower flower && flower.FlowerRenderLayer.HasValue)
                {
                    if (flower.FlowerRenderLayer == 0) thingsLayer0.Add(thing);
                    else thingsLayer2.Add(thing);
                }

                // NE and NW facing fuel factory pipes rendered first
                if (this.rendererType == RendererType.GeneralA && thing.ThingType == ThingType.FuelFactory && thing is IRotatableThing ii)
                {
                    if (ii.Direction.In(Direction.NW, Direction.NE)) thingsLayer0.Add(thing);
                    else thingsLayer2.Add(thing);
                }

                // NE and NW facing shore pump pipes are GeneralA, SE and SW are GeneralD
                if (thing.ThingType == ThingType.ShorePump && thing is IRotatableThing sp)
                { 
                    if (this.rendererType == RendererType.GeneralA && (sp.Direction == Direction.NW || sp.Direction == Direction.NE)) thingsLayer1.Add(thing);
                    else if (this.rendererType == RendererType.GeneralD && (sp.Direction == Direction.SW || sp.Direction == Direction.SE)) thingsLayer1.Add(thing);
                }

                // TODO: These should be defined in the definition file, not here
                if (this.rendererType == RendererType.GeneralA && thing.ThingType.In(ThingType.FoodDispenser, ThingType.KekDispenser, ThingType.WaterDispenser)) thingsLayer1.Add(thing);
                else if (this.rendererType == RendererType.GeneralB && thing is ILab lab2 && !string.IsNullOrEmpty(lab2.ScreenFrame) && lab2 is IRotatableThing ir2 && ir2.Direction.In(Direction.SW, Direction.SE)) thingsLayer1.Add(thing);
            }

            var size = (thingsLayer0.Count + thingsLayer1.Count + thingsLayer2.Count) * 4;

            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[size];
            int i = 0;

            foreach (Thing thing in thingsLayer0)
            {
                var textureName = thing.GetTextureName(0);
                var frame = TextureAtlasManager.GetFrame(this.atlasIdentifier, textureName);
                if (frame == null)
                {
                    i += 4;
                    continue;
                }

                var scale = 0.25f;

                var tile = thing.MainTile;
                var x = tile.CentrePosition.X - (scale * frame.CX);
                var y = tile.CentrePosition.Y - (scale * frame.CY);

                if (thing is IRenderOffsettable ro)
                {
                    x += ro.RenderPositionOffset.X;
                    y += ro.RenderPositionOffset.Y;
                }

                float width = frame.Width * scale;
                float height = frame.Height * scale;

                float tx = frame.X * texScaleX;
                float ty = frame.Y * texScaleY;
                float tw = frame.Width * texScaleX;
                float th = frame.Height * texScaleY;

                var c = this.GetVertexColor(thing);

                vertex[i] = new VertexPositionColorTexture(new Vector3(x, y, 0), c, new Vector2(tx, ty));
                vertex[i + 1] = new VertexPositionColorTexture(new Vector3(x + width, y, 0), c, new Vector2(tx + tw, ty));
                vertex[i + 2] = new VertexPositionColorTexture(new Vector3(x, y + height, 0), c, new Vector2(tx, ty + th));
                vertex[i + 3] = new VertexPositionColorTexture(new Vector3(x + width, y + height, 0), c, new Vector2(tx + tw, ty + th));
                i += 4;
            }

            foreach (Thing thing in thingsLayer1)
            {
                string textureName;
                if (thing.ThingType == ThingType.FoodDispenser && this.rendererType == RendererType.GeneralA) textureName = "FoodDispenser";
                else if (thing.ThingType == ThingType.WaterDispenser && this.rendererType == RendererType.GeneralA) textureName = "WaterDispenser";
                else if (thing.ThingType == ThingType.KekDispenser && this.rendererType == RendererType.GeneralA) textureName = "KekDispenser";
                else if (thing.ThingType == ThingType.ShorePump && this.rendererType != RendererType.GeneralB && thing is IRotatableThing rt) textureName = $"ShorePump_{rt.Direction.ToString()}";
                else if (thing is ILab lab && this.rendererType == RendererType.GeneralB) textureName = lab.ScreenFrame;
                else textureName = thing.GetTextureName(1);

                var frame = TextureAtlasManager.GetFrame(this.atlasIdentifier, textureName);
                if (frame == null)
                {
                    i += 4;
                    continue;
                }

                var scale = 0.25f;

                var tile = thing.MainTile;
                var isTreeTrunk = thing.ThingType == ThingType.Tree && this.rendererType != RendererType.TreeTop;
                var x = tile.CentrePosition.X + (isTreeTrunk ? (thing as Tree).RenderPositionOffset.X : 0 - (scale * frame.CX));
                var y = tile.CentrePosition.Y + (isTreeTrunk ? (thing as Tree).RenderPositionOffset.Y : 0 - (scale * frame.CY));
                if (thing.Definition.Size.X > 1 && thing.ThingType != ThingType.Rocket && thing.ThingType != ThingType.RocketGantry) x += (thing.Definition.Size.X - 1) * 10.667f;
                if (thing is ILandingPod pod) y -= pod.Altitude;

                if (!isTreeTrunk && thing is IRenderOffsettable ro)
                {
                    x += ro.RenderPositionOffset.X;
                    y += ro.RenderPositionOffset.Y;
                }

                float width = 0f;
                float height = 0f;
                if (thing is Tree tree)
                {
                    if (this.rendererType == RendererType.TreeTop)
                    {
                        width = 0.8f * Math.Min((int)tree.Height + 32, 128);
                        height = 0.5f * width;
                        x = tile.CentrePosition.X + tree.RenderPositionOffset.X + 0.25f + (0.5f * tree.RenderSize.X) - (width / 2);
                        y = tree.MainTile.CentrePosition.Y + tree.RenderPositionOffset.Y + (0.5f * (60 - height)) - 26;
                    }
                    else
                    {
                        width = tree.RenderSize.X;
                        height = tree.Height;
                    }
                }
                else
                {
                    width = frame.Width * scale;
                    height = frame.Height * scale;
                }

                float tx = frame.X * texScaleX;
                float ty = frame.Y * texScaleY;
                float tw = frame.Width * texScaleX;
                float th = frame.Height * texScaleY;

                var c = this.rendererType == RendererType.TreeTop ? this.GetVertexColorForTreeTop(thing) : this.GetVertexColor(thing);

                vertex[i] = new VertexPositionColorTexture(new Vector3(x, y, 0), c, new Vector2(tx, ty));
                vertex[i + 1] = new VertexPositionColorTexture(new Vector3(x + width, y, 0), c, new Vector2(tx + tw, ty));
                vertex[i + 2] = new VertexPositionColorTexture(new Vector3(x, y + height, 0), c, new Vector2(tx, ty + th));
                vertex[i + 3] = new VertexPositionColorTexture(new Vector3(x + width, y + height, 0), c, new Vector2(tx + tw, ty + th));

                i += 4;
            }

            // Environment control unit screens
            foreach (var thing in thingsLayer2)
            {
                var textureName = thing.GetTextureName(2);
                var frame = TextureAtlasManager.GetFrame(this.atlasIdentifier, textureName);
                if (frame == null)
                {
                    i += 4;
                    continue;
                }

                var scale = 0.25f;

                var tile = thing.MainTile;
                var x = tile.CentrePosition.X - (scale * frame.CX);
                var y = tile.CentrePosition.Y - (scale * frame.CY);

                if (thing is IRenderOffsettable ro)
                {
                    x += ro.RenderPositionOffset.X;
                    y += ro.RenderPositionOffset.Y;
                }

                float width = frame.Width * scale;
                float height = frame.Height * scale;

                float tx = frame.X * texScaleX;
                float ty = frame.Y * texScaleY;
                float tw = frame.Width * texScaleX;
                float th = frame.Height * texScaleY;

                var c = GetVertexColor(thing, thing is IEnvironmentControl);

                vertex[i] = new VertexPositionColorTexture(new Vector3(x, y, 0), c, new Vector2(tx, ty));
                vertex[i + 1] = new VertexPositionColorTexture(new Vector3(x + width, y, 0), c, new Vector2(tx + tw, ty));
                vertex[i + 2] = new VertexPositionColorTexture(new Vector3(x, y + height, 0), c, new Vector2(tx, ty + th));
                vertex[i + 3] = new VertexPositionColorTexture(new Vector3(x + width, y + height, 0), c, new Vector2(tx + tw, ty + th));

                i += 4;
            }

            this.SetBufferData(rowNum, vertex, size);
        }

        private string GetTexturePathRoot()
        {
            var category = (this.rendererType == RendererType.PlantsAndRocks || this.rendererType == RendererType.TreeTop) ? "PlantsAndRocks" : "Buildings";
            var atlasName = Enum.GetName(typeof(TextureAtlasIdentifiers), this.atlasIdentifier);
            return WorldRenderer.Instance.TextureRes == 0 ? $"{category}\\LoRes\\{atlasName}" : $"{category}\\{atlasName}";
        }

        private Color GetVertexColor(IThing thing, bool alphaSqrt = false)
        {
            var alpha = thing is Blueprint ? (thing as Blueprint).ColourA : thing.RenderAlpha;
            if (alphaSqrt) alpha = (float)Math.Sqrt(alpha);

            var tileIndex = thing.MainTileIndex;
            if (thing.ThingType == ThingType.Door || (thing.ThingType == ThingType.ShorePump && this.rendererType != RendererType.GeneralB))
            {
                tileIndex = thing.MainTile.GetTileToDirection((thing as IRotatableThing).Direction).Index;
            }

            var room = RoomManager.GetRoom(tileIndex);
            var light = thing.AllTiles.Select(t => t.LightSources.Values.Any() ? t.LightSources.Values.Select(v => v.IsOn ? v.Amount : 0).Max() : 0f).Average();
            var artificialLight = Mathf.Max(room?.RenderTopLight ?? 0f, light);
            artificialLight = Mathf.Min(artificialLight, World.WorldLight.NightLightFactor);

            var blueness = (thing is ILab lab && this.rendererType == RendererType.GeneralB) ? 0f : 1f;
            return new Color(artificialLight, 0f, blueness, alpha);
        }

        // Tree tops ignore lamp light
        private Color GetVertexColorForTreeTop(IThing thing)
        {
            var alpha = thing is Blueprint ? (thing as Blueprint).ColourA : thing.RenderAlpha;

            var room = RoomManager.GetRoom(thing.MainTileIndex);
            var artificialLight = Mathf.Min(room?.RenderTopLight ?? 0f, World.WorldLight.NightLightFactor);
            var tree = (thing as ITree) ?? thing.MainTile.ThingsPrimary.OfType<ITree>().FirstOrDefault();
            var warpPhase = tree?.TreeTopWarpPhase ?? 0f;
            
            return new Color(artificialLight, warpPhase, 1f, alpha);
        }

        protected void OnThingAddedOrRemoved(object sender)
        {
            var thing = (sender as IThing);
            if (thing?.MainTile?.Row == null || thing.Definition == null || !thing.Definition.RendererTypes.Contains(this.rendererType)) return;

            this.InvalidateRow(thing.MainTile.Row);
        }
    }
}

namespace SigmaDraconis.Renderers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Config;
    using Settings;
    using Shared;
    using Shadows;
    using UI;
    using UI.Managers;
    using World;
    using World.Blueprints;
    using World.Particles;
    using World.Zones;
    using WorldInterfaces;

    public class WorldRenderer : IRenderer, IDisposable
    {
        // Shadows
        private RenderTarget2D renderTargetForShadows;
        private SpriteBatch shadowSpriteBatch;
        private Texture2D shadowTexture;

        // Water
        private SpriteBatch waterSpriteBatch;
        private Texture2D waterTexture;

        // Night shade
        private Effect nightShadeEffect;

        private bool isShadowsInvalidated = true;
        protected Vector2f scrollPosition = new Vector2i(0, 0);
        protected float zoom;
        protected int viewWidth;
        protected int viewHeight;

        private readonly List<RendererBase> basicRenderers = new List<RendererBase>();
        private readonly List<ShadowRendererOld> shadowRenderersOld = new List<ShadowRendererOld>();
        private readonly List<ShadowRenderer> shadowRenderers = new List<ShadowRenderer>();
        private readonly List<ShadowMeshRenderer> shadowMeshRenderers = new List<ShadowMeshRenderer>();
        private Dictionary<ShadowMeshType, ShadowMeshRenderer> shadowMeshRenderersByMeshType = new Dictionary<ShadowMeshType, ShadowMeshRenderer>();
        private readonly List<IRenderer> otherRenderers = new List<IRenderer>();

        // Basic renderers
        private readonly AnimalsRenderer animalsRenderer;
        private readonly BirdsRenderer birdsRenderer;
        private readonly BlueprintRenderer blueprintRenderer;
        private readonly ColonistBodyRenderer colonistBodyRendererLayer1;
        private readonly ColonistBodyRenderer colonistBodyRendererLayer2;
        private readonly ColonistHeadRenderer colonistHeadRendererLayer1;
        private readonly ColonistHeadRenderer colonistHeadRendererLayer2;
        private readonly ColonistCarryItemRenderer colonistCarryRendererLayer1;
        private readonly ColonistCarryItemRenderer colonistCarryRendererLayer2;
        private readonly CropIconRenderer cropIconRenderer;
        private readonly FlyingInsectsRenderer flyingInsectsRenderer;
        private readonly MicobotParticleRenderer deconstructParticleRenderer;
        private readonly FishRenderer fishRenderer;
        private readonly JobIconRenderer jobIconRenderer;
        private readonly GeneralRenderer generalRendererA;
        private readonly GeneralRenderer generalRendererB;
        private readonly GeneralRenderer generalRendererC;
        private readonly GeneralRenderer plantsAndRocksRenderer;
        private readonly GeneralRenderer generalRendererD;
        private readonly GeologyParticleRenderer geologyParticleRendererLayer1;
        private readonly GeologyParticleRenderer geologyParticleRendererLayer2;
        private readonly ProgressBarRenderer progressBarRenderer;
        private readonly RocketRenderer rocketRenderer;
        private readonly RocketExhaustRenderer rocketExhaustRendererLayer1;
        private readonly RocketExhaustRenderer rocketExhaustRendererLayer2;
        private readonly EnvironmentControlFanRenderer environmentControlFanRenderer;
        private readonly RoofRenderer roofRenderer;
        private readonly RoofBlueprintRenderer roofBlueprintRenderer;
        private readonly EnvironmentControlFanBlueprintRenderer EnvironmentControlFanBlueprintRenderer;
        private readonly SmokeRenderer smokeRenderer;
        private readonly TerrainGridRenderer terrainGridRenderer;
        private readonly GeneralRenderer treeTopRenderer;
        private readonly TreeTopBlueprintRenderer treeTopHarvestingRenderer;
        private readonly WallRenderer wallRenderer;
        

        // Shadow renderers
        private readonly ShadowRendererOld generalShadowRendererOld;
        private readonly WindTurbineShadowRenderer windTurbineShadowRenderer;
        private readonly ShadowRenderer generalShadowRenderer;
        private readonly ShadowRenderer grassShadowRenderer;
        private readonly PoleShadowRenderer poleShadowRenderer;
        private readonly TreeTrunkShadowRenderer treeTrunkShadowRenderer;
        private readonly ColonistShadowRenderer colonistShadowRenderer;
        private readonly DustShadowRenderer dustShadowRenderer;

        // Other renderers
        private readonly BugRenderer bugRenderer;
        private readonly ConduitRenderer conduitRenderer;
        private readonly ConduitBlueprintRenderer conduitBlueprintRenderer;
        private readonly FloorsARenderer floorRendererA;
        private readonly FloorBlueprintRenderer floorBlueprintRenderer;
        private readonly GroundCoverRenderer groundCoverRenderer;
        private readonly GroundCoverCoastRenderer groundCoverCoastRenderer;
        private readonly LampLightRenderer lampLightRenderer;
        private readonly RoomLightRenderer roomLightRenderer;
        private readonly LaunchPadRenderer launchPadRenderer1;
        private readonly LaunchPadRenderer launchPadRenderer2;
        private readonly ResourceOverlayRenderer resourceOverlayRenderer;
        private readonly RocketExhaustGroundRenderer smokeGroundRenderer;
        private readonly TemperatureOverlayRenderer temperatureOverlayRenderer;
        private readonly TerrainAreaOfEffectRenderer terrainAreaOfEffectRenderer;
        private readonly TerrainRendererAbovewater terrainRendererAbovewater;
        private readonly TerrainRendererUnderwater terrainRendererUnderwater;
        private readonly TerrainRendererDeepwater terrainRendererDeepwater;
        private readonly TerrainOverlayRenderer terrainOverlayRenderer;
        private readonly TerrainVirtualOverlayRenderer terrainVirtualOverlayRenderer;
        private readonly TerrainTileHighlightRenderer terrainTileHighlightRenderer;
        private readonly TerrainWetRenderer terrainWetRenderer1;
        private readonly TerrainWetRenderer terrainWetRenderer2;
        private readonly TerrainWetCoastRenderer terrainWetCoastRenderer1;
        private readonly TerrainWetCoastRenderer terrainWetCoastRenderer2;
        private readonly FloorsBRenderer floorRendererB;
        private readonly StackingAreaRenderer stackingAreaRenderer;
        private readonly ZoneRenderer zoneRenderer;

        private readonly Dictionary<RendererType, HashSet<ThingType>> thingTypesByRendererType = new Dictionary<RendererType, HashSet<ThingType>>();

        public static WorldRenderer Instance { get; private set; }

        public bool IsRoofVisible
        {
            get
            {
                return this.roofRenderer.IsRoofVisible;
            }
            set
            {
                this.roofRenderer.IsRoofVisible = value;
                this.roofBlueprintRenderer.IsRoofVisible = value;
            }
        }

        public OverlayType OverlayType
        {
            get
            {
                if (this.resourceOverlayRenderer.IsVisible) return OverlayType.Resources;
                if (this.temperatureOverlayRenderer.IsVisible) return OverlayType.Temperature;
                if (this.cropIconRenderer.IsVisible) return OverlayType.Crops;
                return OverlayType.None;
            }
            set
            {
                this.resourceOverlayRenderer.IsVisible = value == OverlayType.Resources;
                this.temperatureOverlayRenderer.IsVisible = value == OverlayType.Temperature;
                this.jobIconRenderer.IsGeologyVisible = value == OverlayType.Resources;
                this.cropIconRenderer.IsVisible = value == OverlayType.Crops;
            }
        }

        public GraphicsDevice Graphics { get; private set; }
        public ContentManager GeneralContent { get; private set; }
        public ContentManager GameTextureContent { get; private set; }
        public ContentManager TerrainTextureContent { get; private set; }
        public int TextureRes { get; private set; }
        public ClimateType ClimateType { get; private set; }

        public bool ToggleZoneOverlay()
        {
            if (!this.zoneRenderer.IsVisible)
            {
                this.zoneRenderer.IsVisible = true;
                this.zoneRenderer.Zone = ZoneManager.HomeZone;
            }
            else this.zoneRenderer.IsVisible = false;

            return this.zoneRenderer.IsVisible;
        }

        public WorldRenderer()
        {
            if (Instance == null)
            {
                Instance = this;

                this.animalsRenderer = new AnimalsRenderer();
                this.birdsRenderer = new BirdsRenderer();
                this.blueprintRenderer = new BlueprintRenderer();
                this.bugRenderer = new BugRenderer();
                this.colonistBodyRendererLayer1 = new ColonistBodyRenderer(1);
                this.colonistBodyRendererLayer2 = new ColonistBodyRenderer(2);
                this.colonistHeadRendererLayer1 = new ColonistHeadRenderer(1);
                this.colonistHeadRendererLayer2 = new ColonistHeadRenderer(2);
                this.colonistCarryRendererLayer1 = new ColonistCarryItemRenderer(1);
                this.colonistCarryRendererLayer2 = new ColonistCarryItemRenderer(2);
                this.cropIconRenderer = new CropIconRenderer();
                this.flyingInsectsRenderer = new FlyingInsectsRenderer();
                this.colonistShadowRenderer = new ColonistShadowRenderer();
                this.conduitRenderer = new ConduitRenderer();
                this.conduitBlueprintRenderer = new ConduitBlueprintRenderer();
                this.floorRendererA = new FloorsARenderer();
                this.floorRendererB = new FloorsBRenderer();
                this.floorBlueprintRenderer = new FloorBlueprintRenderer();
                this.stackingAreaRenderer = new StackingAreaRenderer();
                this.groundCoverRenderer = new GroundCoverRenderer();
                this.groundCoverCoastRenderer = new GroundCoverCoastRenderer();
                this.generalShadowRendererOld = new ShadowRendererOld();
                this.generalShadowRenderer = new ShadowRenderer(1);
                this.grassShadowRenderer = new ShadowRenderer(2);
                this.geologyParticleRendererLayer1 = new GeologyParticleRenderer() { Layer = 1 };
                this.geologyParticleRendererLayer2 = new GeologyParticleRenderer() { Layer = 2 };
                this.deconstructParticleRenderer = new MicobotParticleRenderer();
                this.lampLightRenderer = new LampLightRenderer();
                this.roomLightRenderer = new RoomLightRenderer();
                this.jobIconRenderer = new JobIconRenderer();
                this.launchPadRenderer1 = new LaunchPadRenderer() { Layer = 1 };
                this.launchPadRenderer2 = new LaunchPadRenderer() { Layer = 2 };
                this.generalRendererA = new GeneralRenderer(RendererType.GeneralA, TextureAtlasIdentifiers.GeneralA);
                this.generalRendererB = new GeneralRenderer(RendererType.GeneralB, TextureAtlasIdentifiers.GeneralB);
                this.generalRendererC = new GeneralRenderer(RendererType.GeneralC, TextureAtlasIdentifiers.GeneralC);
                this.plantsAndRocksRenderer = new GeneralRenderer(RendererType.PlantsAndRocks, TextureAtlasIdentifiers.PlantsAndRocks);
                this.generalRendererD = new GeneralRenderer(RendererType.GeneralD, TextureAtlasIdentifiers.GeneralD);
                this.poleShadowRenderer = new PoleShadowRenderer();
                this.progressBarRenderer = new ProgressBarRenderer();
                this.resourceOverlayRenderer = new ResourceOverlayRenderer();
                this.rocketExhaustRendererLayer1 = new RocketExhaustRenderer() { Layer = 1 };
                this.rocketExhaustRendererLayer2 = new RocketExhaustRenderer() { Layer = 2 };
                this.rocketRenderer = new RocketRenderer();
                this.environmentControlFanRenderer = new EnvironmentControlFanRenderer();
                this.roofRenderer = new RoofRenderer();
                this.roofBlueprintRenderer = new RoofBlueprintRenderer();
                this.EnvironmentControlFanBlueprintRenderer = new EnvironmentControlFanBlueprintRenderer();
                this.smokeGroundRenderer = new RocketExhaustGroundRenderer();
                this.smokeRenderer = new SmokeRenderer();
                this.temperatureOverlayRenderer = new TemperatureOverlayRenderer();
                this.dustShadowRenderer = new DustShadowRenderer();
                this.terrainRendererAbovewater = new TerrainRendererAbovewater();
                this.terrainRendererUnderwater = new TerrainRendererUnderwater();
                this.terrainRendererDeepwater = new TerrainRendererDeepwater();
                this.terrainAreaOfEffectRenderer = new TerrainAreaOfEffectRenderer();
                this.terrainGridRenderer = new TerrainGridRenderer();
                this.terrainOverlayRenderer = new TerrainOverlayRenderer();
                this.terrainVirtualOverlayRenderer = new TerrainVirtualOverlayRenderer();
                this.terrainTileHighlightRenderer = new TerrainTileHighlightRenderer();
                this.treeTrunkShadowRenderer = new TreeTrunkShadowRenderer();
                this.treeTopRenderer = new GeneralRenderer(RendererType.TreeTop, TextureAtlasIdentifiers.PalmTop);
                this.treeTopHarvestingRenderer = new TreeTopBlueprintRenderer();
                this.terrainWetRenderer1 = new TerrainWetRenderer() { Layer = 1 };
                this.terrainWetRenderer2 = new TerrainWetRenderer() { Layer = 2 };
                this.terrainWetCoastRenderer1 = new TerrainWetCoastRenderer() { Layer = 1 };
                this.terrainWetCoastRenderer2 = new TerrainWetCoastRenderer() { Layer = 2 };
                this.wallRenderer = new WallRenderer();
                this.fishRenderer = new FishRenderer();
                this.windTurbineShadowRenderer = new WindTurbineShadowRenderer();
                this.zoneRenderer = new ZoneRenderer();

                this.basicRenderers.Add(this.animalsRenderer);
                this.basicRenderers.Add(this.blueprintRenderer);
                this.basicRenderers.Add(this.colonistBodyRendererLayer1);
                this.basicRenderers.Add(this.colonistBodyRendererLayer2);
                this.basicRenderers.Add(this.colonistCarryRendererLayer1);
                this.basicRenderers.Add(this.colonistCarryRendererLayer2);
                this.basicRenderers.Add(this.colonistHeadRendererLayer1);
                this.basicRenderers.Add(this.colonistHeadRendererLayer2);
                this.basicRenderers.Add(this.cropIconRenderer);
                this.basicRenderers.Add(this.deconstructParticleRenderer);
                this.basicRenderers.Add(this.flyingInsectsRenderer);
                this.basicRenderers.Add(this.generalRendererA);
                this.basicRenderers.Add(this.generalRendererC);
                this.basicRenderers.Add(this.generalRendererB);
                this.basicRenderers.Add(this.plantsAndRocksRenderer);
                this.basicRenderers.Add(this.generalRendererD);
                this.basicRenderers.Add(this.geologyParticleRendererLayer1);
                this.basicRenderers.Add(this.geologyParticleRendererLayer2);
                this.basicRenderers.Add(this.progressBarRenderer);
                this.basicRenderers.Add(this.rocketRenderer);
                this.basicRenderers.Add(this.environmentControlFanRenderer);
                this.basicRenderers.Add(this.roofRenderer);
                this.basicRenderers.Add(this.roofBlueprintRenderer);
                this.basicRenderers.Add(this.EnvironmentControlFanBlueprintRenderer);
                this.basicRenderers.Add(this.terrainGridRenderer);
                this.basicRenderers.Add(this.treeTopRenderer);
                this.basicRenderers.Add(this.treeTopHarvestingRenderer);
                this.basicRenderers.Add(this.jobIconRenderer);
                this.basicRenderers.Add(this.smokeRenderer);
                this.basicRenderers.Add(this.rocketExhaustRendererLayer1);
                this.basicRenderers.Add(this.rocketExhaustRendererLayer2);
                this.basicRenderers.Add(this.wallRenderer);

                this.shadowRenderersOld.Add(this.generalShadowRendererOld);
                this.shadowRenderersOld.Add(this.treeTrunkShadowRenderer);
                this.shadowRenderersOld.Add(this.poleShadowRenderer);
                this.shadowRenderersOld.Add(this.windTurbineShadowRenderer);
                this.shadowRenderers.Add(this.generalShadowRenderer);
                this.shadowRenderers.Add(this.grassShadowRenderer);

                this.otherRenderers.Add(this.birdsRenderer);
                this.otherRenderers.Add(this.bugRenderer);
                this.otherRenderers.Add(this.colonistShadowRenderer);
                this.otherRenderers.Add(this.terrainRendererAbovewater);
                this.otherRenderers.Add(this.terrainRendererUnderwater);
                this.otherRenderers.Add(this.terrainRendererDeepwater);
                this.otherRenderers.Add(this.conduitRenderer);
                this.otherRenderers.Add(this.conduitBlueprintRenderer);
                this.otherRenderers.Add(this.floorRendererA);
                this.otherRenderers.Add(this.floorBlueprintRenderer);
                this.otherRenderers.Add(this.groundCoverRenderer);
                this.otherRenderers.Add(this.groundCoverCoastRenderer);
                this.otherRenderers.Add(this.lampLightRenderer);
                this.otherRenderers.Add(this.roomLightRenderer);
                this.otherRenderers.Add(this.launchPadRenderer1);
                this.otherRenderers.Add(this.launchPadRenderer2);
                this.otherRenderers.Add(this.resourceOverlayRenderer);
                this.otherRenderers.Add(this.temperatureOverlayRenderer);
                this.otherRenderers.Add(this.terrainAreaOfEffectRenderer);
                this.otherRenderers.Add(this.terrainTileHighlightRenderer);
                this.otherRenderers.Add(this.terrainOverlayRenderer);
                this.otherRenderers.Add(this.terrainWetRenderer1);
                this.otherRenderers.Add(this.terrainWetRenderer2);
                this.otherRenderers.Add(this.terrainWetCoastRenderer1);
                this.otherRenderers.Add(this.terrainWetCoastRenderer2);
                this.otherRenderers.Add(this.terrainVirtualOverlayRenderer);
                this.otherRenderers.Add(this.smokeGroundRenderer);
                this.otherRenderers.Add(this.floorRendererB);
                this.otherRenderers.Add(this.stackingAreaRenderer);
                this.otherRenderers.Add(this.dustShadowRenderer);
                this.otherRenderers.Add(this.fishRenderer);
                this.otherRenderers.Add(this.zoneRenderer);
            }
            else
            {
                throw new ApplicationException("WorldRenderer already created");
            }
        }

        public void InitShadowMeshRenderers()
        {
            var defaultShadowRenderer = new ShadowMeshRenderer(ShadowMeshType.General, 20000, 60000, 15000, 6);
            var animatedShadowRender = new ShadowMeshRenderer(ShadowMeshType.Animated, 10000, 30000, 7500, 1);
            var treeTopShadowRenderer = new TreeTopShadowRenderer();
            this.shadowMeshRenderers.Add(defaultShadowRenderer);
            this.shadowMeshRenderers.Add(treeTopShadowRenderer);
            this.shadowMeshRenderers.Add(animatedShadowRender);

            this.shadowMeshRenderersByMeshType = new Dictionary<ShadowMeshType, ShadowMeshRenderer>
            {
                {ShadowMeshType.General, defaultShadowRenderer },
                {ShadowMeshType.Animated, animatedShadowRender },
                {ShadowMeshType.TreeTop, treeTopShadowRenderer }
            };
        }

        public void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.Graphics = graphicsDevice;
            this.GeneralContent = contentManager;
            this.TerrainTextureContent = new ContentManager(contentManager.ServiceProvider, "Content\\Textures");  // For managing terrain textures
            this.GameTextureContent = new ContentManager(contentManager.ServiceProvider, "Content\\Textures");  // For managing other high / low res game textures

            graphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            graphicsDevice.BlendState = BlendState.AlphaBlend;

            PresentationParameters pp = graphicsDevice.PresentationParameters;

            // Shadows
            this.renderTargetForShadows = new RenderTarget2D(graphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false, Graphics.DisplayMode.Format, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            this.shadowSpriteBatch = new SpriteBatch(graphicsDevice);

            // Water
            this.waterSpriteBatch = new SpriteBatch(graphicsDevice);
            this.waterTexture = new Texture2D(graphicsDevice, 1, 1);
            Color[] color = new Color[1] { new Color(50, 62, 74) };
            this.waterTexture.SetData(color);

            // Night shade
            this.nightShadeEffect = contentManager.Load<Effect>("Effects\\SimpleEffect").Clone();
            this.nightShadeEffect.CurrentTechnique = this.nightShadeEffect.Techniques["InverseAlphaTechnique"];

            this.TextureRes = SettingsManager.GetSettingInt(SettingGroup.Graphics, SettingNames.TextureRes).GetValueOrDefault(1);
            this.ClimateType = World.ClimateType;

            foreach (var renderer in this.basicRenderers) renderer.LoadContent();
            foreach (var renderer in this.shadowRenderersOld) renderer.LoadContent(graphicsDevice, contentManager);
            foreach (var renderer in this.shadowRenderers) renderer.LoadContent(graphicsDevice, contentManager);
            foreach (var renderer in this.shadowMeshRenderers) renderer.LoadContent(graphicsDevice, contentManager);

            this.terrainRendererAbovewater.LoadContent(graphicsDevice, contentManager);
            this.terrainRendererUnderwater.LoadContent(graphicsDevice, contentManager);
            this.terrainRendererDeepwater.LoadContent(graphicsDevice, contentManager);

            this.animalsRenderer.LoadContent();
            this.birdsRenderer.LoadContent(graphicsDevice, contentManager);
            this.bugRenderer.LoadContent(graphicsDevice, contentManager);
            this.conduitRenderer.LoadContent(graphicsDevice, contentManager);
            this.conduitBlueprintRenderer.LoadContent(graphicsDevice, contentManager);
            this.colonistShadowRenderer.LoadContent(graphicsDevice, contentManager);
            this.floorRendererA.LoadContent(graphicsDevice, contentManager);
            this.flyingInsectsRenderer.LoadContent();
            this.groundCoverRenderer.LoadContent(graphicsDevice, contentManager);
            this.groundCoverCoastRenderer.LoadContent(graphicsDevice, contentManager);
            this.launchPadRenderer1.LoadContent(graphicsDevice, contentManager);
            this.launchPadRenderer2.LoadContent(graphicsDevice, contentManager);
            this.floorBlueprintRenderer.LoadContent(graphicsDevice, contentManager);
            this.lampLightRenderer.LoadContent(graphicsDevice, contentManager);
            this.roomLightRenderer.LoadContent(graphicsDevice, contentManager);
            this.resourceOverlayRenderer.LoadContent(graphicsDevice, contentManager);
            this.temperatureOverlayRenderer.LoadContent(graphicsDevice, contentManager);
            this.terrainAreaOfEffectRenderer.LoadContent(graphicsDevice, contentManager);
            this.terrainTileHighlightRenderer.LoadContent(graphicsDevice, contentManager);
            this.terrainOverlayRenderer.LoadContent(graphicsDevice, contentManager);
            this.terrainWetRenderer1.LoadContent(graphicsDevice, contentManager);
            this.terrainWetRenderer2.LoadContent(graphicsDevice, contentManager);
            this.terrainWetCoastRenderer1.LoadContent(graphicsDevice, contentManager);
            this.terrainWetCoastRenderer2.LoadContent(graphicsDevice, contentManager);
            this.terrainVirtualOverlayRenderer.LoadContent(graphicsDevice, contentManager);
            this.smokeGroundRenderer.LoadContent(graphicsDevice, contentManager);
            this.dustShadowRenderer.LoadContent(graphicsDevice, contentManager);
            this.floorRendererB.LoadContent(graphicsDevice, contentManager);
            this.stackingAreaRenderer.LoadContent(graphicsDevice, contentManager);
            this.fishRenderer.LoadContent(graphicsDevice, contentManager);
            this.zoneRenderer.LoadContent(graphicsDevice, contentManager);

            // Thing Types by RendererType
            foreach (var rendererType in Enum.GetValues(typeof(RendererType)).Cast<RendererType>())
            {
                this.thingTypesByRendererType.Add(rendererType, Enum.GetValues(typeof(ThingType)).Cast<ThingType>().Where(t => ThingTypeManager.IsRendererType(t, rendererType)).ToHashSet());
            }

            EventManager.Subscribe(EventType.Building, EventSubType.Added, delegate (object obj) { this.OnWorldUpdateEvent(obj); });
            EventManager.Subscribe(EventType.Building, EventSubType.Removed, delegate (object obj) { this.OnWorldUpdateEvent(obj); });
            EventManager.Subscribe(EventType.StackingArea, delegate (object obj) { this.OnWorldUpdateEvent(obj); });
            EventManager.Subscribe(EventType.VirtualBlueprint, delegate (object obj) { this.OnWorldUpdateEvent(obj); });
        }

        public void InvalidateBuffers()
        {
            foreach (var renderer in this.basicRenderers)
            {
                renderer.InvalidateBuffers();
            }

            foreach (var renderer in this.shadowRenderersOld)
            {
                renderer.InvalidateBuffers();
            }

            foreach (var renderer in this.shadowRenderers)
            {
                renderer.InvalidateBuffers();
            }

            foreach (var renderer in this.shadowMeshRenderers)
            {
                renderer.InvalidateBuffers();
            }

            foreach (var renderer in this.otherRenderers)
            {
                renderer.InvalidateBuffers();
            }
        }

        public void Update(Vector2f scrollPos, float zoom, bool isPaused)
        {
            if (World.TerrainRowCount <= 0)
            {
                return;
            }

            try
            {
                var pp = this.Graphics.PresentationParameters;
                if ((pp.BackBufferWidth != this.renderTargetForShadows.Bounds.Width || pp.BackBufferHeight != this.renderTargetForShadows.Bounds.Height) && pp.BackBufferWidth > 0 && pp.BackBufferHeight > 0)
                {
                    this.renderTargetForShadows.Dispose();
                    this.renderTargetForShadows = new RenderTarget2D(this.Graphics, pp.BackBufferWidth, pp.BackBufferHeight, false, Graphics.DisplayMode.Format, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
                }

                if (this.scrollPosition == null
                   || scrollPos.X != this.scrollPosition.X
                   || scrollPos.Y != this.scrollPosition.Y
                   || this.Graphics.Viewport.Width != this.viewWidth
                   || this.Graphics.Viewport.Height != this.viewHeight
                   || this.zoom != zoom)
                {
                    this.InvalidateShadows();
                    this.viewWidth = this.Graphics.Viewport.Width;
                    this.viewHeight = this.Graphics.Viewport.Height;
                    this.scrollPosition.X = scrollPos.X;
                    this.scrollPosition.Y = scrollPos.Y;
                    this.zoom = zoom;
                }

                this.ProcessWorldPropertyChanges();
                this.ProcessRoomLightChanges();
                if (EventManager.IsTemperatureOverlayInvalidated && World.WorldTime.FrameNumber % 29 == 0)  // Expensive, don't invalidate every frame
                {
                    this.temperatureOverlayRenderer.InvalidateBuffers();
                    EventManager.IsTemperatureOverlayInvalidated = false;
                }

                var prevTextureRes = this.TextureRes;
                this.TextureRes = SettingsManager.GetSettingInt(SettingGroup.Graphics, SettingNames.TextureRes).GetValueOrDefault(1);
                var textureResChanged = prevTextureRes != this.TextureRes;
                var climateTypeChanged = false;
                if (textureResChanged)
                {
                    this.GameTextureContent.Unload();
                    this.TerrainTextureContent.Unload();
                    this.ClimateType = World.ClimateType;
                }
                else
                {
                    var prevClimateType = this.ClimateType;
                    this.ClimateType = World.ClimateType;
                    climateTypeChanged = prevClimateType != this.ClimateType;
                    if (climateTypeChanged) this.TerrainTextureContent.Unload();
                }

                foreach (var renderer in this.basicRenderers)
                {
                    if (textureResChanged) renderer.ReloadContent();
                    renderer.Update(World.WorldTime, scrollPos, zoom, isPaused);
                }

                var sunVector = World.WorldLight.SunVector;
                var shadowDetail = SettingsManager.GetSettingInt(SettingGroup.Graphics, SettingNames.ShadowDetail).GetValueOrDefault(2);

                foreach (var renderer in this.shadowRenderersOld)
                {
                    renderer.Update(World.WorldTime, scrollPos, zoom, sunVector, shadowDetail, isPaused);
                }

                foreach (var renderer in this.shadowRenderers)
                {
                    renderer.Update(World.WorldTime, scrollPos, zoom, sunVector, shadowDetail, isPaused);
                }


                foreach (var renderer in this.shadowMeshRenderers)
                {
                    renderer.Update(scrollPos, zoom, sunVector, shadowDetail, isPaused);
                }

                if (textureResChanged)
                {
                    this.groundCoverRenderer.ReloadContent();
                    this.groundCoverCoastRenderer.ReloadContent();
                    this.terrainRendererAbovewater.ReloadContent();
                    this.terrainRendererUnderwater.ReloadContent();
                    this.fishRenderer.ReloadContent();
                    this.bugRenderer.ReloadContent();
                    this.birdsRenderer.ReloadContent();
                    this.conduitRenderer.ReloadContent();
                    this.conduitBlueprintRenderer.ReloadContent();
                    this.floorRendererA.ReloadContent();
                    this.floorBlueprintRenderer.ReloadContent();
                    this.floorRendererB.ReloadContent();
                    this.stackingAreaRenderer.ReloadContent();
                    this.launchPadRenderer1.ReloadContent();
                    this.launchPadRenderer2.ReloadContent();
                }
                else if (climateTypeChanged)
                {
                    this.terrainRendererAbovewater.ReloadContent();
                    this.groundCoverRenderer.ReloadContent();
                    this.groundCoverCoastRenderer.ReloadContent();
                    this.birdsRenderer.ReloadContent();
                }

                this.animalsRenderer.Update(World.WorldTime, scrollPos, zoom);
                this.bugRenderer.Update(scrollPos, zoom);
                this.birdsRenderer.Update(scrollPos, zoom);
                this.colonistShadowRenderer.Update(World.WorldTime, scrollPos, zoom, sunVector, shadowDetail);
                this.conduitRenderer.Update(scrollPos, zoom);
                this.conduitBlueprintRenderer.Update(scrollPos, zoom);
                this.floorRendererA.Update(scrollPos, zoom);
                this.groundCoverRenderer.Update(scrollPos, zoom);
                this.groundCoverCoastRenderer.Update(scrollPos, zoom);
                this.launchPadRenderer1.Update(scrollPos, zoom);
                this.launchPadRenderer2.Update(scrollPos, zoom);
                this.floorBlueprintRenderer.Update(scrollPos, zoom);
                this.lampLightRenderer.Update(scrollPos, zoom);
                this.roomLightRenderer.Update(scrollPos, zoom);
                this.dustShadowRenderer.Update(World.WorldTime, scrollPos, zoom, sunVector);
                this.flyingInsectsRenderer.Update(World.WorldTime, scrollPos, zoom);
                if (this.resourceOverlayRenderer.IsVisible) this.resourceOverlayRenderer.Update(scrollPos, zoom);
                if (this.temperatureOverlayRenderer.IsVisible) this.temperatureOverlayRenderer.Update(scrollPos, zoom);
                this.terrainRendererAbovewater.Update(scrollPos, zoom);
                this.terrainRendererUnderwater.Update(scrollPos, zoom);
                this.terrainRendererDeepwater.Update(scrollPos, zoom);
                this.terrainAreaOfEffectRenderer.Update(scrollPos, zoom);
                this.terrainTileHighlightRenderer.Update(scrollPos, zoom);
                this.terrainOverlayRenderer.Update(scrollPos, zoom);
                this.terrainVirtualOverlayRenderer.Update(scrollPos, zoom);

                this.terrainWetRenderer1.Update(scrollPos, zoom);
                this.terrainWetRenderer2.Update(scrollPos, zoom);
                this.terrainWetCoastRenderer1.Update(scrollPos, zoom);
                this.terrainWetCoastRenderer2.Update(scrollPos, zoom);

                this.smokeGroundRenderer.Update(scrollPos, zoom);
                this.floorRendererB.Update(scrollPos, zoom);
                this.stackingAreaRenderer.Update(scrollPos, zoom);
                this.fishRenderer.Update(scrollPos, zoom);
                this.zoneRenderer.Update(scrollPos, zoom);
            }
            catch (Exception ex)
            {
                ExceptionManager.CurrentExceptions.Add(ex);
            }

            EventManager.MovedAnimals.Clear();
            EventManager.MovedBugs.Clear();
            EventManager.MovedColonists.Clear();
            EventManager.MovedFlyingInsects.Clear();

            PerfMonitor.ShadowTriCount = this.shadowMeshRenderers.Sum(r => r.TriCount);
        }

        public void InvalidateShadows()
        {
            this.isShadowsInvalidated = true;
        }

        public void ClearShadows()
        {
            foreach (var renderer in this.shadowRenderersOld)
            {
                renderer.Clear();
            }

            foreach (var renderer in this.shadowRenderers)
            {
                renderer.Clear();
            }

            foreach (var renderer in this.shadowMeshRenderers)
            {
                renderer.Clear();
            }

            this.colonistShadowRenderer.Clear();
            this.dustShadowRenderer.Clear();
        }


        public void Draw(Vector2f scrollPos, float zoom, RenderTarget2D renderTarget)
        {
            if (World.TerrainRowCount <= 0)
            {
                return;
            }

            try
            {
                this.PrepareShadows();

                this.Graphics.SetRenderTarget(renderTarget);
                this.DrawInner(scrollPos, zoom);
            }
            catch (Exception ex)
            {
                ExceptionManager.CurrentExceptions.Add(ex);
            }
        }

        private void DrawInner(Vector2f scrollPos, float zoom)
        {
            //this.graphics.Clear(new Color(53, 59, 185));
            this.Graphics.Clear(new Color(Constants.WaterColourR, Constants.WaterColourG, Constants.WaterColourB));

            var firstRow = ((World.Height - 4) + (int)(CoordinateHelper.GetWorldPosition(Graphics, scrollPos, zoom, 0, 0).Y) / 16) * 3;
            var lastRow = (World.Height + 20 + (int)(CoordinateHelper.GetWorldPosition(Graphics, scrollPos, zoom, 0, Graphics.ScissorRectangle.Height).Y) / 16) * 3;

            // Draw terrain first
            this.terrainRendererDeepwater.Draw();
            this.terrainRendererUnderwater.Draw();
            this.fishRenderer.Draw();
            this.DrawWaterSurface();
            this.terrainRendererAbovewater.Draw();
            this.terrainGridRenderer.IsVisible = SettingsManager.GetSettingBool(SettingGroup.Graphics, SettingNames.EnableTerrainGrid).GetValueOrDefault();
            if (this.terrainGridRenderer.IsVisible)
            {
                for (int j = Math.Max(0, firstRow); j < World.SmallTileRowCount && j < lastRow; ++j)
                {
                    this.terrainGridRenderer.Draw(j);
                }
            }

            // Draw stuff on the ground that want shadows on top
            this.terrainWetRenderer1.Draw();
            this.terrainWetCoastRenderer1.Draw();
            this.terrainWetRenderer2.Draw();
            this.terrainWetCoastRenderer2.Draw();
            this.groundCoverRenderer.Draw();
            this.groundCoverCoastRenderer.Draw();
            this.conduitRenderer.Draw();

            this.terrainTileHighlightRenderer.Draw();

            var virtualThingType = PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Build ? PlayerWorldInteractionManager.CurrentThingTypeToBuild : null;

            if (virtualThingType == ThingType.FoundationMetal || virtualThingType == ThingType.FoundationStone || virtualThingType == ThingType.ConduitNode)
            {
                this.terrainOverlayRenderer.Draw();
            }

            this.floorRendererA.Draw();
            this.floorRendererB.Draw();

            this.bugRenderer.Draw();

            if (!virtualThingType.In(ThingType.FoundationMetal, ThingType.FoundationStone, ThingType.ConduitNode))
            {
                this.terrainOverlayRenderer.Draw();
            }

            if (virtualThingType == ThingType.ConduitNode || virtualThingType == ThingType.OreScanner || TileHighlightManager.TileHighlightsPulsing.Any())
            {
                this.terrainVirtualOverlayRenderer.Draw();
            }

            if (this.resourceOverlayRenderer.IsVisible) this.resourceOverlayRenderer.Draw();
            else if (this.temperatureOverlayRenderer.IsVisible) this.temperatureOverlayRenderer.Draw();

            if (this.zoneRenderer.IsVisible) this.zoneRenderer.Draw();

            this.launchPadRenderer1.Draw();
            this.smokeGroundRenderer.Draw();
            this.launchPadRenderer2.Draw();

            // Draw shadows on the ground
            this.DrawShadows();

            // Blurprint renderers on top of shadows
            this.conduitBlueprintRenderer.Draw();
            this.floorBlueprintRenderer.Draw();

            this.terrainTileHighlightRenderer.Draw();
            this.terrainAreaOfEffectRenderer.Draw();

            this.stackingAreaRenderer.Draw();

            var isRocketExhaustActive = RocketExhaustSimulator.IsActive || LanderExhaustSimulator.IsActive;

            for (int i = 0; i < World.SmallTileRowCount; i++)
            {
                if (i >= firstRow && i <= lastRow)
                {
                    this.colonistBodyRendererLayer1.Draw(i);
                    this.colonistHeadRendererLayer1.Draw(i);
                    this.colonistCarryRendererLayer1.Draw(i);
                    this.geologyParticleRendererLayer1.Draw(i);
                    this.animalsRenderer.Draw(i);
                    this.generalRendererA.Draw(i);
                    this.generalRendererB.Draw(i);
                    this.generalRendererC.Draw(i);
                    this.plantsAndRocksRenderer.Draw(i);
                    this.colonistBodyRendererLayer2.Draw(i);
                    this.colonistHeadRendererLayer2.Draw(i);
                    this.colonistCarryRendererLayer2.Draw(i);
                    if (isRocketExhaustActive) this.rocketExhaustRendererLayer1.Draw(i);
                    this.rocketRenderer.Draw(i);
                    if (isRocketExhaustActive) this.rocketExhaustRendererLayer2.Draw(i);
                    this.generalRendererD.Draw(i);
                    this.geologyParticleRendererLayer2.Draw(i);
                    this.deconstructParticleRenderer.Draw(i);
                    this.smokeRenderer.Draw(i);
                    this.blueprintRenderer.Draw(i);                       // The "blueprint" overlays must be added towards the end
                    if (this.cropIconRenderer.IsVisible) this.cropIconRenderer.Draw(i);
                    this.wallRenderer.Draw(i);
                    this.environmentControlFanRenderer.Draw(i);
                    this.EnvironmentControlFanBlueprintRenderer.Draw(i);
                    this.roofRenderer.Draw(i);
                    this.roofBlueprintRenderer.Draw(i);
                    this.flyingInsectsRenderer.Draw(i);
                    this.treeTopRenderer.Draw(i);
                    this.treeTopHarvestingRenderer.Draw(i);
                    this.progressBarRenderer.Draw(i);
                    this.jobIconRenderer.Draw(i);
                }
                else
                {
                    if (isRocketExhaustActive) this.rocketExhaustRendererLayer1.Draw(i);
                    this.rocketRenderer.Draw(i);
                    if (isRocketExhaustActive) this.rocketExhaustRendererLayer2.Draw(i);
                }
            }

            this.birdsRenderer.Draw();
        }

        private void OnWorldUpdateEvent(object sender)
        {
            var thing = (sender as Thing);
            if (thing?.MainTile != null)
            {
                var row = thing.MainTile.Row;
                var def = ThingTypeManager.GetDefinition(thing.ThingType);
                if (def != null && def.RendererTypes != null)
                {
                    foreach (var rendererType in def.RendererTypes)
                    {
                        switch (rendererType)
                        {
                            case RendererType.GeneralA:
                                this.generalRendererA.InvalidateRow(row);
                                if (thing.ThingType.In(ThingType.Biolab, ThingType.MaterialsLab, ThingType.GeologyLab)) this.generalRendererB.InvalidateRow(row);
                                break;
                            case RendererType.GeneralB:
                                this.generalRendererB.InvalidateRow(row);
                                break;
                            case RendererType.GeneralC:
                                this.generalRendererC.InvalidateRow(row);
                                if (thing.ThingType.In(ThingType.FoodDispenser, ThingType.WaterDispenser, ThingType.KekDispenser)) this.generalRendererA.InvalidateRow(row);
                                if (thing.ThingType == ThingType.WindTurbine) this.floorRendererB.InvalidateBuffers();
                                break;
                            case RendererType.PlantsAndRocks:
                                this.plantsAndRocksRenderer.InvalidateRow(row);
                                break;
                            case RendererType.GeneralD:
                                this.generalRendererD.InvalidateRow(row);
                                break;
                            case RendererType.Conduit:
                                this.conduitRenderer.InvalidateBuffers();
                                break;
                            case RendererType.Wall:
                                this.wallRenderer.InvalidateRow(row);
                                this.floorRendererB.InvalidateBuffers();
                                break;
                            case RendererType.FloorB:
                                this.floorRendererB.InvalidateBuffers();
                                break;
                            case RendererType.Roof:
                                this.roofRenderer.InvalidateRow(row);
                                break;
                            case RendererType.EnvironmentControlFan:
                                this.environmentControlFanRenderer.InvalidateRow(row);
                                break;
                            case RendererType.Rocket:
                                this.rocketRenderer.InvalidateBuffers();
                                break;
                            case RendererType.StackingArea:
                                this.stackingAreaRenderer.InvalidateBuffers();
                                break;
                        }
                    }
                }

                if (thing.ThingType == ThingType.ShorePump)
                {
                    this.generalRendererA.InvalidateRow(row);
                    this.generalRendererD.InvalidateRow(row);
                }
            }
        }

        private void ProcessWorldPropertyChanges()
        {
            while (EventManager.HasWorldPropertyChangeEvent)
            {
                var e = EventManager.DequeueWorldPropertyChangeEvent();

                if (e.PropertyName == nameof(IThing.IsDesignatedForRecycling) || e.PropertyName == "TilesToSurvey")
                {
                    // Switch recycle icon on or off
                    this.jobIconRenderer.InvalidateRow(e.TerrainRow.Value);
                    continue;
                }

                if (e.PropertyName == nameof(ISmallTile.GroundCoverDensity))
                {
                    var tile = World.GetSmallTile(e.ThingId);
                    if (tile?.TerrainType == TerrainType.Coast) this.groundCoverCoastRenderer.InvalidateBuffers();
                    else this.groundCoverRenderer.InvalidateBuffers();

                    continue;
                }

                if (e.PropertyName == nameof(ISmallTile.BiomeType))
                {
                    this.terrainRendererAbovewater.InvalidateBuffers();
                    this.terrainWetRenderer1.InvalidateBuffers();
                    this.terrainWetRenderer2.InvalidateBuffers();
                    this.terrainWetCoastRenderer1.InvalidateBuffers();
                    this.terrainWetCoastRenderer2.InvalidateBuffers();

                    continue;
                }

                if (e.PropertyName == nameof(World.ResourcesForDeconstruction) || e.PropertyName == "plantsForHarvest" || e.PropertyName == nameof(IMine.IsMineExhausted))
                {
                    this.jobIconRenderer.InvalidateRow(e.TerrainRow.Value);
                }
                else if (e.PropertyName == nameof(ISmallTile.MineResourceCount) || e.PropertyName == nameof(ISmallTile.IsMineResourceVisible))
                {
                    this.resourceOverlayRenderer.InvalidateBuffers();
                }
                else if (e.PropertyName == nameof(ISmallTile.MineResourceSurveyProgress))
                {
                    this.progressBarRenderer.InvalidateRow(e.TerrainRow.Value);
                    this.geologyParticleRendererLayer1.InvalidateRow(e.TerrainRow.Value);
                    this.geologyParticleRendererLayer2.InvalidateRow(e.TerrainRow.Value);
                }
                else if (e.PropertyName == nameof(IStackingArea.ItemType))
                {
                    this.stackingAreaRenderer.InvalidateBuffers();
                }

                // Everything past this point is about ThingTypes
                if (!e.ThingType.HasValue) continue;

                if (thingTypesByRendererType[RendererType.Conduit].Contains(e.ThingType.Value))
                {
                    this.conduitRenderer.InvalidateBuffers();
                    continue;
                }

                if (thingTypesByRendererType[RendererType.Fish].Contains(e.ThingType.Value))
                {
                    this.fishRenderer.InvalidateBuffers();
                    continue;
                }

                if (thingTypesByRendererType[RendererType.Birds].Contains(e.ThingType.Value))
                {
                    this.birdsRenderer.InvalidateBuffers();
                    continue;
                }

                if (Constants.ItemTypesByResourceStackType.ContainsKey(e.ThingType.Value))
                {
                    if (e.PropertyName == nameof(Blueprint.ColourA)) this.blueprintRenderer.InvalidateRow(e.TerrainRow.Value);
                    else this.generalRendererA.InvalidateRow(e.TerrainRow.Value);

                    if (ShadowManager.Contains(e.ThingType.Value)) this.generalShadowRenderer.InvalidateThing(e.ThingId);
                    if (ShadowManager.MeshTypesByThingType.ContainsKey(e.ThingType.Value))
                    {
                        foreach (var meshType in ShadowManager.MeshTypesByThingType[e.ThingType.Value]) this.shadowMeshRenderersByMeshType[meshType].InvalidateThing(e.ThingId);
                    }
                    continue;
                }

                var def = ThingTypeManager.GetDefinition(e.ThingType.Value);
                
                var rendererTypes = def.RendererTypes ?? new List<RendererType>();
                if (e.PropertyName == nameof(IAnimatedThing.AnimationFrame))
                {
                    if (e.ThingType == ThingType.Lamp)
                    {
                        this.InvalidateShadows();   // For lights
                        this.lampLightRenderer.InvalidateBuffers();
                        continue;
                    }

                    // TODO: Gantry needs def file
                    if (e.ThingType == ThingType.RocketGantry)
                    {
                        this.generalRendererA.InvalidateRow(e.TerrainRow.Value);
                        this.generalRendererD.InvalidateRow(e.TerrainRow.Value);
                        continue;
                    }

                    // Animation frame changes
                    if (e.TerrainRow.HasValue)
                    {
                        foreach (var rendererType in rendererTypes)
                        {
                            switch (rendererType)
                            {
                                case RendererType.GeneralA:
                                    this.generalRendererA.InvalidateRow(e.TerrainRow.Value);
                                    break;
                                case RendererType.GeneralB:
                                    this.generalRendererB.InvalidateRow(e.TerrainRow.Value);
                                    break;
                                case RendererType.GeneralC:
                                    this.generalRendererC.InvalidateRow(e.TerrainRow.Value);
                                    break;
                                case RendererType.PlantsAndRocks:
                                    this.plantsAndRocksRenderer.InvalidateRow(e.TerrainRow.Value);
                                    break;
                                case RendererType.GeneralD:
                                    this.generalRendererD.InvalidateRow(e.TerrainRow.Value);
                                    break;
                                case RendererType.Wall:
                                    this.wallRenderer.InvalidateRow(e.TerrainRow.Value);
                                    break;
                                case RendererType.Colonist:
                                    this.colonistBodyRendererLayer1.InvalidateRow(e.TerrainRow.Value);
                                    this.colonistBodyRendererLayer2.InvalidateRow(e.TerrainRow.Value);
                                    this.colonistHeadRendererLayer1.InvalidateRow(e.TerrainRow.Value);
                                    this.colonistHeadRendererLayer2.InvalidateRow(e.TerrainRow.Value);
                                    this.colonistCarryRendererLayer1.InvalidateRow(e.TerrainRow.Value);
                                    this.colonistCarryRendererLayer2.InvalidateRow(e.TerrainRow.Value);
                                    break;
                            }
                        }
                    }

                    if (e.ThingType.Value == ThingType.Grass || e.ThingType.Value == ThingType.CoastGrass) this.grassShadowRenderer.InvalidateThing(e.ThingId);
                    else if (ShadowManager.Contains(e.ThingType.Value)) this.generalShadowRenderer.InvalidateThing(e.ThingId);
                    if (ShadowManager.ContainsMeshWithFrames(e.ThingType.Value) && ShadowManager.MeshTypesByThingType.ContainsKey(e.ThingType.Value))
                    {
                        foreach (var meshType in ShadowManager.MeshTypesByThingType[e.ThingType.Value].Where(mt => ShadowManager.ContainsMeshWithFrames(e.ThingType.Value, mt)))
                        {
                            this.shadowMeshRenderersByMeshType[meshType].InvalidateThing(e.ThingId);
                        }
                    }
                }
                else if (e.PropertyName == nameof(ILab.ScreenFrame))
                {
                    // Lab screen changes
                    if (e.NewValue is string s && (s.EndsWith("NW") || s.EndsWith("NE"))) this.generalRendererA.InvalidateRow(e.TerrainRow.Value);
                    else this.generalRendererB.InvalidateRow(e.TerrainRow.Value);
                }
                else if (e.PropertyName == nameof(IEnvironmentControl.ScreenAnimationFrame))
                {
                    // Environment control unit screen changes
                    this.generalRendererA.InvalidateRow(e.TerrainRow.Value);
                }
                else if (e.PropertyName == nameof(IEnvironmentControl.FanAnimationFrame))
                {
                    // Environment control unit fan
                    this.environmentControlFanRenderer.InvalidateRow(e.TerrainRow.Value);
                }
                else if (e.PropertyName == nameof(IPlantWithAnimatedFlower.FlowerFrame))
                {
                    this.plantsAndRocksRenderer.InvalidateRow(e.TerrainRow.Value);
                }
                else if (e.PropertyName == nameof(ITree.Height))
                {
                    this.plantsAndRocksRenderer.InvalidateRow(e.TerrainRow.Value);   // Tree trunk
                    this.treeTopRenderer.InvalidateRow(e.TerrainRow.Value);
                    this.shadowMeshRenderersByMeshType[ShadowMeshType.TreeTop].InvalidateThing(e.ThingId);
                }
                else if (e.PropertyName == nameof(IPlanter.JobProgress) || e.PropertyName == nameof(IFruitPlant.HarvestJobProgress))
                {
                    // Planter progress bars
                    this.progressBarRenderer.InvalidateRow(e.TerrainRow.Value);
                }
                else if (e.PropertyName == nameof(IColonist.CarriedItemTypeBack) 
                    || e.PropertyName == nameof(IColonist.CarriedItemTypeArms)
                    || e.PropertyName == nameof(IColonist.RaisedArmsFrame) 
                    || e.PropertyName == nameof(IColonist.SleepFrame))
                {
                    this.colonistBodyRendererLayer1.InvalidateRow(e.TerrainRow.Value);
                    this.colonistBodyRendererLayer2.InvalidateRow(e.TerrainRow.Value);
                    if (e.PropertyName == nameof(IColonist.SleepFrame))
                    {
                        this.colonistHeadRendererLayer1.InvalidateRow(e.TerrainRow.Value);
                        this.colonistHeadRendererLayer2.InvalidateRow(e.TerrainRow.Value);
                    }
                    else if (e.PropertyName == nameof(IColonist.CarriedItemTypeArms))
                    {
                        this.colonistCarryRendererLayer1.InvalidateRow(e.TerrainRow.Value);
                        this.colonistCarryRendererLayer2.InvalidateRow(e.TerrainRow.Value);
                    }
                }
                else if (e.PropertyName == nameof(IThing.RenderAlpha))
                {
                    // Alpha changes
                    foreach (var rendererType in def.RendererTypes)
                    {
                        if (e.TerrainRow.HasValue)
                        {
                            if ((rendererType == RendererType.GeneralA || e.ThingType.In(ThingType.FoodDispenser, ThingType.WaterDispenser, ThingType.KekDispenser)) && e.TerrainRow.HasValue)
                            {
                                this.generalRendererA.InvalidateRow(e.TerrainRow.Value);
                            }

                            if (e.ThingType == ThingType.ShorePump)
                            {
                                this.generalRendererA.InvalidateRow(e.TerrainRow.Value);
                                this.generalRendererD.InvalidateRow(e.TerrainRow.Value);
                            }

                            switch (rendererType)
                            {
                                case RendererType.GeneralB:
                                    this.generalRendererB.InvalidateRow(e.TerrainRow.Value);
                                    break;
                                case RendererType.GeneralC:
                                    this.generalRendererC.InvalidateRow(e.TerrainRow.Value);
                                    break;
                                case RendererType.PlantsAndRocks:
                                    this.plantsAndRocksRenderer.InvalidateRow(e.TerrainRow.Value);
                                    break;
                                case RendererType.GeneralD:
                                    this.generalRendererD.InvalidateRow(e.TerrainRow.Value);
                                    break;
                                case RendererType.Rocket:
                                    this.rocketRenderer.InvalidateRow(e.TerrainRow.Value);
                                    break;
                                case RendererType.EnvironmentControlFan:
                                    this.environmentControlFanRenderer.InvalidateRow(e.TerrainRow.Value);
                                    break;
                                case RendererType.Roof:
                                    this.roofRenderer.InvalidateRow(e.TerrainRow.Value);
                                    break;
                                case RendererType.Wall:
                                    this.wallRenderer.InvalidateRow(e.TerrainRow.Value);
                                    break;
                                case RendererType.Colonist:
                                    this.colonistBodyRendererLayer1.InvalidateRow(e.TerrainRow.Value);
                                    this.colonistBodyRendererLayer2.InvalidateRow(e.TerrainRow.Value);
                                    this.colonistHeadRendererLayer1.InvalidateRow(e.TerrainRow.Value);
                                    this.colonistHeadRendererLayer2.InvalidateRow(e.TerrainRow.Value);
                                    this.colonistCarryRendererLayer1.InvalidateRow(e.TerrainRow.Value);
                                    this.colonistCarryRendererLayer2.InvalidateRow(e.TerrainRow.Value);
                                    break;
                            }
                        }

                        if (rendererType == RendererType.Conduit)
                        {
                            this.conduitRenderer.InvalidateBuffers();
                        }
                        else if (rendererType == RendererType.Floor)
                        {
                            this.floorRendererA.InvalidateBuffers();
                        }
                        else if (rendererType == RendererType.FloorB || e.ThingType == ThingType.WindTurbine || e.ThingType == ThingType.Wall)
                        {
                            this.floorRendererB.InvalidateBuffers();
                        }
                        else if (rendererType == RendererType.LaunchPad)
                        {
                            this.launchPadRenderer1.InvalidateBuffers();
                            this.launchPadRenderer2.InvalidateBuffers();
                        }

                        if (e.ThingType == ThingType.Tree)
                        {
                            this.treeTopRenderer.InvalidateBuffers();
                        }
                    }
                }
                else if (e.PropertyName == nameof(IThing.ShadowAlpha))
                {
                    if (e.ThingType.Value == ThingType.Grass || e.ThingType.Value == ThingType.CoastGrass) this.grassShadowRenderer.InvalidateThing(e.ThingId);
                    else if (ShadowManager.Contains(e.ThingType.Value)) this.generalShadowRenderer.InvalidateThing(e.ThingId);
                    if (ShadowManager.MeshTypesByThingType.ContainsKey(e.ThingType.Value))
                    {
                        foreach (var meshType in ShadowManager.MeshTypesByThingType[e.ThingType.Value]) this.shadowMeshRenderersByMeshType[meshType].InvalidateAlpha(e.ThingId);
                    }
                }
                else if (e.PropertyName == nameof(Blueprint.ColourA))
                {
                    this.blueprintRenderer.InvalidateRow(e.TerrainRow.Value);
                    if (e.ThingType == ThingType.Tree) this.treeTopHarvestingRenderer.InvalidateRow(e.TerrainRow.Value);
                }
                else if (e.PropertyName == nameof(ILandingPod.Altitude))
                {
                    if (e.ThingType == ThingType.LandingPod)
                    {
                        this.shadowMeshRenderersByMeshType[ShadowMeshType.Animated].InvalidateThing(e.ThingId);
                    }
                    else if (e.ThingType == ThingType.Rocket)
                    {
                        this.rocketRenderer.InvalidateRow(e.TerrainRow.Value);
                    }
                }
                else if (this.cropIconRenderer.IsVisible && e.TerrainRow.HasValue && (e.PropertyName == nameof(IPlanter.CurrentCropTypeId) || e.PropertyName == nameof(IPlanter.SelectedCropTypeId)))
                {
                    this.cropIconRenderer.InvalidateRow(e.TerrainRow.Value);
                }
            }
        }

        private void ProcessRoomLightChanges()
        {
            if (!EventManager.HasRoomLightChangeEvent) return;

            this.roomLightRenderer.InvalidateBuffers();

            while (EventManager.HasRoomLightChangeEvent)
            {
                var e = EventManager.DequeueRoomLightChangeEvent();
                this.InvalidateRow(e.ThingType, e.TerrainRow);

                if (e.ThingType.In(ThingType.Biolab, ThingType.MaterialsLab, ThingType.GeologyLab)) this.generalRendererB.InvalidateRow(e.TerrainRow);      // Screens
                else if (e.ThingType.In(ThingType.FoodDispenser, ThingType.WaterDispenser, ThingType.KekDispenser)) this.generalRendererA.InvalidateRow(e.TerrainRow);    // Stands
            }
        }

        private void InvalidateRow(ThingType thingType, int terrainRow)
        {
            var def = ThingTypeManager.GetDefinition(thingType, false);
            if (def != null && def.RendererTypes != null)
            {
                foreach (var rendererType in def.RendererTypes)
                {
                    switch (rendererType)
                    {
                        case RendererType.GeneralA: this.generalRendererA.InvalidateBuffers(); break;
                        case RendererType.GeneralB: this.generalRendererB.InvalidateBuffers(); break;
                        case RendererType.GeneralC: this.generalRendererC.InvalidateBuffers(); break;
                        case RendererType.PlantsAndRocks:
                            this.plantsAndRocksRenderer.InvalidateBuffers();
                            break;
                        case RendererType.GeneralD: this.generalRendererD.InvalidateBuffers(); break;
                        case RendererType.Animals: this.animalsRenderer.InvalidateRow(terrainRow); break;
                        case RendererType.Birds: this.birdsRenderer.InvalidateBuffers(); break;
                        case RendererType.Bug: this.bugRenderer.InvalidateBuffers(); break;
                        case RendererType.Colonist:
                            this.colonistBodyRendererLayer1.InvalidateRow(terrainRow);
                            this.colonistBodyRendererLayer2.InvalidateRow(terrainRow);
                            this.colonistHeadRendererLayer1.InvalidateRow(terrainRow);
                            this.colonistHeadRendererLayer2.InvalidateRow(terrainRow);
                            this.colonistCarryRendererLayer1.InvalidateRow(terrainRow);
                            this.colonistCarryRendererLayer2.InvalidateRow(terrainRow);
                            break;
                        case RendererType.Conduit: this.conduitRenderer.InvalidateBuffers(); break;
                        case RendererType.Floor: this.floorRendererA.InvalidateBuffers(); break;
                        case RendererType.FloorB: this.floorRendererB.InvalidateBuffers(); break;
                        case RendererType.FlyingInsects: this.flyingInsectsRenderer.InvalidateRow(terrainRow); break;
                        case RendererType.LaunchPad:
                            this.launchPadRenderer1.InvalidateBuffers();
                            this.launchPadRenderer2.InvalidateBuffers();
                            break;
                        case RendererType.Rocket: this.rocketRenderer.InvalidateRow(terrainRow); break;
                        case RendererType.Roof: this.roofRenderer.InvalidateRow(terrainRow); break;
                        case RendererType.EnvironmentControlFan: this.roofRenderer.InvalidateRow(terrainRow); break;
                        case RendererType.TreeTop: this.treeTopRenderer.InvalidateRow(terrainRow); break;
                        case RendererType.Wall: this.wallRenderer.InvalidateRow(terrainRow); break;
                        case RendererType.Fish: this.fishRenderer.InvalidateBuffers(); break;
                    }
                }

                if (thingType == ThingType.ShorePump)
                {
                    this.generalRendererA.InvalidateRow(terrainRow);
                    this.generalRendererD.InvalidateRow(terrainRow);
                }
            }
        }

        private float GetShadowAlpha()
        {
            var f = World.WorldTime.DayFraction;
            var result = 0f;
            if (f < 0.28125f) result = 0f;
            else if (f < 0.3125f) result = 0.2f * (1f + (float)Math.Sin(((32f * (f - 0.28125f)) - 0.5f) * (float)Math.PI));
            else if (f < 0.6875f) result = 0.4f;
            else if (f < 0.71875f) result = 0.2f * (1f + (float)Math.Sin(((32f * (0.71875f - f)) - 0.5f) * (float)Math.PI));
            return result.Clamp(0f, 0.3f);
        }

        private void PrepareShadows()
        {
            if (this.shadowTexture == null || this.isShadowsInvalidated)
            {
                this.shadowTexture = this.DrawShadowsInner();
                this.isShadowsInvalidated = false;
            }
        }

        private void DrawShadows()
        {
            var shadowAlpha = this.GetShadowAlpha();
            if (shadowAlpha == 0)
            {
                // Night
                var nightAlpha = World.WorldLight.NightLightFactor * 0.75f;  
                PresentationParameters pp = this.Graphics.PresentationParameters;
                var r = new Rectangle(0, 0, pp.BackBufferWidth, pp.BackBufferHeight);
                Matrix projection = Matrix.CreateOrthographicOffCenter(0, this.Graphics.Viewport.Width, this.Graphics.Viewport.Height, 0, 0, 1);
                this.nightShadeEffect.Parameters["xViewProjection"].SetValue(projection);
                this.nightShadeEffect.CurrentTechnique = this.nightShadeEffect.Techniques["InverseAlphaTechnique"];
                this.shadowSpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, effect: this.nightShadeEffect);
                this.shadowSpriteBatch.Draw(this.renderTargetForShadows, r, new Color(0.02f, 0.03f, 0.04f, nightAlpha));
                this.shadowSpriteBatch.End();
                return;
            }

            this.shadowSpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            this.shadowSpriteBatch.Draw(this.shadowTexture, new Vector2(0, 0), null, new Color(0, 0, 0, shadowAlpha), 0, new Vector2(0, 0), 1f, SpriteEffects.None, 1);
            this.shadowSpriteBatch.End();
        }

        private Texture2D DrawShadowsInner()
        {
            var shadowAlpha = this.GetShadowAlpha();

            this.Graphics.SetRenderTarget(renderTargetForShadows);
            if (shadowAlpha > 0.0001f)
            {
                this.Graphics.Clear(new Color(0f, 0f, 0f, 0f));
                this.Graphics.RasterizerState = new RasterizerState { CullMode = CullMode.None };

                try
                {
                    foreach (var renderer in this.shadowRenderersOld) renderer.Draw();
                    foreach (var renderer in this.shadowRenderers) renderer.Draw();
                    foreach (var renderer in this.shadowMeshRenderers) renderer.Draw();

                    this.colonistShadowRenderer.Draw();
                    this.dustShadowRenderer.Draw();
                }
                catch (Exception ex)
                {
                    ExceptionManager.CurrentExceptions.Add(ex);
                }
            }
            else
            {
                this.Graphics.Clear(new Color(0f, 0f, 0f, 0f));
                this.lampLightRenderer.Draw();
                this.roomLightRenderer.Draw();
            }

            this.Graphics.SetRenderTarget(null);

            return renderTargetForShadows;
        }

        private void DrawWaterSurface()
        {
            PresentationParameters pp = this.Graphics.PresentationParameters;
            var r = new Rectangle(0, 0, pp.BackBufferWidth, pp.BackBufferHeight);
            this.waterSpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            this.waterSpriteBatch.Draw(this.waterTexture, r, new Color(0.5f, 0.6f, 0.7f, 0.5f));
            this.waterSpriteBatch.End();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.renderTargetForShadows != null) this.renderTargetForShadows.Dispose();
                if (this.shadowSpriteBatch != null) this.shadowSpriteBatch.Dispose();

                foreach (var renderer in this.basicRenderers.OfType<IDisposable>())
                {
                    renderer.Dispose();
                }

                foreach (var renderer in this.shadowRenderersOld.OfType<IDisposable>())
                {
                    renderer.Dispose();
                }

                foreach (var renderer in this.shadowRenderers.OfType<IDisposable>())
                {
                    renderer.Dispose();
                }

                foreach (var renderer in this.shadowMeshRenderers.OfType<IDisposable>())
                {
                    renderer.Dispose();
                }

                foreach (var renderer in this.otherRenderers.OfType<IDisposable>())
                {
                    renderer.Dispose();
                }
            }
        }
    }
}

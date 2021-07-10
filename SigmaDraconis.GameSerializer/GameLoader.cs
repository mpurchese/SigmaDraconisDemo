namespace SigmaDraconis.GameSerializer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;

    using ProtoBuf;

    using Draconis.Shared;

    using AI;
    using AnimalAI;
    using CheckList;
    using Commentary;
    using Shared;
    using World;
    using World.Buildings;
    using World.Blueprints;
    using World.Flora;
    using World.Particles;
    using World.Prefabs;
    using World.Projects;
    using World.Rooms;
    using World.Terrain;
    using World.Zones;
    using WorldControllers;
    using WorldInterfaces;

    public class GameLoader : IDisposable
    {
        private readonly FileStream fileStream;
        private readonly BinaryFormatter binaryFormatter;
        private readonly BinaryReader binaryReader;

        public GameVersion GameVersion { get; private set; }
        public GameVersion InitialGameVersion { get; private set; }
        public Dictionary<string, string> AdditionalProperties { get; private set; }
        public Vector2i ScrollPosition { get; private set; }
        public int ZoomLevel { get; private set; }
        public WorldTime Time { get; private set; }
        public int WorldWidth { get; private set; }
        public int WorldHeight { get; private set; }

        public GameLoader(string path)
        {
            this.fileStream = new FileStream(path, FileMode.Open);
            this.binaryFormatter = new BinaryFormatter();
            this.binaryReader = new BinaryReader(this.fileStream);
        }

        public void LoadHeader()
        {
            this.GameVersion = (GameVersion)this.binaryFormatter.Deserialize(this.fileStream);
            if (this.GameVersion.Major > 0 || this.GameVersion.Minor > 1) this.InitialGameVersion = (GameVersion)this.binaryFormatter.Deserialize(this.fileStream);
            this.AdditionalProperties = this.LoadObject<Dictionary<string, string>>();
            this.ScrollPosition = (Vector2i)this.binaryFormatter.Deserialize(this.fileStream);
            this.ZoomLevel = (int)this.binaryFormatter.Deserialize(this.fileStream);
            this.WorldWidth = (int)this.binaryFormatter.Deserialize(this.fileStream);
            this.WorldHeight = (int)this.binaryFormatter.Deserialize(this.fileStream);
            this.Time = this.LoadObject<WorldTime>();
        }

        public void Load()
        {
            this.LoadHeader();

            ResourceStackingController.Clear();
            WorldController.Clear(this.WorldWidth);
            CommentaryController.Reset();
            CheckListController.Reset();
            ZoneManager.Loading = true;

            // Terrain
            var tiles = new List<BigTile>(this.WorldWidth * this.WorldHeight);
            for (int i = 0; i < this.WorldWidth * this.WorldHeight; i++)
            {
                tiles.Add(new BigTile(i, 0, 0, 0)
                {
                    TerrainType = (TerrainType)this.binaryReader.ReadByte(),
                    BigTileTextureIdentifier = (BigTileTextureIdentifier)this.binaryReader.ReadInt32()
                });
            }

            string section;
            do
            {
                section = this.LoadObject<string>();
                switch (section)
                {
                    case Headers.OreAndSoilV1:
                        this.LoadOreAndSoil();
                        break;
                    case Headers.ThingsV1:
                        World.Load(this.Time, tiles, this.LoadThings(), this.LoadList<KeyValuePair<int, int>>());
                        break;
                    case Headers.BlueprintsV1:
                        this.LoadBlueprints();
                        break;
                    case Headers.PrefabsV1:
                        World.Prefabs = this.LoadObject<PrefabCollection>();
                        break;
                    case Headers.AI:  // Old
                        var version = this.LoadObject<int>();   // Version number, placeholder
                        this.LoadAI(version);
                        break;
                    case Headers.AIV1:  // Old
                        this.LoadAI(1);
                        break;
                    case Headers.WeatherV1:
                        this.LoadWeather();
                        break;
                    case Headers.ParticlesV1:
                        this.LoadParticles();
                        break;
                    case Headers.Jobs:
                    case Headers.JobsV1:   // Old
                        this.LoadJobs(section);
                        break;
                    case Headers.SeedsV1:
                        this.LoadSeeds();
                        break;
                    case Headers.HintsV1:
                        this.IgnoreList();
                        // Ignore: Feature removed in 0.3.0
                        break;
                    case Headers.WarningsV1:
                        WarningsController.Deserialize(this.LoadObject<Dictionary<WarningType, List<string>>>());
                        break;
                    case Headers.ZonesV1:
                        this.LoadZones();
                        break;
                    case Headers.RoomsV1:
                        this.LoadRooms();
                        break;
                    case Headers.ProjectsV1:
                        ProjectManager.SetProjectsRemainingWork(this.LoadObject<Dictionary<int, int>>());
                        break;
                    case Headers.MothershipV1:
                        this.LoadMothership();
                        break;
                    case Headers.GeologyV1:
                        this.LoadGeology();
                        break;
                    case Headers.GroundCover:
                        this.LoadObject<int>();   // Version number, placeholder
                        this.LoadGroundCover();
                        break;
                    case Headers.GroundWater:
                        this.LoadObject<int>();   // Version number, placeholder
                        this.LoadGroundWater();
                        break;
                    case Headers.Stats:
                        this.LoadStats();
                        break;
                    case Headers.CheckList:
                        this.LoadObject<int>();   // Version number, placeholder
                        CheckListController.Deserialize(this.LoadObject<CheckListSerializationObject>());
                        break;
                    case Headers.Commentary:
                        this.LoadObject<int>();   // Version number, placeholder
                        CommentaryController.Deserialize(this.LoadObject<CommentarySerializationObject>());
                        break;
                    case Headers.Misc:
                    case Headers.MiscV1:
                    case Headers.MiscV2:
                        this.LoadMisc(section);
                        break;
                    case Headers.EOF:
                        break;
                    default:
                        throw new Exception($"Unrecognised section header: {section}");
                }
            }
            while (section != Headers.EOF);

            // Infections
            //InfectionController.DistinctInfections = this.LoadList<Infection>();
            //InfectionController.InfectionPool = this.LoadList<Infection>();
            //InfectionController.EvolutionLevel = this.LoadObject<int>();

            if (this.GameVersion.Major > 0 || this.GameVersion.Minor > 1) World.InitialGameVersion = this.InitialGameVersion;

            // Upgrade from pre-v0.4
            foreach (var old in World.GetThings<BatteryCellFactoryOld>(ThingType.BatteryCellFactory).ToList())
            {
                World.RemoveThing(old);
                World.AddThing(new BatteryCellFactory(old.MainTile, old.Direction)
                {
                    IsReady = old.IsReady,
                    ConstructionProgress = old.ConstructionProgress, 
                    MaintenanceLevel = 1.0, 
                    RepairPriority = WorkPriority.Normal
                });
            }

            foreach (var old in World.GetThings<CompositesFactoryOld>(ThingType.CompositesFactory).ToList())
            {
                World.RemoveThing(old);
                World.AddThing(new CompositesFactory(old.MainTile, old.Direction)
                {
                    IsReady = old.IsReady,
                    ConstructionProgress = old.ConstructionProgress,
                    MaintenanceLevel = 1.0,
                    RepairPriority = WorkPriority.Normal
                });
            }

            foreach (var old in World.GetThings<SolarCellFactoryOld>(ThingType.SolarCellFactory).ToList())
            {
                World.RemoveThing(old);
                World.AddThing(new SolarCellFactory(old.MainTile, old.Direction)
                {
                    IsReady = old.IsReady,
                    ConstructionProgress = old.ConstructionProgress,
                    MaintenanceLevel = 1.0,
                    RepairPriority = WorkPriority.Normal
                });
            }

            foreach (var old in World.GetThings<GlassFactoryOld>(ThingType.GlassFactory).ToList())
            {
                World.RemoveThing(old);
                World.AddThing(new GlassFactory(old.MainTile, old.Direction)
                {
                    IsReady = old.IsReady,
                    ConstructionProgress = old.ConstructionProgress,
                    MaintenanceLevel = 1.0,
                    RepairPriority = WorkPriority.Normal
                });
            }

            foreach (var old in World.GetThings<SoilSynthesiser>(ThingType.SoilSynthesiser).ToList())
            {
                World.RemoveThing(old);
                World.AddThing(new CompostFactory(old.MainTile, old.Direction)
                {
                    IsReady = old.IsReady,
                    ConstructionProgress = old.ConstructionProgress,
                    MaintenanceLevel = 1.0,
                    RepairPriority = WorkPriority.Normal,
                    AllowOrganics = true
                });
            }

            foreach (var tile in World.SmallTiles)
            {
                tile.UpdateIsCorridor();
            }

            if (this.GameVersion.Major == 0 && this.GameVersion.Minor < 5)
            {
                foreach (var tile in World.SmallTiles.Where(t => t.BiomeType == BiomeType.Wet))
                {
                    tile.GroundCoverDensity *= 2;
                }

                foreach (var tile in World.SmallTiles.Where(t => t.TerrainType == TerrainType.Coast))
                {
                    var adj = tile.AdjacentTiles4.Where(t => t.TerrainType == TerrainType.Dirt).ToList();
                    if (adj.Any()) tile.GroundCoverDensity = (int)(adj.Average(t => t.GroundCoverDensity));
                }
            }

            foreach (var colonist in World.GetThings<IColonist>(ThingType.Colonist))
            {
                if (!colonist.WorkPriorities.ContainsKey(ColonistPriority.ResearchEngineer)) colonist.WorkPriorities.Add(ColonistPriority.ResearchEngineer, 0);
                if (!colonist.WorkPriorities.ContainsKey(ColonistPriority.ResearchGeologist)) colonist.WorkPriorities.Add(ColonistPriority.ResearchGeologist, 0);
            }

            if (this.GameVersion.Major == 0 && this.GameVersion.Minor < 8)
            {
                foreach (var colonist in World.GetThings<IColonist>(ThingType.Colonist))
                {
                    if (colonist.Skill == SkillType.Engineer) colonist.WorkPriorities[ColonistPriority.ResearchEngineer] = 3;
                    else if (colonist.Skill == SkillType.Geologist) colonist.WorkPriorities[ColonistPriority.ResearchGeologist] = 3;
                }
            }
            else if (this.GameVersion.Major == 0 && this.GameVersion.Minor == 8)
            {
                foreach (var colonist in World.GetThings<IColonist>(ThingType.Colonist))
                {
                    if (colonist.Skill == SkillType.Engineer)
                    {
                        colonist.WorkPriorities[ColonistPriority.ResearchEngineer] = colonist.WorkPriorities[ColonistPriority.ResearchBotanist];
                        colonist.WorkPriorities[ColonistPriority.ResearchBotanist] = 0;
                    }
                    else if (colonist.Skill == SkillType.Geologist)
                    {
                        colonist.WorkPriorities[ColonistPriority.ResearchGeologist] = colonist.WorkPriorities[ColonistPriority.ResearchBotanist];
                        colonist.WorkPriorities[ColonistPriority.ResearchBotanist] = 0;
                    }
                }
            }

            if (this.GameVersion.Major == 0 && this.GameVersion.Minor < 9)
            {
                foreach (var colonist in World.GetThings<IColonist>(ThingType.Colonist))
                {
                    colonist.SetKekPolicy(KekPolicy.Normal);
                    colonist.SetWorkPolicy(WorkPolicy.Normal);
                }
            }
            
            if (this.GameVersion.Major == 0 && this.GameVersion.Minor < 13)
            {
                // Fix for bug in v0.12
                foreach (var plant in World.GetThings<IAnimatedThing>(ThingType.SmallPlant9).Where(p => p.AnimationFrame > 16).ToList())
                {
                    World.RemoveThing(plant);
                }

                // Heater changes in v0.13
                foreach (var heater in World.GetThings<IHeater>(ThingType.DirectionalHeater))
                {
                    heater.IsOn = heater.HeaterSetting != RoomTemperatureSetting.Off;
                    heater.IsAutomatic = heater.HeaterSetting == RoomTemperatureSetting.Automatic;
                }

                // Lamp changes in v0.13
                foreach (var lamp in World.GetThings<ILamp>(ThingType.Lamp))
                {
                    lamp.IsOn = lamp.LightSetting != RoomLightSetting.Off;
                    lamp.IsAutomatic = lamp.LightSetting == RoomLightSetting.Automatic;
                }

                // Environment control changes in v0.13
                foreach (var ec in World.GetThings<IEnvironmentControl>(ThingType.EnvironmentControl).Where(e => !e.IsOn))
                {
                    ec.TogglePower();
                }
            }

            World.WorldLight.Update(World.WorldTime);
            World.RefreshBuildableArea();
        }

        private void LoadOreAndSoil()
        {
            var resources = this.LoadObject<Dictionary<int, MineTileResource>>();
            var soilTypes = this.LoadObject<Dictionary<int, BiomeType>>();
            var groundCoverDensities = this.LoadObject<Dictionary<int, int>>();
            foreach (var tile in World.SmallTiles.Where(t => t.TerrainType == TerrainType.Dirt || t.TerrainType == TerrainType.Coast))
            {
                if (tile.TerrainType == TerrainType.Dirt)
                {
                    if (resources.ContainsKey(tile.Index)) tile.SetResources(resources[tile.Index]);
                    if (soilTypes.ContainsKey(tile.Index)) tile.BiomeType = soilTypes[tile.Index];
                }

                if (groundCoverDensities.ContainsKey(tile.Index))
                {
                    tile.GroundCoverDensity = groundCoverDensities[tile.Index];
                    tile.GroundCoverMaxDensity = groundCoverDensities[tile.Index];
                }
            }
        }

        private List<Thing> LoadThings()
        {
            var things = this.LoadList<Thing>();
            foreach (var thing in things)
            {
                thing.AfterDeserialization();
            }

            return things;
        }

        private void LoadBlueprints()
        {
            World.ConfirmedBlueprints = this.LoadObject<Dictionary<int, Blueprint>>();
            World.RecycleBlueprints = this.LoadObject<Dictionary<int, Blueprint>>();
            BlueprintController.IsVirtualBlueprintBlocked = this.LoadObject<bool>();
            foreach (var blueprint in World.ConfirmedBlueprints.Values.Union(World.RecycleBlueprints.Values))
            {
                blueprint.AfterDeserialization();
            }

            // Make renderer update, must be done after all are loaded
            foreach (var blueprint in World.ConfirmedBlueprints.Values.Union(World.RecycleBlueprints.Values))
            {
                EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Updated, blueprint);
            }
        }

        private void LoadAI(int version)
        {
            ColonistController.AIs = this.LoadObject<Dictionary<int, ColonistAI>>();
            AnimalController.TortoiseAIs = this.LoadObject<Dictionary<int, TortoiseAI>>();
            if (version > 1)
            {
                AnimalController.RedBugAIs = this.LoadObject<Dictionary<int, RedBugAI>>();
                AnimalController.BlueBugAIs = this.LoadObject<Dictionary<int, BlueBugAI>>();
            }
            else
            {
                AnimalController.RedBugAIs.Clear();
                AnimalController.BlueBugAIs.Clear();
            }

            if (version > 2)
            {
                AnimalController.SnowTortoiseAIs = this.LoadObject<Dictionary<int, SnowTortoiseAI>>();
            }
            else
            {
                AnimalController.SnowTortoiseAIs.Clear();
            }
        }

        private void LoadWeather()
        {
            World.Temperature = this.LoadObject<int>();
            World.Wind = this.LoadObject<int>();
        }

        private void LoadParticles()
        {
            SmokeSimulator.SetAllParticles(this.LoadObject<Dictionary<int, List<SmokeParticle>>>());
            RocketExhaustSimulator.SetAllParticles(this.LoadObject<List<SmokeParticle>>());
            LanderExhaustSimulator.SetAllParticles(this.LoadList<SmokeParticle>());
            MicrobotParticleController.SetAllParticles(this.LoadObject<Dictionary<int, List<MicrobotParticle>>>());
        }

        private void LoadJobs(string header)
        {
            var version = (header == Headers.JobsV1) ? 0 : this.LoadObject<int>();
            World.ResourcesForDeconstruction = this.LoadObject<Dictionary<int, int>>();
            if (version >= 1) World.HasResourcesForDeconstructionBeenUsed = this.LoadObject<bool>();
            else World.HasResourcesForDeconstructionBeenUsed = true;
            ResourceDeconstructionController.Jobs = this.LoadObject<Dictionary<int, ResourceDeconstructionJob>>();
            if (version >= 2) ResourceStackingController.Deserialize(this.LoadObject<Dictionary<ItemType, int>>());
        }

        private void LoadSeeds()
        {
            PlantGrowthController.SeedQueue = new Queue<PlantSeed>();
            var seedList = this.LoadList<PlantSeed>();
            foreach (var seed in seedList.OrderBy(s => s.NextUpdateFrame)) PlantGrowthController.SeedQueue.Enqueue(seed);
        }

        private void LoadZones()
        {
            ZoneManager.HomeZone.Clear();
            var homeZone = this.LoadList<int>();
            foreach (var t in homeZone) ZoneManager.HomeZone.AddNode(t);
            ZoneManager.Loading = false;
        }
        
        private void LoadRooms()
        {
            RoomManager.Rooms = this.LoadList<Room>();
            RoomManager.UpdateDictionaries();
            foreach (var room in RoomManager.Rooms) room.SendUpdateEvents();
        }

        private void LoadGeology()
        {
            GeologyController.TilesToSurvey = this.LoadList<int>().ToHashSet();
        }

        private void LoadGroundCover()
        {
            GroundCoverController.Deserialize(this.LoadObject<Dictionary<int, int>>());
        }

        private void LoadGroundWater()
        {
            GroundWaterController.Deserialize(this.LoadObject<Dictionary<int, TileGroundWaterDetail>>(), this.GameVersion);
        }

        private void LoadMothership()
        {
            MothershipController.SetColonistPlaceholders(this.LoadObject<List<ColonistPlaceholder>>().Cast<IColonistPlaceholder>().ToList());
            MothershipController.SetPropertiesFromLoad(this.LoadObject<Dictionary<string, string>>());
        }

        private void LoadStats()
        {
            this.LoadObject<int>();   // Version number
            WorldStats.Deserialize(this.LoadObject<Dictionary<string, long>>());
        }

        private void LoadMisc(string version)
        {
            int vnum;
            if (version == Headers.MiscV1) vnum = 1;
            else if (version == Headers.MiscV2) vnum = 2;
            else vnum = this.LoadObject<int>();

            World.InitialEmptyTileFraction = this.LoadObject<float>();
            if (vnum < 3) WorldStats.Set(WorldStatKeys.ColonistsWoken, this.LoadObject<int>());   // Replaced by LoadStats
            World.DefaultCropId = this.LoadObject<int>();
            World.ClimateType = (ClimateType)this.LoadObject<int>();
            if (vnum > 1) World.LastTileSurveyedByGeologist = this.LoadObject<int>();
        }

        public List<T> LoadList<T>()
        {
            int count = Serializer.DeserializeWithLengthPrefix<int>(this.binaryReader.BaseStream, PrefixStyle.Base128);
            if (count <= 0) return count == 0 ? new List<T>() : null;
            return Serializer.DeserializeWithLengthPrefix<List<T>>(this.binaryReader.BaseStream, PrefixStyle.Base128);
        }

        public void IgnoreList()
        {
            int count = Serializer.DeserializeWithLengthPrefix<int>(this.binaryReader.BaseStream, PrefixStyle.Base128);
            if (count > 0)
            {
                Serializer.TryReadLengthPrefix(this.binaryReader.BaseStream, PrefixStyle.Base128, out int length);
                this.binaryReader.BaseStream.Seek(length, SeekOrigin.Current);
            }
        }

        public T LoadObject<T>()
        {
            return Serializer.DeserializeWithLengthPrefix<T>(this.binaryReader.BaseStream, PrefixStyle.Base128);
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
                this.binaryReader.Close();
                this.fileStream.Close();
                this.binaryReader.Dispose();
                this.fileStream.Dispose();
            }
        }
    }
}

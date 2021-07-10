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
    using World.Particles;
    using World.Projects;
    using World.Rooms;
    using World.Terrain;
    using World.Zones;
    using WorldControllers;

    public class GameSaver : IDisposable
    {
        private readonly FileStream fileStream;
        private readonly BinaryFormatter binaryFormatter;
        private readonly BinaryWriter binaryWriter;

        public GameSaver(string path)
        {
            this.fileStream = new FileStream(path, FileMode.Create);
            this.binaryFormatter = new BinaryFormatter();
            this.binaryWriter = new BinaryWriter(this.fileStream);
        }

        public void Save(Dictionary<string, string> properties, Vector2i scrollPosition, int zoomLevel)
        {
            this.binaryFormatter.Serialize(this.fileStream, GameVersion.CurrentGameVersion);
            this.binaryFormatter.Serialize(this.fileStream, World.InitialGameVersion);
            Serializer.SerializeWithLengthPrefix(this.fileStream, properties, PrefixStyle.Base128);

            this.binaryFormatter.Serialize(this.fileStream, scrollPosition);
            this.binaryFormatter.Serialize(this.fileStream, zoomLevel);
            this.binaryFormatter.Serialize(this.fileStream, World.Width);
            this.binaryFormatter.Serialize(this.fileStream, World.Height);

            // Time
            Serializer.SerializeWithLengthPrefix(this.binaryWriter.BaseStream, World.WorldTime, PrefixStyle.Base128);

            // Terrain
            foreach (var tile in World.BigTiles)
            {
                this.binaryWriter.Write((byte)tile.TerrainType);
                this.binaryWriter.Write((int)tile.BigTileTextureIdentifier);
            }

            // Ore and soil
            var resources = new Dictionary<int, MineTileResource>();
            var soilTypes = new Dictionary<int, BiomeType>();
            var groundCoverDensities = new Dictionary<int, int>();
            foreach (var tile in World.SmallTiles.Where(t => t.TerrainType == TerrainType.Dirt || t.TerrainType == TerrainType.Coast))
            {
                if (tile.TerrainType == TerrainType.Dirt)
                {
                    var resource = tile.GetResources();
                    if (resource is MineTileResource mineTileResource) resources.Add(tile.Index, mineTileResource);

                    if (tile.BiomeType != BiomeType.Dry)
                    {
                        soilTypes.Add(tile.Index, tile.BiomeType);
                    }
                }

                if (tile.GroundCoverDensity > 0)
                {
                    groundCoverDensities.Add(tile.Index, tile.GroundCoverDensity);
                }
            }

            this.SaveObject(Headers.OreAndSoilV1);
            this.SaveObject(resources);
            this.SaveObject(soilTypes);
            this.SaveObject(groundCoverDensities);

            // Things
            this.SaveObject(Headers.ThingsV1);
            var allThings = World.GetAllThings();
            foreach (var thing in allThings) thing.BeforeSerialization();
            this.SaveList(allThings);
            this.SaveList(World.GetFoodCounts().ToList());

            // Blueprint manager
            this.SaveObject(Headers.BlueprintsV1);
            foreach (var thing in World.ConfirmedBlueprints.Values) thing.BeforeSerialization();
            foreach (var thing in World.RecycleBlueprints.Values) thing.BeforeSerialization();
            this.SaveObject(World.ConfirmedBlueprints);
            this.SaveObject(World.RecycleBlueprints);
            this.SaveObject(BlueprintController.IsVirtualBlueprintBlocked);

            // Prefabs
            this.SaveObject(Headers.PrefabsV1);
            this.SaveObject(World.Prefabs);

            // AI
            this.SaveSectionHeader(Headers.AI, 3);
            this.SaveObject(ColonistController.AIs);
            this.SaveObject(AnimalController.TortoiseAIs);
            this.SaveObject(AnimalController.RedBugAIs);
            this.SaveObject(AnimalController.BlueBugAIs);
            this.SaveObject(AnimalController.SnowTortoiseAIs);

            // Weather
            this.SaveObject(Headers.WeatherV1);
            this.SaveObject(World.Temperature);
            this.SaveObject(World.Wind);

            // Smoke and harvesting particle effects
            this.SaveObject(Headers.ParticlesV1);
            this.SaveObject(SmokeSimulator.GetAllParticles());
            this.SaveObject(RocketExhaustSimulator.GetAllParticles());
            this.SaveList(LanderExhaustSimulator.GetAllParticles());
            this.SaveObject(MicrobotParticleController.GetAllParticles());

            // Jobs
            this.SaveSectionHeader(Headers.Jobs, 2);
            this.SaveObject(World.ResourcesForDeconstruction);
            this.SaveObject(World.HasResourcesForDeconstructionBeenUsed);
            this.SaveObject(ResourceDeconstructionController.Jobs);
            this.SaveObject(ResourceStackingController.Serialize());

            // Seeds
            this.SaveObject(Headers.SeedsV1);
            this.SaveList(PlantGrowthController.SeedQueue.ToList());

            // Infections
            //this.SaveList(InfectionController.DistinctInfections);
            //this.SaveList(InfectionController.InfectionPool);
            //this.SaveObject(InfectionController.EvolutionLevel);

            this.SaveObject(Headers.WarningsV1);
            this.SaveObject(WarningsController.Serialize());

            this.SaveObject(Headers.ZonesV1);
            this.SaveList(ZoneManager.HomeZone.Nodes.Keys.ToList());

            this.SaveObject(Headers.RoomsV1);
            this.SaveList(RoomManager.Rooms);

            this.SaveObject(Headers.ProjectsV1);
            this.SaveObject(ProjectManager.GetProjectsRemainingWork());

            this.SaveObject(Headers.MothershipV1);
            this.SaveObject(MothershipController.GetColonistPlaceholders());
            this.SaveObject(MothershipController.GetPropertiesForSave());

            this.SaveObject(Headers.GeologyV1);
            this.SaveList(GeologyController.TilesToSurvey.ToList());

            this.SaveSectionHeader(Headers.GroundCover, 1);
            this.SaveObject(GroundCoverController.Serialize());

            this.SaveSectionHeader(Headers.GroundWater, 1);
            this.SaveObject(GroundWaterController.Serialize());

            this.SaveSectionHeader(Headers.Stats, 1);
            this.SaveObject(WorldStats.Serialize());

            this.SaveSectionHeader(Headers.Commentary, 1);
            this.SaveObject(CommentaryController.Serialize());

            this.SaveSectionHeader(Headers.CheckList, 1);
            this.SaveObject(CheckListController.Serialize());

            this.SaveSectionHeader(Headers.Misc, 3);
            this.SaveObject(World.InitialEmptyTileFraction);
            this.SaveObject(World.DefaultCropId);
            this.SaveObject((int)World.ClimateType);
            this.SaveObject(World.LastTileSurveyedByGeologist);

            this.SaveObject(Headers.EOF);
        }

        private void SaveList<T>(List<T> list)
        {
            Serializer.SerializeWithLengthPrefix(this.binaryWriter.BaseStream, list == null ? -1 : list.Count, PrefixStyle.Base128);
            if (list.Count > 0)
            {
                Serializer.SerializeWithLengthPrefix(this.binaryWriter.BaseStream, list, PrefixStyle.Base128);
            }
        }

        private void SaveObject<T>(T obj)
        {
            Serializer.SerializeWithLengthPrefix(this.binaryWriter.BaseStream, obj, PrefixStyle.Base128);
        }

        private void SaveSectionHeader(string header, int version)
        {
            Serializer.SerializeWithLengthPrefix(this.binaryWriter.BaseStream, header, PrefixStyle.Base128);
            Serializer.SerializeWithLengthPrefix(this.binaryWriter.BaseStream, version, PrefixStyle.Base128);
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
                this.binaryWriter.Close();
                this.fileStream.Close();
                this.binaryWriter.Dispose();
                this.fileStream.Dispose();
            }
        }
    }
}

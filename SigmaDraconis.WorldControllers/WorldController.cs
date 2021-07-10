namespace SigmaDraconis.WorldControllers
{
    using Draconis.Shared;
    using Shared;
    using World;
    using World.Rooms;
    using World.Particles;
    using World.Projects;
    using World.Zones;

    public static class WorldController
    {
        public static void Update(bool isLastUpdateInFrame, Vector2f scrollPos, float zoom, float width, float height)
        {
            World.WorldTime.Increment();
            WeatherController.Update();
            ResourceNetworkController.UpdateStartOfFrame(false);
            ResourceDeconstructionController.Update();
            SmokeSimulator.Update();
            RocketExhaustSimulator.Update();
            LanderExhaustSimulator.Update();
            MicrobotParticleController.Update();

            //if (World.WorldTime.FrameNumber != lastFrame + 1 || !InfectionController.IsInitialised) InfectionController.Init();
            PlantGrowthController.Update();
            GroundWaterController.Update();
            //InfectionController.Update();
            FlyingInsectController.Update();
            WaterAnimalsController.Update(isLastUpdateInFrame);
            BirdsController.Update(isLastUpdateInFrame, scrollPos, zoom, width, height);
            RoomManager.Update();
            MothershipController.Update();

            World.Update();
            if (World.ClimateType != ClimateType.Snow) GroundCoverController.Update();  // Do after world update as it uses queue of World.TilesWithGroundCoverRemovedQueue

            if (World.WorldTime.FrameNumber % 31 == 0) WarningsController.Update();

            ResourceNetworkController.UpdateEndOfFrame();
        }

        public static void Clear(int mapSize = 0)
        {
            ResourceNetworkController.Clear();
            BlueprintController.Reset();
            RoomManager.Clear();
            World.Clear(mapSize);
            PathFinderBlockManager.Reset();
            ProjectManager.Init();
            PlantGrowthController.Clear();
            WaterAnimalsController.Stop();
            BirdsController.Stop();
            ResourceDeconstructionController.Clear();
            SmokeSimulator.Clear();
            RocketExhaustSimulator.Clear();
            LanderExhaustSimulator.Clear();
            MicrobotParticleController.Clear();
            MothershipController.Reset();
            WarningsController.Clear();
            GeologyController.Clear();
            GroundWaterController.Clear();
            GroundCoverController.Clear();
        }
    }
}

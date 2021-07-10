namespace SigmaDraconis.World.Rooms
{
    using Draconis.Shared;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using Buildings;
    using WorldInterfaces;

    [ProtoContract]
    public class Room : IRoom
    {
        private static int nextId = 1;

        [ProtoMember(1)]
        public float Light { get; set; } = 0;

        [ProtoMember(2)]
        public int Id { get; }

        [ProtoMember(3)]
        public List<int> TileIDs { get; } = new List<int>();

        [ProtoMember(4)]
        public List<int> RoofIDs
        {
            get { return this.Roofs?.Select(t => t.Id)?.ToList(); }
            set { this.Roofs = value.Select(t => World.GetThing(t)).OfType<IBuildableThing>().ToList(); }
        }

        [ProtoMember(5)]
        public double Temperature { get; set; }

        [ProtoMember(6)]
        private float artificialLight;

        [ProtoMember(7)]
        private float renderTopLight = 0;

        [ProtoMember(8)]
        public double HeatLossRate { get; private set; }

        public List<IBuildableThing> Roofs { get; set; }
        public List<ISmallTile> Tiles { get; private set; }

        public void SetTiles(IEnumerable<ISmallTile> tiles)
        {
            if (this.Tiles == null) this.Tiles = new List<ISmallTile>();
            this.Tiles.Clear();
            this.Tiles.AddRange(tiles);
            this.TileIDs.Clear();
            this.TileIDs.AddRange(tiles.Select(t => t.Index));
        }

        public void AddTiles(IEnumerable<ISmallTile> tiles)
        {
            if (this.Tiles == null) this.Tiles = new List<ISmallTile>();
            this.Tiles.AddRange(tiles);
            this.TileIDs.AddRange(tiles.Select(t => t.Index));
        }

        public float RenderTopLight
        {
            get { return this.renderTopLight; }
            set
            {
                if (Math.Abs(this.renderTopLight - value) > 0.001)
                {
                    this.renderTopLight = value;
                    this.SendUpdateEvents();
                }
            }
        }

        public float ArtificialLight
        {
            get { return this.artificialLight; }
            set
            {
                if (Math.Abs(this.artificialLight - value) > 0.001)
                {
                    this.artificialLight = value;
                    this.SendUpdateEvents();
                }
            }
        }

        public void SendUpdateEvents()
        {
            foreach (var thing in this.Tiles.SelectMany(t => t.ThingsAll).Distinct())
            {
                thing.UpdateRoom();
                var def = thing.Definition;
                if (def.RendererTypes.Any(r => !r.In(RendererType.Roof, RendererType.Bug, RendererType.Conduit, RendererType.Floor, RendererType.FloorB, RendererType.SnowAnimal)))
                {
                    EventManager.EnqueueRoomLightChangeEvent((thing as Thing).MainTile.Row, thing.ThingType);
                }
            }
        }

        public bool IsComplete => Roofs.All(r => r.IsReady);

        // Deserialization ctor
        private Room()
        {
        }

        public Room(List<ISmallTile> tiles)
        {
            this.Id = nextId++;
            this.SetTiles(tiles);
            this.Roofs = new List<IBuildableThing>();
            this.Light = 0.5f * (1f - World.WorldLight.NightLightFactor);
            this.Temperature = World.Temperature;
        }

        [ProtoAfterDeserialization]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Called by ProtoBuf")]
        private void AfterDeserialization()
        {
            if (nextId <= this.Id) nextId = this.Id + 1;
            if (this.Roofs == null) this.Roofs = new List<IBuildableThing>();
            if (this.Tiles == null) this.Tiles = this.TileIDs.Select(i => World.GetSmallTile(i)).ToList();
        }

        public void Update()
        {
            // For now, heat escapes at a rate proportial to the temperature difference from outside
            var tempDiff = this.Temperature - World.Temperature;
            this.HeatLossRate = 0.00005 * tempDiff;

            foreach (var door in World.GetThings<Door>(ThingType.Door).Where(d => this.TileIDs.Contains(d.MainTile.Index) && d.IsOpen))
            {
                var otherTile = door.AllTiles.FirstOrDefault(t => !this.TileIDs.Contains(t.Index));
                if (otherTile != null)
                {
                    var otherTemp = RoomManager.GetTileTemperature(otherTile.Index);
                    var diff = this.Temperature - otherTemp;
                    this.HeatLossRate += 0.002 * diff / this.TileIDs.Count;

                    var otherRoom = RoomManager.GetRoom(otherTile.Index);
                    if (otherRoom != null)
                    {
                        // Transfer a small amount of heat to the other room
                        var transfer = 0.002 * diff / (this.TileIDs.Count * otherRoom.TileIDs.Count);
                        otherRoom.Temperature += transfer;
                        this.HeatLossRate += transfer;
                    }
                }
            }

            this.Temperature -= this.HeatLossRate;

            this.Light = Math.Max(World.WorldLight.Brightness * 0.9f, this.ArtificialLight);
            this.RenderTopLight = this.ArtificialLight > 0 ? (float)Math.Max(this.ArtificialLight, 1.0 - (World.WorldLight.LightFactorN + World.WorldLight.LightFactorE + World.WorldLight.LightFactorS + World.WorldLight.LightFactorW)) : 0;
        }

        public override string ToString()
        {
            return $"Room {this.Id}, size = {this.Tiles.Count}";
        }
    }
}

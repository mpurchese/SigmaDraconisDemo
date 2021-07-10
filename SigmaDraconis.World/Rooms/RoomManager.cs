namespace SigmaDraconis.World.Rooms
{
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;

    public static class RoomManager
    {
        public static List<Room> Rooms = new List<Room>();

        private static readonly Dictionary<int, Room> tiles = new Dictionary<int, Room>();

        public static void Clear()
        {
            tiles.Clear();
        }

        public static void Update()
        {
            foreach (var room in Rooms) room.Update();
        }

        public static void UpdateDictionaries()
        {
            tiles.Clear();

            foreach (var room in Rooms)
            {
                foreach (var tile in room.Tiles)
                {
                    if (!tiles.ContainsKey(tile.Index)) tiles.Add(tile.Index, room);
                }
            }
        }

        public static void CreateRoom(List<IBuildableThing> newRoofs)
        {
            var roomTiles = newRoofs.Select(r => r.MainTile).ToList();
            var room = new Room(roomTiles) { Roofs = newRoofs };
            Rooms.Add(room);

            foreach (var tile in roomTiles)
            {
                if (tiles.ContainsKey(tile.Index)) tiles[tile.Index] = room;
                else tiles.Add(tile.Index, room);
            }

            room.SendUpdateEvents();
        }

        /// <summary>
        /// Check whether removing a wall will split join two rooms, and if so then do it.
        /// </summary>
        public static void MergeRooms(IWall wall)
        {
            var room1 = GetRoom(wall.MainTileIndex);
            var room2 = GetRoom(wall.MainTile.GetTileToDirection(wall.Direction).Index);
            if (room1 != null && room2 != null && room1 != room2)
            {
                var tempTot = (room1.Temperature * room1.Tiles.Count) + (room2.Temperature * room2.Tiles.Count);
                room1.AddTiles(room2.Tiles);
                room1.Roofs.AddRange(room2.Roofs);
                room1.Temperature = tempTot / room1.Tiles.Count;
                foreach (var t in room2.TileIDs) tiles[t] = room1;

                Rooms.Remove(room2);
                room1.SendUpdateEvents();
            }
        }

        /// <summary>
        /// Check whether a wall will split room into two, and if so then do it.
        /// </summary>
        public static void SplitRoom(IWall wall)
        {
            var t1 = wall.MainTile;
            var room1 = GetRoom(t1.Index);
            if (room1 == null) return;

            var t2 = wall.MainTile.GetTileToDirection(wall.Direction);
            var room2 = GetRoom(t2.Index);
            if (room1 == room2)
            {
                // Rooms only split if there is no other way from one to the other.
                var area1 = GetArea(t1);
                if (!area1.Contains(t2))
                {
                    var area2 = GetArea(t2);
                    var newRoom = new Room(area2.ToList())
                    {
                        Roofs = area2.SelectMany(a => a.ThingsPrimary.Where(b => b.ThingType == ThingType.Roof).OfType<IBuildableThing>()).ToList(),
                        Temperature = room1.Temperature
                    };

                    Rooms.Add(newRoom);
                    foreach (var t in newRoom.TileIDs) tiles[t] = newRoom;

                    room1.SetTiles(area1);
                    room1.Roofs = area1.SelectMany(a => a.ThingsPrimary.Where(b => b.ThingType == ThingType.Roof).OfType<IBuildableThing>()).ToList();
                    room1.ArtificialLight = 0;

                    room1.SendUpdateEvents();
                    newRoom.SendUpdateEvents();
                }
            }
        }

        public static List<IBuildableThing> GetAllRoofsForRoom(int tileIndex)
        {
            if (!tiles.ContainsKey(tileIndex)) return new List<IBuildableThing>();

            var room = tiles[tileIndex];
            if (room == null) return new List<IBuildableThing>();

            var result = room.Tiles.SelectMany(t => t.ThingsPrimary.Where(p => p.ThingType == ThingType.Roof)).OfType<IBuildableThing>().ToList();
            return result;
        }

        public static float GetTileLightLevel(int tileIndex, bool activeOnly = true)
        {
            return Mathf.Max(GetTileArtificialLightLevel(tileIndex, activeOnly), World.WorldLight.Brightness);
        }

        public static float GetTileArtificialLightLevel(int tileIndex, bool activeOnly = true)
        {
            if (World.WorldLight.NightLightFactor == 0f) return 0f;

            var tile = World.GetSmallTile(tileIndex);
            var lampLight = tile.LightSources.Values.Any() ? tile.LightSources.Values.Sum(h => (h.IsOn || !activeOnly) ? h.Amount : 0) : 0f;
            if (lampLight > 0.5f) lampLight = 0.5f;

            var roomLight = tiles.ContainsKey(tileIndex) ? Mathf.Min(tiles[tileIndex]?.Light ?? 0, 0.5f) : 0f;
            var artificialLight = Mathf.Min(World.WorldLight.NightLightFactor, Mathf.Max(lampLight, roomLight));
            return artificialLight;
        }

        public static float GetTileTemperature(int tileIndex, bool activeOnly = true, bool includeSleepPods = false)
        {
            var tile = World.GetSmallTile(tileIndex);
            if (!activeOnly && includeSleepPods)
            {
                // Check for sleep pod
                var pod = tile.ThingsPrimary.OfType<ISleepPod>().FirstOrDefault();
                if (pod?.IsHeaterSwitchedOn == true) return pod.TargetTemp;
            }

            var heat = tile.HeatSources.Values.Any() ? tile.HeatSources.Values.Sum(h => (h.IsOn || !activeOnly) ? h.Amount : 0) : 0f;
            if (!tiles.ContainsKey(tileIndex)) return heat + World.Temperature;

            var room = tiles[tileIndex];
            if (room == null) return heat + World.Temperature;

            return heat + (float)room.Temperature;
        }

        // Exclude heat due to client heater
        public static float GetTileTemperature(int tileIndex, int heaterID)
        {
            var tile = World.GetSmallTile(tileIndex);
            var heat = tile.HeatSources.Values.Any() ? tile.HeatSources.Select(h => (h.Key != heaterID && h.Value.IsOn) ? h.Value.Amount : 0).Max() : 0f;
            if (!tiles.ContainsKey(tileIndex)) return heat + World.Temperature;

            var room = tiles[tileIndex];
            if (room == null) return heat + World.Temperature;

            return heat + (float)room.Temperature;
        }

        public static Room GetRoom(int tileIndex)
        {
            if (!tiles.ContainsKey(tileIndex)) return null;
            return tiles[tileIndex];
        }

        // Used by recycling job to remove room
        public static void RemoveRoom(int tileIndex)
        {
            if (!tiles.ContainsKey(tileIndex)) return;

            // Removing one roof removes the whole room

            var room = tiles[tileIndex];
            room.Light = 0;
            room.SendUpdateEvents();
            Rooms.Remove(room);

            foreach (var tile in room.Tiles)
            {
                if (tiles.ContainsKey(tile.Index)) tiles.Remove(tile.Index);
            }
        }

        private static HashSet<ISmallTile> GetArea(ISmallTile t1)
        {
            // Get all tiles connected to a tile
            var openNodes = new List<ISmallTile> { t1 };
            var closedNodes = new HashSet<ISmallTile> { t1 };
            while (openNodes.Any())
            {
                foreach (var n in openNodes.ToList())
                {
                    openNodes.Remove(n);

                    var ne = n.TileToNE;
                    var se = n.TileToSE;
                    var sw = n.TileToSW;
                    var nw = n.TileToNW;

                    if (ne != null && !closedNodes.Contains(ne) && !n.HasWallOrDoorToDirection(Direction.NE))
                    {
                        openNodes.Add(ne);
                        closedNodes.Add(ne);
                    }

                    if (se != null && !closedNodes.Contains(se) && !n.HasWallOrDoorToDirection(Direction.SE))
                    {
                        openNodes.Add(se);
                        closedNodes.Add(se);
                    }

                    if (sw != null && !closedNodes.Contains(sw) && !n.HasWallOrDoorToDirection(Direction.SW))
                    {
                        openNodes.Add(sw);
                        closedNodes.Add(sw);
                    }

                    if (nw != null && !closedNodes.Contains(nw) && !n.HasWallOrDoorToDirection(Direction.NW))
                    {
                        openNodes.Add(nw);
                        closedNodes.Add(nw);
                    }
                }
            }

            return closedNodes;
        }
    }
}

namespace SigmaDraconis.AnimalAI
{
    using ProtoBuf;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.PathFinding;
    using World.Zones;
    using WorldInterfaces;
    using System;

    [ProtoContract]
    public class ActionWalk : ActionBase
    {
        private const int framesPerUpdate = 2;   // For performance optimisation

        [ProtoMember(1)]
        private PathNode n1 = null;    // The next node we are heading to next

        [ProtoMember(2)]
        private PathNode n2 = null;    // The next but one node

        [ProtoMember(3)]
        private float currentSpeed = 0;

        [ProtoMember(4)]
        public Path Path { get; private set; }

        [ProtoMember(5)]
        public Vector2f EndOffset;

        [ProtoMember(6)]
        public Direction EndDirection;

        [ProtoMember(7)]
        public float EndOffsetFlexibility;

        [ProtoMember(8)]
        public int? GivingWayTo;

        [ProtoMember(9)]
        public int WaitTimer;

        [ProtoMember(10)]
        public int IgnorePointBlocksTimer;

        [ProtoMember(11)]
        public bool IsStart;

        [ProtoMember(12)]
        public bool IsStartOffset;

        [ProtoMember(13)]
        private Dictionary<int, int> animalsInTheWay = new Dictionary<int, int>();   // Timers, we give up trying to go around another animal after a few frames

        private readonly List<Circle> blockingCircles = new List<Circle>();
        private readonly List<Tuple<float, float>> blockedAngleRanges = new List<Tuple<float, float>>();
        private readonly List<IThing> thingsToCheckForBlocks = new List<IThing>();
        private float currentMaxSpeed = 0;
        private float? forceAngle = null;
        private Vector2f currentTargetPos;
        private int frame = 0;
        private readonly float maxSpeed = 0.005f;
        private bool pathCheckFailed;
        private ISmallTile n1Tile;
        private ISmallTile n2Tile;

        // For detecting when colonists are stuck
        public Queue<Vector2f> historicalPositions = new Queue<Vector2f>();
        public int? StuckTileIndex = null;

        // Deserialisation ctor
        protected ActionWalk() { }

        public ActionWalk(IAnimal animal, Path path, Vector2f endOffset, Direction endDirection = Direction.None, float endOffsetFlexibility = 0.01f) : base(animal)
        {
            this.IsStart = true;
            this.IsStartOffset = animal.PositionOffset.Length() > 1f;
            this.Path = path;
            this.EndOffset = endOffset;
            this.EndDirection = endDirection;
            this.EndOffsetFlexibility = endOffsetFlexibility;
            var first = path.RemainingNodes.Peek();
            if (first.X == animal.MainTile.X && first.Y == animal.MainTile.Y) path.RemainingNodes.Dequeue();  // Current position, ignore
            if (path.RemainingNodes.Any()) this.n1 = path.RemainingNodes.Dequeue();
            if (path.RemainingNodes.Any()) this.n2 = path.RemainingNodes.Dequeue();
            this.maxSpeed = animal.ThingType == ThingType.RedBug ? 0.01f : 0.005f;

            this.ReleaseTileBlock();
        }

        public void DoBackgroundUpdate()
        {
            if (this.frame % framesPerUpdate != 0 || this.currentTargetPos == null) return;  // For performance reasons, only update every other frame

            try
            {
                var pos = this.Animal.Position;
                var posTile = World.GetSmallTile((int)(pos.X + 0.5f), (int)(pos.Y + 0.5f));
                if (this.n1 != null && this.n1Tile == null) this.n1Tile = World.GetSmallTile(this.n1.X, this.n1.Y);
                if (this.n2 != null && this.n2Tile == null) this.n2Tile = World.GetSmallTile(this.n2.X, this.n2.Y);

                var tilesToCheck = this.Animal.MainTile.AdjacentTiles8.ToDictionary(t => t.Index, t => t);
                if (posTile != this.Animal.MainTile && posTile != this.n1Tile)
                {
                    foreach (var tile in posTile.AdjacentTiles8)
                    {
                        if (!tilesToCheck.ContainsKey(tile.Index)) tilesToCheck.Add(tile.Index, tile);
                    }
                }

                if (this.n1Tile != null && this.n1Tile != this.Animal.MainTile)
                {
                    foreach (var tile in this.n1Tile.AdjacentTiles8)
                    {
                        if (!tilesToCheck.ContainsKey(tile.Index)) tilesToCheck.Add(tile.Index, tile);
                    }
                }

                if (this.n2Tile != null && this.n2Tile != this.Animal.MainTile && this.n2Tile != this.n1Tile && this.n2Tile != posTile)
                {
                    foreach (var tile in this.n2Tile.AdjacentTiles8)
                    {
                        if (!tilesToCheck.ContainsKey(tile.Index)) tilesToCheck.Add(tile.Index, tile);
                    }
                }

                if (!tilesToCheck.ContainsKey(this.Animal.MainTileIndex)) tilesToCheck.Add(this.Animal.MainTileIndex, this.Animal.MainTile);
                if (!tilesToCheck.ContainsKey(posTile.Index)) tilesToCheck.Add(posTile.Index, posTile);

                var things = new HashSet<int>();
                this.thingsToCheckForBlocks.Clear();
                this.thingsToCheckForBlocks.AddRange(tilesToCheck.Values.SelectMany(t => t.ThingsAll).Distinct().Where(t => t != this.Animal));

                this.blockingCircles.Clear();
                this.blockedAngleRanges.Clear();

                var targetVec = this.currentTargetPos - this.Animal.Position;
                var dist = Mathf.Min(targetVec.Length(), (currentMaxSpeed * 50f).Clamp(0.2f, 0.5f));

                foreach (var thing in this.thingsToCheckForBlocks)
                {
                    if (thing.ThingType == ThingType.Bush && (this.Animal.ThingType == ThingType.BlueBug || this.Animal.ThingType == ThingType.RedBug)) continue;
                    if (thing.ThingType == ThingType.SmallPlant4 && this.Animal.ThingType == ThingType.BlueBug) continue;
                    if (thing.ThingType == ThingType.BigSpineBush && this.Animal.ThingType == ThingType.SnowTortoise) continue;

                    var tileBlockModel = thing.TileBlockModel;

                    var tiles = thing is IMoveableThing ? new List<ISmallTile> { thing.MainTile } : thing.AllTiles;
                    foreach (var tile in tiles)
                    {
                        // Find the closest angle to tgtAngle that is not between a pair of object tangents
                        if (tileBlockModel == TileBlockModel.Circle || thing.ThingType == ThingType.LandingPod || thing.ThingType == ThingType.Colonist || thing.ThingType == ThingType.Tortoise)
                        {
                            var radius = this.Animal.ThingType == ThingType.Tortoise ? 0.6f : 0.4f;
                            var circle = new Circle(tile.TerrainPosition, radius);
                            this.blockingCircles.Add(circle);
                            this.AvoidCircle(dist, circle);
                        }
                        else if (tileBlockModel == TileBlockModel.Point && this.IgnorePointBlocksTimer == 0 && this.Path?.RemainingNodes.Any() == true)
                        {
                            var p = (Vector2f)tile.TerrainPosition;
                            if (thing is IPositionOffsettable po) p += po.PositionOffset;
                            if ((p - this.Animal.Position).Length() > 0.4f)
                            {
                                var circle = new Circle(p, 0.3f);
                                blockingCircles.Add(circle);
                                this.AvoidCircle(dist, circle);
                            }
                        }
                        else if (tileBlockModel == TileBlockModel.Square || tileBlockModel == TileBlockModel.Pod)
                        {
                            // Square is represented by 12 circles
                            var circles = new Circle[12] {
                            new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y - 0.5f, 0.15f),
                            new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y - 0.17f, 0.15f),
                            new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y + 0.17f, 0.15f),
                            new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y + 0.5f, 0.15f),
                            new Circle(tile.TerrainPosition.X - 0.17f, tile.TerrainPosition.Y - 0.5f, 0.15f),
                            new Circle(tile.TerrainPosition.X - 0.17f, tile.TerrainPosition.Y + 0.5f, 0.15f),
                            new Circle(tile.TerrainPosition.X + 0.17f, tile.TerrainPosition.Y - 0.5f, 0.15f),
                            new Circle(tile.TerrainPosition.X + 0.17f, tile.TerrainPosition.Y + 0.5f, 0.15f),
                            new Circle(tile.TerrainPosition.X + 0.5f, tile.TerrainPosition.Y - 0.5f, 0.15f),
                            new Circle(tile.TerrainPosition.X + 0.5f, tile.TerrainPosition.Y - 0.17f, 0.15f),
                            new Circle(tile.TerrainPosition.X + 0.5f, tile.TerrainPosition.Y + 0.17f, 0.15f),
                            new Circle(tile.TerrainPosition.X + 0.5f, tile.TerrainPosition.Y + 0.5f, 0.15f)
                        };
                            blockingCircles.AddRange(circles);
                            foreach (var circle in circles) this.AvoidCircle(dist, circle);
                        }
                        else if (tileBlockModel == TileBlockModel.Wall || tileBlockModel == TileBlockModel.Door)
                        {
                            if (thing.MainTile == tile)
                            {
                                // Wall is represented by 4 circles
                                var circles = (thing as IWall).Direction == Direction.SE
                                    ? new Circle[4] {
                                new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y + 0.5f, 0.2f),
                                new Circle(tile.TerrainPosition.X - 0.17f, tile.TerrainPosition.Y + 0.5f, 0.2f),
                                new Circle(tile.TerrainPosition.X + 0.17f, tile.TerrainPosition.Y + 0.5f, 0.2f),
                                new Circle(tile.TerrainPosition.X + 0.5f, tile.TerrainPosition.Y + 0.5f, 0.2f) }
                                    : new Circle[4] {
                                new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y - 0.5f, 0.2f),
                                new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y - 0.17f, 0.2f),
                                new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y + 0.17f, 0.2f),
                                new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y + 0.5f, 0.2f)
                                };

                                blockingCircles.AddRange(circles);
                                foreach (var circle in circles) this.AvoidCircle(dist, circle);
                            }
                        }
                        else if (thing is IAnimal c && this.IgnorePointBlocksTimer == 0)
                        {
                            if (this.Animal.MovingAngle.HasValue && (AnimalController.GetAI(c.Id) is IAnimalAI otherAI)
                                && Mathf.Abs(Mathf.AngleBetween((c.Position - this.Animal.Position).Angle(), this.Animal.MovingAngle.Value)) < Mathf.PI * 0.5f) // In front
                            {
                                // If other animal is giving way to us, then ask them to stop so we can get past easily
                                // If they are close to their destination they may refuse.
                                var otherWaiting = false;
                                if (c.IsDead)
                                {
                                    otherWaiting = true;
                                }
                                else if (otherAI.CurrentActivity?.CurrentAction is ActionWalk w && w.GivingWayTo == this.Animal.Id)
                                {
                                    otherWaiting = w.RequestWait();
                                }

                                if (otherAI.CurrentActivity?.CurrentAction is ActionWalk aw && aw.Path?.EndPosition == this.Path?.EndPosition)
                                {
                                    // Going to the same tile!
                                    this.Path = null;
                                    this.IsFailed = true;
                                    this.IsFinished = true;
                                    return;
                                }

                                // If animal is moving in the same direction and is in front, then we should give way
                                if (!otherWaiting && c.MovingAngle.HasValue && c.CurrentSpeed > 0.002f
                                    && Mathf.Abs(Mathf.AngleBetween(c.MovingAngle.Value, this.Animal.MovingAngle.Value)) < Mathf.PI * 0.5f)  // Similar direction (<90 deg difference)
                                {
                                    var distance = (c.Position - this.Animal.Position).Length() - 0.4f;
                                    currentMaxSpeed = Mathf.Max(0, Mathf.Min(currentMaxSpeed, distance * 0.5f));
                                    this.GivingWayTo = (currentMaxSpeed < this.maxSpeed) ? c.Id : (int?)null;
                                }
                            }

                            // This mechanism allows us to give up trying to go around another colonist, and just go through them.
                            if (this.animalsInTheWay == null) this.animalsInTheWay = new Dictionary<int, int>();
                            if (!this.animalsInTheWay.ContainsKey(c.Id)) this.animalsInTheWay.Add(c.Id, 0);
                            else this.animalsInTheWay[c.Id] += 2;

                            if (this.animalsInTheWay[c.Id] < 120)
                            {
                                if ((c.Position - this.Animal.Position).Length() > 0.5f)
                                {
                                    var circle = new Circle(c.Position.X, c.Position.Y, 0.36f);
                                    blockingCircles.Add(circle);
                                    this.AvoidCircle(dist * 2f, circle);
                                }
                            }
                        }
                    }
                }

                this.pathCheckFailed = this.Path?.RemainingNodes?.Any() == true && !this.CheckPath();
            }
            catch
            {
                // Non-critical async code - ignore errors here
            }
        }

        public override void Update()
        {
            if (this.WaitTimer > 0)
            {
                if (this.WaitTimer > 0) this.WaitTimer--;
                return;
            }

            foreach (var kv in this.animalsInTheWay.Where(k => k.Value > 0).ToList()) this.animalsInTheWay[kv.Key]--;

            this.frame++;
            if (this.frame % framesPerUpdate != 0) return;  // For performance reasons, only update every other frame

            this.GivingWayTo = null;
            var pos = this.Animal.Position;
            var n1Pos = new Vector2f(this.n1.X, this.n1.Y);
            this.currentMaxSpeed = this.maxSpeed;  // For giving way to other colonists
            var dontCutCorner = false;
            this.forceAngle = null;

            if (this.n1Tile == null && this.n1 != null) this.n1Tile = World.GetSmallTile(this.n1.X, this.n1.Y);
            if (this.n2Tile == null && this.n2 != null) this.n2Tile = World.GetSmallTile(this.n2.X, this.n2.Y);

            if (this.pathCheckFailed)
            {
                // Path is blocked, so stop.
                this.Path.RemainingNodes.Clear();
                this.n2 = null;
                this.n2Tile = null;
            }
            else if (this.n2 != null)
            {
                var globalNode = ZoneManager.AnimalZone.Nodes[this.n1Tile.Index];
                if (globalNode.GetLink(this.n1.ForwardDirection) == null)
                {
                    // Path is blocked, so stop.
                    this.Path.RemainingNodes.Clear();
                    this.n2 = null;
                    this.n2Tile = null;
                }
            }

            var posTile = World.GetSmallTile((int)(pos.X + 0.5f), (int)(pos.Y + 0.5f));
            if (posTile == null)
            {
                // Something wrong here
                this.IsFinished = true;
                this.Animal.IsMoving = false;
                this.Animal.UpdateRenderPos();
                return;
            }

            var n0 = posTile != null && ZoneManager.AnimalZone.ContainsNode(posTile.Index) ? ZoneManager.AnimalZone.Nodes[posTile.Index] : null;
            if (n0 != null && this.n1 != null && n0.AllLinks.Any() && (n0.X != this.n1.X || n0.Y != this.n1.Y) && n0.AllLinks.All(l => l.X != this.n1.X && l.Y != this.n1.Y))
            {
                // Something went wrong here, next tile is not adjacent, stop.
                this.IsFinished = true;
                this.Animal.IsMoving = false;
                this.Animal.SetPosition(posTile);
                this.Animal.UpdateRenderPos();

                //EventManager.MovedAnimals.AddIfNew(this.Animal.Id);
                base.Update();
                return;
            }

            // If at n1 or closer to n2 than n1, then shift waypoints and update home tile
            if (this.n2 != null)
            {
                var n2Pos = new Vector2f(this.n2.X, this.n2.Y);
                var d1 = n1Pos - pos;
                var d2 = n2Pos - pos;
                var d1length = d1.Length();
                if (d1length < 0.1f || (d2.Length() < d1length && CanCutCorner(posTile, this.n1Tile, this.n2Tile)))   // Don't cut corner around anything with a square block, as we tend to get stuck
                {
                    this.n1 = this.n2;
                    if (this.Path.RemainingNodes.Any())
                    {
                        this.n2 = this.Path.RemainingNodes.Dequeue();
                    }
                    else
                    {
                        this.n2 = null;
                        this.n2Tile = null;
                    }

                    this.n1Tile = this.n1 != null ? World.GetSmallTile(this.n1.X, this.n1.Y) : null;
                    this.n2Tile = this.n2 != null ? World.GetSmallTile(this.n2.X, this.n2.Y) : null;
                }
            }

            if (this.currentSpeed < 0.01) dontCutCorner = true;  // Don't start corner cutting until we get up some speed, helps avoid getting stuck
            if (posTile.X == this.Path.StartPosition.X && posTile.Y == this.Path.StartPosition.Y && this.IsStartOffset)
            {
                // Don't cut corners if starting at an offset
                dontCutCorner = true;
            }

            // Pick our target.  If it's the last one then it may have an offset.
            var target = (!dontCutCorner && this.n2 != null) ? this.n2 : this.n1;
            this.currentTargetPos = new Vector2f(target.X, target.Y);

            if (target == this.n1 && target.X == this.Path.EndPosition.X && target.Y == this.Path.EndPosition.Y)
            {
                this.currentTargetPos += this.EndOffset;  // Head for offset only when at end of path
            }


            var targetVec = this.currentTargetPos - this.Animal.Position;
            var tgtAngle = targetVec.Angle();
            var dist = Mathf.Min(targetVec.Length(), (currentMaxSpeed * 50f).Clamp(0.2f, 0.5f));

            if (!this.GivingWayTo.HasValue && blockedAngleRanges.Any())
            {
                var newAngle1 = tgtAngle;
                var newAngle2 = tgtAngle;
                while (1 == 1)
                {
                    if (blockedAngleRanges.All(r => (r.Item1 > newAngle1 || r.Item2 < newAngle1) && (r.Item1 - Mathf.twoPI > newAngle1 || r.Item2 - Mathf.twoPI < newAngle1)))
                    {
                        tgtAngle = newAngle1;
                        break;
                    }

                    if (blockedAngleRanges.All(r => (r.Item1 > newAngle2 || r.Item2 < newAngle2) && (r.Item1 - Mathf.twoPI > newAngle2 || r.Item2 - Mathf.twoPI < newAngle2)
                        && (r.Item1 + Mathf.twoPI > newAngle2 || r.Item2 + Mathf.twoPI < newAngle2)))
                    {
                        tgtAngle = newAngle2;
                        break;
                    }

                    newAngle1 -= 0.1f;
                    newAngle2 += 0.1f;
                    if (newAngle2 - newAngle1 > Mathf.PI * 1.99f)
                    {
                        currentMaxSpeed = 0f;
                        break;
                    }
                }

                if (newAngle1 != tgtAngle && newAngle2 != tgtAngle)
                {
                    if (newAngle1 > tgtAngle) newAngle1 -= Mathf.twoPI;
                    if (newAngle2 < tgtAngle) newAngle2 += Mathf.twoPI;
                    var diff1 = (tgtAngle - newAngle1) % (Mathf.twoPI);
                    var diff2 = (newAngle2 - tgtAngle) % (Mathf.twoPI);
                    tgtAngle = diff1 > diff2 ? newAngle2 : newAngle1;
                }
            }

            this.currentTargetPos = this.Animal.Position + (new Vector2f(0, -dist).Rotate(tgtAngle));

            var delta = this.currentTargetPos - pos;
            var d = delta.Length();
            if (d.ApproxEquals(0, 0.001f) || (this.currentSpeed < 0.004f && d.ApproxEquals(0, this.EndOffsetFlexibility)))
            {
                this.Animal.MovingAngle = null;

                // In end position, rotate if required
                if (this.EndDirection == Direction.None) this.IsFinished = true;
                else
                {
                    var angle = DirectionHelper.GetAngleFromDirection(this.EndDirection);
                    this.IsFinished = this.RotateToAngle(angle, out float a, Mathf.PI * framesPerUpdate / 120f);
                }

                this.Animal.IsMoving = !this.IsFinished;
            }
            else
            {
                if (this.forceAngle.HasValue)
                {
                    tgtAngle = this.forceAngle.Value;
                }

                if (!this.RotateToAngle(tgtAngle - (Mathf.PI * 0.25f), out float a, Mathf.PI * framesPerUpdate / 120f))
                {
                    // Slow down if rotating close to target, otherwise can end up circling
                    this.currentSpeed = Mathf.Min(this.currentSpeed, d);
                }
                else this.IsStart = false;   // Facing right direction, can start moving

                foreach (var circle in this.blockingCircles)
                {
                    var objAngle = Mathf.Abs(Mathf.AngleBetween((circle.Centre - this.Animal.Position).Angle(), this.Animal.MovingAngle.GetValueOrDefault()));
                    if (objAngle < Mathf.PI * 0.48f)   // In front  
                    {
                        var distance = (circle.Centre - this.Animal.Position).Length() - circle.Radius;
                        distance = distance * 1f / Mathf.Cos(objAngle);
                        if (distance * 0.8f < currentMaxSpeed)
                        {
                            currentMaxSpeed = Math.Max(distance * 0.8f, this.maxSpeed * 0.1f);
                        }
                    }
                }

                if (!this.IsStart || this.currentSpeed > 0)
                {
                    this.currentSpeed = Mathf.Min(this.currentSpeed + (this.maxSpeed * this.Animal.Acceleration), currentMaxSpeed);
                }

                if (this.currentSpeed > currentMaxSpeed * 0.1f && this.currentSpeed * 20f > d) this.currentSpeed = Mathf.Max(currentMaxSpeed * 0.1f, 0.05f * d);

                if (this.forceAngle.HasValue && Mathf.Abs(a) > 0.1f)
                {
                    this.currentSpeed = 0f;
                }
                else if (Math.Abs(a) > 1.0f && this.currentSpeed > 0.1f * this.maxSpeed) this.currentSpeed = 0.1f * this.maxSpeed;
                else if (Math.Abs(a) > 0.5f && this.currentSpeed > 0.5f * this.maxSpeed) this.currentSpeed = 0.5f * this.maxSpeed;

                // Modified speed makes motion less smooth and so hopefully appear more realistic
                var modifiedSpeed = this.currentSpeed;
                if (this.Animal.ThingType == ThingType.Tortoise && this.currentSpeed > 0.5f * this.maxSpeed) modifiedSpeed += (0.3f * this.maxSpeed * Mathf.Sin(this.frame / 4f));

                var vec = new Vector2f(0, -modifiedSpeed).Rotate(this.Animal.Rotation + (Mathf.PI * 0.25f));

                this.Animal.Position += vec * framesPerUpdate;
                this.Animal.CurrentSpeed = this.currentSpeed * framesPerUpdate;
                this.Animal.MovingAngle = this.Animal.Rotation + (Mathf.PI * 0.25f);
                this.Animal.IsMoving = true;
            }

            var targetTile = World.GetSmallTile(target.X, target.Y);
            var currentTile = World.GetSmallTile((int)(this.Animal.Position.X + 0.5f), (int)(this.Animal.Position.Y + 0.5f)) ?? this.Animal.MainTile;
            if (targetTile != currentTile 
                && DirectionHelper.GetDirectionFromAdjacentPositions(currentTile.X, currentTile.Y, targetTile.X, targetTile.Y) == Direction.None
                && DirectionHelper.GetDirectionFromAdjacentPositions(currentTile.X, currentTile.Y, this.n1Tile.X, this.n1Tile.Y) != Direction.None)
            {
                // Target tile not adjacent, set N1 to be the target for the purpose of setting colonist position on the terrain
                targetTile = this.n1Tile;
            }

            this.historicalPositions.Enqueue(this.Animal.Position);
            if (this.historicalPositions.Count > 90)
            {
                var p = this.historicalPositions.Dequeue();
                var diff = (p - this.Animal.Position).Length();
                if (diff < 0.3f)
                {
                    if (this.IgnorePointBlocksTimer == 0)
                    {
                        this.IgnorePointBlocksTimer = 1;
                    }
                    else if (this.IgnorePointBlocksTimer > 100)
                    {
                        // Stuck
                        this.IsFailed = true;
                        this.IsFinished = true;
                        this.Animal.IsMoving = false;
                        this.StuckTileIndex = n1Tile != posTile ? n1Tile?.Index : null;
                        this.Animal.UpdateRenderPos();
                    }
                }
                else this.IgnorePointBlocksTimer = 0;
            }

            if (this.IgnorePointBlocksTimer > 0)
            {
                this.IgnorePointBlocksTimer++;
                if (this.IgnorePointBlocksTimer > 60) this.IgnorePointBlocksTimer = 0;
            }

            this.Animal.UpdateRenderPos();

            //EventManager.MovedAnimals.AddIfNew(this.Animal.Id);

            base.Update();
        }

        public bool RequestWait()
        {
            // Another colonist is asking us to wait so that we can get past.
            if (!this.Path?.RemainingNodes.Any() == true && this.n2 == null) return false;  // Almost at destination, would rather not stop now.

            this.WaitTimer = 30;
            return true;
        }

        private void AvoidCircle(float dist, Circle circle)
        {
            if (FindTangents(circle, this.Animal.Position, dist, out float tangent1, out float tangent2))
            {
                this.blockedAngleRanges.Add(new Tuple<float, float>(tangent1, tangent2));
            }
        }

        private bool FindTangents(Circle circle, Vector2f colonistPos, float distance, out float tangent1, out float tangent2)
        {
            tangent1 = 0;
            tangent2 = 0;

            var a = circle.Centre - colonistPos;                  // Vec from colonist to object
            var objAngle = a.Angle();                   // Angle to the object
            var al = a.Length();
            if (al - circle.Radius > distance) return false;   // Object is far away

            // Angle between object and tangent, or 90 degrees if we are inside the circle 
            if (circle.Radius > al)
            {
                this.forceAngle = objAngle > Mathf.PI ? objAngle + Mathf.PI : objAngle - Mathf.PI;
                return true;
            }

            var asin = circle.Radius < al ? Mathf.Asin(circle.Radius / al) : Mathf.PI * 0.51f;
            tangent1 = objAngle - asin;
            tangent2 = objAngle + asin;
            if (tangent1 < 0)
            {
                tangent1 += Mathf.PI * 2f;
                tangent2 += Mathf.PI * 2f;
            }

            return true;
        }

        private bool CheckPath()
        {
            foreach (var node in this.Path.RemainingNodes.Take(4))    // Max 4 tiles ahead for animals
            {
                if (node.ForwardDirection == Direction.None) return true;  // End of path

                var index = node.X + (node.Y * World.Width * 3);
                if (!ZoneManager.AnimalZone.ContainsNode(index)) return false;   // Something went wrong
                var zoneNode = ZoneManager.AnimalZone.Nodes[index];
                if (zoneNode.GetLink(node.ForwardDirection) == null) return false;  // Path blocked
                var tile = World.GetSmallTile(node.X, node.Y);
                if (tile == null) return false;   // Something went wrong
                if (tile.ThingsAll.Any(t => t is IAnimal && t != this.Animal)) return false;  // Another animal or colonist in the way
            }

            return true;
        }

        private static bool CanCutCorner(ISmallTile posTile, ISmallTile n1, ISmallTile n2)
        {
            if (posTile == null || n1 == null || n2 == null || posTile == n1) return true;

            if (n2.ThingsAll.Any(t => t.ThingType == ThingType.SleepPod)) return false;   // Don't cut corner into sleep pod

            if (posTile.AdjacentTiles8.Contains(n2)) return false;  // Can't cut between adjacent tiles

            ISmallTile cornerTile = null;
            if (posTile.TileToNE == n1 && n1.TileToNW == n2) cornerTile = posTile.TileToN;
            else if (posTile.TileToNE == n1 && n1.TileToSE == n2) cornerTile = posTile.TileToE;
            else if (posTile.TileToSE == n1 && n1.TileToNE == n2) cornerTile = posTile.TileToE;
            else if (posTile.TileToSE == n1 && n1.TileToSW == n2) cornerTile = posTile.TileToS;
            else if (posTile.TileToSW == n1 && n1.TileToSE == n2) cornerTile = posTile.TileToS;
            else if (posTile.TileToSW == n1 && n1.TileToNW == n2) cornerTile = posTile.TileToW;
            else if (posTile.TileToNW == n1 && n1.TileToSW == n2) cornerTile = posTile.TileToW;
            else if (posTile.TileToNW == n1 && n1.TileToNE == n2) cornerTile = posTile.TileToN;

            // Check for square blocks
            return cornerTile == null || cornerTile.ThingsAll.All(t => t.TileBlockModel != TileBlockModel.Square && t.ThingType != ThingType.SleepPod);
        }
    }
}

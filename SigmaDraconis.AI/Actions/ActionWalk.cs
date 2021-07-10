namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Cards.Interface;
    using Config;
    using Shared;
    using World;
    using World.PathFinding;
    using World.Zones;
    using WorldInterfaces;
    using System;

    [ProtoContract]
    public class ActionWalk : ActionBase
    {
        public const float MaxSpeed = 0.02f;

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
        public bool IsWorking;

        [ProtoMember(14)]
        public bool IsEscapingBlock;

        [ProtoMember(15)]
        private Dictionary<int, int> colonistsInTheWay = new Dictionary<int, int>();   // Timers, we give up trying to go around a colonist after a few frames

        private readonly List<Tuple<float, float>> blockedAngleRanges = new List<Tuple<float, float>>();
        private List<ISmallTile> tilesToCheck = null;
        private float? forceAngle = null;

        // For detecting when colonists are stuck
        public Queue<Vector2f> historicalPositions = new Queue<Vector2f>();
        public int? StuckTileIndex = null;

        // Deserialisation ctor
        protected ActionWalk() { }

        public ActionWalk(IColonist colonist, Path path, Vector2f endOffset, Direction endDirection = Direction.None, float endOffsetFlexibility = 0.01f) : base(colonist)
        {
            this.IsStart = true;
            this.IsStartOffset = colonist.PositionOffset.Length() > 1f;
            this.IsWorking = this.Colonist.IsWorking || this.Colonist.ActivityType.GetAttribute<IsWorkAttribute>()?.Value == true;
            this.Path = path;
            this.EndOffset = endOffset;
            this.EndDirection = endDirection;
            this.EndOffsetFlexibility = endOffsetFlexibility;
            var first = path.RemainingNodes.Peek();
            if (first.X == colonist.MainTile.X && first.Y == colonist.MainTile.Y) path.RemainingNodes.Dequeue();  // Current position, ignore
            if (path.RemainingNodes.Any()) this.n1 = path.RemainingNodes.Dequeue();
            if (path.RemainingNodes.Any()) this.n2 = path.RemainingNodes.Dequeue();

            this.ReleaseTileBlock();
        }

        public override void Update()
        {
            this.Colonist.IsWorking = false;
            if (this.WaitTimer > 0 || this.Colonist.WaitForDoor || this.Colonist.RaisedArmsFrame > 0)
            {
                if (this.WaitTimer > 0) this.WaitTimer--;
                this.Colonist.WaitForDoor = false;
                return;
            }

            foreach (var kv in this.colonistsInTheWay.Where(k => k.Value > 0).ToList()) this.colonistsInTheWay[kv.Key]--;

            if (n1 == null)
            {
                this.IsFinished = true;
                this.Colonist.IsMoving = false;
                base.Update();
                return;
            }

            this.GivingWayTo = null;
            var pos = this.Colonist.Position;
            var n1Pos = new Vector2f(n1.X, n1.Y);
            var n1Tile = World.GetSmallTile(n1.X, n1.Y);
            var n2Tile = n2 != null ? World.GetSmallTile(n2.X, n2.Y) : null;
            var currentMaxSpeed = MaxSpeed;  // For giving way to other colonists
            var dontCutCorner = false;
            this.forceAngle = null;

            if (this.Path?.RemainingNodes?.Any() == true && !this.CheckPath())
            {
                // Path is blocked, so stop.
                this.Path.RemainingNodes.Clear();
                n2 = null;
            }
            else if (n2 != null)
            {
                var globalNode = ZoneManager.GlobalZone.Nodes[n1Tile.Index];
                if (globalNode.GetLink(n1.ForwardDirection) == null)
                {
                    // Path is blocked, so stop.
                    this.Path.RemainingNodes.Clear();
                    n2 = null;
                }
            }

            var posTile = World.GetSmallTile((int)(pos.X + 0.5f), (int)(pos.Y + 0.5f));
            var n0 = posTile != null && ZoneManager.GlobalZone.ContainsNode(posTile.Index) ? ZoneManager.GlobalZone.Nodes[posTile.Index] : null;
            //if (n1Tile != null && posTile != null && n1Tile != posTile && !posTile.AdjacentTiles8.Contains(n1Tile))
            if (n0 != null && n1 != null && n0.AllLinks.Any() && (n0.X != n1.X || n0.Y != n1.Y) && n0.AllLinks.All(l => l.X != n1.X && l.Y != n1.Y))
            {
                // Something went wrong here, next tile is not adjacent, stop.
                this.IsFinished = true;
                this.Colonist.IsMoving = false;
                this.Colonist.SetPosition(posTile);
                var offset1 = this.Colonist.Position - this.Colonist.MainTile.TerrainPosition;
                this.Colonist.PositionOffset.X = (offset1.X + offset1.Y) * 10.66667f;
                this.Colonist.PositionOffset.Y = (offset1.Y - offset1.X) * 5.33333f;
                this.Colonist.RenderPos = new Vector2f((10.6666667f * this.Colonist.Position.X) + (10.6666667f * this.Colonist.Position.Y) + 10.6666667f, (5.3333333f * this.Colonist.Position.Y) - (5.3333333f * this.Colonist.Position.X) + 16f);
                EventManager.MovedColonists.AddIfNew(this.Colonist.Id);
                base.Update();
                return;
            }

            // If at n1 or closer to n2 than n1, then shift waypoints and update colonist tile
            if (n2 != null)
            {
                var n2Pos = new Vector2f(n2.X, n2.Y);
                var d1 = n1Pos - pos;
                var d2 = n2Pos - pos;
                var d1length = d1.Length();
                if (d1length < 0.1f || posTile == n1Tile || (d2.Length() < d1length && CanCutCorner(posTile, n1Tile, n2Tile)))   // Don't cut corner around anything with a square block, as we tend to get stuck
                {
                    n1 = n2;
                    if (this.Path.RemainingNodes.Any())
                    {
                        n2 = this.Path.RemainingNodes.Dequeue();
                    }
                    else
                    {
                        n2 = null;
                        n2Tile = null;
                    }

                    n1Tile = n1 != null ? World.GetSmallTile(n1.X, n1.Y) : null;
                    n2Tile = n2 != null ? World.GetSmallTile(n2.X, n2.Y) : null;

                    this.tilesToCheck = null;
                }
            }

            // Are we going through a door?  If so then we can't try to cut corners, as this causes problems.
            foreach (var door in n1Tile.ThingsAll.OfType<IDoor>().Where(x => x.State != DoorState.LockedClosed))
            {
                if ((posTile != n1Tile && door.AllTiles.Contains(posTile)) || (n2Tile != null && door.AllTiles.Contains(n2Tile)))
                {
                    dontCutCorner = true;
                    door.Open();
                    break;
                }
            }

            if (this.currentSpeed < 0.01) dontCutCorner = true;  // Don't start corner cutting until we get up some speed, helps avoid getting stuck
            else if (posTile.ThingsPrimary.Any(t => t.ThingType == ThingType.SleepPod) || (n2Tile != null && n2Tile.ThingsPrimary.Any(t => t.ThingType == ThingType.SleepPod)))
            {
                // Don't cut corners when entering or leaving pod
                dontCutCorner = true;
            }
            else if (posTile.X == this.Path.StartPosition.X && posTile.Y == this.Path.StartPosition.Y && this.IsStartOffset)
            {
                // Don't cut corners if starting at an offset
                dontCutCorner = true;
            }

            if (posTile.ThingsPrimary.Any(t => t.ThingType.IsFoundation()))
            {
                // Walk faster on foundation
                currentMaxSpeed *= 1.2f;
            }

            // Fast walker etc.
            var percentBonus = this.Colonist.Cards.GetEffectsSum(CardEffectType.WalkSpeed);
            if (this.IsWorking)
            {
                // Work speed bonus also affects walk speed when working
                percentBonus += this.Colonist.Cards.GetEffectsSum(CardEffectType.WorkWalkSpeed);
            }

            if (percentBonus < -50) percentBonus = -50;
            currentMaxSpeed *= 0.01f * (100 + percentBonus);

            // Pick our target.  If it's the last one then it may have an offset.
            var target = (!dontCutCorner && n2 != null) ? n2 : n1;
            var targetPos = new Vector2f(target.X, target.Y);

            if (target == n1 && target.X == this.Path.EndPosition.X && target.Y == this.Path.EndPosition.Y)
            {
                targetPos += this.EndOffset;  // Head for offset only when at end of path
            }

            var blockingCircles = new List<Circle>();

            if (this.IsEscapingBlock) this.tilesToCheck = new List<ISmallTile>();
            else if (this.tilesToCheck == null)
            {
                this.tilesToCheck = target == n2
                    ? this.Colonist.MainTile.AdjacentTiles8.Union(n1Tile.AdjacentTiles8).Union(n2Tile.AdjacentTiles8).Union(posTile.AdjacentTiles8).Distinct().ToList()
                    : this.Colonist.MainTile.AdjacentTiles8.Union(n1Tile.AdjacentTiles8).Union(posTile.AdjacentTiles8).Distinct().ToList();
                this.tilesToCheck.AddIfNew(this.Colonist.MainTile);
                this.tilesToCheck.AddIfNew(posTile);
            }

            var targetVec = targetPos - this.Colonist.Position;
            var tgtAngle = targetVec.Angle();
            var dist = Mathf.Min(targetVec.Length(), (currentMaxSpeed * 50f).Clamp(0.1f, 0.5f));
            this.blockedAngleRanges.Clear();
            foreach (var thing in this.tilesToCheck.SelectMany(t => t.ThingsAll).Distinct())
            {
                if (thing == this.Colonist || (thing is IColonist c1 && c1.IsDead)) continue;  // Don't block self, and also ignore dead colonists for now

                if (this.Colonist.TargetBuilingID == thing.Id) continue;

                var tiles = thing is IMoveableThing ? new List<ISmallTile> { thing.MainTile } : thing.AllTiles;
                foreach (var tile in tiles)
                {
                    // Find the closest angle to tgtAngle that is not between a pair of object tangents
                    if (thing.TileBlockModel == TileBlockModel.Circle && (thing.ThingType != ThingType.LandingPod || this.Colonist.ActivityType != ColonistActivityType.LeavingLandingPod))
                    {
                        var circle = new Circle(tile.TerrainPosition, 0.6f);
                        blockingCircles.Add(circle);
                        this.AvoidCircle(dist, circle);
                    }
                    else if (thing.TileBlockModel == TileBlockModel.SmallCircle)
                    {
                        var circle = new Circle(tile.TerrainPosition, 0.45f);
                        blockingCircles.Add(circle);
                        this.AvoidCircle(dist, circle);
                    }
                    else if (thing.TileBlockModel == TileBlockModel.Point && this.IgnorePointBlocksTimer == 0 && this.Path.RemainingNodes.Any())
                    {
                        var p = (Vector2f)tile.TerrainPosition;
                        if (thing is IPositionOffsettable po) p += po.PositionOffset;
                        if ((p - this.Colonist.Position).Length() > 0.4f)
                        {
                            var circle = new Circle(p, 0.3f);
                            blockingCircles.Add(circle);
                            this.AvoidCircle(dist, circle);
                        }
                    }
                    else if (thing.TileBlockModel == TileBlockModel.Square)
                    {
                        // Square is approximated to a large circle
                        var circle = new Circle(tile.TerrainPosition, 0.8f);
                        blockingCircles.Add(circle);
                        this.AvoidCircle(dist, circle);

                        // Square is represented by 12 circles
                        //var circles = new Circle[12] {
                        //    new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y - 0.5f, 0.15f),
                        //    new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y - 0.17f, 0.15f),
                        //    new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y + 0.17f, 0.15f),
                        //    new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y + 0.5f, 0.15f),
                        //    new Circle(tile.TerrainPosition.X - 0.17f, tile.TerrainPosition.Y - 0.5f, 0.15f),
                        //    new Circle(tile.TerrainPosition.X - 0.17f, tile.TerrainPosition.Y + 0.5f, 0.15f),
                        //    new Circle(tile.TerrainPosition.X + 0.17f, tile.TerrainPosition.Y - 0.5f, 0.15f),
                        //    new Circle(tile.TerrainPosition.X + 0.17f, tile.TerrainPosition.Y + 0.5f, 0.15f),
                        //    new Circle(tile.TerrainPosition.X + 0.5f, tile.TerrainPosition.Y - 0.5f, 0.15f),
                        //    new Circle(tile.TerrainPosition.X + 0.5f, tile.TerrainPosition.Y - 0.17f, 0.15f),
                        //    new Circle(tile.TerrainPosition.X + 0.5f, tile.TerrainPosition.Y + 0.17f, 0.15f),
                        //    new Circle(tile.TerrainPosition.X + 0.5f, tile.TerrainPosition.Y + 0.5f, 0.15f)
                        //};
                        //blockingCircles.AddRange(circles);
                        //foreach (var circle in circles) this.AvoidCircle(dist, circle);
                    }
                    else if (thing.TileBlockModel == TileBlockModel.Wall)
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
                    else if (thing.TileBlockModel == TileBlockModel.Door)
                    {
                        if (thing.MainTile == tile)
                        {
                            // Door is represented by 4 circles
                            //var circles = (thing as IWall).Direction == Direction.SE ? new Circle[4] {
                            //    new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y + 0.5f, 0.2f),
                            //    new Circle(tile.TerrainPosition.X - 0.225f, tile.TerrainPosition.Y + 0.5f, 0.2f),
                            //    new Circle(tile.TerrainPosition.X + 0.225f, tile.TerrainPosition.Y + 0.5f, 0.2f),
                            //    new Circle(tile.TerrainPosition.X + 0.5f, tile.TerrainPosition.Y + 0.5f, 0.2f) }
                            //    : new Circle[4] {
                            //    new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y - 0.5f, 0.2f),
                            //    new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y - 0.225f, 0.2f),
                            //    new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y + 0.225f, 0.2f),
                            //    new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y + 0.5f, 0.2f)
                            //};

                            var circles = (thing as IWall).Direction == Direction.SE ? new Circle[2] {
                                new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y + 0.5f, 0.2f),
                                //new Circle(tile.TerrainPosition.X - 0.225f, tile.TerrainPosition.Y + 0.5f, 0.2f),
                                //new Circle(tile.TerrainPosition.X + 0.225f, tile.TerrainPosition.Y + 0.5f, 0.2f),
                                new Circle(tile.TerrainPosition.X + 0.5f, tile.TerrainPosition.Y + 0.5f, 0.2f) }
                                : new Circle[2] {
                                new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y - 0.5f, 0.2f),
                               // new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y - 0.225f, 0.2f),
                               // new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y + 0.225f, 0.2f),
                                new Circle(tile.TerrainPosition.X - 0.5f, tile.TerrainPosition.Y + 0.5f, 0.2f)
                            };

                            blockingCircles.AddRange(circles);
                            foreach (var circle in circles) this.AvoidCircle(dist, circle);
                        }
                    }
                    else if (thing.TileBlockModel == TileBlockModel.Pod && this.IgnorePointBlocksTimer == 0)
                    {
                        // Four corners blocked
                        var circles = new Circle[4]
                        {
                            new Circle(tile.TerrainPosition.X - 0.3f, tile.TerrainPosition.Y - 0.3f, 0.25f),
                            new Circle(tile.TerrainPosition.X - 0.3f, tile.TerrainPosition.Y + 0.3f, 0.25f),
                            new Circle(tile.TerrainPosition.X + 0.3f, tile.TerrainPosition.Y + 0.3f, 0.25f),
                            new Circle(tile.TerrainPosition.X + 0.3f, tile.TerrainPosition.Y - 0.3f, 0.25f),
                        //new Circle(tile.TerrainPosition.X - 0.225f, tile.TerrainPosition.Y - 0.4f, 0.2f),
                        //new Circle(tile.TerrainPosition.X - 0.4f, tile.TerrainPosition.Y - 0.225f, 0.2f),
                        //new Circle(tile.TerrainPosition.X + 0.225f, tile.TerrainPosition.Y - 0.4f, 0.2f),
                        //new Circle(tile.TerrainPosition.X + 0.4f, tile.TerrainPosition.Y - 0.225f, 0.2f),
                        //new Circle(tile.TerrainPosition.X - 0.225f, tile.TerrainPosition.Y + 0.4f, 0.2f),
                        //new Circle(tile.TerrainPosition.X - 0.4f, tile.TerrainPosition.Y + 0.225f, 0.2f),
                        //new Circle(tile.TerrainPosition.X + 0.225f, tile.TerrainPosition.Y + 0.4f, 0.2f),
                        //new Circle(tile.TerrainPosition.X + 0.4f, tile.TerrainPosition.Y + 0.225f, 0.2f)
                        };

                        blockingCircles.AddRange(circles);
                        foreach (var circle in circles) this.AvoidCircle(dist, circle);
                    }
                    else if (thing is IColonist c)
                    {
                        if (this.Colonist.MovingAngle.HasValue && Mathf.Abs(Mathf.AngleBetween((c.Position - this.Colonist.Position).Angle(), this.Colonist.MovingAngle.Value)) < Mathf.PI * 0.5f) // In front
                        {
                            // If other colonist is giving way to us, then ask them to stop so we can get past easily
                            // If they are close to their destination they may refuse.
                            var otherAI = ColonistController.AIs[c.Id];
                            var otherWaiting = false;
                            if (c.IsDead)
                            {
                                otherWaiting = true;
                            }
                            else if (otherAI.CurrentActivity?.CurrentAction is ActionWalk w && w.GivingWayTo == this.Colonist.Id)
                            {
                                otherWaiting = w.RequestWait();
                            }

                            if (!otherWaiting && otherAI.CurrentActivity?.CurrentAction is ActionWalk aw && aw.Path?.EndPosition == this.Path?.EndPosition)
                            {
                                // Going to the same tile!
                                this.Path = null;
                                this.IsFailed = true;
                                this.IsFinished = true;
                                return;
                            }

                            // If colonist is moving in the same direction and is in front, then we should give way
                            if (!otherWaiting && c.MovingAngle.HasValue && c.CurrentSpeed > 0.002f
                                && Mathf.Abs(Mathf.AngleBetween(c.MovingAngle.Value, this.Colonist.MovingAngle.Value)) < Mathf.PI * 0.5f)  // Similar direction (<90 deg difference)
                            {
                                var distance = (c.Position - this.Colonist.Position).Length() - 0.4f;
                                currentMaxSpeed = Mathf.Max(0, Mathf.Min(currentMaxSpeed, distance * 0.5f));
                                this.GivingWayTo = (currentMaxSpeed < MaxSpeed) ? c.Id : (int?)null;
                            }
                        }

                        // This mechanism allows us to give up trying to go around another colonist, and just go through them.
                        if (this.colonistsInTheWay == null) this.colonistsInTheWay = new Dictionary<int, int>();
                        if (!this.colonistsInTheWay.ContainsKey(c.Id)) this.colonistsInTheWay.Add(c.Id, 0);
                        else this.colonistsInTheWay[c.Id]+=2;

                        if (this.colonistsInTheWay[c.Id] < 120)
                        {
                            if ((c.Position - this.Colonist.Position).Length() > 0.5f)
                            {
                                var circle = new Circle(c.Position.X, c.Position.Y, 0.36f);
                                blockingCircles.Add(circle);
                                this.AvoidCircle(dist * 2f, circle);
                            }
                        }
                    }
                }
            }

            if (!this.GivingWayTo.HasValue && blockedAngleRanges.Any())
            {
                var newAngle1 = tgtAngle;
                var newAngle2 = tgtAngle;
                while (1 == 1)
                {
                    if (blockedAngleRanges.All(r => (r.Item1 > newAngle1 || r.Item2 < newAngle1) && (r.Item1 - Mathf.PI * 2f > newAngle1 || r.Item2 - Mathf.PI * 2f < newAngle1)))
                    {
                        tgtAngle = newAngle1;
                        break;
                    }

                    if (blockedAngleRanges.All(r => (r.Item1 > newAngle2 || r.Item2 < newAngle2) && (r.Item1 - Mathf.PI * 2f > newAngle2 || r.Item2 - Mathf.PI * 2f < newAngle2)
                        && (r.Item1 + Mathf.PI * 2f > newAngle2 || r.Item2 + Mathf.PI * 2f < newAngle2)))
                    {
                        tgtAngle = newAngle2;
                        break;
                    }

                    newAngle1 -= 0.02f;
                    newAngle2 += 0.02f;
                    if (newAngle2 - newAngle1 > Mathf.PI * 1.99f)
                    {
                        currentMaxSpeed = 0f;
                        break;
                    }
                }

                if (newAngle1 != tgtAngle && newAngle2 != tgtAngle)
                {
                    if (newAngle1 > tgtAngle) newAngle1 -= Mathf.PI * 2f;
                    if (newAngle2 < tgtAngle) newAngle2 += Mathf.PI * 2f;
                    var diff1 = (tgtAngle - newAngle1) % (Mathf.PI * 2f);
                    var diff2 = (newAngle2 - tgtAngle) % (Mathf.PI * 2f);
                    tgtAngle = diff1 > diff2 ? newAngle2 : newAngle1;
                }
            }

            //targetPos = this.Colonist.Position + (new Vector2f(0, -dist).Rotate(tgtAngle));

            var delta = targetPos - pos;
            var d = delta.Length();
            if (d.ApproxEquals(0, 0.001f) || (this.currentSpeed < 0.004f && d.ApproxEquals(0, this.EndOffsetFlexibility)))
            {
                this.Colonist.MovingAngle = null;

                // In end position, rotate if required
                if (this.EndDirection == Direction.None) this.IsFinished = true;
                else
                {
                    var angle = DirectionHelper.GetAngleFromDirection(this.EndDirection);
                    this.IsFinished = this.RotateToAngle(angle, out float a);
                }

                this.Colonist.IsMoving = !this.IsFinished;
            }
            else
            {
                if (this.forceAngle.HasValue)
                {
                    tgtAngle = this.forceAngle.Value;
                }

                if (!this.RotateToAngle(tgtAngle - (Mathf.PI * 0.25f), out float a))
                {
                    // Slow down if rotating close to target, otherwise can end up circling
                    this.currentSpeed = Mathf.Min(this.currentSpeed, d);
                }
                else this.IsStart = false;   // Facing right direction, can start moving

                foreach (var circle in blockingCircles)
                {
                    var objAngle = Mathf.Abs(Mathf.AngleBetween((circle.Centre - this.Colonist.Position).Angle(), this.Colonist.MovingAngle.GetValueOrDefault()));
                    if (objAngle < Mathf.PI * 0.48f)   // In front  
                    {
                        var distance = (circle.Centre - this.Colonist.Position).Length() - circle.Radius;
                        distance = distance * 1f / Mathf.Cos(objAngle);
                        if (distance * 0.8f < currentMaxSpeed)
                        {
                            //sb.AppendLine($"I'm slowing down to {distance * 0.8f} tiles/frame because something is in front of me.");
                            currentMaxSpeed = distance * 0.8f;// Mathf.Max(0, Mathf.Min(currentMaxSpeed, distance * 0.5f));
                        }
                    }
                }

                if (!this.IsStart || this.currentSpeed > 0)
                {
                    this.currentSpeed = Mathf.Min(this.currentSpeed + (MaxSpeed * 0.02f), currentMaxSpeed);
                }

                if (this.currentSpeed > currentMaxSpeed * 0.1f && this.currentSpeed * 20f > d) this.currentSpeed = Mathf.Max(currentMaxSpeed * 0.1f, 0.05f * d);

                // if (a > 0) this.currentSpeed = (0.02f - a).Clamp(0f, this.currentSpeed);  // Slow down when turning
                if (this.forceAngle.HasValue && Mathf.Abs(a) > 0.1f)
                {
                    this.currentSpeed = 0f;
                }
                else if (Math.Abs(a) > 1.0f && this.currentSpeed > 0.002f) this.currentSpeed = 0.002f;
                else if (Math.Abs(a) > 0.5f && this.currentSpeed > 0.01f) this.currentSpeed = 0.01f;

                var vec = new Vector2f(0, -this.currentSpeed).Rotate(this.Colonist.Rotation + (Mathf.PI * 0.25f));
                this.Colonist.Position += vec;
                this.Colonist.CurrentSpeed = this.currentSpeed;
                this.Colonist.MovingAngle = this.Colonist.Rotation + (Mathf.PI * 0.25f);// this.currentSpeed > 0 ? angle : (float?)null;
                this.Colonist.IsMoving = true;

                //posTile = World.GetSmallTile((int)(pos.X + 0.5f), (int)(pos.Y + 0.5f));
                //if (this.Colonist.MainTile != posTile)
                //{
                //    var allTiles = new List<ISmallTile> { posTile };
                //    if (n1Tile != null && n1Tile != posTile) allTiles.Add(n1Tile);
                //    if (n2Tile != null && n2Tile != n1Tile) allTiles.Add(n2Tile);
                //    this.Colonist.SetPosition(posTile, allTiles);
                //}
            }

            var targetTile = World.GetSmallTile(target.X, target.Y);
            var currentTile = World.GetSmallTile((int)(this.Colonist.Position.X + 0.5f), (int)(this.Colonist.Position.Y + 0.5f));
            if (targetTile != currentTile 
                && DirectionHelper.GetDirectionFromAdjacentPositions(currentTile.X, currentTile.Y, targetTile.X, targetTile.Y) == Direction.None
                && DirectionHelper.GetDirectionFromAdjacentPositions(currentTile.X, currentTile.Y, n1Tile.X, n1Tile.Y) != Direction.None)
            {
                // Target tile not adjacent, set N1 to be the target for the purpose of setting colonist position on the terrain
                targetTile = n1Tile;
            }

            if (this.Colonist.MainTile != currentTile
                || (this.Colonist.AllTiles.Count == 1 && currentTile != targetTile)
                || (this.Colonist.AllTiles.Count > 1 && currentTile == targetTile)
                || (this.Colonist.AllTiles.Count > 1 && currentTile != this.Colonist.AllTiles[0]))
            {
                // Main tile is the target tile.
                if (targetTile != currentTile) this.Colonist.SetPosition(currentTile, new List<ISmallTile> { currentTile, targetTile });
                else this.Colonist.SetPosition(currentTile);

                this.tilesToCheck = null;
            }

            foreach (var t in targetTile.ThingsPrimary.OfType<IColonist>().Where(c => c != this.Colonist && c.IsIdle && !c.IsDead))
            {
                // Ask idle colonist to move out of the way
                t.RequestGiveWay(new List<ISmallTile> { this.Colonist.MainTile, n1Tile, n2Tile });
            }

            this.historicalPositions.Enqueue(this.Colonist.Position);
            if (this.historicalPositions.Count > 90)
            {
                var p = this.historicalPositions.Dequeue();
                var diff = (p - this.Colonist.Position).Length();
                if (diff < 0.5f)
                {
                    this.IgnorePointBlocksTimer++;
                    if (this.IgnorePointBlocksTimer > 180)
                    {
                        // Stuck
                        this.IsFailed = true;
                        this.IsFinished = true;
                        this.Colonist.IsMoving = false;
                        this.StuckTileIndex = n1Tile != posTile ? n1Tile?.Index : null;
                        posTile = World.GetSmallTile((int)(pos.X + 0.5f), (int)(pos.Y + 0.5f));
                        this.Colonist.SetPosition(posTile);
                    }
                    if (this.IgnorePointBlocksTimer > 120)
                    {
                        this.IsEscapingBlock = true;
                    }
                }
                else
                {
                    this.IgnorePointBlocksTimer = 0;
                    this.IsEscapingBlock = false;
                }
            }

            var offset = this.Colonist.Position - this.Colonist.MainTile.TerrainPosition;
            this.Colonist.PositionOffset.X = (offset.X + offset.Y) * 10.66667f;
            this.Colonist.PositionOffset.Y = (offset.Y - offset.X) * 5.33333f;
            this.Colonist.RenderPos = new Vector2f((10.6666667f * this.Colonist.Position.X) + (10.6666667f * this.Colonist.Position.Y) + 10.6666667f, (5.3333333f * this.Colonist.Position.Y) - (5.3333333f * this.Colonist.Position.X) + 16f);
            EventManager.MovedColonists.AddIfNew(this.Colonist.Id);

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
            if (this.FindTangents(circle, this.Colonist.Position, dist, out float tangent1, out float tangent2))
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
            var count = 0;

            foreach (var node in this.Path.RemainingNodes)
            {
                if (node.ForwardDirection == Direction.None) return true;  // End of path

                var index = node.X + (node.Y * World.Width * 3);
                if (!ZoneManager.GlobalZone.ContainsNode(index)) return false;   // Something went wrong
                var globalNode = ZoneManager.GlobalZone.Nodes[index];
                if (globalNode.GetLink(node.ForwardDirection) == null) return false;  // Path blocked

                count++;
                if (count > 5) return true;  // Max 5 tiles ahead
            }

            return true;
        }

        private static bool CanCutCorner(ISmallTile posTile, ISmallTile n1, ISmallTile n2)
        {
            if (posTile == null || n1 == null || n2 == null || posTile == n1) return true;

            if (n2.ThingsAll.Any(t => t.ThingType == ThingType.SleepPod)) return false;   // Don't cut corner into sleep pod

            if (posTile.AdjacentTiles8.Contains(n2)) return false;  // Can't cut between adjacent tiles
            //{
                
            //    && ZoneManager.GlobalZone.Nodes[posTile.Index].AllLinks.Contains(ZoneManager.GlobalZone.Nodes[n2.Index])) 
            //    }

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

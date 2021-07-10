namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Cards.Interface;
    using Projects;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class Lab : Building, ILab, IRotatableThing, IColonistInteractive
    {
        private Energy energyPerHour;
        private Energy energyPerFrame;
        private Energy energyStartup;
        private Project projectDefinition;
        private LabStatus labStatus;
        private int workStopTimer;

        [ProtoMember(1)]
        public Energy EnergyUseRate { get; set; } = 0;

        [ProtoMember(2)]
        public WorkPriority LabPriority { get; set; }

        [ProtoMember(4)]
        public bool HasPower { get; private set; }

        [ProtoMember(5)]
        public int SelectedProjectTypeId { get; private set; }

        [ProtoMember(6)]
        public LabStatus LabStatus
        {
            get { return this.labStatus; }
            private set
            {
                if (this.labStatus != value)
                {
                    this.labStatus = value;
                    // Whether the adjacent tile is reserved as an access corridor depends on current status of the lab
                    if (this.MainTile != null) this.MainTile.GetTileToDirection(this.Direction).UpdateIsCorridor();
                }
            }
        }

        [ProtoMember(7)]
        public double? Progress { get; private set; }

        [ProtoMember(8)]
        public bool IsWorking { get; private set; }

        [ProtoMember(9)]
        public int ScreenPosition { get; private set; }

        [ProtoMember(10)]
        public string ScreenFrame { get; private set; }

        [ProtoMember(14)]
        public Direction Direction { get; private set; }

        [ProtoMember(15)]
        public double WorkRate { get; private set; }

        [ProtoMember(16)]
        private Dictionary<int, int> colonistsByAccessTile;

        [ProtoMember(17)]
        public int AssignedColonistDistance { get; set; }

        [ProtoMember(19)]
        public bool IsLabSwitchedOn { get; set; }

        public Dictionary<CardType, int> WorkRateEffects { get; private set; }

        public bool RequiresAccessNow => this.IsReady && !this.LabStatus.In(LabStatus.SelectProject, LabStatus.Offline);

        // For deserialization
        private Lab() : base(ThingType.None)
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        public Lab(ISmallTile tile, Direction direction, ThingType type) : base(type, tile, 1)
        {
            this.Direction = direction;
            this.UpdateEnergyRequirements();
            this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        public override void AfterConstructionComplete()
        {
            this.IsLabSwitchedOn = true;
            base.AfterConstructionComplete();
        }

        public override void AfterAddedToWorld()
        {
            if (this.IsReady)
            {
                this.projectDefinition = ProjectManager.GetDefinition(this.SelectedProjectTypeId);
                if (this.Direction == Direction.E) this.Direction = Direction.SE;
                this.AnimationFrame = 1;
                this.UpdateScreenFrame();
                this.UpdateEnergyRequirements();
            }

            base.AfterAddedToWorld();
        }

        private void UpdateEnergyRequirements()
        {
            if (this.ThingType == ThingType.Biolab)
            {
                this.energyPerHour = Energy.FromKwH(Constants.BiolabEnergyUsage);
                this.energyStartup = Energy.FromKwH(Constants.BiolabMinStartEnergy);
            }
            else if (this.ThingType == ThingType.GeologyLab)
            {
                this.energyPerHour = Energy.FromKwH(Constants.GeologyLabEnergyUsage);
                this.energyStartup = Energy.FromKwH(Constants.GeologyLabMinStartEnergy);
            }
            else  // if (this.ThingType == ThingType.MaterialsLab)
            {
                this.energyPerHour = Energy.FromKwH(Constants.MaterialsLabEnergyUsage);
                this.energyStartup = Energy.FromKwH(Constants.MaterialsLabMinStartEnergy);
            }

            this.energyPerFrame = this.energyPerHour / Constants.FramesPerHour;
        }

        public void SetProject(int projectTypeId)
        {
            this.projectDefinition = ProjectManager.GetDefinition(projectTypeId);
            this.SelectedProjectTypeId = projectTypeId;
            if (this.projectDefinition != null)
            {
                if (this.LabStatus == LabStatus.SelectProject)
                {
                    this.LabStatus = LabStatus.WaitingForColonist;
                    if (this.LabPriority == WorkPriority.Disabled) this.LabPriority = WorkPriority.Low;
                }

                if (this.projectDefinition.RemainingWork > this.projectDefinition.TotalWork) this.projectDefinition.RemainingWork = this.projectDefinition.TotalWork;
                this.Progress = (this.projectDefinition.TotalWork - this.projectDefinition.RemainingWork) / (float)this.projectDefinition.TotalWork;
            }
            else
            {
                this.LabStatus = LabStatus.SelectProject;
                this.Progress = 0;
            }
        }

        /// <summary>
        /// Returns true when research complete
        /// </summary>
        public bool DoJob(double speed, Dictionary<CardType, int> effects = null)
        {
            this.WorkRateEffects = effects;

            if (this.projectDefinition == null || this.IsRecycling) return true;   // Done
            if (this.projectDefinition.IsDone)
            {
                this.projectDefinition = null;
                this.SelectedProjectTypeId = 0;
                this.Progress = 0;
                return true;
            }

            this.IsWorking = true;
            this.workStopTimer = 3;
            this.WorkRate = speed;
            return false;
        }

        public override void Update()
        {
            // Retract screens if recycling.  This must be done here as UpdateLab won't be called.
            if (this.IsRecycling)
            {
                this.IsWorking = false;
                this.Progress = 0;
                this.projectDefinition = null;
                this.IsLabSwitchedOn = false;
                this.UpdateScreenFrame();
            }

            foreach (var id in this.colonistsByAccessTile.Keys.ToList())
            {
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && c.ActivityType == ColonistActivityType.Lab) continue;
                this.colonistsByAccessTile.Remove(id);
            }

            base.Update();
        }

        public Energy UpdateLab()
        {
            Energy energyUsed = 0;
            this.EnergyUseRate = 0;

            var network = World.ResourceNetwork;
            if (network == null) return 0;

            this.HasPower = network.CanTakeEnergy(HasPower ? energyPerFrame : energyStartup) == true;

            if (!this.IsLabSwitchedOn)
            {
                this.LabStatus = LabStatus.Offline;
            }
            else if (this.projectDefinition != null)
            {
                if (this.HasPower)
                {
                    if (this.IsWorking)
                    {
                        network.TakeEnergy(energyPerFrame);
                        energyUsed = energyPerFrame;
                        this.EnergyUseRate = energyPerHour;
                        this.LabStatus = LabStatus.InProgress;

                        if (this.ScreenPosition == 60 && (this.WorkRate > 0.999 || Rand.NextDouble() <= this.WorkRate))
                        {
                            this.IncrementProgress();
                            if (this.projectDefinition?.IsDone == false && this.WorkRate > 1.0 && Rand.NextDouble() <= this.WorkRate - 1.0)
                            {
                                // Bonus chance if work rate > 100%
                                this.IncrementProgress();
                            }
                        }
                    }
                    else this.LabStatus = LabStatus.WaitingForColonist;
                }
                else
                {
                    this.LabStatus = LabStatus.NoPower;
                }
            }
            else
            {
                this.LabStatus = LabStatus.SelectProject;
            }

            if (this.projectDefinition != null && this.projectDefinition.RemainingWork > this.projectDefinition.TotalWork) this.projectDefinition.RemainingWork = this.projectDefinition.TotalWork;
            this.Progress = this.projectDefinition != null ? (this.projectDefinition.TotalWork - this.projectDefinition.RemainingWork) / (float)this.projectDefinition.TotalWork : 0;
            this.UpdateScreenFrame();

            if (this.workStopTimer == 0) this.IsWorking = false;     // Colonist has to reset this flag at least every few frames
            else this.workStopTimer--;

            return energyUsed;
        }

        private void IncrementProgress()
        {
            this.projectDefinition.RemainingWork--;
            if (this.projectDefinition.RemainingWork > this.projectDefinition.TotalWork) this.projectDefinition.RemainingWork = this.projectDefinition.TotalWork;
            this.Progress = (this.projectDefinition.TotalWork - this.projectDefinition.RemainingWork) / (float)this.projectDefinition.TotalWork;

            if (this.projectDefinition.IsDone)
            {
                WorldStats.Increment(WorldStatKeys.ProjectsComplete);
                var colonistId = this.colonistsByAccessTile.Values.FirstOrDefault(v => v > 0);
                if (colonistId > 0) EventManager.EnqueueColonistEvent(ColonistEventType.ProjectComplete, colonistId);
                this.projectDefinition = null;
                this.SelectedProjectTypeId = 0;
                this.Progress = 0;
            }
        }

        private void UpdateScreenFrame()
        {
            var prevMain = this.ScreenFrame;

            if (this.IsWorking)
            {
                if (this.ScreenPosition < 60)
                {
                    this.ScreenPosition++;
                    if (this.ScreenPosition >= 3 && (this.ScreenPosition - 1) % 3 != this.ScreenPosition % 3)
                    {
                        this.ScreenFrame = $"LabScreen_{this.ScreenPosition / 3}_{this.Direction}";
                    }
                }
                else if (this.LabStatus == LabStatus.InProgress && this.Direction.In(Direction.SE, Direction.SW))
                {
                    this.ScreenFrame = $"LabScreen_21_{this.Direction}";   // Screens and keyboard lit, only visible when lab is facing us
                }
                else
                {
                    this.ScreenFrame = $"LabScreen_20_{this.Direction}";   // Blank screen fully extended
                }
            }
            else if (this.ScreenPosition > 0 && (this.HasPower || this.IsRecycling))
            {
                this.ScreenPosition--;
                if ((this.ScreenPosition + 1) % 3 != this.ScreenPosition % 3)
                {
                    this.ScreenFrame = this.ScreenPosition >= 3 ? $"LabScreen_{this.ScreenPosition / 3}_{this.Direction}" : "";
                }
            }

            if (prevMain != this.ScreenFrame)
            {
                EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.ScreenFrame), this.ScreenFrame, prevMain, this.mainTile.Row, this.ThingType);
            }
        }

        public override string GetTextureName(int layer = 1)
        {
            return layer == 1 ? $"{base.GetTextureName()}_{this.Direction}" : this.ScreenFrame;
        }

        public IEnumerable<ISmallTile> GetAllAccessTiles()
        {
            var tile = this.mainTile.GetTileToDirection(this.Direction);
            if (tile == null || !tile.CanWorkInTile || this.MainTile.HasWallToDirection(this.Direction)) return new List<ISmallTile>(0);   // Can't work here
            return new List<ISmallTile>(1) { tile };
        }

        public IEnumerable<ISmallTile> GetAccessTiles(int? colonistId = null)
        {
            if (!this.IsReady || this.LabStatus.In(LabStatus.SelectProject, LabStatus.Offline)) return new List<ISmallTile>(0);

            this.CleanupColonistAssignments();
            var tile = this.mainTile.GetTileToDirection(this.Direction);
            if (tile == null || !tile.CanWorkInTile || this.MainTile.HasWallToDirection(this.Direction)) return new List<ISmallTile>(0);   // Can't work here
            if (tile.ThingsPrimary.Any(t => t is IColonist c && (colonistId == null || c.Id != colonistId) && !c.IsMoving && !c.IsRelaxing)) return new List<ISmallTile>(0);   // Blocked by another colonist
            if (colonistId.HasValue && this.colonistsByAccessTile.ContainsKey(tile.Index) && this.colonistsByAccessTile[tile.Index] != colonistId) return new List<ISmallTile>(0);  // Assigned to someone else

            return new List<ISmallTile>(1) { tile };
        }

        public bool CanAssignColonist(int colonistId, int? tileIndex = null)
        {
            var canAccess = tileIndex.HasValue
                ? this.GetAccessTiles(colonistId).Any(t => t.Index == tileIndex.Value)
                : this.GetAccessTiles(colonistId).Any();

            return canAccess && this.CanColonistDoProject(colonistId);
        }

        private bool CanColonistDoProject(int colonistId)
        {
            var requiredSkill = this.projectDefinition?.SkillType;
            return World.GetThing(colonistId) is IColonist colonist && requiredSkill.HasValue && (colonist.Skill == requiredSkill.Value || colonist.Skill == SkillType.Programmer);
        }

        public void AssignColonist(int colonistId, int tileIndex)
        {
            if (!this.colonistsByAccessTile.ContainsKey(tileIndex)) this.colonistsByAccessTile.Add(tileIndex, colonistId);
            else this.colonistsByAccessTile[tileIndex] = colonistId;
        }

        private void CleanupColonistAssignments()
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            foreach (var id in this.colonistsByAccessTile.Keys.ToList())
            {
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && c.TargetBuilingID == this.Id) continue;
                this.colonistsByAccessTile.Remove(id);
            }
        }

        public override bool CanRecycle()
        {
            if (this.ScreenPosition > 0) return false;  // Lab is in use
            return base.CanRecycle();
        }
    }
}

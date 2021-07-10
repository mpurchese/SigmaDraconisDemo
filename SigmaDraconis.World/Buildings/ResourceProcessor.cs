namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class ResourceProcessor : FactoryBuilding, IResourceProcessor
    {
        private float soundPitch = 0f;

        protected static Dictionary<ItemType, int> itemTypeStartFrames = new Dictionary<ItemType, int> { { ItemType.Coal, 4 }, { ItemType.IronOre, 43 }, { ItemType.Stone, 82 }, { ItemType.Metal, 121 }, { ItemType.Biomass, 160 }, { ItemType.Compost, 199 } };

        [ProtoMember(1)]
        private Dictionary<int, int> colonistsByAccessTile;

        [ProtoMember(2)]
        private int reverseProcessingTimer;

        [ProtoMember(3)]
        private int? waitingColonistId;

        [ProtoMember(4)]
        private int? waitingColonistIdTimer;

        public bool RequiresAccessNow => this.IsReady && this.IsSwitchedOn;

        #region Constructors

        // For deserialization
        private ResourceProcessor() : base()
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        public ResourceProcessor(ISmallTile tile) : base(ThingType.ResourceProcessor, tile, 1)
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        #endregion

        #region IColonistInteractive implementation

        public IEnumerable<ISmallTile> GetAllAccessTiles()
        {
            if (!this.IsReady || !this.IsSwitchedOn) yield break;

            for (int i = 4; i <= 7; i++)   // NE, SE, SW, NW
            {
                var direction = (Direction)i;
                var tile = this.mainTile.GetTileToDirection(direction);
                if (tile == null || !tile.CanWorkInTile || this.MainTile.HasWallToDirection(direction)) continue;   // Can't work here
                yield return tile;
            }
        }

        public IEnumerable<ISmallTile> GetAccessTiles(int? colonistId = null)
        {
            this.CleanupColonistAssignments();
            for (int i = 4; i <= 7; i++)   // NE, SE, SW, NW
            {
                var direction = (Direction)i;
                var tile = this.mainTile.GetTileToDirection(direction);
                if (tile == null || !tile.CanWorkInTile || this.MainTile.HasWallToDirection(direction)) continue;   // Can't work here
                if (tile.ThingsPrimary.Any(t => t is IColonist c && (colonistId == null || c.Id != colonistId) && !c.IsMoving && !c.IsRelaxing)) continue;   // Blocked by another colonist
                if (colonistId.HasValue && this.colonistsByAccessTile.ContainsKey(tile.Index) && this.colonistsByAccessTile[tile.Index] != colonistId) continue;  // Assigned to someone else
                yield return tile;
            }
        }

        public bool CanAssignColonist(int colonistId, int? tileIndex = null)
        {
            return tileIndex.HasValue
                ? this.GetAccessTiles(colonistId).Any(t => t.Index == tileIndex.Value)
                : this.GetAccessTiles(colonistId).Any();
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
                if (this.IsSwitchedOn && World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && c.TargetBuilingID == this.Id) continue;
                this.colonistsByAccessTile.Remove(id);
            }
        }

        #endregion

        #region IResourceProcessor implementation

        /// <summary>
        /// Called by colonist to check whether an item can be added to the processor.  Call before using AddResource.
        /// </summary>
        /// <param name="colonistId">If colonist is rejected, they'll get priority next time</param>
        /// <returns>True when the item can be added.</returns>
        public bool CanAddResource(int colonistId)
        {
            if (this.waitingColonistId.HasValue && this.waitingColonistId != colonistId) return false;
            if (!this.IsSwitchedOn) return false;
            if (this.InputItemType == ItemType.None && this.OutputItemType == ItemType.None) return true;

            this.waitingColonistId = colonistId;
            this.waitingColonistIdTimer = 10;
            return false;
        }

        /// <summary>
        /// Called by colonist to add an item to the processor.  Check first using CanAddResource.
        /// </summary>
        /// <param name="itemType">Type of resource to add</param>
        public void AddResource(ItemType itemType)
        {
            this.InputItemType = itemType;
            if (this.OutputItemType == ItemType.None) this.StartProcessing();
            this.UpdateAnimationFrame();
        }

        /// <summary>
        /// Called repeatedly by colonist to run the processor in reverse and take an item out of the network.
        /// </summary>
        /// <param name="itemType">Type of resource to take</param>
        /// <returns>True when the item is available.  At this point it is removed and should be added to the colonist's inventory.</returns>
        public bool RequestUnprocessedResource(ItemType itemType)
        {
            this.reverseProcessingTimer = 30;

            if (!this.IsSwitchedOn) return false;
            if (this.FactoryProgress >= 0.001 && this.FactoryProgress < 0.999) return false;   // In progress
            if (this.InputItemType != itemType && this.InputItemType != ItemType.None) return false;               // Something else in the input slot

            if (this.InputItemType == itemType && this.FactoryProgress < 0.001)
            {
                // Finish
                this.InputItemType = ItemType.None;
                this.FactoryProgress = 0;
                this.FactoryStatus = this.IsSwitchedOn ? FactoryStatus.Standby : FactoryStatus.Offline;
                this.reverseProcessingTimer = 0;
                return true;
            }

            if (this.InputItemType == ItemType.None && this.OutputItemType == itemType)
            {
                // Start using output slot
                this.InputItemType = itemType;
                this.OutputItemType = ItemType.None;
                this.FactoryStatus = FactoryStatus.InProgressReverse;
                this.FactoryProgress = 1;
            }
            else if (this.InputItemType == ItemType.None && World.ResourceNetwork?.CanTakeItems(this, itemType, 1) == true)
            {
                // Start by taking from network, swapping if necessary
                if (this.OutputItemType != ItemType.None) World.ResourceNetwork.SwapItems(this.OutputItemType, itemType);
                else World.ResourceNetwork.TakeItems(this, itemType, 1);
                this.OutputItemType = ItemType.None;
                this.InputItemType = itemType;
                this.FactoryStatus = FactoryStatus.InProgressReverse;
                this.FactoryProgress = 1;   // Progress will run backwards
            }

            return false;
        }

        public override void UpdateSound()
        {
            if (this.definitionSoundVolume > 0 && (this.smokeSoundRate > 0 || this.soundVolume > 0))
            {
                this.soundVolume = (float)(this.smokeSoundRate).Clamp(this.soundVolume - this.definitionSoundFade, this.soundVolume + this.definitionSoundFade);
                EventManager.EnqueueSoundUpdateEvent(this.id, this.soundVolume < 0.001f, this.soundVolume * this.definitionSoundVolume, 0, false, this.soundPitch);
            }
        }

        /// <summary>
        /// Called by the network to update every frame.
        /// </summary>
        public override Energy UpdateFactory()
        {
            this.smokeSoundRate = 0f;
            if (this.waitingColonistIdTimer > 0)
            {
                this.waitingColonistIdTimer--;
                if (this.waitingColonistIdTimer == 0) this.waitingColonistId = null;
            }

            switch (this.FactoryStatus)
            {
                case FactoryStatus.Offline:
                case FactoryStatus.Initialising:
                    if (this.IsSwitchedOn) this.FactoryStatus = FactoryStatus.Standby;
                    break;
                case FactoryStatus.Standby:
                    if (this.InputItemType != ItemType.None) this.StartProcessing();   // Shouldn't get here - but may still somehow be possible
                    else if (!this.IsSwitchedOn) this.FactoryStatus = FactoryStatus.Offline;
                    break;
                case FactoryStatus.InProgress:
                    this.FactoryProgress += 1.0 / Constants.ResourceProcessorFramesToProcess;
                    this.smokeSoundRate = this.FactoryProgress < 0.5 ? 0.2f : 1f;
                    this.soundPitch = (float)(this.FactoryProgress - 0.5) / 4f;
                    if (this.FactoryProgress > 0.999)
                    {
                        this.FactoryProgress = 1.0;
                        this.OutputItemType = this.InputItemType;
                        this.InputItemType = ItemType.None;
                        this.FactoryStatus = FactoryStatus.WaitingToDistribute;
                        this.TryDistribute();
                    }
                    break;
                case FactoryStatus.InProgressReverse:
                    this.reverseProcessingTimer--;
                    if (this.reverseProcessingTimer > 0 && this.FactoryProgress > 0)
                    {
                        this.FactoryProgress -= 1.0 / Constants.ResourceProcessorFramesToProcess;
                        this.smokeSoundRate = this.FactoryProgress < 0.5 ? 0.2f : 1f;
                        this.soundPitch = (float)(this.FactoryProgress - 0.5) / 4f;
                        if (this.FactoryProgress < 0.001)
                        {
                            this.FactoryProgress = 0.0;
                        }
                    }
                    else if (this.reverseProcessingTimer <= 0) this.FactoryStatus = FactoryStatus.InProgress;  // Colonist moved away, reprocess the resource
                    break;
                case FactoryStatus.WaitingToDistribute:
                    this.TryDistribute();
                    break;
            }

            this.UpdateAnimationFrame();
            this.UpdateSound();
            return 0;
        }

        private void StartProcessing()
        {
            this.FactoryStatus = FactoryStatus.InProgress;
        }

        protected override void TryDistribute()
        {
            if (World.ResourceNetwork?.AddItem(this.OutputItemType) == true)
            {
                this.FactoryProgress = 0f;
                this.OutputItemType = ItemType.None;
                if (this.InputItemType != ItemType.None) this.StartProcessing();
                else this.FactoryStatus = this.IsSwitchedOn ? FactoryStatus.Standby : FactoryStatus.Offline;
            }
        }

        protected override void UpdateAnimationFrame()
        {
            if (this.FactoryStatus == FactoryStatus.WaitingToDistribute)
            {
                this.AnimationFrame = World.WorldTime.Minute % 2 == 0 ? 2 : 1;   // Flashing yellow
            }
            else if (this.InputItemType == ItemType.None)
            {
                this.AnimationFrame = this.IsSwitchedOn ? 3 : 1;   // Green for standby
            }
            else if (itemTypeStartFrames.ContainsKey(this.InputItemType))
            {
                var p = (int)(this.FactoryProgress * 60);
                if (p > 9) p = (p - 21).Clamp(9, 38);
                this.AnimationFrame = p + itemTypeStartFrames[this.InputItemType];
            }
        }

        #endregion
    }
}

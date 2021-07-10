namespace SigmaDraconis.World.Fauna
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ProtoBuf;

    using Draconis.Shared;

    using Cards;
    using Cards.Interface;
    using Config;
    using Language;
    using Medical;
    using Shared;
    using Rooms;
    using Zones;
    using WorldInterfaces;

    [ProtoContract]
    public class Colonist : Animal, IColonist, IRecyclableThing
    {
        private ColonistActivityType activityType;
        private bool isDoingWorkActivity;

        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public int ColourCode { get; set; }

        [ProtoMember(3)]
        public ColonistBody Body { get; set; }

        [ProtoMember(5)]
        public int? CarriedCropType { get; set; }

        [ProtoMember(6)]
        public ColonistActivityType ActivityType
        {
            get { return this.activityType; }
            set
            {
                if (this.activityType != value)
                {
                    this.activityType = value;
                    this.isDoingWorkActivity = value.GetAttribute<IsWorkAttribute>()?.Value == true;
                }
            }
        }

        [ProtoMember(7)]
        private CardCollection cardsForSerialize;

        [ProtoMember(9)]
        public int RaisedArmsFrame { get; set; }

        [ProtoMember(11)]
        public int SleepFrame { get; set; }

        [ProtoMember(12)]
        public int? TargetBuilingID { get; set; }

        [ProtoMember(13)]
        public bool IsWorking { get; set; }

        [ProtoMember(14)]
        private List<int> recentMealsAsList;
        private Queue<int> recentMeals;

        [ProtoMember(15)]
        public ItemType CarriedItemTypeArms { get; set; }

        [ProtoMember(16)]
        public ItemType CarriedItemTypeBack { get; set; }

        [ProtoMember(17)]
        public Dictionary<ColonistPriority, int> WorkPriorities { get; set; }

        [ProtoMember(18)]
        private Dictionary<int, int> foodOpinions;

        [ProtoMember(19)]
        public int HairColourCode { get; set; }

        [ProtoMember(20)]
        public new string ShortName { get; set; }

        [ProtoMember(21)]
        public uint FramesSinceArrival { get; set; }

        [ProtoMember(22)]
        public bool? SleptInPod { get; private set; }

        [ProtoMember(23)]
        public bool? SleptOutside { get; private set; }

        [ProtoMember(24)]
        public int? SleptTemperature { get; private set; }

        [ProtoMember(25)]
        public int SleptTemperatureSum { get; private set; }

        [ProtoMember(26)]
        public int SleptFrames { get; private set; }

        [ProtoMember(27)]
        public int Happiness { get; private set; }

        [ProtoMember(29)]
        public bool IsRelaxing { get; set; }

        [ProtoMember(30)]
        public uint FramesRoaming { get; private set; }

        [ProtoMember(31)]
        public uint? FramesSinceRoam { get; private set; }

        [ProtoMember(32)]
        private uint roamTimeout;

        [ProtoMember(34)]
        public double RestRatePerHour { get; private set; }

        [ProtoMember(35)]
        private Dictionary<int, int> framesSocialByColonist;

        [ProtoMember(36)]
        private Dictionary<int, int> framesSinceSocialByColonist;

        [ProtoMember(38)]
        public int LookingForWaterCounter { get; set; }

        [ProtoMember(39)]
        public int LookingForFoodCounter { get; set; }

        [ProtoMember(40)]
        public int IdleTimer { get; private set; }

        [ProtoMember(41)]
        public bool IsIdle { get; set; }

        [ProtoMember(42)]
        public Dictionary<int, int> TimeSinceSocialByColonist { get; private set; }

        [ProtoMember(44)]
        public int LastFoodType { get; private set; }

        [ProtoMember(45)]
        protected Dictionary<int, int> colonistsByAccessTile;  // For IColonistInteractive implementation

        [ProtoMember(46)]
        public int LastLabProjectId { get; set; }

        [ProtoMember(47)]
        public ItemType TargetItemType { get; set; }

        [ProtoMember(52)]
        public HashSet<int> GiveWayRequestTiles { get; private set; }    // Has a request to move away from these tiles

        [ProtoMember(53)]
        public bool IsActivityFinished { get; set; }

        [ProtoMember(54)]
        public SkillType Skill { get; set; }

        [ProtoMember(55)]
        public List<string> Story { get; set; }

        [ProtoMember(57)]
        private readonly ColonistStress stress;

        [ProtoMember(58)]
        private StressLevel stressAtLastDiaryUpdate = StressLevel.None;

        [ProtoMember(59)]
        private int framesSinceCardsUpdate;

        [ProtoMember(60)]
        public bool IsArrived { get; set; }

        [ProtoMember(61)]
        public int ColonistToWelcome { get; set; }

        [ProtoMember(62)]
        public int ColonistToMourn { get; set; }

        [ProtoMember(65)]
        public int OreScannerTileForComment { get; set; }

        [ProtoMember(70)]
        public ItemType LastResourceFound { get; set; }

        [ProtoMember(71)]
        public MineResourceDensity LastResourceDensityFound { get; set; }

        [ProtoMember(75)]
        public ItemType ScannerResourceFound { get; set; }

        [ProtoMember(76)]
        public MineResourceDensity ScannerResourceDensityFound { get; set; }

        [ProtoMember(77)]
        public KekPolicy KekPolicy { get; private set; }

        [ProtoMember(78)]
        public WorkPolicy WorkPolicy { get; private set; }

        [ProtoMember(79)]
        public int WorkCooldownTimer { get; private set; }

        // Commentary story
        [ProtoMember(80)]
        public int StorySport { get; set; }

        [ProtoMember(81)]
        public int StoryInstrument { get; set; }

        [ProtoMember(82)]
        public int StoryWorkedplace { get; set; }

        [ProtoMember(83)]
        public int DrinkingKekFrame { get; set; }

        [ProtoMember(84)]
        public int KekHappinessTimer { get; set; }

        [ProtoMember(85)]
        public int StrikeCooldownTimer { get; private set; }

        [ProtoMember(90)]
        private List<long> recentMealTimesAsList;
        private Queue<long> recentMealTimes;

        public int FramesSinceWaking => this.stress.FramesSinceWaking;
        
        public int StressRateOfChange => (int)Math.Round(this.stress.RateOfChange);
        public StressLevel StressLevel => this.stress.CurrentLevel;

        public double HungerDisplay => 0.01 * (100.0 - this.Body.Nourishment).Clamp(0, 100);
        public double ThirstDisplay => 0.01 * (100.0 - this.Body.Hydration).Clamp(0, 100);
        public double TirednessDisplay => 0.01 * (100.0 - this.Body.Energy).Clamp(0, 100);
        public double StressDisplay => 0.01 * this.stress.Value.Clamp(0, 100);
        public double HungerRateOfChangeDisplay => 0.0 - this.Body.HungerRate * 3600.0;
        public double ThirstRateOfChangeDisplay => 0.0 - this.Body.ThirstRate * 3600.0;
        public double TirednessRateOfChangeDisplay => this.RestRatePerHour;
        public double StressRateOfChangeDisplay => 0.0 - this.stress.RateOfChange * 3600.0;

        public string CanRecycleReason { get; protected set; }
        public bool RequiresAccessNow => this.IsDead && this.RecyclePriority != WorkPriority.Disabled;

        public ICardCollection Cards { get; private set; }

        public string CurrentActivityDescription
        {
            get
            {
                IThing target;
                string targetName;
                string itemTypeName;
                switch (this.activityType)
                {
                    case ColonistActivityType.Construct:
                        var blueprintId = this.TargetBuilingID.GetValueOrDefault();
                        target = World.ConfirmedBlueprints.ContainsKey(blueprintId) ? World.ConfirmedBlueprints[blueprintId] : null;
                        targetName = target?.DisplayNameLower ?? "";
                        return LanguageManager.Get<ColonistActivityType>(this.activityType, targetName);
                    case ColonistActivityType.Deconstruct:
                        target = World.GetThing(this.TargetBuilingID.GetValueOrDefault());
                        targetName = target is IPlant ? target.ShortNameLower : target?.DisplayNameLower ?? "";
                        return LanguageManager.Get<ColonistActivityType>(this.activityType, targetName);
                    case ColonistActivityType.HaulDropoff:
                    case ColonistActivityType.HaulPickup:
                        target = World.GetThing(this.TargetBuilingID.GetValueOrDefault());
                        targetName = target is IResourceStack 
                            ? LanguageManager.Get<StringsForColonistPanel>(StringsForColonistPanel.ResourceStack) 
                            : LanguageManager.GetNameLower(ThingType.ResourceProcessor);
                        itemTypeName = LanguageManager.Get<StringsForItemTypeLower>(this.TargetItemType == ItemType.None ? this.CarriedItemTypeBack : this.TargetItemType);
                        return LanguageManager.Get<ColonistActivityType>(this.activityType, itemTypeName, targetName);
                    case ColonistActivityType.DrinkKek when this.DrinkingKekFrame == 0:
                        return LanguageManager.Get<ColonistActivityType>(ColonistActivityType.GetKek);
                    case ColonistActivityType.DrinkKek:
                        var percent = this.DrinkingKekFrame * 100 / Constants.ColonistFramesToDrinkKek;
                        return LanguageManager.Get<ColonistActivityType>(ColonistActivityType.DrinkKek, percent);
                    case ColonistActivityType.Relax when this.TimeSinceSocialByColonist.Any(c => c.Value == 0):
                        return LanguageManager.Get<ColonistActivityType>(ColonistActivityType.Social);
                }

                return LanguageManager.Get<ColonistActivityType>(this.activityType);
            }
        }

        public bool IsRenderLayer1 { get; private set; }
        public bool WaitForDoor { get; set; }
        public double WorkRate { get; private set; }

        public int RecycleTime => Constants.DeadColonistRecycleTime;

        public override bool IsHungry => this.Body.Nourishment < 60;
        public override bool IsThirsty => this.Body.Hydration < 60;

        public Colonist() : base(ThingType.Colonist)
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            if (this.recentMeals == null) this.recentMeals = new Queue<int>();
            if (this.recentMealTimes == null) this.recentMealTimes = new Queue<long>();
            if (this.Cards == null) this.Cards = new CardCollection();
            if (this.Body == null) this.Body = new ColonistBody();
            if (this.stress == null) this.stress = new ColonistStress();
        }

        public Colonist(ISmallTile tile) : base(ThingType.Colonist, tile)
        {
            this.TimeSinceSocialByColonist = new Dictionary<int, int>();
            this.UpdateRenderRow();
            this.Position = new Vector2f(this.mainTile.TerrainPosition.X, this.mainTile.TerrainPosition.Y);
            this.RenderPos = new Vector2f((10.6666667f * this.Position.X) + (10.6666667f * this.Position.Y) + 10.6666667f, (5.3333333f * this.Position.Y) - (5.3333333f * this.Position.X) + 16f);
            this.WorkPriorities = new Dictionary<ColonistPriority, int>
            {
                { ColonistPriority.ResearchBotanist, 3 },
                { ColonistPriority.ResearchEngineer, 3 },
                { ColonistPriority.ResearchGeologist, 3 },
                { ColonistPriority.Maintain, 4 },
                { ColonistPriority.Deconstruct, 6 },
                { ColonistPriority.Haul, 7 },
                { ColonistPriority.FarmPlant, 8 },
                { ColonistPriority.FarmHarvest, 8 },
                { ColonistPriority.FruitHarvest, 8 },
                { ColonistPriority.Construct, 9 },
            };

            this.RenderAlpha = 0f;
            this.Cards = new CardCollection();
            this.Story = new List<string>();
            this.colonistsByAccessTile = new Dictionary<int, int>();
            this.Body = new ColonistBody() { Temperature = 20.0, Hydration = 100.0, Nourishment = 100.0, Energy = 100.0 };
            this.framesSocialByColonist = new Dictionary<int, int>();
            this.framesSinceSocialByColonist = new Dictionary<int, int>();
            this.recentMeals = new Queue<int>();
            this.recentMealTimes = new Queue<long>();

            this.StorySport = Rand.Next(10);
            if (this.StorySport > 4) this.StorySport = 0;
            this.StoryInstrument = Rand.Next(10);
            if (this.StoryInstrument > 4) this.StoryInstrument = 0;
            this.StoryWorkedplace = Rand.Next(10);
            if (this.StoryWorkedplace > 5) this.StoryWorkedplace = 0;

            this.WorkPolicy = WorkPolicy.Normal;
            this.KekPolicy = KekPolicy.Normal;

            this.stress = new ColonistStress();
        }

        public override void BeforeSerialization()
        {
            this.cardsForSerialize = this.Cards as CardCollection;
            this.recentMealsAsList = this.recentMeals.ToList();
            this.recentMealTimesAsList = this.recentMealTimes.ToList();
            base.BeforeSerialization();
        }

        public override void AfterDeserialization()
        {
            this.RenderPos = new Vector2f((10.6666667f * this.Position.X) + (10.6666667f * this.Position.Y) + 10.6666667f, (5.3333333f * this.Position.Y) - (5.3333333f * this.Position.X) + 16f);
            if (this.TimeSinceSocialByColonist == null) this.TimeSinceSocialByColonist = new Dictionary<int, int>();
            if (this.IsDead) this.CenterOnTile();

            if (this.framesSocialByColonist == null)
            {
                this.framesSocialByColonist = new Dictionary<int, int>();
                this.framesSinceSocialByColonist = new Dictionary<int, int>();
            }

            if (this.recentMeals == null)
            {
                this.recentMeals = new Queue<int>();
                this.recentMealTimes = new Queue<long>();
            }

            if (this.recentMealsAsList != null)
            {
                foreach (var meal in this.recentMealsAsList)
                {
                    this.recentMeals.Enqueue(meal);
                    this.recentMealTimes.Enqueue(0);
                }
            }

            if (this.recentMealTimesAsList != null)
            {
                foreach (var frame in this.recentMealTimesAsList) this.recentMealTimes.Enqueue(frame);
            }

            while (this.recentMealTimes.Count > this.recentMeals.Count) this.recentMealTimes.Dequeue();

            // Temporary - for v0.3 changes
            if (!this.foodOpinions.ContainsKey(Constants.MushFoodType)) this.foodOpinions.Add(Constants.MushFoodType, -1);
            if (World.InitialGameVersion.Major == 0 && World.InitialGameVersion.Minor < 3 && this.ActivityType != ColonistActivityType.LeavingLandingPod) this.IsArrived = true;  // For commentary

            // Temporary - for v0.4 changes
            if (!this.WorkPriorities.ContainsKey(ColonistPriority.FruitHarvest)) this.WorkPriorities.Add(ColonistPriority.FruitHarvest, this.WorkPriorities[ColonistPriority.FarmHarvest]);

            this.Cards = this.cardsForSerialize;

            // Needed because of changes to opinions algorithm in v0.11
            this.UpdateDietCard(out _, out _);

            base.AfterDeserialization();
        }

        public override void Update()
        {
            this.FramesSinceArrival++;
            if (this.IsIdle) this.IdleTimer++;
            else this.IdleTimer = 0;

            if (this.IsDead)
            {
                if (this.IsDesignatedForRecycling)
                {
                    this.IsRecycling = true;
                }

                return;
            }

            if (this.ColonistToWelcome != 0)
            {
                EventManager.EnqueueColonistEvent(ColonistEventType.Welcome, this.Id, this.ColonistToWelcome);
                this.ColonistToWelcome = 0;
            }

            if (this.ColonistToMourn != 0)
            {
                EventManager.EnqueueColonistEvent(ColonistEventType.Death, this.Id, this.ColonistToMourn);
                this.ColonistToMourn = 0;
            }

            if (this.OreScannerTileForComment != 0)
            {
                var tile = World.GetSmallTile(this.OreScannerTileForComment);
                this.ScannerResourceFound = tile.MineResourceType;
                this.ScannerResourceDensityFound = tile.MineResourceDensity;
                EventManager.EnqueueColonistEvent(ColonistEventType.ScannerResourceFound, this.Id, this.OreScannerTileForComment);
                this.OreScannerTileForComment = 0;
            }

            if (this.WorkCooldownTimer > 0) this.WorkCooldownTimer--;
            if (this.StrikeCooldownTimer > 0) this.StrikeCooldownTimer--;

            var currentTile = World.GetSmallTile((int)(this.Position.X + 0.5f), (int)(this.Position.Y + 0.5f));
            var sleepPod = currentTile.ThingsPrimary.OfType<ISleepPod>().FirstOrDefault();
            var worldTemperature = this.GetEffectiveTileTemperature();

            var prevHydration = this.Body.Hydration;
            var prevNourishment = this.Body.Nourishment;
            var prevDehydration = this.Body.DehydrationLevel;
            var prevStarvation = this.Body.StarvationLevel;
            var prevBodyTemperature = this.Body.Temperature;

            this.Body.Update(worldTemperature, 100 + this.Cards.GetEffectsSum(CardEffectType.HungerRate), 100 + this.Cards.GetEffectsSum(CardEffectType.ThirstRate));
            if (this.Body.IsDead)
            {
                this.IsDead = true;
                this.Cards.Clear();
                World.UpdateCanHarvestFruit();
                World.UpdateCanDoGeology();
                World.UpdateCanFarm();
                this.CenterOnTile();
                this.UpdateRenderPos();
                World.UpdateThingPosition(this);  // Updates World.ThingsByRow
                EventManager.MovedColonists.AddIfNew(this.Id);  // Causes shadow to be removed
                WorldStats.Increment(WorldStatKeys.ColonistsDied);

                // Prod up to two other colonists to mourn our death
                foreach (var otherColonist in World.GetThings<IColonist>(ThingType.Colonist)
                    .Where(c => c.Id != this.Id && !c.IsDead)
                    .OrderBy(c => Guid.NewGuid())
                    .Take(2))
                {
                    otherColonist.ColonistToMourn = this.Id;
                }
            }
            else
            {
                // Roaming logic.  Colonist must roam for 1 hours to get card.  If they stop for 2 hours, the timer resets.
                if (this.ActivityType == ColonistActivityType.Roam)
                {
                    this.FramesRoaming++;
                    this.roamTimeout = 7200;
                    if (this.FramesRoaming >= Constants.ColonistRoamFramesForCard) this.FramesSinceRoam = 0;
                }
                else
                {
                    if (this.FramesSinceRoam.HasValue) this.FramesSinceRoam++;
                    if (this.roamTimeout > 0)
                    {
                        this.roamTimeout--;
                        if (this.roamTimeout == 0) this.FramesRoaming = 0;
                    }
                }
            }

            if (this.KekHappinessTimer > 0) this.KekHappinessTimer--;

            this.framesSinceCardsUpdate++;
            if (this.framesSinceCardsUpdate >= 8) this.UpdateCards();

            if (!this.Body.IsDead)
            {
                if (this.Body.Temperature >= 25 && prevBodyTemperature < 25)
                {
                    EventManager.EnqueueColonistEvent(ColonistEventType.Hot2, this.Id);
                }
                else if (this.Body.Temperature >= 22 && prevBodyTemperature < 22)
                {
                    EventManager.EnqueueColonistEvent(ColonistEventType.Hot1, this.Id);
                }
                else if (this.Body.Temperature <= 15 && prevBodyTemperature > 15)
                {
                    EventManager.EnqueueColonistEvent(ColonistEventType.Cold2, this.Id);
                }
                else if (this.Body.Temperature <= 18 && prevBodyTemperature > 18)
                {
                    EventManager.EnqueueColonistEvent(ColonistEventType.Cold1, this.Id);
                }
            }

            if (this.Body.IsSleeping)
            {
                // 5 hours to restore up to 75% rest
                this.RestRatePerHour = 75 / 5.0;
                this.Body.Energy += this.RestRatePerHour / 3600.0;
                if (this.Body.Energy > 100f)
                {
                    this.Body.Energy = 100f;
                }

                this.PositionOffset = Vector2f.Zero;
                this.Position = this.MainTile.TerrainPosition;
                this.RenderRow = this.mainTile.Row;

                if ((int)this.FacingDirection < 4 || !this.Rotation.ApproxEquals(DirectionHelper.GetAngleFromDirection(this.FacingDirection), 0.1f))
                {
                    // Colonist has to face NE, NW, SE or SW for sleep animation to work
                    this.Rotation += Mathf.PI / 60f;
                    if (this.Rotation > Mathf.PI * 2f) this.Rotation -= Mathf.PI * 2f;
                    this.FacingDirection = DirectionHelper.GetDirectionFromAngle(this.Rotation);
                    this.UpdateMovingAnimationFrame();
                }
                else if (this.SleepFrame < 16)
                {
                    this.SleepFrame++;
                    EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.SleepFrame), this.MainTile.Row, ThingType.Colonist);
                }
            }
            else
            {
                this.RestRatePerHour = this.IsResting ? -2.0 : -4.0;
                var prevEnergy = this.Body.Energy;
                this.Body.Energy += this.RestRatePerHour / 3600.0;
            }

            if (this.activityType != ColonistActivityType.DrinkKek) this.DrinkingKekFrame = 0;
            var stressModifier = this.Cards.GetEffectsSum(CardEffectType.StressRate) * 0.5;
            this.stress.Update(!this.Body.IsSleeping, this.IsWorking || this.isDoingWorkActivity, this.Cards.GetEffectsAny(CardEffectType.Workaholism), this.DrinkingKekFrame > 0, stressModifier);

            if (this.SleepFrame > 0 && !this.Body.IsSleeping)
            {
                this.SleepFrame--;
                EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.SleepFrame), null, this.SleepFrame, this.MainTile.Row, ThingType.Colonist);
            }
            
            if (this.RaisedArmsFrame > 0 && (!this.IsWorking && !this.IsRelaxing))
            {
                this.RaisedArmsFrame--;
                EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.RaisedArmsFrame), null, this.RaisedArmsFrame, this.MainTile.Row, ThingType.Colonist);
            }

            if (this.IsDead)
            {
                this.Body.Nourishment = 0;
                this.Body.Hydration = 0;
                this.Body.Energy = 0;
                this.TargetBuilingID = null;
                this.Body.IsSleeping = false;
                this.IsWorking = false;

                PathFinderBlockManager.RemoveBlocks(this.Id);
                ZoneManager.HomeZone.UpdateNode(this.MainTileIndex);
                ZoneManager.GlobalZone.UpdateNode(this.MainTileIndex);
                ZoneManager.AnimalZone.UpdateNode(this.MainTileIndex);
                EventManager.RaiseEvent(EventType.Zone, null);

                this.AnimationFrame = 32 + (this.GetAnimationFrameForFacingDirection() / 4);
                EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.AnimationFrame), null, this.AnimationFrame, this.MainTile.Row, ThingType.Colonist);
                return;
            }

            if (this.IsMoving)
            {
                this.UpdateMovingAnimationFrame();
                this.IsResting = false;
            }
            else if (this.Body.IsSleeping)
            {
                this.Body.SleepTimer--;
                this.SleptFrames++;
                this.SleptTemperatureSum += (int)worldTemperature;
                if (this.Body.SleepTimer == 0 || this.Body.Energy > 99.999f)
                {
                    // Wake up
                    this.ActivityType = ColonistActivityType.None;
                    this.Body.IsSleeping = false;
                    this.SleptTemperature = this.SleptTemperatureSum / this.SleptFrames;
                    this.SleptTemperatureSum = 0;
                    this.SleptFrames = 0;

                    // Diary entry and sleep quality
                    var sleepQuality = 0;
                    var ownPod = false;
                    if (this.SleptOutside == true)
                    {
                        EventManager.EnqueueColonistEvent(ColonistEventType.EndSleepOutside, this.Id);
                        sleepQuality -= 4;
                    }
                    else if (this.SleptInPod != true)
                    {
                        EventManager.EnqueueColonistEvent(ColonistEventType.EndSleepNoPod, this.Id);
                        sleepQuality -= 3;
                    }
                    else
                    {
                        var pod = this.MainTile.ThingsPrimary.OfType<ISleepPod>().FirstOrDefault();
                        if (pod?.OwnerID == this.Id)
                        {
                            ownPod = true;
                            sleepQuality++;
                        }
                    }

                    var coldTolerance = this.Cards.GetEffectsSum(CardEffectType.ColdTolerance);
                    var heatTolerance = this.Cards.GetEffectsSum(CardEffectType.HeatTolerance);
                    if (this.SleptTemperature + coldTolerance < 15)
                    {
                        var penalty = (int)((20 - this.SleptTemperature - coldTolerance) / 5);
                        EventManager.EnqueueColonistEvent(ColonistEventType.SleptCold, this.Id);
                        sleepQuality -= penalty;
                    }
                    else if (this.SleptTemperature > 25 + heatTolerance)
                    {
                        var penalty = (int)((this.SleptTemperature - 20 - heatTolerance) / 5);
                        EventManager.EnqueueColonistEvent(ColonistEventType.SleptHot, this.Id);
                        sleepQuality -= penalty;
                    }
                    else if (ownPod)
                    {
                        EventManager.EnqueueColonistEvent(ColonistEventType.EndSleepOwnPod, this.Id);
                    }
                    else
                    {
                        EventManager.EnqueueColonistEvent(ColonistEventType.EndSleep, this.Id);
                    }

                    // Cards for sleep quality
                    if (this.Cards.Contains(CardType.SleepGood))
                    {
                        this.Cards.Remove(CardType.SleepGood);
                    }

                    if (this.Cards.Contains(CardType.SleepBad))
                    {
                        this.Cards.Remove(CardType.SleepBad);
                    }

                    if (sleepQuality > 0)
                    {
                        var card = this.Cards.Add(CardType.SleepGood);
                        card.Effects[CardEffectType.Happiness] = sleepQuality;
                    }
                    else if (sleepQuality < 0)
                    {
                        var card = this.Cards.Add(CardType.SleepBad);
                        card.Effects[CardEffectType.Happiness] = sleepQuality;
                    }
                }
            }
            else if (this.IsWorking)
            {
                this.RaiseArms();
            }
            else if (this.WaitTimer > 0)
            {
                this.WaitTimer--;
            }

            if (this.RenderAlpha < 0.9999f)
            {
                if (!(this.MainTile.ThingsPrimary.FirstOrDefault(t => t.ThingType == ThingType.LandingPod) is ILandingPod pod) || pod.Altitude <= 0.001f) this.RenderAlpha = 1f;
            }

            var frameMod = this.FramesSinceArrival % 60;
            if (frameMod == 0)
            {
                // Keep track of who we are socialising with
                foreach (var c in this.TimeSinceSocialByColonist.Keys.ToList()) this.TimeSinceSocialByColonist[c]++;
                if (this.IsRelaxing)
                {
                    var table = this.MainTile.GetTileToDirection(this.FacingDirection)?.ThingsPrimary?.OfType<ITable>()?.FirstOrDefault();
                    var otherColonists = table?.GetOtherColonists(this.Id);
                    if (otherColonists?.Any() == true)
                    {
                        foreach (var oc in otherColonists)
                        {
                            if (!(World.GetThing(oc) is IColonist other) || other.MainTile.AdjacentTiles4.All(t => t != table.MainTile)) continue;  // Other colonist assigned to table, but not here yet
                            if (!this.TimeSinceSocialByColonist.ContainsKey(oc)) this.TimeSinceSocialByColonist.Add(oc, 0);
                            else this.TimeSinceSocialByColonist[oc] = 0;
                        }
                    }
                }

                // Loneliness effect increases every 48 hours when we are on our own
                if (this.FramesSinceArrival >= 172800 && WorldStats.Get(WorldStatKeys.ColonistsWoken) == 1)
                {
                    this.Cards.UpdateLonelinessCard(Math.Min((int)this.FramesSinceArrival / 172800, 5));
                }
                else this.Cards.UpdateLonelinessCard(0);

                if (this.FramesSinceArrival > 0 && !this.Body.IsSleeping && !this.Body.IsDead && this.MainTile.ThingsPrimary.All(t => t.ThingType != ThingType.LandingPod))
                {
                    // Diary entries for workload level
                    if (this.stress.CurrentLevel != this.stressAtLastDiaryUpdate)
                    {
                        this.stressAtLastDiaryUpdate = this.stress.CurrentLevel;
                        switch (this.stress.CurrentLevel)
                        {
                            case StressLevel.Low: 
                                EventManager.EnqueueColonistEvent(ColonistEventType.WorkloadLow, this.Id);
                                break;
                            case StressLevel.Moderate:
                                EventManager.EnqueueColonistEvent(ColonistEventType.WorkloadModerate, this.Id);
                                break;
                            case StressLevel.High: 
                                EventManager.EnqueueColonistEvent(ColonistEventType.WorkloadHigh, this.Id);
                                break;
                            case StressLevel.Extreme: 
                                EventManager.EnqueueColonistEvent(ColonistEventType.WorkloadExtreme, this.Id);
                                break;
                        }
                    }

                    // Periodic events for commentary
                    if (this.Cards.Contains(CardType.Roam) && this.ActivityType != ColonistActivityType.Roam && this.FramesSinceRoam < 1800)
                    {
                        EventManager.EnqueueColonistEvent(ColonistEventType.HadWalk, this.Id);
                    }

                    if (this.activityType == ColonistActivityType.Relax && this.IsRelaxing && !this.IsMoving)
                    {
                        EventManager.EnqueueColonistEvent(ColonistEventType.Relaxing, this.Id);
                    }

                    foreach (var kv in this.framesSinceSocialByColonist)
                    {
                        if (kv.Value > 60 && kv.Value <= 120 && framesSocialByColonist[kv.Key] >= Constants.ColonistSocialFramesForCard)
                        {
                            EventManager.EnqueueColonistEvent(ColonistEventType.TalkedWith, this.Id, kv.Key);
                        }
                    }
                }
            }

            if (this.FramesSinceArrival > 0 && this.FramesSinceArrival % 3600 == 0)
            {
                // Every hour, randomly update our mood.
                // If we are in a good or bad mood, this has a 10% chance to end.
                // If not, then we have a 5% chance to begin a mood.
                if (this.Cards.Contains(CardType.GoodMood))
                {
                    if (Rand.Next(10) == 0) this.Cards.Remove(CardType.GoodMood);
                }
                else if (this.Cards.Contains(CardType.BadMood))
                {
                    if (Rand.Next(10) == 0) this.Cards.Remove(CardType.BadMood);
                }
                else if (Rand.Next(20) == 0)
                {
                    if (Rand.Next(2) == 0)
                    {
                        // Good mood
                        var bonus = Rand.Next(3) + 1;
                        var card = this.Cards.AddMoodCard(bonus);
                        card.Effects[CardEffectType.Happiness] = bonus;
                        bool r = Rand.Next(2) == 0;
                        EventManager.EnqueueColonistEvent(r ? ColonistEventType.GoodMood2 : ColonistEventType.GoodMood1, this.Id);
                    }
                    else
                    {
                        // Bad mood
                        var penalty = Rand.Next(5) + 1;
                        var card = this.Cards.AddMoodCard(0 - penalty);
                        card.Effects[CardEffectType.Happiness] = 0 - penalty;
                        bool r = Rand.Next(2) == 0;
                        EventManager.EnqueueColonistEvent(r ? ColonistEventType.BadMood2 : ColonistEventType.BadMood1, this.Id);
                    }
                }
            }

            base.Update();
        }

        private void CenterOnTile()
        {
            var posTile = World.GetSmallTile((int)(this.Position.X + 0.5f), (int)(this.Position.Y + 0.5f));
            this.SetPosition(posTile);
            this.PositionOffset = Vector2f.Zero;
            this.Position = posTile.TerrainPosition;
        }

        public override Vector2f GetWorldPosition()
        {
            var result = base.GetWorldPosition();

            var pod = this.MainTile.ThingsPrimary.OfType<ILandingPod>().FirstOrDefault();
            if (pod != null) result.Y -= pod.Altitude;

            return result;
        }

        public void UpdateMovingAnimationFrame()
        {
            var frame = 31 - (int)(((this.Rotation / Mathf.PI) + 0.75f) * 16f);
            if (frame < 0) frame += 32;
            if (frame > 31) frame -= 32;
            this.AnimationFrame = frame;
        }

        public override void AfterAddedToWorld()
        {
            World.UpdateCanHarvestFruit();
            World.UpdateCanDoGeology();
            World.UpdateCanFarm();
            EventManager.EnqueueSoundAddEvent(this.id, "Construct");
            base.AfterAddedToWorld();
        }

        public override void BeforeRemoveFromWorld()
        {
            if (this.Skill == SkillType.Engineer) EventManager.EnqueueSoundRemoveEvent(this.id);
            base.BeforeRemoveFromWorld();
        }

        /// <summary>
        /// Used for testing suitability of a tile for sleep
        /// </summary>
        public int GetTileSleepScore(ISmallTile tile)
        {
            // No Pod, Outdoors, Not Dark, Hot, Cold
            var result = 100;

            var pod = tile.ThingsPrimary.OfType<ISleepPod>().FirstOrDefault();
            if (pod == null)
            {
                result -= 20;
            }

            var room = RoomManager.GetRoom(tile.Index);

            if (pod == null)
            {
                if (room == null)
                {
                    result -= 20;
                }

                var light = room?.Light ?? World.WorldLight.Brightness;
                if (light >= 0.3f)
                {
                    result -= (int)(20 * (light - 0.25)).Clamp(0.0, 0.1);   // 0 - 10%
                }
            }

            var temperature = this.GetEffectiveTileTemperature();
            if (temperature > 25)
            {
                result -= (int)(0.5 * (temperature - 20.0)).Clamp(0.0, 0.1);   // 0 - 10% for 20-40C
            }
            else if (temperature < 15)
            {
                result -= (int)(0.5 * (20 - temperature)).Clamp(0.0, 0.1);   // 0 - 10% for 0-20C
            }

            return result;
        }

        private float GetEffectiveTileTemperature()
        {
            var sleepPod = this.MainTile.ThingsPrimary.OfType<ISleepPod>().FirstOrDefault();
            var temperature = sleepPod?.Temperature ?? RoomManager.GetTileTemperature(this.MainTile.Index);

            if (this.Cards.Contains(CardType.HeatTolerant) && temperature > 20) temperature -= Mathf.Min(5, temperature - 20);
            if (this.Cards.Contains(CardType.ColdTolerant) && temperature < 20) temperature += Mathf.Min(5, 20 - temperature);

            return temperature;
        }

        public double GetWorkRate()
        {
            var result = 1.0 + (0.01 * this.Cards.GetEffectsSum(CardEffectType.WorkSpeed));
            if (result < 0.1) result = 0.0;

            this.WorkRate = result;
            return result;
        }

        public double GetWorkRate(out Dictionary<CardType, int> effects)
        {
            effects = this.Cards.GetCardsByEffect(CardEffectType.WorkSpeed).ToDictionary(kv => kv.Key, kv => kv.Value);
            var total = effects.Values.Sum();

            var result = 1.0 + (0.01 * total);
            if (result < 0.1) result = 0.0;

            this.WorkRate = result;
            return result;
        }

        public void AddCard(CardType cardType)
        {
            this.Cards.Add(cardType);
        }

        private int GetAnimationFrameForFacingDirection()
        {
            if (this.FacingDirection == Direction.SW) return 0;
            if (this.FacingDirection == Direction.S) return 4;
            if (this.FacingDirection == Direction.SE) return 8;
            if (this.FacingDirection == Direction.E) return 12;
            if (this.FacingDirection == Direction.NE) return 16;
            if (this.FacingDirection == Direction.N) return 20;
            if (this.FacingDirection == Direction.NW) return 24;
            return 28;  // W
        }

        public void RaiseArms()
        {
            var frame = this.GetAnimationFrameForFacingDirection();
            if (this.AnimationFrame != frame) this.AnimationFrame = frame;
            if (this.RaisedArmsFrame < 16 || (this.ActivityType != ColonistActivityType.Lab && this.RaisedArmsFrame < 18))
            {
                this.RaisedArmsFrame++;
                EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.RaisedArmsFrame), null, this.RaisedArmsFrame, this.MainTile.Row, ThingType.Colonist);
            }
        }

        public override void UpdateRenderRow()
        {
            this.PrevRenderRow = this.RenderRow;
            this.RenderRow = this.mainTile.Row;

            var offset = this.Position - this.MainTile.TerrainPosition;
            var offsetY = offset.Y - offset.X;

            var renderRowExact = this.RenderRow + offsetY;
            if (renderRowExact % 1 > 0.5f)
            {
                this.RenderRow = (int)renderRowExact + 1;
                this.IsRenderLayer1 = this.mainTile.ThingsPrimary.All(t => t.ThingType != ThingType.SleepPod);  // This is correct for entering a NE / NW facing pod, may be wrong for others
            }
            else
            {
                this.RenderRow = (int)renderRowExact;
                this.IsRenderLayer1 = false;
            }

            //if (offsetY >= 0.5f) this.RenderRow += 1 + (int)(offsetY - 0.5f);
            //else if (offsetY <= -0.5f) this.RenderRow -= 1 + (int)((0 - offsetY) - 0.5f);
        }

        public void BeginSleeping()
        {
            if (!this.Body.IsSleeping)
            {
                this.Body.IsSleeping = true;
                this.Body.SleepTimer = 5 * WorldTime.SecondsInMinute * WorldTime.MinutesInHour;
                this.SleptInPod = this.mainTile.ThingsPrimary.Any(t => t is ISleepPod);
                if (this.SleptInPod == false)
                {
                    var room = RoomManager.GetRoom(this.MainTileIndex);
                    this.SleptOutside = room == null;
                }
                else this.SleptOutside = false;

                // Commentary events
                if (this.SleptOutside == true)
                {
                    EventManager.EnqueueColonistEvent(ColonistEventType.StartSleepOutside, this.Id);
                }
                else if (this.SleptInPod != true)
                {
                    EventManager.EnqueueColonistEvent(ColonistEventType.StartSleepNoPod, this.Id);
                }
                else if (this.FramesSinceArrival < 72000)
                {
                    EventManager.EnqueueColonistEvent(ColonistEventType.StartSleepFirstNight, this.Id);
                }
                else
                {
                    EventManager.EnqueueColonistEvent(ColonistEventType.StartSleep, this.Id);
                }

                if (this.Cards.Contains(CardType.Tired1)) this.Cards.Remove(CardType.Tired1);
                if (this.Cards.Contains(CardType.Tired2)) this.Cards.Remove(CardType.Tired2);
                if (this.Cards.Contains(CardType.SleepBad)) this.Cards.Remove(CardType.SleepBad);
                if (this.Cards.Contains(CardType.SleepGood)) this.Cards.Remove(CardType.SleepGood);
            }
        }

        public void Eat(int foodType, double nourishment, bool firstFrame)
        {
            this.Body.Eat(nourishment);

            if (firstFrame)
            {
                this.LastFoodType = foodType;
                this.recentMeals.Enqueue(foodType);
                if (this.recentMeals.Count > 4) this.recentMeals.Dequeue();

                this.recentMealTimes.Enqueue(World.WorldTime.FrameNumber);
                if (this.recentMealTimes.Count > 4) this.recentMealTimes.Dequeue();
            }
        }

        public void UpdateDietCard(out int varietyHappiness, out int lastMealRepeatCount)
        {
            var mealCount = 0;
            var opinionSum = 0;
            var mealTypes = new HashSet<int>();

            var lastMeal = -1;
            lastMealRepeatCount = 0;

            foreach (var meal in this.recentMeals)
            {
                var def = CropDefinitionManager.GetDefinition(meal);
                if (def != null)
                {
                    var opinion = this.GetFoodOpinion(meal);
                    mealCount++;
                    opinionSum += opinion.GetValueOrDefault();
                    if (meal != lastMeal)
                    {
                        mealTypes.Add(meal);
                        lastMeal = meal;
                        lastMealRepeatCount = 1;
                    }
                    else lastMealRepeatCount++;
                }
            }

            var likesHappiness = 0;
            if (opinionSum > 2) likesHappiness = 2;
            else if (opinionSum > 0) likesHappiness = 1;
            else if (opinionSum < -2) likesHappiness = -2;
            else if (opinionSum < 0) likesHappiness = -1;

            varietyHappiness = 0;
            if (mealTypes.Count > 3) varietyHappiness = 2;
            else if (mealTypes.Count > 2) varietyHappiness = 1;
            else if (mealCount >= 3 && mealTypes.Count == 1) varietyHappiness = -1;

            var totalHappiness = likesHappiness + varietyHappiness;

            var cardType = CardType.NeutralDiet;
            if (totalHappiness > 0) cardType = CardType.GoodDiet;
            else if (totalHappiness < 0) cardType = CardType.BadDiet;

            if (!this.Cards.Contains(cardType))
            {
                if (this.Cards.Contains(CardType.GoodDiet)) this.Cards.Remove(CardType.GoodDiet);
                if (this.Cards.Contains(CardType.NeutralDiet)) this.Cards.Remove(CardType.NeutralDiet);
                if (this.Cards.Contains(CardType.BadDiet)) this.Cards.Remove(CardType.BadDiet);

                this.Cards.Add(cardType);
            }

            var card = this.Cards.Cards[cardType] as DietCard;
            card.Update(likesHappiness, varietyHappiness
                , this.foodOpinions.Where(o => o.Key != Constants.MushFoodType && o.Value > 0).Select(o => CropDefinitionManager.GetDefinition(o.Key).DisplayNameLower).ToList()
                , this.foodOpinions.Where(o => o.Key != Constants.MushFoodType && o.Value < 0).Select(o => CropDefinitionManager.GetDefinition(o.Key).DisplayNameLower).ToList());
            this.Cards.IsDisplayInvalidated = true;
        }

        public void Drink(double hydration)
        {
            this.Body.Drink(hydration);
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var result = new Dictionary<ItemType, int>
            {
                { ItemType.Metal, 0 },
                { ItemType.Biomass, this.IsDead ? Constants.DeadColonistOrganicsYield : 0 },
                { ItemType.IronOre, 0 }
            };

            return result;
        }

        public override int GetDeconstructionYield(ItemType resourceType)
        {
            return (resourceType == ItemType.Biomass && this.IsDead) ? Constants.DeadColonistOrganicsYield : 0;
        }

        public override string ToString()
        {
            return this.Name;
        }

        public bool CanRecycle()
        {
            return this.IsDead;
        }

        public virtual void Recycle()
        {
            this.IsDesignatedForRecycling = true;
        }

        public void RequestGiveWay(List<ISmallTile> tiles)
        {
            if (this.GiveWayRequestTiles == null) this.GiveWayRequestTiles = new HashSet<int>();
            foreach (var t in tiles)
            {
                if (t != null && !this.GiveWayRequestTiles.Contains(t.Index)) this.GiveWayRequestTiles.Add(t.Index);
            }
        }

        private void UpdateCards()
        {
            // New colony?
            this.Cards.UpdateNewColonyCard(Constants.ColonistNewColonyBonusHours - World.WorldTime.TotalHoursPassed);

            // New arrival?
            var newArrivalHappiness = 0;
            var hoursSinceArrival = this.FramesSinceArrival / 3600;
            if (hoursSinceArrival < Constants.ColonistNewArrivalBonusHours)
            {
                newArrivalHappiness = (int)(1 + (Constants.ColonistNewArrivalBonusHours - hoursSinceArrival) / (Constants.ColonistNewArrivalBonusHours / Constants.ColonistNewArrivalHappiness));
                if (newArrivalHappiness > Constants.ColonistNewArrivalHappiness) newArrivalHappiness = Constants.ColonistNewArrivalHappiness;
            }
            this.Cards.UpdateNewArrivalCard(newArrivalHappiness);

            // Happiness
            this.Happiness = this.Cards.GetEffectsSum(CardEffectType.Happiness);
            this.Cards.UpdateHappinessCard(this.Happiness);

            // Workload
            this.Cards.UpdateWorkloadCard(this.StressLevel);

            // Heat and cold
            var hypothermia = this.Body.MedicalConditions.ContainsKey(MedicalConditionType.Hypothermia) ? this.Body.MedicalConditions[MedicalConditionType.Hypothermia].Severity : 0.0;
            var hyperthermia = this.Body.MedicalConditions.ContainsKey(MedicalConditionType.Hyperthermia) ? this.Body.MedicalConditions[MedicalConditionType.Hyperthermia].Severity : 0.0;
            this.Cards.UpdateHotColdCard(this.Body.Temperature, (int)hypothermia, (int)hyperthermia);

            // Thirst and dehydration
            var dehydration = this.Body.MedicalConditions.ContainsKey(MedicalConditionType.Dehydration) ? this.Body.MedicalConditions[MedicalConditionType.Dehydration].Severity : 0.0;
            this.Cards.UpdateThirstCard(this.Body.Hydration, (int)dehydration);

            // Hunger and starvation
            var starvation = this.Body.MedicalConditions.ContainsKey(MedicalConditionType.Starvation) ? this.Body.MedicalConditions[MedicalConditionType.Starvation].Severity : 0.0;
            this.Cards.UpdateHungerCard(this.Body.Nourishment, (int)starvation);

            // Tiredness
            if (!this.Body.IsSleeping) this.Cards.UpdateTirednessCard(this.Body.Energy);

            // Roaming
            this.Cards.UpdateRoamCard(this.FramesSinceRoam, this.FramesRoaming);

            // Social
            if (this.IsRelaxing && this.mainTile.AdjacentTiles4.SelectMany(t => t.ThingsPrimary).OfType<ITable>().FirstOrDefault() is ITable table)
            {
                var otherColonists = table.MainTile.AdjacentTiles4.SelectMany(t => t.ThingsPrimary).OfType<IColonist>().Where(c => c != this).Select(c => c.Id).ToList();
                foreach (var c in otherColonists)
                {
                    if (!this.framesSocialByColonist.ContainsKey(c))
                    {
                        this.framesSocialByColonist.Add(c, 0);
                        this.framesSinceSocialByColonist.Add(c, 0);
                    }
                    else
                    {
                        this.framesSocialByColonist[c] += this.framesSinceCardsUpdate;
                        this.framesSinceSocialByColonist[c] = 0;
                    }
                }

                foreach (var id in this.framesSocialByColonist.Keys.Where(c => !otherColonists.Contains(c)).ToList())
                {
                    this.framesSinceSocialByColonist[id] += this.framesSinceCardsUpdate;
                    if (this.framesSinceSocialByColonist[id] > Constants.ColonistSocialCardTimeout) this.framesSocialByColonist[id] = 0;
                }
            }
            else
            {
                foreach (var id in this.framesSocialByColonist.Keys.ToList())
                {
                    this.framesSinceSocialByColonist[id] += this.framesSinceCardsUpdate;
                    if (this.framesSinceSocialByColonist[id] > Constants.ColonistSocialCardTimeout) this.framesSocialByColonist[id] = 0;
                }
            }

            this.Cards.UpdateSocialCards(this.framesSocialByColonist, this.framesSinceSocialByColonist, World.GetThings<IColonist>(ThingType.Colonist).ToDictionary(c => c.Id, c => c.ShortName));

            // Improved kek?
            this.Cards.UpdateKekCard(this.KekHappinessTimer);

            // Dark?
            var posTile = World.GetSmallTile((int)(this.Position.X + 0.5f), (int)(this.Position.Y + 0.5f));
            var light = RoomManager.GetTileLightLevel(posTile.Index);
            this.Cards.UpdateDarknessCard(light, !this.Body.IsSleeping);

            // Working at lab outside?
            var workingAtLabOutside = this.ActivityType == ColonistActivityType.Lab && this.IsWorking && this.MainTile.ThingsPrimary.OfType<IBuildableThing>().All(t => t.ThingType != ThingType.Roof || !t.IsReady);
            this.Cards.UpdateWorkOutdoorsCard(workingAtLabOutside);

            this.framesSinceCardsUpdate = 0;
        }

        public IEnumerable<CropDefinition> GetFoodLikes()
        {
            if (this.foodOpinions == null) this.foodOpinions = new Dictionary<int, int>();
            return this.foodOpinions.Where(kv => kv.Value > 0).Select(kv => CropDefinitionManager.GetDefinition(kv.Key)).Where(d => d.CanEat);
        }

        public IEnumerable<CropDefinition> GetFoodDisikes()
        {
            if (this.foodOpinions == null) this.foodOpinions = new Dictionary<int, int>();
            return this.foodOpinions.Where(kv => kv.Value < 0).Select(kv => CropDefinitionManager.GetDefinition(kv.Key)).Where(d => d.CanEat);
        }

        public IEnumerable<CropDefinition> GetFoodNeutral()
        {
            if (this.foodOpinions == null) this.foodOpinions = new Dictionary<int, int>();
            return this.foodOpinions.Where(kv => kv.Value == 0).Select(kv => CropDefinitionManager.GetDefinition(kv.Key)).Where(d => d.CanEat);
        }

        public IReadOnlyCollection<Tuple<int, long>> GetRecentMeals()
        {
            var result = new List<Tuple<int, long>>();
            var meals = this.recentMeals.ToList();
            var times = this.recentMealTimes.ToList();
            for (var i = 0; i < meals.Count; i++)
            {
                result.Add(new Tuple<int, long>(meals[i], times.Count > i ? times[i] : 0));
            }

            return result;
        }

        public int? GetFoodOpinion(int cropType, bool pickRandomIfNew = false)
        {
            if (this.foodOpinions == null) this.foodOpinions = new Dictionary<int, int>();
            if (!this.foodOpinions.ContainsKey(cropType))
            {
                if (!pickRandomIfNew) return null;

                var likes = 0;
                var neutral = 0;
                var dislikes = 0;
                foreach (var kv in this.foodOpinions)
                {
                    switch (kv.Value)
                    {
                        case 1: likes++; break;
                        case 0: neutral++; break;
                        case -1: dislikes++; break;
                    }
                }

                var maxNeutral = World.ClimateType == ClimateType.Snow ? 2 : 3;
                int opinion;
                do
                {
                    opinion = Rand.Next(3) - 1;
                }
                while ((opinion == 1 && likes > 2) || (opinion == 0 && neutral > maxNeutral) || (opinion == -1 && dislikes > 2));

                this.foodOpinions.Add(cropType, opinion);
            }

            return this.foodOpinions[cropType];
        }

        public void SetFoodOpinion(int cropType, int opinion)
        {
            if (this.foodOpinions == null) this.foodOpinions = new Dictionary<int, int>();
            if (!this.foodOpinions.ContainsKey(cropType)) this.foodOpinions.Add(cropType, opinion);
            else this.foodOpinions[cropType] = opinion;
        }

        public void SetKekPolicy(KekPolicy newPolicy)
        {
            this.KekPolicy = newPolicy;
        }

        public void SetWorkPolicy(WorkPolicy newPolicy)
        {
            if (this.WorkPolicy == newPolicy) return;

            this.WorkPolicy = newPolicy;
            this.WorkCooldownTimer = 0;
            this.StrikeCooldownTimer = 0;
        }

        public int GetFoodScoreForEating(int foodType)
        {
            if (this.foodOpinions == null) this.foodOpinions = new Dictionary<int, int>();
            var opinion = this.foodOpinions.ContainsKey(foodType) ? (this.foodOpinions[foodType] * 3) : 1;
            return opinion + (this.recentMeals?.Skip(1)?.Contains(foodType) == true ? 0 : 2);
        }

        public bool IsWillingToWork(bool isUrgent)
        {
            if (this.WorkPolicy == WorkPolicy.None || this.StrikeCooldownTimer > 0) return false;

            if (this.Cards.GetEffectsAny(CardEffectType.OnStrike))
            {
                this.StrikeCooldownTimer = 3600;
                return false;
            }

            if (isUrgent) return true;

            if (this.Cards.GetEffectsAny(CardEffectType.UrgentWorkOnly) || this.WorkCooldownTimer > 0) return false;

            if (this.WorkPolicy == WorkPolicy.Relaxed && this.StressLevel >= StressLevel.Moderate)
            {
                this.WorkCooldownTimer = 3600;
                return false;
            }

            if (this.WorkPolicy == WorkPolicy.Normal && this.StressLevel >= StressLevel.High)
            {
                this.WorkCooldownTimer = 3600;
                return false;
            }

            return true;
        }

        #region IColonistInteractive implementation (used for recycling the dead)

        public IEnumerable<ISmallTile> GetAllAccessTiles()
        {
            for (int i = 0; i <= 7; i++)   // NE, SE, SW, NW
            {
                var direction = (Direction)i;
                var tile = this.mainTile.GetTileToDirection(direction);
                if (tile == null || !tile.CanWorkInTile || this.MainTile.HasWallToDirection(direction)) continue;   // Can't work here
                yield return tile;
            }
        }

        public IEnumerable<ISmallTile> GetAccessTiles(int? colonistId = null)
        {
            if (!this.IsDead) yield break;

            this.CleanupColonistAssignments();
            if (colonistId.HasValue && this.colonistsByAccessTile.Any(c => c.Value != colonistId)) yield break;
            else if (!colonistId.HasValue && this.colonistsByAccessTile.Any()) yield break;

            for (int i = 0; i <= 7; i++)
            {
                var direction = (Direction)i;
                var tile = this.mainTile.GetTileToDirection(direction);
                if (tile == null || !tile.CanWorkInTile || this.MainTile.HasWallToDirection(direction)) continue;   // Can't work here
                if (tile.ThingsPrimary.Any(t => t is IColonist c && (colonistId == null || c.Id != colonistId) && !c.IsMoving && !c.IsRelaxing)) continue;   // Blocked by another colonist
                yield return tile;
            }
        }

        public bool CanAssignColonist(int colonistId, int? tileIndex = null)
        {
            this.CleanupColonistAssignments();
            if (this.colonistsByAccessTile.ContainsValue(colonistId)) return true;
            if (this.colonistsByAccessTile.Any()) return false;

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
                // Deconstruction must finish once started - cannot unassign.
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && c.ActivityType == ColonistActivityType.Deconstruct && !c.IsDead) continue;
                this.colonistsByAccessTile.Remove(id);
            }
        }

        #endregion
    }
}

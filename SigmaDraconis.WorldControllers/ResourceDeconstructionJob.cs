namespace SigmaDraconis.WorldControllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using World;
    using World.Blueprints;
    using World.Flora;
    using World.ResourceStacks;
    using WorldInterfaces;

    [ProtoContract]
    public class ResourceDeconstructionJob
    {
        protected static int nextId = 0;

        private IColonist colonist;
        private IRecyclableThing target;
        private readonly List<Blueprint> targetBlueprints = new List<Blueprint>();
        private ResourceStack stack;
        private Blueprint stackBlueprint;

        [ProtoMember(1)]
        public int ID { get; private set; }

        [ProtoMember(2)]
        public int ColonistId { get; private set; }

        [ProtoMember(3)]
        public int TargetId { get; private set; }

        [ProtoMember(4)]
        public int? TargetBlueprintId { get; private set; }

        [ProtoMember(5)]
        public int StackId { get; private set; }

        [ProtoMember(6)]
        public int? StackBlueprintId { get; private set; }

        [ProtoMember(7)]
        public double TotalWork { get; private set; }

        [ProtoMember(8)]
        public double WorkDone { get; private set; }

        public bool IsFinished => this.WorkDone >= this.TotalWork;

        // Ctor for deserialization
        public ResourceDeconstructionJob()
        {
        }

        public ResourceDeconstructionJob(IColonist colonist, IRecyclableThing target, int frames)
        {
            this.ID = ++nextId;
            this.colonist = colonist;
            this.ColonistId = colonist.Id;
            this.target = target;
            this.TargetId = target.Id;
            this.TotalWork = frames;
            this.WorkDone = 0;
        }

        [ProtoAfterDeserialization]
        private void AfterDeserialization()
        {
            if (this.ID >= nextId) nextId = this.ID + 1;
            this.colonist = World.GetThing(this.ColonistId) as IColonist;
            this.target = World.GetThing(this.TargetId) as IRecyclableThing;
            this.stack = World.GetThing(this.StackId) as ResourceStack;
            if (this.IsFinished) return;

            this.targetBlueprints.Clear();
            if (this.TargetBlueprintId.HasValue)
            {
                // Add all blueprints of same thingtype in tile (designed for coast grass)
                var target1 = World.RecycleBlueprints[this.TargetBlueprintId.Value];
                this.targetBlueprints.AddRange(World.RecycleBlueprints.Values.Where(b => b.Id == target1.Id || (b.MainTileIndex == target1.MainTileIndex && b.ThingType == target1.ThingType)));
            }

            if (this.StackBlueprintId.HasValue) this.stackBlueprint = World.RecycleBlueprints[this.StackBlueprintId.Value];
        }

        public void Start()
        {
            if (this.target is IOreScanner scanner && scanner.AnimationFrame > 1) return;
            
            this.target.IsRecycling = true;
            EventManager.RaiseEvent(EventType.Thing, EventSubType.Recycling, this.target);

            // A purple overlay for the thing being harvested, invisible to begin with
            var g = (target as ITree)?.TreeTopWarpPhase ?? 0f;   // Use green channel for treetop warps

            this.targetBlueprints.Clear();
            foreach (var thing in this.target.MainTile.ThingsPrimary.Where(t => t.ThingType == this.target.ThingType))
            {
                var target = new Blueprint(this.target.ThingType, this.target.MainTile, 0.4f, g, 0.8f) { ColourA = 0f, ThingId = this.target.Id };
                if (thing is IAnimatedThing a) target.AnimationFrame = a.AnimationFrame;
                if (thing is IRotatableThing r) target.Direction = r.Direction;
                if (thing is IRenderOffsettable ro) target.RenderPositionOffset = ro.RenderPositionOffset;
                World.RecycleBlueprints.Add(target.Id, target);
                EventManager.RaiseEvent(EventType.RecycleBlueprint, EventSubType.Added, target);
                this.targetBlueprints.Add(target);
            }

            this.TargetBlueprintId = this.targetBlueprints[0].Id;

            // Create the resource stack, invisible to begin with
            var yield = this.target.GetDeconstructionYield();
            var tile = this.target.AllTiles.FirstOrDefault(t => this.colonist.MainTile.AdjacentTiles8.Contains(t) && t.ThingsAll.All(u => u == this.target));
            if (tile == null) tile = this.target.AllTiles.FirstOrDefault(t => t.ThingsAll.All(u => u == this.target));
            if (tile == null) tile = this.target.AllTiles.FirstOrDefault();
            if (tile != null)
            {
                var itemType = ItemType.None;
                if (yield.ContainsKey(ItemType.Biomass) && yield[ItemType.Biomass] > 0) itemType = ItemType.Biomass;
                else if (yield.ContainsKey(ItemType.IronOre) && yield[ItemType.IronOre] > 0) itemType = ItemType.IronOre;
                else if (yield.ContainsKey(ItemType.Coal) && yield[ItemType.Coal] > 0) itemType = ItemType.Coal;
                else if (yield.ContainsKey(ItemType.Metal) && yield[ItemType.Metal] > 0) itemType = ItemType.Metal;
                else if (yield.ContainsKey(ItemType.Stone) && yield[ItemType.Stone] > 0) itemType = ItemType.Stone;

                if (Constants.ResourceStackTypes.ContainsKey(itemType))
                {
                    var thingType = Constants.ResourceStackTypes[itemType];
                    this.stack = new ResourceStack(thingType, tile, yield[itemType])
                    {
                        RenderAlpha = 0,
                        ShadowAlpha = 0
                    };

                    World.AddThing(this.stack);
                    this.StackId = this.stack.Id;

                    // A purple overlay for new resource stack, invisible to begin with
                    this.stackBlueprint = new Blueprint(this.stack.ThingType, this.stack.MainTile, 0.4f, 0f, 0.8f)
                    {
                        ColourA = 0f,
                        ThingId = this.StackId,
                        AnimationFrame = this.stack.ItemCount
                    };

                    this.StackBlueprintId = this.stackBlueprint.Id;
                    World.RecycleBlueprints.Add(this.stackBlueprint.Id, this.stackBlueprint);
                    EventManager.RaiseEvent(EventType.RecycleBlueprint, EventSubType.Added, this.stackBlueprint);
                }
            }
        }

        public void Update()
        {
            if (!this.target.IsRecycling)
            {
                this.Start();
                return;
            }

            // 1. Fade in overlay for the target to 50%
            // 2. Fade out target, fade in target overlay to 100%, fade in stack overlay to 100%
            // 3. Fade out target overlay, fade out stack overlay, fade in stack

            this.WorkDone += Math.Max(0.1, this.colonist.GetWorkRate());
            if (this.target != null)
            {
                var progress = Math.Min(100, (int)(100 * (this.WorkDone / this.TotalWork)));
                foreach (var thing in this.target.MainTile.ThingsPrimary.Where(t => t.ThingType == this.target.ThingType))
                {
                    this.target.RecycleProgress = progress;
                }
            }

            if (this.WorkDone >= this.TotalWork || this.target == null)
            {
                // Complete.  Remove everything apart from the stack of resources.
                foreach (var b in this.targetBlueprints)
                {
                    b.ColourA = 0;
                    World.RecycleBlueprints.Remove(b.Id);
                    EventManager.RaiseEvent(EventType.RecycleBlueprint, EventSubType.Removed, b);
                }

                this.stackBlueprint.ColourA = 0;
                World.RecycleBlueprints.Remove(this.stackBlueprint.Id);
                EventManager.RaiseEvent(EventType.RecycleBlueprint, EventSubType.Removed, this.stackBlueprint);

                if (this.target != null)
                {
                    foreach (var thing in this.target.MainTile.ThingsPrimary.Where(t => t.ThingType == this.target.ThingType).ToList())
                    {
                        World.RemoveThing(thing);
                    }
                }

                this.stack.IsReady = true;
                this.stack.HaulPriority = this.target?.RecyclePriority ?? WorkPriority.Low;

                this.SetStackAlpha(1f);
            }
            else if (this.WorkDone >= this.TotalWork * 2 / 3)
            {
                // More than 2 3rds.  Fade out target overlay, fade out stack overlay, fade in stack, fade out shadows
                var fraction = ((float)this.WorkDone * 3f / (float)this.TotalWork) - 2f;

                this.SetTargetBlueprintAlpha(1f - fraction);
                this.SetStackBlueprintAlpha(1f - fraction);
                this.SetStackAlpha(fraction);
                this.SetShadowAlpha(1f - fraction);
            }
            else if (this.WorkDone >= this.TotalWork * 1 / 3)
            {
                // More than 1 3rd.  Fade out target, fade in target overlay to 100%, fade in stack overlay to 100%
                var fraction = ((float)this.WorkDone * 3f / (float)this.TotalWork) - 1f;

                this.SetTargetAlpha(1f - fraction);
                this.SetTargetBlueprintAlpha(0.5f + (fraction * 0.5f));
                this.SetStackBlueprintAlpha(fraction);
            }
            else
            {
                // Less than 1 3rd.  Fade in overlay for the target to 50%
                this.SetTargetBlueprintAlpha((float)this.WorkDone * 1.5f / (float)this.TotalWork);
            }
        }

        private void SetStackAlpha(float alpha)
        {
            this.stack.RenderAlpha = alpha;
            this.stack.ShadowAlpha = alpha;
            EventManager.EnqueueWorldPropertyChangeEvent(this.stack.Id, nameof(this.stack.RenderAlpha), this.stack.MainTile.Row, this.stack.ThingType);
        }

        private void SetStackBlueprintAlpha(float alpha)
        {
            this.stackBlueprint.ColourA = alpha;
            EventManager.EnqueueWorldPropertyChangeEvent(this.stackBlueprint.Id, nameof(this.stackBlueprint.ColourA), this.stackBlueprint.MainTile.Row, this.stackBlueprint.ThingType);
        }

        private void SetTargetBlueprintAlpha(float alpha)
        {
            foreach (var b in this.targetBlueprints)
            {
                b.ColourA = alpha;
                EventManager.EnqueueWorldPropertyChangeEvent(b.Id, nameof(b.ColourA), b.MainTile.Row, b.ThingType);
            }
        }

        private void SetTargetAlpha(float alpha)
        {
            foreach (var thing in this.target.MainTile.ThingsPrimary.Where(t => t.ThingType == this.target.ThingType))
            {
                thing.RenderAlpha = alpha;
                EventManager.EnqueueWorldPropertyChangeEvent(thing.Id, nameof(thing.RenderAlpha), thing.MainTile.Row, thing.ThingType);
            }
        }

        private void SetShadowAlpha(float alpha)
        {
            foreach (var thing in this.target.MainTile.ThingsPrimary.Where(t => t.ThingType == this.target.ThingType))
            {
                thing.ShadowAlpha = alpha;
                if (thing is Tree t)
                {
                    // Cause shadow update
                    EventManager.RaiseEvent(EventType.Shadow, EventSubType.Updated, t);
                }
            }
        }
    }
}

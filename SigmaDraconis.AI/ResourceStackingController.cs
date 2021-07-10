namespace SigmaDraconis.AI
{
    using Draconis.Shared;
    using Shared;
    using System.Collections.Generic;
    using System.Linq;
    using World;
    using World.PathFinding;
    using World.Zones;
    using WorldInterfaces;

    public static class ResourceStackingController
    {
        private static bool isInitialised;
        private static HashSet<ItemType> itemTypes = new HashSet<ItemType>();
        private static readonly Dictionary<int, ResourceStackProxy> stackProxies = new Dictionary<int, ResourceStackProxy>();
        private static Dictionary<ItemType, int> siloLevelTargets = new Dictionary<ItemType, int>();
        private static Dictionary<ItemType, int> siloLevelsPredicted = new Dictionary<ItemType, int>();
        private static int siloSpaceActual;
        private static int siloSpacePredicted;
        private static readonly Dictionary<int, ResourceStackingJob> jobsInProgressByColonist = new Dictionary<int, ResourceStackingJob>();
        private static readonly Dictionary<int, int> inaccessibleStacks = new Dictionary<int, int>();

        public static void Clear()
        {
            isInitialised = false;
            stackProxies.Clear();
            siloLevelTargets.Clear();
            jobsInProgressByColonist.Clear();
            JobFinder.Clear();
        }

        private static void Init()
        {
            itemTypes = Constants.ResourceStackTypes.Keys.ToHashSet();
            siloLevelTargets = itemTypes.ToDictionary(i => i, i => 10);
            siloLevelsPredicted = itemTypes.ToDictionary(i => i, i => 0);
            isInitialised = true;
        }

        public static Dictionary<ItemType, int> Serialize()
        {
            return siloLevelTargets;
        }

        public static void Deserialize(Dictionary<ItemType, int> obj)
        {
            if (!isInitialised) Init();
            else Clear();
            siloLevelTargets = obj.ToDictionary(o => o.Key, o => o.Value);
        }

        public static void SetTarget(ItemType itemType, int value)
        {
            if (!isInitialised) Init();
            siloLevelTargets[itemType] = value;
        }

        public static int GetTarget(ItemType itemType)
        {
            if (!isInitialised) Init();
            return siloLevelTargets[itemType];
        }

        public static ResourceStackingJob GetJobForColonist(IColonist colonist)
        {
            if (colonist.WorkPriorities[ColonistPriority.Haul] == 0 && !Constants.ResourceStackTypes.ContainsKey(colonist.CarriedItemTypeBack)) return null;
            if (colonist.CarriedItemTypeBack != ItemType.None && !Constants.ResourceStackTypes.ContainsKey(colonist.CarriedItemTypeBack)) return null;

            if (!isInitialised) Init();

            if (World.ResourceNetwork == null) return null;

            var existingJob = jobsInProgressByColonist.ContainsKey(colonist.Id) ? jobsInProgressByColonist[colonist.Id] : null;
            if (existingJob != null) UpdatePredictionsReverse(existingJob);
            else if (siloLevelsPredicted.ContainsKey(colonist.CarriedItemTypeBack)) siloLevelsPredicted[colonist.CarriedItemTypeBack]--;

            JobFinder.Reset(colonist);

            // Is colonist carrying something already?
            if (colonist.CarriedItemTypeBack != ItemType.None)
            {
                Log($"{colonist.ShortName} is requesting a job.  Xe is already carrying {colonist.CarriedItemTypeBack}");

                // Any stacks need resource?
                for (WorkPriority priority = WorkPriority.Urgent; priority >= WorkPriority.Low; priority--)
                {
                    var candidates = new List<ResourceStackProxy>();
                    foreach (var s in JobFinder.GetStacks(priority).Where(s => s.Type == colonist.CarriedItemTypeBack && s.PredictedItemCount < Constants.ResourceStackMaxSizes[s.Type]))
                    {
                        if ((s.Mode == StackingAreaMode.TargetStackSize && s.PredictedItemCount < s.TargetCount)
                            || (s.Mode == StackingAreaMode.TargetSiloLevel && siloLevelsPredicted[colonist.CarriedItemTypeBack] >= siloLevelTargets[colonist.CarriedItemTypeBack]))
                        {
                            if (s.Stack.CanAssignColonist(colonist.Id, colonist.MainTileIndex))  // Colonist doesn't have to move
                            {
                                if (existingJob == null && siloLevelsPredicted.ContainsKey(colonist.CarriedItemTypeBack)) siloLevelsPredicted[colonist.CarriedItemTypeBack]++;

                                if (existingJob != null && existingJob.Target == s.Stack)
                                {
                                    Log($"Colonist should continue existing {existingJob}");
                                    existingJob.Path = null;
                                    UpdatePredictions(existingJob);
                                    existingJob.Priority = WorkPriority.Urgent;
                                    return existingJob;
                                }

                                var job = new ResourceStackingJob(colonist, s.Stack, colonist.CarriedItemTypeBack, WorkPriority.Urgent, null);
                                Log($"Job offer: {job}.");
                                return job;
                            }
                            else candidates.Add(s);
                        }
                    }

                    if (candidates.Any() && FindPath(colonist.MainTile, candidates.SelectMany(c => c.Stack.GetAccessTiles(colonist.Id)).ToList()) is Path path)
                    {
                        var stack = candidates.FirstOrDefault(c => c.Stack.CanAssignColonist(colonist.Id, World.GetSmallTile(path.EndPosition).Index));
                        if (stack != null)
                        {
                            if (existingJob == null && siloLevelsPredicted.ContainsKey(colonist.CarriedItemTypeBack)) siloLevelsPredicted[colonist.CarriedItemTypeBack]++;

                            if (existingJob != null && existingJob.Target == stack)
                            {
                                Log($"Colonist should continue existing {existingJob}");
                                UpdatePredictions(existingJob);
                                existingJob.Path = path;
                                existingJob.Priority = WorkPriority.Urgent;
                                return existingJob;
                            }

                            var job = new ResourceStackingJob(colonist, stack.Stack, colonist.CarriedItemTypeBack, WorkPriority.Urgent, path);
                            Log($"Job offer: {job}.");
                            return job;
                        }
                    }
                }

                // Take resource to network?
                if (siloSpacePredicted > 0 && (siloSpacePredicted > 1 || !(existingJob?.Target is IResourceStack)) && siloSpaceActual > 0)
                {
                    foreach (var rp in JobFinder.GetResourceProcessors())
                    {
                        if (rp.CanAssignColonist(colonist.Id, colonist.MainTileIndex))  // Colonist doesn't have to move
                        {
                            if (existingJob == null && siloLevelsPredicted.ContainsKey(colonist.CarriedItemTypeBack)) siloLevelsPredicted[colonist.CarriedItemTypeBack]++;

                            if (existingJob != null && existingJob.Target == rp)
                            {
                                Log($"Colonist should continue existing {existingJob}");
                                existingJob.Path = null;
                                UpdatePredictions(existingJob);
                                existingJob.Priority = WorkPriority.Urgent;
                                return existingJob;
                            }

                            var job = new ResourceStackingJob(colonist, rp, colonist.CarriedItemTypeBack, WorkPriority.Urgent, null);
                            Log($"Job offer: {job}.");
                            return job;
                        }

                        var path = FindPath(colonist.MainTile, rp.GetAccessTiles(colonist.Id).ToList());
                        {
                            if (existingJob == null && siloLevelsPredicted.ContainsKey(colonist.CarriedItemTypeBack)) siloLevelsPredicted[colonist.CarriedItemTypeBack]++;

                            if (existingJob != null && existingJob.Target == rp)
                            {
                                Log($"Colonist should continue existing {existingJob}");
                                existingJob.Path = path;
                                UpdatePredictions(existingJob);
                                existingJob.Priority = WorkPriority.Urgent;
                                return existingJob;
                            }

                            var job = new ResourceStackingJob(colonist, rp, colonist.CarriedItemTypeBack, WorkPriority.Urgent, path);
                            Log($"Job offer: {job}.");
                            return job;
                        }
                    }
                }

                // Overflow stacks?
                for (WorkPriority priority = WorkPriority.Urgent; priority >= WorkPriority.Low; priority--)
                {
                    var candidates = JobFinder.GetStacks(priority).Where(s => s.Type == colonist.CarriedItemTypeBack && s.Mode == StackingAreaMode.OverflowOnly && s.PredictedItemCount < Constants.ResourceStackMaxSizes[s.Type]).ToList();
                    var adjacent = candidates.FirstOrDefault(c => c.Stack.CanAssignColonist(colonist.Id, colonist.MainTileIndex));
                    if (adjacent != null)
                    {
                        if (existingJob == null && siloLevelsPredicted.ContainsKey(colonist.CarriedItemTypeBack)) siloLevelsPredicted[colonist.CarriedItemTypeBack]++;

                        if (existingJob != null && existingJob.Target == adjacent)
                        {
                            Log($"Colonist should continue existing {existingJob}");
                            UpdatePredictions(existingJob);
                            existingJob.Priority = WorkPriority.Urgent;
                            return existingJob;
                        }

                        var job = new ResourceStackingJob(colonist, adjacent.Stack, colonist.CarriedItemTypeBack, WorkPriority.Urgent, null);
                        Log($"Job offer: {job}.");
                        return job;
                    }

                    if (candidates.Any() && FindPath(colonist.MainTile, candidates.SelectMany(c => c.Stack.GetAccessTiles(colonist.Id)).ToList()) is Path path)
                    {
                        var stack = candidates.FirstOrDefault(c => c.Stack.CanAssignColonist(colonist.Id, World.GetSmallTile(path.EndPosition).Index));
                        if (stack != null)
                        {
                            if (existingJob == null && siloLevelsPredicted.ContainsKey(colonist.CarriedItemTypeBack)) siloLevelsPredicted[colonist.CarriedItemTypeBack]++;

                            if (existingJob != null && existingJob.Target == stack)
                            {
                                Log($"Colonist should continue existing {existingJob}");
                                UpdatePredictions(existingJob);
                                existingJob.Priority = WorkPriority.Urgent;
                                return existingJob;
                            }

                            var job = new ResourceStackingJob(colonist, stack.Stack, colonist.CarriedItemTypeBack, WorkPriority.Urgent, path);
                            Log($"Job offer: {job}.");
                            return job;
                        }
                    }
                }
            }
            else
            {
                Log($"{colonist.ShortName} is requesting a job.");

                for (WorkPriority priority = WorkPriority.Urgent; priority >= WorkPriority.Low; priority--)
                {
                    // First check if something needs picking up and taking somewhere else.  Order by closest first.
                    // Adjacent stacks promoted to urgent, unless we already have a job in which case only that one is urgent if it's adjacent.
                    var enumerable = !(existingJob?.Target is IResourceStack existingJobStack)
                        ? (priority == WorkPriority.Urgent ? JobFinder.GetAdjacentStacks().Concat(JobFinder.GetStacks(priority, true)) : JobFinder.GetStacks(priority, true))
                        : (priority == WorkPriority.Urgent ? JobFinder.GetAdjacentStacks(existingJobStack).Concat(JobFinder.GetStacks(priority, existingJobStack)) : JobFinder.GetStacks(priority, existingJobStack));
                    var ignoredStacks = inaccessibleStacks.Keys.ToHashSet();
                    foreach (var stack in enumerable.Where(s => !ignoredStacks.Contains(s.Stack.Id)))
                    {
                        var isAdjacent = stack.Stack.CanAssignColonist(colonist.Id, colonist.MainTileIndex);

                        if (stack.PredictedItemCount == 0 || stack.Mode == StackingAreaMode.OverflowOnly) continue;
                        if (!(stack.Mode == StackingAreaMode.RemoveStack || stack.Mode == StackingAreaMode.OverflowOnly
                            || (stack.Mode == StackingAreaMode.TargetStackSize && stack.PredictedItemCount > stack.TargetCount)
                            || (stack.Mode == StackingAreaMode.TargetSiloLevel && siloLevelsPredicted[stack.Type] < siloLevelTargets[stack.Type]))) continue;

                        var target = GetTargetForPickupFromStack(stack, colonist, stack.Mode == StackingAreaMode.TargetSiloLevel);
                        if (target != null)
                        {
                            var path = isAdjacent ? null : FindPath(colonist.MainTile, stack.Stack.GetAccessTiles(colonist.Id).ToList());
                            if (isAdjacent || path != null)
                            {
                                if (existingJob != null && existingJob.Target == target && existingJob.Source == stack.Stack)
                                {
                                    Log($"Colonist should continue existing {existingJob}");
                                    existingJob.Path = path;
                                    UpdatePredictions(existingJob);
                                    return existingJob;
                                }

                                var job = new ResourceStackingJob(stack.Stack, target, stack.Stack.ItemType, priority, path);
                                Log($"Job offer: {job}.");
                                return job;
                            }
                            else
                            {
                                // Optimisation: Don't keep trying to get to the same inaccessible stack
                                inaccessibleStacks.Add(stack.Stack.Id, 23);
                            }
                        }
                    }

                    // Now check if any stacks are requesting items from the network
                    IResourceProcessor nearestProcessor = null;
                    Path pathToProcessor = null;
                    foreach (var processor in JobFinder.GetResourceProcessors())
                    {
                        var isAdjacent = processor.CanAssignColonist(colonist.Id, colonist.MainTileIndex);
                        var path = isAdjacent ? null : FindPath(colonist.MainTile, processor.GetAccessTiles(colonist.Id).ToList());
                        if (!isAdjacent && path == null) continue;

                        nearestProcessor = processor;
                        pathToProcessor = path;
                        break;
                    }

                    if (nearestProcessor != null)
                    {
                        foreach (var s in JobFinder.GetStacks(priority).Where(s => s.PredictedItemCount < Constants.ResourceStackMaxSizes[s.Type] && s.Mode != StackingAreaMode.OverflowOnly && s.Mode != StackingAreaMode.RemoveStack))
                        {
                            if ((s.Mode == StackingAreaMode.TargetStackSize && s.PredictedItemCount < s.TargetCount && siloLevelsPredicted[s.Type] > 0)
                                || (s.Mode == StackingAreaMode.TargetSiloLevel && siloLevelsPredicted[s.Type] > siloLevelTargets[s.Type]))
                            {
                                if (existingJob != null && existingJob.Target == s.Stack && existingJob.Source == nearestProcessor)
                                {
                                    Log($"Colonist should continue existing {existingJob}");
                                    existingJob.Path = pathToProcessor;
                                    UpdatePredictions(existingJob);
                                    return existingJob;
                                }

                                var job = new ResourceStackingJob(nearestProcessor, s.Stack, s.Type, priority, pathToProcessor);
                                Log($"Job offer: {job}.");
                                return job;
                            }
                        }
                    }

                    // If network is full and we have at least one overflow stack, then go through each IResourceProviderBuilding.  If they are waiting to distribute then we can take their output.
                    if (nearestProcessor != null && siloSpaceActual <= 0 && siloSpacePredicted <= 0)
                    {
                        foreach (var provider in World.ResourceNetwork.GetResourceProviders().Where(p => p.FactoryStatus == FactoryStatus.WaitingToDistribute))
                        {
                            foreach (var s in JobFinder.GetStacks(priority).Where(s => s.Type == provider.OutputItemType && s.PredictedItemCount < Constants.ResourceStackMaxSizes[s.Type] && s.Mode != StackingAreaMode.RemoveStack))
                            {
                                var isAdjacent = s.Stack.CanAssignColonist(colonist.Id, colonist.MainTileIndex);
                                var path = isAdjacent ? null : FindPath(colonist.MainTile, s.Stack.GetAccessTiles(colonist.Id).ToList());
                                if (isAdjacent || path != null)   // Finding path proves we can access this stack
                                {
                                    if (existingJob != null && existingJob.Target == s.Stack && existingJob.Source == nearestProcessor)
                                    {
                                        Log($"Colonist should continue existing {existingJob}");
                                        existingJob.Path = pathToProcessor;
                                        UpdatePredictions(existingJob);
                                        return existingJob;
                                    }

                                    var job = new ResourceStackingJob(nearestProcessor, s.Stack, s.Type, priority, pathToProcessor);
                                    Log($"Job offer: {job}.");
                                    return job;
                                }
                            }
                        }

                        // Take anything if silos full but nothing is waiting to distribute
                        foreach (var s in JobFinder.GetStacks(priority).Where(s => s.PredictedItemCount < Constants.ResourceStackMaxSizes[s.Type] && s.Mode != StackingAreaMode.RemoveStack && World.ResourceNetwork.CanTakeItems(nearestProcessor, s.Type, 1)))
                        {
                            var isAdjacent = s.Stack.CanAssignColonist(colonist.Id, colonist.MainTileIndex);
                            var path = isAdjacent ? null : FindPath(colonist.MainTile, s.Stack.GetAccessTiles(colonist.Id).ToList());
                            if (isAdjacent || path != null)   // Finding path proves we can access this stack
                            {
                                if (existingJob != null && existingJob.Target == s.Stack && existingJob.Source == nearestProcessor)
                                {
                                    Log($"Colonist should continue existing {existingJob}");
                                    existingJob.Path = pathToProcessor;
                                    UpdatePredictions(existingJob);
                                    return existingJob;
                                }

                                var job = new ResourceStackingJob(nearestProcessor, s.Stack, s.Type, priority, pathToProcessor);
                                Log($"Job offer: {job}.");
                                return job;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static IColonistInteractive GetTargetForPickupFromStack(ResourceStackProxy source, IColonist colonist, bool networkOnly)
        {
            // Any other stacks need resource?
            if (!networkOnly)
            {
                for (WorkPriority priority = WorkPriority.Urgent; priority >= WorkPriority.Low; priority--)
                {
                    var candidates = new List<ResourceStackProxy>();
                    foreach (var s in JobFinder.GetStacks(priority).Where(s => s != source && s.Type == source.Type && s.PredictedItemCount < Constants.ResourceStackMaxSizes[s.Type]))
                    {
                        if (s.Mode == StackingAreaMode.TargetStackSize && s.PredictedItemCount < s.TargetCount) candidates.Add(s);
                        else if (s.Mode == StackingAreaMode.TargetSiloLevel && siloLevelsPredicted[source.Type] >= siloLevelTargets[source.Type]) candidates.Add(s);
                    }

                    if (candidates.Any() && FindPath(colonist.MainTile, candidates.SelectMany(c => c.Stack.GetAccessTiles(colonist.Id)).ToList()) is Path path)
                    {
                        var stack = candidates.FirstOrDefault(c => c.Stack.CanAssignColonist(colonist.Id, World.GetSmallTile(path.EndPosition).Index));
                        if (stack != null) return stack.Stack;
                    }
                }
            }

            // Take resource to network?
            if (siloSpacePredicted > 0 && siloSpaceActual > 0 && (source.Mode != StackingAreaMode.TargetSiloLevel || siloLevelsPredicted[source.Type] < siloLevelTargets[source.Type]))
            {
                foreach (var processor in JobFinder.GetResourceProcessors())
                {
                    var isAdjacent = processor.CanAssignColonist(colonist.Id, colonist.MainTileIndex);
                    var path = isAdjacent ? null : FindPath(colonist.MainTile, processor.GetAccessTiles(colonist.Id).ToList());
                    if (!isAdjacent && path == null) continue;

                    return processor;
                }
            }

            // Overflow stacks?
            if (!networkOnly)
            {
                for (WorkPriority priority = WorkPriority.Urgent; priority >= WorkPriority.Low; priority--)
                {
                    var candidates = JobFinder.GetStacks(priority).Where(s => s.Type == colonist.CarriedItemTypeBack && s.Mode == StackingAreaMode.OverflowOnly && s.PredictedItemCount < Constants.ResourceStackMaxSizes[s.Type]).ToList();
                    if (candidates.Any() && FindPath(colonist.MainTile, candidates.SelectMany(c => c.Stack.GetAccessTiles(colonist.Id)).ToList()) is Path path)
                    {
                        var stack = candidates.FirstOrDefault(c => c.Stack.CanAssignColonist(colonist.Id, World.GetSmallTile(path.EndPosition).Index));
                        if (stack != null) return stack.Stack;
                    }
                }
            }

            return null;
        }

        public static void Update()
        {
            if (!isInitialised) Init();
            if (World.ResourceNetwork == null) return;

            foreach (var key in inaccessibleStacks.Keys.ToList())
            {
                if (inaccessibleStacks[key] == 1) inaccessibleStacks.Remove(key);
                else inaccessibleStacks[key]--;
            }

            JobFinder.Update();

            // Update predicted silo and stack levels
            siloSpaceActual = World.ResourceNetwork.ResourcesCapacity - World.ResourceNetwork.CountResources;
            siloSpacePredicted = siloSpaceActual;
            foreach (var stack in stackProxies.Values) stack.PredictedItemCount = stack.Count;
            var colonists = World.GetThings<IColonist>(ThingType.Colonist).Where(c => !c.IsDead).ToList();
            foreach (var itemType in siloLevelsPredicted.Keys.ToList())
            {
                siloLevelsPredicted[itemType] = World.ResourceNetwork.GetItemTotal(itemType);
            }

            foreach (var rp in World.GetThings<IResourceProcessor>(ThingType.ResourceProcessor).Where(p => p.FactoryStatus == FactoryStatus.InProgress))
            {
                if (siloLevelsPredicted.ContainsKey(rp.InputItemType)) siloLevelsPredicted[rp.InputItemType]++;
            }

            foreach (var colonist in colonists)
            {
                var job = jobsInProgressByColonist.ContainsKey(colonist.Id) ? jobsInProgressByColonist[colonist.Id] : null;
                if (job != null && colonist.ActivityType != ColonistActivityType.HaulDropoff && colonist.ActivityType != ColonistActivityType.HaulPickup)
                {
                    Log($"Job abandoned: {job}.");
                    jobsInProgressByColonist.Remove(colonist.Id);
                }
                else if (job != null) UpdatePredictions(job);
            }
        }

        public static void ClaimJob(IColonist colonist, ResourceStackingJob job)
        {
            Log($"Job accepted: {job}.");
            if (jobsInProgressByColonist.ContainsKey(colonist.Id))
            {
                UpdatePredictionsReverse(jobsInProgressByColonist[colonist.Id]);
                jobsInProgressByColonist[colonist.Id] = job;
            }
            else
            {
                if (siloLevelsPredicted.ContainsKey(colonist.CarriedItemTypeBack))
                {
                    siloLevelsPredicted[colonist.CarriedItemTypeBack]--;
                    siloSpacePredicted++;
                }

                jobsInProgressByColonist.Add(colonist.Id, job);
            }

            UpdatePredictions(job);
        }

        public static void JobInTransit(IColonist colonist)
        {
            if (jobsInProgressByColonist.ContainsKey(colonist.Id))
            {
                var job = jobsInProgressByColonist[colonist.Id];
                job.IsInTransit = true;
                UpdatePredictions(job);
            }
        }

        public static void JobCompleted(IColonist colonist)
        {
            var job = jobsInProgressByColonist.ContainsKey(colonist.Id) ? jobsInProgressByColonist[colonist.Id] : null;
            if (job == null) return;

            Log($"Job completed: {job}.");
            jobsInProgressByColonist.Remove(colonist.Id);
        }

        private static void UpdatePredictions(ResourceStackingJob job)
        {
            if (job.Target is IResourceProcessor)
            {
                siloLevelsPredicted[job.ItemType]++;
                siloSpacePredicted--;
            }
            else if (job.Target is IResourceStack && stackProxies.ContainsKey(job.Target.Id))
            {
                stackProxies[job.Target.Id].PredictedItemCount++;
            }

            if (job.IsInTransit) return;

            if (job.Source is IResourceProcessor rp && rp.FactoryStatus != FactoryStatus.InProgressReverse)
            {
                siloLevelsPredicted[job.ItemType]--;
                siloSpacePredicted++;
            }
            else if (job.Source is IResourceStack && stackProxies.ContainsKey(job.Source.Id))
            {
                stackProxies[job.Source.Id].PredictedItemCount--;
            }
        }

        private static void UpdatePredictionsReverse(ResourceStackingJob job)
        {
            if (job.Target is IResourceProcessor)
            {
                siloLevelsPredicted[job.ItemType]--;
                siloSpacePredicted++;
            }
            else if (job.Target is IResourceStack && stackProxies.ContainsKey(job.Target.Id))
            {
                stackProxies[job.Target.Id].PredictedItemCount--;
            }

            if (job.IsInTransit) return;

            if (job.Source is IResourceProcessor rp && rp.FactoryStatus != FactoryStatus.InProgressReverse)
            {
                siloLevelsPredicted[job.ItemType]++;
                siloSpacePredicted--;
            }
            else if (job.Source is IResourceStack && stackProxies.ContainsKey(job.Source.Id))
            {
                stackProxies[job.Source.Id].PredictedItemCount++;
            }
        }

        private static Path FindPath(ISmallTile startTile, List<ISmallTile> candidateTiles)
        {
            if (!candidateTiles.Any()) return null;

            ISmallTile targetTile = null;
            if (candidateTiles.Count > 1 && ZoneManager.GlobalZone.Nodes.ContainsKey(startTile.Index))
            {
                // Expanding circle method faster if there are many possible targets
                var openNodes = new HashSet<int> { startTile.Index };
                var closedNodes = new HashSet<int> { startTile.Index };
                while (openNodes.Any() && targetTile == null)
                {
                    var list = openNodes.ToList();
                    openNodes.Clear();
                    foreach (var n in list)
                    {
                        var o = ZoneManager.GlobalZone.Nodes[n];
                        foreach (var l in o.AllLinks)
                        {
                            if (!closedNodes.Contains(l.Index))
                            {
                                if (candidateTiles.FirstOrDefault(t => t.Index == l.Index) is ISmallTile tile)
                                {
                                    targetTile = tile;
                                    break;
                                }

                                openNodes.Add(l.Index);
                                closedNodes.Add(l.Index);
                            }
                        }

                        if (targetTile != null) break;
                    }
                }
            }
            else if (candidateTiles.Count == 1) targetTile = candidateTiles[0];

            if (targetTile != null)
            {
                // If we are standing next to a stationary colonist or rover, find a way around it.
                var path = PathFinder.FindPath(startTile.Index, targetTile.Index, ZoneManager.GlobalZone.Nodes);
                if (path?.RemainingNodes?.Any() == true) return path;
            }

            return null;
        }

        private static void Log(string message)
        {
            //if (Constants.IsResourceStackLoggingEnabled) Logger.Instance.Log(World.WorldTime, "ResourceStackingController", message);
        }
    }
}

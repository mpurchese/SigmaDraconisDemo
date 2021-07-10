namespace SigmaDraconis.AI
{
    using Draconis.Shared;
    using Shared;
    using System.Collections.Generic;
    using System.Linq;
    using World;
    using World.Zones;
    using WorldInterfaces;

    internal static class JobFinder
    {
        private static bool isInitialised;
        private static HashSet<ItemType> itemTypes = new HashSet<ItemType>();
        private static HashSet<ThingType> stackTypes = new HashSet<ThingType>();
        private static readonly HashSet<WorkPriority> priorities = new HashSet<WorkPriority>() { WorkPriority.Low, WorkPriority.Normal, WorkPriority.High, WorkPriority.Urgent };
        private static readonly Dictionary<int, ResourceStackProxy> stackProxies = new Dictionary<int, ResourceStackProxy>();

        // Optimisation - get potential targets by distance first
        private static readonly List<IResourceProcessor> resourceProcessorsByDistance = new List<IResourceProcessor>();
        private static readonly List<ResourceStackProxy> stackingAreasAdjacent = new List<ResourceStackProxy>();
        private static readonly Dictionary<WorkPriority, List<ResourceStackProxy>> stackingAreasByPriorityAndDistance = new Dictionary<WorkPriority, List<ResourceStackProxy>>();

        private static bool updatePending = true;
        private static IColonist currentColonist;
        private static bool foundAdjacent = false;
        private static int resourceProcessorsNotFound = 0;
        private static readonly HashSet<IColonistInteractive> targetsToFind = new HashSet<IColonistInteractive>();
        private static Dictionary<WorkPriority, int> resourceStacksNotFound = new Dictionary<WorkPriority, int>();
        private static IEnumerator<IThing> targetEnumerator;
        private static bool foundAll = false;

        private static void Init()
        {
            itemTypes = Constants.ResourceStackTypes.Keys.ToHashSet();
            stackTypes = Constants.ResourceStackTypes.Values.ToHashSet();
            stackingAreasByPriorityAndDistance.Add(WorkPriority.Low, new List<ResourceStackProxy>());
            stackingAreasByPriorityAndDistance.Add(WorkPriority.Normal, new List<ResourceStackProxy>());
            stackingAreasByPriorityAndDistance.Add(WorkPriority.High, new List<ResourceStackProxy>());
            stackingAreasByPriorityAndDistance.Add(WorkPriority.Urgent, new List<ResourceStackProxy>());
            isInitialised = true;
        }

        public static void Clear()
        {
            // Reset() will do the rest
            stackProxies.Clear();
        }

        public static void Reset(IColonist colonist)
        {
            if (!isInitialised) Init();
            if (updatePending) DoUpdate();
            currentColonist = colonist;
            foundAll = false;
            foundAdjacent = false;
            resourceProcessorsNotFound = 0;
            targetEnumerator = null;
            resourceStacksNotFound = priorities.ToDictionary(p => p, p => 0);
            resourceProcessorsByDistance.Clear();
            stackingAreasAdjacent.Clear();
            stackingAreasByPriorityAndDistance[WorkPriority.Low].Clear();
            stackingAreasByPriorityAndDistance[WorkPriority.Normal].Clear();
            stackingAreasByPriorityAndDistance[WorkPriority.High].Clear();
            stackingAreasByPriorityAndDistance[WorkPriority.Urgent].Clear();
            targetsToFind.Clear();

            if (!ZoneManager.GlobalZone.Nodes.ContainsKey(currentColonist.MainTileIndex)) return;

            var typesToFind = GetTypesToFind(currentColonist);
            if (!typesToFind.Any()) return;

            // Catalogue targets we need to find
            foreach (var candidate in World.GetThings<IColonistInteractive>(typesToFind.ToArray()))
            {
                if (candidate is IResourceProcessor r)
                {
                    if (!r.IsSwitchedOn || !r.IsReady) continue;
                    resourceProcessorsNotFound++;
                    targetsToFind.Add(candidate);
                }
                else if (candidate is IResourceStack s && s.HaulPriority != WorkPriority.Disabled)
                {
                    resourceStacksNotFound[s.HaulPriority]++;
                    targetsToFind.Add(candidate);
                }
            }
        }

        // If an existing job stack is specified, then all others will be ignored 
        public static IEnumerable<ResourceStackProxy> GetAdjacentStacks(IResourceStack existingJobStack = null)
        {
            if (existingJobStack == null)
            {
                foreach (var stack in stackingAreasAdjacent.ToList()) yield return stack;
            }
            else if(stackingAreasAdjacent.FirstOrDefault(a => a.Stack?.Id == existingJobStack.Id) is ResourceStackProxy existing)
            {
                yield return existing;
                yield break;
            }

            if (targetEnumerator == null) targetEnumerator = TargetFinder().GetEnumerator();
            while (!foundAdjacent)
            {
                foundAll |= !targetEnumerator.MoveNext();
                if (foundAll) yield break;

                var next = targetEnumerator.Current;
                if (next is IResourceStack rs && stackProxies.ContainsKey(rs.Id))
                {
                    var proxy = stackProxies[rs.Id];
                    stackingAreasByPriorityAndDistance[proxy.Priority].Add(proxy);
                    if (!foundAdjacent)
                    {
                        stackingAreasAdjacent.Add(proxy);
                        if (existingJobStack == null) yield return proxy;
                        else if (existingJobStack.Id == proxy.Stack?.Id)
                        {
                            yield return proxy;
                            yield break;
                        }
                    }
                }
                else if (next is IResourceProcessor rp)
                {
                    resourceProcessorsByDistance.Add(rp);
                }
            };
        }

        public static IEnumerable<ResourceStackProxy> GetStacks(WorkPriority priority, bool excludeAdjacent = false)
        {
            foreach (var stack in stackingAreasByPriorityAndDistance[priority].ToList())
            {
                if (!excludeAdjacent || !stackingAreasAdjacent.Contains(stack)) yield return stack;
            }

            if (targetEnumerator == null) targetEnumerator = TargetFinder().GetEnumerator();
            while (resourceStacksNotFound[priority] > 0) 
            {
                foundAll |= !targetEnumerator.MoveNext();
                if (foundAll) yield break;

                var next = targetEnumerator.Current;
                if (next is IResourceStack rs && stackProxies.ContainsKey(rs.Id))
                {
                    var proxy = stackProxies[rs.Id];
                    stackingAreasByPriorityAndDistance[proxy.Priority].Add(proxy);
                    if (!foundAdjacent) stackingAreasAdjacent.Add(proxy);
                    if (proxy.Priority == priority && (!excludeAdjacent || foundAdjacent)) yield return proxy;
                }
                else if (next is IResourceProcessor rp)
                {
                    resourceProcessorsByDistance.Add(rp);
                }
            };
        }

        public static IEnumerable<ResourceStackProxy> GetStacks(WorkPriority priority, IResourceStack stackToIgnore)
        {
            foreach (var stack in stackingAreasByPriorityAndDistance[priority].ToList())
            {
                if (stackToIgnore?.Id != stack.Stack.Id) yield return stack;
            }

            if (targetEnumerator == null) targetEnumerator = TargetFinder().GetEnumerator();
            while (resourceStacksNotFound[priority] > 0)
            {
                foundAll |= !targetEnumerator.MoveNext();
                if (foundAll) yield break;

                var next = targetEnumerator.Current;
                if (next is IResourceStack rs && stackProxies.ContainsKey(rs.Id))
                {
                    var proxy = stackProxies[rs.Id];
                    stackingAreasByPriorityAndDistance[proxy.Priority].Add(proxy);
                    if (!foundAdjacent) stackingAreasAdjacent.Add(proxy);
                    if (proxy.Priority == priority && (stackToIgnore?.Id != proxy.Stack.Id)) yield return proxy;
                }
                else if (next is IResourceProcessor rp)
                {
                    resourceProcessorsByDistance.Add(rp);
                }
            };
        }

        public static IEnumerable<IResourceProcessor> GetResourceProcessors()
        {
            foreach (var rp in resourceProcessorsByDistance.ToList()) yield return rp;

            if (targetEnumerator == null) targetEnumerator = TargetFinder().GetEnumerator();
            while (resourceProcessorsNotFound > 0)
            {
                foundAll |= !targetEnumerator.MoveNext();
                if (foundAll) yield break;

                var next = targetEnumerator.Current;
                if (next is IResourceStack rs && stackProxies.ContainsKey(rs.Id))
                {
                    var proxy = stackProxies[rs.Id];
                    stackingAreasByPriorityAndDistance[WorkPriority.Low].Add(proxy);
                    if (!foundAdjacent) stackingAreasAdjacent.Add(proxy);
                }
                else if (next is IResourceProcessor rp)
                {
                    resourceProcessorsByDistance.Add(rp);
                    yield return rp;
                }
            };
        }

        public static void Update()
        {
            updatePending = true;
        }

        private static void DoUpdate()
        {
            if (!isInitialised) Init();

            var allStacks = World.GetThings<IResourceStack>(stackTypes.ToArray()).Where(s => s.IsReady && s.HaulPriority != WorkPriority.Disabled).ToList();
            var ids = new HashSet<int>();
            foreach (var stack in allStacks)
            {
                ids.Add(stack.Id);
                if (!stackProxies.ContainsKey(stack.Id)) stackProxies.Add(stack.Id, new ResourceStackProxy(stack));
                else stackProxies[stack.Id].Update(stack);
            }

            if (stackProxies.Count > allStacks.Count)
            {
                foreach (var id in stackProxies.Keys.ToList())
                {
                    if (!ids.Contains(id)) stackProxies.Remove(id);
                }
            }

            updatePending = false;
        }

        private static IEnumerable<IThing> TargetFinder()
        {
            if (!targetsToFind.Any()) yield break;

            // Build list of tiles to look out for
            var tilesWithTargets = new Dictionary<ISmallTile, List<IColonistInteractive>>();
            foreach (var target in targetsToFind)
            {
                foreach (var tile in target.GetAccessTiles(currentColonist.Id))
                {
                    if (tilesWithTargets.ContainsKey(tile)) tilesWithTargets[tile].Add(target);
                    else tilesWithTargets.Add(tile, new List<IColonistInteractive> { target });
                }
            }

            // Check colonist current position
            if (tilesWithTargets.ContainsKey(currentColonist.MainTile))
            {
                foreach (var thing in tilesWithTargets[currentColonist.MainTile])
                {
                    if (!targetsToFind.Contains(thing)) continue;
                    targetsToFind.Remove(thing);
                    if (thing is IResourceProcessor) resourceProcessorsNotFound--;
                    else if (thing is IResourceStack rs) resourceStacksNotFound[rs.HaulPriority]--;

                    yield return thing;
                    if (!targetsToFind.Any()) break;
                }
            }

            foundAdjacent = true;
            if (!targetsToFind.Any()) yield break;

            // Expanding circle up to 40 tiles
            var openNodes = new HashSet<int> { currentColonist.MainTileIndex };
            var closedNodes = new HashSet<int> { currentColonist.MainTileIndex };
            var distance = 0;
            while (openNodes.Any() && tilesWithTargets.Any() && distance <= 40)
            {
                distance++;
                var list = openNodes.ToList();
                openNodes.Clear();
                foreach (var n in list)
                {
                    var o = ZoneManager.GlobalZone.Nodes[n];
                    foreach (var l in o.AllLinks)
                    {
                        if (!closedNodes.Contains(l.Index))
                        {
                            var tile = World.GetSmallTile(l.Index);
                            if (tilesWithTargets.ContainsKey(tile))
                            {
                                foreach (var thing in tilesWithTargets[tile])
                                {
                                    if (!targetsToFind.Contains(thing)) continue;
                                    targetsToFind.Remove(thing);
                                    if (thing is IResourceProcessor) resourceProcessorsNotFound--;
                                    else if (thing is IResourceStack rs) resourceStacksNotFound[rs.HaulPriority]--;

                                    yield return thing;
                                    if (!targetsToFind.Any()) yield break;
                                }
                            }

                            openNodes.Add(l.Index);
                            closedNodes.Add(l.Index);
                        }
                    }
                }
            }

            if (!targetsToFind.Any()) yield break;

            // Order the rest by crow-flies distance
            var colonistX = currentColonist.MainTile.X;
            var colonistY = currentColonist.MainTile.Y;
            foreach (var thing in targetsToFind.OrderBy(t => ((t.MainTile.X - colonistX) * (t.MainTile.X - colonistX)) + ((t.MainTile.Y - colonistY) * (t.MainTile.Y - colonistY))))
            {
                if (thing is IResourceProcessor) resourceProcessorsNotFound--;
                else if (thing is IResourceStack rs) resourceStacksNotFound[rs.HaulPriority]--;
                yield return thing;
            }
        }

        private static List<ThingType> GetTypesToFind(IColonist colonist)
        {
            var typesToFind = new List<ThingType>();
            if (colonist.WorkPriorities[ColonistPriority.Haul] > 0)
            {
                typesToFind.Add(ThingType.ResourceProcessor);
                typesToFind.AddRange(stackTypes);
            }
            else if (itemTypes.Contains(colonist.CarriedItemTypeBack))
            {
                typesToFind.Add(ThingType.ResourceProcessor);
                typesToFind.Add(Constants.ResourceStackTypes[colonist.CarriedItemTypeBack]);
            }

            return typesToFind;
        }
    }
}

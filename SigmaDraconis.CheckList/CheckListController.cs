namespace SigmaDraconis.CheckList
{
    using Draconis.UI;
    using Context;
    using Language;
    using Operators;
    using Requirements;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using World;

    public static class CheckListController
    {
        private static readonly List<ItemDefinition> definitions = new List<ItemDefinition>();
        private static readonly Dictionary<int, Item> items = new Dictionary<int, Item>();
        private static readonly ConcurrentQueue<Item> itemsForDisplay = new ConcurrentQueue<Item>();
        private static bool isRunning = false;
        private static long lastUpdateFrame = 0;
        private static CancellationTokenSource cancellationTokenSource;

        public static int CurrentLanguageId { get; private set; }
        public static bool HaveItemsForDisplay => itemsForDisplay.Any();
        public static int NewItemCount => itemsForDisplay.Count(i => !i.IsRead && !i.IsComplete);
        public static int ActiveItemId { get; set; }
        public static bool IsReset { get; set; }

        public static List<Item> GetItemsForDisplay(bool incompleteOnly, out bool languageChanged)
        {
            languageChanged = CurrentLanguageId != UIStatics.CurrentLanguageId;
            if (languageChanged) LoadText();

            return incompleteOnly 
                ? itemsForDisplay.Where(i => !i.IsComplete).ToList()
                : itemsForDisplay.ToList();
        }

        public static List<Item> GetAllItems(out bool languageChanged)
        {
            languageChanged = CurrentLanguageId != UIStatics.CurrentLanguageId;
            if (languageChanged) LoadText();

            return items.Values.ToList();
        }

        public static Item GetItem(int id)
        {
            return items.ContainsKey(id) ? items[id] : null;
        }

        public static List<int> GetCompletedItemIds()
        {
            return itemsForDisplay.Where(i => i.IsComplete).Select(i => i.Id).ToList();
        }

        public static CheckListSerializationObject Serialize()
        {
            while (isRunning) Thread.Sleep(20);

            return new CheckListSerializationObject
            {
                LastUpdateFrame = lastUpdateFrame,
                ItemsStarted = items.Where(i => i.Value.IsStarted).Select(i => i.Key).ToList(),
                ItemsCompleted = items.Where(i => i.Value.IsComplete).Select(i => i.Key).ToList(),
                ItemsRead = items.Where(i => i.Value.IsRead).Select(i => i.Key).ToList(),
                ActiveItemId = ActiveItemId
            };
        }

        public static void Deserialize(CheckListSerializationObject obj)
        {
            Reset();
            while (isRunning) Thread.Sleep(50);

            lastUpdateFrame = obj.LastUpdateFrame;
            ActiveItemId = obj.ActiveItemId;
            foreach (var kv in items)
            {
                kv.Value.IsStarted = obj.ItemsStarted?.Contains(kv.Key) == true;
                kv.Value.IsComplete = obj.ItemsCompleted?.Contains(kv.Key) == true;
                kv.Value.IsRead = obj.ItemsRead?.Contains(kv.Key) == true;
                if (kv.Value.IsStarted) itemsForDisplay.Enqueue(kv.Value);
            }
        }

        public static void Load()
        {
            LoadDefinitions();
            LoadText();
        }

        private static void LoadDefinitions()
        {
            definitions.Clear();
            items.Clear();
            var path = Path.Combine("Config", "CheckList", "CheckListDefs.txt");
            using (var sr = File.OpenText(path))
            {
                var lineNumber = 0;
                ItemDefinition def = null;
                Item item = null;
                while (!sr.EndOfStream)
                {
                    lineNumber++;
                    try
                    {
                        var line = sr.ReadLine();
                        if (line.Contains('#')) line = line.Substring(0, line.IndexOf('#'));
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var fields = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                        if (fields.Length >= 2 && fields[0] == "ITEM")
                        {
                            if (def != null) definitions.Add(def);
                            def = new ItemDefinition(int.Parse(fields[1]));
                            definitions.Add(def);
                            item = new Item(def.Id);
                            items.Add(def.Id, item);
                        }
                        else if (item != null && fields.Length >= 2 && fields[0] == "ICON")
                        {
                            item.IconName = fields[1];
                        }
                        else if (def != null && fields.Length > 3 && fields[0] == "START")
                        {
                            RequirementBase requirement = null;
                            var alternatives = new List<RequirementBase>();  // For OR operator
                            for (int i = 0; i < fields.Length - 3; i += 4)
                            {
                                var newRequirement = ParseRequirement(fields[i + 1], fields[i + 2], fields[i + 3]);
                                if (requirement != null && fields[i] == "OR") requirement.AddAlternative(newRequirement);
                                else if (fields[i] == "AND") def.AddRequirementStart(newRequirement);
                                else requirement = newRequirement;
                            }

                            if (requirement != null) def.AddRequirementStart(requirement);
                        }
                        else if (def != null && fields.Length > 3 && fields[0] == "COMPLETE")
                        {
                            RequirementBase requirement = null;
                            var alternatives = new List<RequirementBase>();  // For OR operator
                            for (int i = 0; i < fields.Length - 3; i += 4)
                            {
                                var newRequirement = ParseRequirement(fields[i + 1], fields[i + 2], fields[i + 3]);
                                if (requirement != null && fields[i] == "OR") requirement.AddAlternative(newRequirement);
                                else if (fields[i] == "AND") def.AddRequirementComplete(newRequirement);
                                else requirement = newRequirement;
                            }

                            if (requirement != null) def.AddRequirementComplete(requirement);
                        }
                        else if (def != null && fields.Length > 0)
                        {
                            throw new Exception("Unrecognised instruction: " + fields[0]);
                        }
                    }
                    catch
                    {
                        throw new Exception($"Error on line {lineNumber} of {path}");
                    }
                }
            }
        }

        private static RequirementBase ParseRequirement(string typeField, string opField, string valField)
        {
            var typeSplit = typeField.Split('.');
            var op = Operator.FromString(opField);
            switch (typeSplit[0])
            {
                case "COUNT": 
                    var cr = new CountRequirement(typeSplit[1], typeSplit.Length > 2 ? typeSplit[2] : "", op, int.Parse(valField));
                    if (!CheckListContext.ProxiesByThingType.ContainsKey(cr.ThingType)) CheckListContext.ProxiesByThingType.Add(cr.ThingType, new List<ThingProxy>());
                    return cr;
                case "ITEMCOUNT":
                    var ir = new ItemCountRequirement(typeSplit[1], op, int.Parse(valField));
                    if (!CheckListContext.ItemTypeCounts.ContainsKey(ir.ItemType)) CheckListContext.ItemTypeCounts.Add(ir.ItemType, 0);
                    return ir;
                case "HOUR": return new HourRequirement(op, int.Parse(valField));
                case "STORAGECOUNT": return new StorageCountRequirement(typeSplit[1], op, int.Parse(valField));
                case "ITEMCOMPLETE": return new ItemCompleteRequirement(int.Parse(typeSplit[1]), op, bool.Parse(valField));
                case "PROJECTCOMPLETE": return new ProjectCompleteRequirement(int.Parse(typeSplit[1]), op, bool.Parse(valField));
                case "MOTHERSHIPSTATUS": return new MothershipStatusRequirement(op, valField);
                case "BOTANISTWOKEN": return new BotanistWokenRequirement(op, bool.Parse(valField));
                case "GEOLOGISTWOKEN": return new GeologistWokenRequirement(op, bool.Parse(valField));
                case "HAVEFOODFROMFRUIT": return new HaveFoodFromFruitRequirement(op, bool.Parse(valField));
                case "HAVEFOODFROMCROPS": return new HaveFoodFromCropsRequirement(op, bool.Parse(valField));
                case "HAVEBOTANIST": return new HaveBotanistRequirement(op, bool.Parse(valField));
                case "HAVEGEOLOGIST": return new HaveGeologistRequirement(op, bool.Parse(valField));
                case "HAVEPUMPINDOORS": return new HavePumpIndoorsRequirement(op, bool.Parse(valField));
                case "CLIMATE": return new ClimateRequirement(typeSplit[1], op, int.Parse(valField));
                case "ROCKETSLAUNCHED": return new RocketsLaunchedRequirement(op, int.Parse(valField));
                case "ARRIVEDCOLONISTCOUNT": return new ArrivedColonistCountRequirement(op, int.Parse(valField));
                case "ALLCOLONISTSHAVEOWNSLEEPPOD": return new AllColonistsHaveOwnSleepPodRequirement(op, bool.Parse(valField));
                default: throw new Exception("No requirement type " + typeSplit[0]);
            }
        }

        private static void LoadText()
        {
            CurrentLanguageId = UIStatics.CurrentLanguageId;
            var path = Path.Combine("Config", "Language", LanguageManager.CurrentLanguage, "CheckList.txt");
            using (var sr = File.OpenText(path))
            {
                var lineNumber = 0;
                Item currentItem = null;
                while (!sr.EndOfStream)
                {
                    lineNumber++;
                    try
                    {
                        var line = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                        var fields = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                        switch (fields[0])
                        {
                            case "ITEM":
                                var id = fields.Length > 1 ? int.Parse(fields[1]) : 0;
                                currentItem = items.ContainsKey(id) ? items[id] : null;
                                break;
                            case "TITLE" when currentItem != null:
                                currentItem.Title = line.Substring(6);
                                break;
                            case "TEXT1" when currentItem != null:
                                currentItem.Text1 = line.Substring(6);
                                break;
                            case "TEXT2" when currentItem != null:
                                currentItem.Text2 = line.Substring(6);
                                break;
                        }
                    }
                    catch
                    {
                        throw new Exception($"Error on line {lineNumber} of {path}");
                    }
                }
            }
        }

        public static void Reset()
        {
            lastUpdateFrame = 0;
            IsReset = true;
            if (cancellationTokenSource != null) cancellationTokenSource.Cancel();
            while (isRunning) Thread.Sleep(50);
            while (itemsForDisplay.TryDequeue(out _));
            foreach (var item in items.Values)
            {
                item.IsStarted = false;
                item.IsComplete = false;
                item.IsRead = false;
            }
        }

        public static Task Update()
        {
            if (isRunning) return null;

            if (World.WorldTime.FrameNumber - lastUpdateFrame < 64) return null;

            lastUpdateFrame = World.WorldTime.FrameNumber;
            CheckListContext.Update();

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            return Task.Factory.StartNew(Run, token);
        }

        internal static void Run()
        {
            isRunning = true;
            var isFinished = false;
            try
            {
                var index = -1;
                while (!isFinished && index < definitions.Count - 1 && cancellationTokenSource?.IsCancellationRequested != true)
                {
                    index++;
                    var def = definitions[index];
                    var item = items[def.Id];
                    if (item.IsComplete) continue;

                    if (!item.IsStarted && def.TestStart())
                    {
                        item.IsStarted = true;
                        itemsForDisplay.Enqueue(item);
                    }

                    if (item.IsStarted && !item.IsComplete && def.TestComplete()) item.IsComplete = true;
                }
            }
            catch
            {
                // Errors here not critical
            }

            isRunning = false;
        }
    }
}

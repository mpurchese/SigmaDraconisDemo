namespace SigmaDraconis.Commentary
{
    using Draconis.UI;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Language;
    using Operators;
    using Params;
    using Requirements;
    using Shared;
    using World;

    public static class CommentaryController
    {
        private static readonly ConcurrentStack<Comment> commentHistory = new ConcurrentStack<Comment>();
        private static readonly ConcurrentQueue<Comment> commentQueueNormal = new ConcurrentQueue<Comment>();
        private static readonly ConcurrentQueue<Comment> commentQueueImportant = new ConcurrentQueue<Comment>();
        private static readonly ConcurrentQueue<Comment> commentQueueUrgent = new ConcurrentQueue<Comment>();
        private static readonly List<CommentDefinition> generalDefs = new List<CommentDefinition>();
        private static readonly Dictionary<ColonistEventType, List<CommentDefinition>> eventDefs = new Dictionary<ColonistEventType, List<CommentDefinition>>();
        private static readonly Dictionary<int, CommentDefinition> definitionMap = new Dictionary<int, CommentDefinition>();
        private static readonly ConcurrentDictionary<int, long> definitionFramesLastUsed = new ConcurrentDictionary<int, long>();
        private static readonly HashSet<int> queuedDefs = new HashSet<int>();
        private static long lastUpdateFrame = 0;
        private static bool isRunning = false;
        private static int currentLanguageId;
        private static CancellationTokenSource cancellationTokenSource;
        
        public static List<Comment> CommentHistory => commentHistory.ToList();
        public static int CommentHistoryCount => commentHistory.Count;

        public static CommentarySerializationObject Serialize()
        {
            while (isRunning) Thread.Sleep(50);

            var commentHistoryList = commentHistory.ToList();
            commentHistoryList.Reverse();
            return new CommentarySerializationObject
            {
                CommentsHistory = commentHistoryList,
                CommentsQueueNormal = commentQueueNormal.ToList(),
                CommentsQueueImportant = commentQueueImportant.ToList(),
                CommentsQueueUrgent = commentQueueUrgent.ToList(),
                DefinitionFramesLastUsed = definitionFramesLastUsed.ToDictionary(kv => kv.Key, kv => kv.Value),
                LastUpdateFrame = lastUpdateFrame
            };
        }

        public static void Deserialize(CommentarySerializationObject obj)
        {
            Reset();
            while (isRunning) Thread.Sleep(50);

            if (obj.CommentsHistory != null)
            {
                foreach (var comment in obj.CommentsHistory) commentHistory.Push(comment);
            }

            if (obj.CommentsQueueNormal != null)
            {
                foreach (var comment in obj.CommentsQueueNormal)
                {
                    commentQueueNormal.Enqueue(comment);
                    if (!queuedDefs.Contains(comment.DefintionId)) queuedDefs.Add(comment.DefintionId);
                }
            }

            if (obj.CommentsQueueImportant != null)
            {
                foreach (var comment in obj.CommentsQueueImportant)
                {
                    commentQueueImportant.Enqueue(comment);
                    if (!queuedDefs.Contains(comment.DefintionId)) queuedDefs.Add(comment.DefintionId);
                }
            }

            if (obj.CommentsQueueUrgent != null)
            {
                foreach (var comment in obj.CommentsQueueUrgent)
                {
                    commentQueueUrgent.Enqueue(comment);
                    if (!queuedDefs.Contains(comment.DefintionId)) queuedDefs.Add(comment.DefintionId);
                }
            }

            if (obj.DefinitionFramesLastUsed != null)
            {
                foreach (var kv in obj.DefinitionFramesLastUsed) definitionFramesLastUsed.TryAdd(kv.Key, kv.Value);
            }

            lastUpdateFrame = obj.LastUpdateFrame;
        }

        public static void Load()
        {
            LoadDefinitions();
            LoadText();
        }

        private static void LoadDefinitions()
        {
            generalDefs.Clear();
            definitionMap.Clear();
            var path = Path.Combine("Config", "Commentary", "CommentaryDefs.txt");
            using (var sr = File.OpenText(path))
            {
                var lineNumber = 0;
                CommentDefinition def = null;
                ColonistEventType eventType = ColonistEventType.None;
                while (!sr.EndOfStream)
                {
                    lineNumber++;
                    try
                    {
                        var line = sr.ReadLine();
                        if (line.Contains('#')) line = line.Substring(0, line.IndexOf('#'));
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var fields = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                        if (def == null && fields.Length >= 2 && fields[0] == "MSG")
                        {
                            def = new CommentDefinition(int.Parse(fields[1]));
                            eventType = ColonistEventType.None;
                            for (int i = 2; i < fields.Length; i++)
                            {
                                switch(fields[i])
                                {
                                    case "EVENT": eventType = (ColonistEventType)Enum.Parse(typeof(ColonistEventType), fields[i + 1]); i++; break;
                                    case "REPEAT": def.RepeatDelay = int.Parse(fields[i + 1]); i++; break;
                                    case "STICKY": def.IsSticky = true; break;
                                    case "IMPORTANT": def.IsImportant = true; break;
                                    case "URGENT": def.IsUrgent = true; break;
                                    case "FOLLOWEDBY": def.FollowedBy = int.Parse(fields[i + 1]); i++; break;
                                    case "SLEEP": def.IsSleepComment = true; break;
                                    case "SEQUENCEONLY": def.IsSequenceOnly = true; break;
                                    case "DONTFOLLOW": def.DontFollow.Add(int.Parse(fields[i + 1])); i++; break;
                                }
                            }
                        }
                        else if (def != null && fields.Length >= 1 && fields[0] == "END")
                        {
                            if (eventType != ColonistEventType.None)
                            {
                                if (!eventDefs.ContainsKey(eventType)) eventDefs.Add(eventType, new List<CommentDefinition>());
                                eventDefs[eventType].Add(def);
                            }
                            else if (!def.IsSequenceOnly) generalDefs.Add(def);

                            definitionMap.Add(def.Id, def);
                            def = null;
                        }
                        else if (def != null && fields.Length > 3 && fields[0] == "IF")
                        {
                            RequirementBase requirement = null;
                            var alternatives = new List<RequirementBase>();  // For OR operator
                            for (int i = 0; i < fields.Length - 3; i += 4)
                            {
                                var newRequirement = ParseRequirement(fields[i + 1], fields[i + 2], fields[i + 3]);
                                if (requirement != null && fields[i] == "OR") requirement.AddAlternative(newRequirement);
                                else if (fields[i] == "AND") def.AddRequirement(newRequirement);
                                else requirement = newRequirement;
                            }

                            if (requirement != null) def.AddRequirement(requirement);
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
                case "ACTIVITY": return new ActivityRequirement(typeSplit[1], op, bool.Parse(valField));
                case "ANYPLANTSTOHARVEST": return new AnyPlantsForHarvestRequirement(op, bool.Parse(valField));
                case "ADJACENT": return new AdjacentRequirement(typeSplit[1], typeSplit.Length > 2 ? typeSplit[2] : "", op, bool.Parse(valField));
                case "CARD": return new ColonistCardRequirement(typeSplit[1], op, bool.Parse(valField));
                case "CLIMATE": return new ClimateRequirement(typeSplit[1], op, int.Parse(valField));
                case "COLONISTENERGY": return new ColonistEnergyRequirement(op, double.Parse(valField));
                case "COUNT": 
                    var cr = new CountRequirement(typeSplit[1], typeSplit.Length > 2 ? typeSplit[2] : "", op, int.Parse(valField));
                    if (!CommentaryContext.ProxiesByThingType.ContainsKey(cr.ThingType)) CommentaryContext.ProxiesByThingType.Add(cr.ThingType, new List<ThingProxy>());
                    return cr;
                case "COUNTFACTORYSTATUS":
                    var cfs = new CountFactoryStatusRequirement(typeSplit[1], typeSplit[2], op, int.Parse(valField));
                    if (!CommentaryContext.ProxiesByThingType.ContainsKey(cfs.ThingType)) CommentaryContext.ProxiesByThingType.Add(cfs.ThingType, new List<ThingProxy>());
                    return cfs;
                case "COUNTSKILL": return new CountColonistsBySkillRequirement(typeSplit[1], op, int.Parse(valField));
                case "COUNTAVAILABLESLEEPPODS": return new CountAvailableSleepPodsRequirement(op, int.Parse(valField));
                case "DICE": return new DiceRequirement(int.Parse(typeSplit[1]), op, int.Parse(valField));
                case "FIRSTCOMMENT": return new FirstCommentRequirement(op, bool.Parse(valField));
                case "FOODPREFERENCE": return new FoodPreferenceRequirement(typeSplit[1], typeSplit[2], op, bool.Parse(valField));
                case "HOUR": return new HourRequirement(op, int.Parse(valField));
                case "ITEMCOUNT":
                    var ir = new ItemCountRequirement(typeSplit[1], op, int.Parse(valField));
                    if (!CommentaryContext.ItemTypeCounts.ContainsKey(ir.ItemType)) CommentaryContext.ItemTypeCounts.Add(ir.ItemType, 0);
                    return ir;
                case "LASTPROJECT": return new LastProjectRequirement(op, int.Parse(valField));
                case "LOCATION": return new ColonistLocationRequirement(typeSplit[1], op, bool.Parse(valField));
                case "LOWFOOD": return new LowFoodRequirement(op, bool.Parse(valField));
                case "NETWORK": return new NetworkRequirement(typeSplit[1], op, float.Parse(valField, NumberStyles.Any, CultureInfo.InvariantCulture));
                case "OTHERCOLONIST": return new OtherColonistRequirement(typeSplit[1], op, int.Parse(valField));
                case "SKILL": return new ColonistsSkillRequirement(typeSplit[1], op, bool.Parse(valField));
                case "STAT":
                    var sr = new StatRequirement(typeSplit[1], op, long.Parse(valField));
                    if (!CommentaryContext.Stats.ContainsKey(typeSplit[1])) CommentaryContext.Stats.Add(typeSplit[1], 0);
                    return sr;
                case "TIMEUNTILARRIVE": return new TimeUntilArriveRequirement(op, int.Parse(valField));
                case "TIMEUNTILWAKE": return new TimeUntilCanWakeRequirement(op, int.Parse(valField));
                case "WEATHER": return new WeatherRequirement(typeSplit[1], op, int.Parse(valField));
                case "WORLDLIGHT": return new WorldLightRequirement(typeSplit[1], op, float.Parse(valField, NumberStyles.Any, CultureInfo.InvariantCulture));
                default: throw new Exception("No requirement type " + typeSplit[0]);
            }
        }

        private static void LoadText()
        {
            currentLanguageId = UIStatics.CurrentLanguageId;

            foreach (var value in definitionMap.Values) value.SetTemplate("", new List<ITemplateParam>());

            var path = Path.Combine("Config", "Language", LanguageManager.CurrentLanguage, "Commentary.txt");
            using (var sr = File.OpenText(path))
            {
                var lineNumber = 0;
                while (!sr.EndOfStream)
                {
                    lineNumber++;
                    try
                    {
                        var line = sr.ReadLine();
                        if (line.Contains('#')) line = line.Substring(0, line.IndexOf('#'));
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var fields = line.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);

                        if (fields.Length == 2 && int.TryParse(fields[0], out int id) && definitionMap.ContainsKey(id))
                        {
                            var index = 0;
                            var template = fields[1];
                            var templateParams = new List<ITemplateParam>(); 
                            while (template.Contains("["))
                            {
                                var start = template.IndexOf('[');
                                var end = template.IndexOf(']');
                                var part = template.Substring(start, end + 1 - start);
                                switch (part)
                                {
                                    case "[arrivingskill]": templateParams.Add(new ArrivingSkillParam()); break;
                                    case "[arrivingname]": templateParams.Add(new ArrivingNameParam()); break;
                                    case "[name]": templateParams.Add(new NameParam()); break;
                                    case "[othername]": templateParams.Add(new OtherNameParam()); break;
                                    case "[resource]": templateParams.Add(new ResourceParam()); break;
                                    case "[resourcedensity]": templateParams.Add(new ResourceDensityParam()); break;
                                    case "[scannerresource]": templateParams.Add(new ScannerResourceParam()); break;
                                    case "[scannerresourcedensity]": templateParams.Add(new ScannerResourceDensityParam()); break;
                                    case "[skill]": templateParams.Add(new SkillParam()); break;
                                    case "[lastfood]": templateParams.Add(new LastFoodParam()); break;
                                }

                                template = template.Replace(part, $"{{{index}}}");
                                index++;
                            }

                            definitionMap[id].SetTemplate(template, templateParams);
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
            if (cancellationTokenSource != null) cancellationTokenSource.Cancel();
            commentHistory.Clear();
            queuedDefs.Clear();
            while (commentQueueNormal.TryDequeue(out _));
            while (commentQueueImportant.TryDequeue(out _)) ;
            while (commentQueueUrgent.TryDequeue(out _)) ;
            CommentaryContext.Reset();
            definitionFramesLastUsed.Clear();
        }

        public static Task Update()
        {
            if (isRunning) return null;

            var contextUpdated = ProcessEvents();
            if (World.WorldTime.FrameNumber - lastUpdateFrame < 64) return null;

            lastUpdateFrame = World.WorldTime.FrameNumber;

            if (currentLanguageId != UIStatics.CurrentLanguageId) LoadText();

            if (!contextUpdated) CommentaryContext.Update();

            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            return Task.Factory.StartNew(Run, token);
        }

        public static Comment GetLatest()
        {
            try
            {
                if (!isRunning && CommentaryContext.FrameNumber == 0) CommentaryContext.Update();     // Game just loaded

                // Current is the one at the top of the stack.
                commentHistory.TryPeek(out Comment currentComment);
                var currDef = currentComment != null && definitionMap.ContainsKey(currentComment.DefintionId) ? definitionMap[currentComment.DefintionId] : null;

                // Update if:
                // Message has been displayed 3 seconds and there is a more urgent one waiting, or it's no-longer relevant
                // Message has been displayed 6 seconds and it's not sticky and there is another one on any queue
                // Message has been displayed 12 seconds and it's not sticky
                // Message has been displayed 120 seconds even if it is sticky

                // <3 seconds: Never hide
                if (currentComment?.FrameDisplayed + 180 > World.WorldTime.FrameNumber) return currentComment;

                // <6 seconds
                var isStillRelevant = currDef != null && currDef.CheckStillApplies(CommentaryContext.LiveColonists.FirstOrDefault(c => c.Id == currentComment.ColonistId));
                var isAnyWaiting = commentQueueNormal.Any() || commentQueueImportant.Any() || commentQueueUrgent.Any();
                var isMoreUrgentWaiting = isAnyWaiting && ((currDef?.IsUrgent != true && commentQueueUrgent.Any()) || (currDef?.IsUrgent != true && currDef?.IsImportant != true && commentQueueImportant.Any()));

                if (currentComment?.FrameDisplayed + 360 > World.WorldTime.FrameNumber && isStillRelevant && !isMoreUrgentWaiting)
                {
                    return currentComment;
                }

                // <12 seconds
                if (currentComment?.FrameDisplayed + 720 > World.WorldTime.FrameNumber && isStillRelevant && !isMoreUrgentWaiting && (currDef.IsSticky || !isAnyWaiting))
                {
                    return currentComment;
                }

                // 12 - 120 seconds
                if (isStillRelevant && currDef.IsSticky && currentComment?.FrameDisplayed + 7200 > World.WorldTime.FrameNumber && !isMoreUrgentWaiting)
                {
                    return currentComment;
                }

                if (currentComment != null) currentComment.FrameHidden = World.WorldTime.FrameNumber;

                Comment next = null;
                while (commentQueueUrgent.Any())
                {
                    commentQueueUrgent.TryDequeue(out next);
                    var def = next != null && definitionMap.ContainsKey(next.DefintionId) ? definitionMap[next.DefintionId] : null;
                    if (def == null) continue;
                    if (queuedDefs.Contains(def.Id)) queuedDefs.Remove(def.Id);
                    var colonist = CommentaryContext.LiveColonists.FirstOrDefault(c => c.Id == next.ColonistId);
                    if (def != null && def.CheckStillApplies(colonist))
                    {
                        definitionFramesLastUsed[def.Id] = World.WorldTime.FrameNumber;
                        next.FrameDisplayed = World.WorldTime.FrameNumber;
                        commentHistory.Push(next);
                        if (!CommentaryContext.ColonistsWithComments.Contains(colonist.Id)) CommentaryContext.ColonistsWithComments.Add(colonist.Id);
                        return next;
                    }
                }

                while (commentQueueImportant.Any())
                {
                    commentQueueImportant.TryDequeue(out next);
                    var def = next != null && definitionMap.ContainsKey(next.DefintionId) ? definitionMap[next.DefintionId] : null;
                    if (def == null) continue;
                    if (queuedDefs.Contains(def.Id)) queuedDefs.Remove(def.Id);
                    var colonist = CommentaryContext.LiveColonists.FirstOrDefault(c => c.Id == next.ColonistId);
                    if (def != null && def.CheckStillApplies(colonist))
                    {
                        definitionFramesLastUsed[def.Id] = World.WorldTime.FrameNumber;
                        next.FrameDisplayed = World.WorldTime.FrameNumber;
                        commentHistory.Push(next);
                        if (!CommentaryContext.ColonistsWithComments.Contains(colonist.Id)) CommentaryContext.ColonistsWithComments.Add(colonist.Id);
                        return next;
                    }
                }

                while (commentQueueNormal.Any())
                {
                    commentQueueNormal.TryDequeue(out next);
                    var def = next != null && definitionMap.ContainsKey(next.DefintionId) ? definitionMap[next.DefintionId] : null;
                    if (def == null) continue;
                    if (queuedDefs.Contains(def.Id)) queuedDefs.Remove(def.Id);
                    var colonist = CommentaryContext.LiveColonists.FirstOrDefault(c => c.Id == next.ColonistId);
                    if (def != null && def.CheckStillApplies(colonist))
                    {
                        definitionFramesLastUsed[def.Id] = World.WorldTime.FrameNumber;
                        next.FrameDisplayed = World.WorldTime.FrameNumber;
                        commentHistory.Push(next);
                        if (!CommentaryContext.ColonistsWithComments.Contains(colonist.Id)) CommentaryContext.ColonistsWithComments.Add(colonist.Id);
                        return next;
                    }
                }
            }
            catch { }   // Ignore threading errors

            return null;
        }

        internal static void Run()
        {
            if (commentQueueUrgent.Any()) return;

            isRunning = true;
            var isFinished = false;
            try
            {
                var index = -1;
                while (!isFinished && index < generalDefs.Count - 1 && cancellationTokenSource?.IsCancellationRequested != true)
                {
                    index++;

                    var def = generalDefs[index];
                    if (queuedDefs.Contains(def.Id)) continue;
                    if (queuedDefs.Intersect(def.DontFollow).Any()) continue;
                    if (!def.IsValid) continue;

                    if (!def.IsUrgent && (commentQueueUrgent.Any() || commentQueueUrgent.Any())) continue;

                    if (!def.IsUrgent && !def.IsImportant && (commentQueueUrgent.Any() || commentQueueUrgent.Any() || commentQueueNormal.Any())) continue;

                    commentHistory.TryPeek(out Comment lastComment);
                    if (lastComment?.DefintionId == def.Id) continue;

                    var frameLastUsed = definitionFramesLastUsed.ContainsKey(def.Id) ? definitionFramesLastUsed[def.Id] : 0;
                    foreach (var id in def.DontFollow) if (definitionFramesLastUsed.ContainsKey(id)) frameLastUsed = Math.Max(frameLastUsed, definitionFramesLastUsed[id]);

                    foreach (var colonist in CommentaryContext.LiveColonists.Where(c => !c.IsSleeping).OrderBy(c => Guid.NewGuid()))
                    {
                        if (def.Test(colonist, frameLastUsed, false))
                        {
                            EnqueueComment(def, colonist, null);
                            isFinished = true;
                            break;
                        }
                    }
                }
            }
            catch
            {
                // Errors here not critical
            }

            isRunning = false;
        }

        // Returns true if context updated
        private static bool ProcessEvents()
        {
            if (!EventManager.HasColonistEvent) return false;

            var e = EventManager.DequeueColonistEvent();
            if (!eventDefs.ContainsKey(e.EventType)) return false;

            try
            {
                CommentaryContext.Update();
                var colonist = CommentaryContext.LiveColonists.FirstOrDefault(c => c.Id == e.ColonistId);
                if (colonist == null) return true;

                foreach (var def in eventDefs[e.EventType])
                {
                    if (queuedDefs.Contains(def.Id)) continue;
                    if (colonist.IsSleeping && !def.IsSleepComment) continue;

                    commentHistory.TryPeek(out Comment lastComment);
                    if (lastComment?.DefintionId == def.Id) continue;

                    var frameLastUsed = definitionFramesLastUsed.ContainsKey(def.Id) ? definitionFramesLastUsed[def.Id] : 0;
                    var otherColonist = e.OtherColonistId.HasValue ? CommentaryContext.LiveColonists.SingleOrDefault(c => c.Id == e.OtherColonistId) : null;
                    if (otherColonist == null && e.EventType == ColonistEventType.Death) otherColonist = CommentaryContext.DeadColonists.SingleOrDefault(c => c.Id == e.OtherColonistId);
                    if (def.Test(colonist, frameLastUsed, false, otherColonist))
                    {
                        EnqueueComment(def, colonist, otherColonist);
                        break;
                    }
                }
            }
            catch 
            {
                // Errors here not critical
            }

            return true;
        }

        private static void EnqueueComment(CommentDefinition def, ColonistProxy colonist, ColonistProxy otherColonist)
        {
            if (def.IsUrgent) commentQueueUrgent.Enqueue(new Comment(def, colonist, otherColonist));
            else if (def.IsImportant) commentQueueImportant.Enqueue(new Comment(def, colonist, otherColonist));
            else commentQueueNormal.Enqueue(new Comment(def, colonist, otherColonist));

            queuedDefs.Add(def.Id);

            if (def.FollowedBy > 0 && definitionMap.ContainsKey(def.FollowedBy)) EnqueueComment(definitionMap[def.FollowedBy], colonist, otherColonist);
        }
    }
}

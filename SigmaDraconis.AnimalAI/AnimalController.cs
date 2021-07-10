namespace SigmaDraconis.AnimalAI
{
    using System.Collections.Generic;
    using System.Linq;
    using Shared;
    using World;
    using WorldInterfaces;

    public static class AnimalController
    {
        public static Dictionary<int, TortoiseAI> TortoiseAIs = new Dictionary<int, TortoiseAI>();
        public static Dictionary<int, SnowTortoiseAI> SnowTortoiseAIs = new Dictionary<int, SnowTortoiseAI>();
        public static Dictionary<int, RedBugAI> RedBugAIs = new Dictionary<int, RedBugAI>();
        public static Dictionary<int, BlueBugAI> BlueBugAIs = new Dictionary<int, BlueBugAI>();

        public static void Update()
        {
            var tortoises = World.GetThings<IAnimal>(ThingType.Tortoise).ToList();
            foreach (var id in TortoiseAIs.Keys.ToList())
            {
                if (tortoises.All(c => c.Id != id)) TortoiseAIs.Remove(id);
            }

            foreach (var tortoise in tortoises)
            {
                if (!TortoiseAIs.ContainsKey(tortoise.Id)) TortoiseAIs.Add(tortoise.Id, new TortoiseAI(tortoise));
                var ai = TortoiseAIs[tortoise.Id];
                ai.Update();
                tortoise.Update();
            }

            var redBugs = World.GetThings<IAnimal>(ThingType.RedBug).ToList();
            foreach (var id in RedBugAIs.Keys.ToList())
            {
                if (redBugs.All(c => c.Id != id)) RedBugAIs.Remove(id);
            }

            foreach (var bug in redBugs)
            {
                if (!RedBugAIs.ContainsKey(bug.Id)) RedBugAIs.Add(bug.Id, new RedBugAI(bug));
                var ai = RedBugAIs[bug.Id];
                ai.Update();
                bug.Update();
            }

            var blueBugs = World.GetThings<IAnimal>(ThingType.BlueBug).ToList();
            foreach (var id in BlueBugAIs.Keys.ToList())
            {
                if (blueBugs.All(c => c.Id != id)) BlueBugAIs.Remove(id);
            }

            foreach (var bug in blueBugs)
            {
                if (!BlueBugAIs.ContainsKey(bug.Id)) BlueBugAIs.Add(bug.Id, new BlueBugAI(bug));
                var ai = BlueBugAIs[bug.Id];
                ai.Update();
                bug.Update();
            }
        }

        public static IAnimalAI GetAI(int id)
        {
            if (World.ClimateType == ClimateType.Snow)
            {
                if (SnowTortoiseAIs.ContainsKey(id)) return SnowTortoiseAIs[id];
            }
            else
            {
                if (TortoiseAIs.ContainsKey(id)) return TortoiseAIs[id];
                if (RedBugAIs.ContainsKey(id)) return RedBugAIs[id];
                if (BlueBugAIs.ContainsKey(id)) return BlueBugAIs[id];
            }

            return null;
        }

        public static void DoBackgroundUpdate()
        {
            var tortoises = World.GetThings<IAnimal>(ThingType.Tortoise).ToList();
            foreach (var tortoise in tortoises)
            {
                if (TortoiseAIs.ContainsKey(tortoise.Id)) TortoiseAIs[tortoise.Id].DoBackgroundUpdate();
            }

            var redBugs = World.GetThings<IAnimal>(ThingType.RedBug).ToList();
            foreach (var bug in redBugs)
            {
                if (RedBugAIs.ContainsKey(bug.Id)) RedBugAIs[bug.Id].DoBackgroundUpdate();
            }

            var blueBugs = World.GetThings<IAnimal>(ThingType.BlueBug).ToList();
            foreach (var bug in blueBugs)
            {
                if (BlueBugAIs.ContainsKey(bug.Id)) BlueBugAIs[bug.Id].DoBackgroundUpdate();
            }
        }
    }
}

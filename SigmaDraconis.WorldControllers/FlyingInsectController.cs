namespace SigmaDraconis.WorldControllers
{
    using System.Linq;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.Fauna;
    using World.Flora;
    using WorldInterfaces;

    public static class FlyingInsectController
    {
        private static int frame = 0;

        public static void Update()
        {
            frame++;
            if (frame % 120 == 0)
            {
                if (World.Temperature >= 10 && World.WorldLight.Brightness > 0.8)
                {
                    var flowers = World.GetThings(ThingType.SmallPlant3).OfType<SmallPlant3>().Where(p => p.AnimationFrame >= 40).ToList();
                    if (World.GetThings(ThingType.Bee).Count() < flowers.Count() * 0.5f)   // Max 1 bee per 2 flowers
                    {
                        foreach (var plant in flowers.OfType<SmallPlant3>().Where(p => p.BeeID == 0))
                        {
                            if (Rand.Next(50) > 0) continue;

                            // Must have at least two other unoccupied flowers within 2 - 20 tiles in order to spawn
                            var otherCount = 0;
                            foreach (var other in flowers.Where(f => f.Id != plant.Id && f.BeeID == 0 && (f.MainTile.TerrainPosition - plant.MainTile.TerrainPosition).Length().Between(2f, 20f)))
                            {
                                otherCount++;
                                if (otherCount == 2) break;
                            }

                            if (otherCount < 2) continue;

                            var bee = new Bee(plant.MainTile) { FlowerID = plant.Id, Angle = 11.25f * Rand.Next(31) * Mathf.PI / 180f };
                            World.AddThing(bee);
                            bee.Init();
                            plant.BeeID = bee.Id;
                        }
                    }
                }
                else
                {
                    foreach (var bee in World.GetThings<IFlyingInsect>(ThingType.Bee).Where(b => b.Height == 0 && !b.IsFadingOut && !b.IsFadingIn).ToList())
                    {
                        var r = (int)(World.WorldLight.Brightness * 50);
                        if (r > 1 && Rand.Next(r) > 0) continue;
                        bee.IsFadingOut = true;
                    }
                }
            }
        }
    }
}

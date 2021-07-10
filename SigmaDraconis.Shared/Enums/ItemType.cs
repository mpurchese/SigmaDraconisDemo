namespace SigmaDraconis.Shared
{
    public enum ItemType
    {
        None = 0,
        Metal = 1,
        Biomass = 2,
        IronOre = 3,
        LiquidFuel = 4,
        Food = 5,
        Coal = 6,
        Stone = 7,
        Mush = 8,
        BatteryCells = 9,
        Compost = 10,
        SolarCells = 11,
        Glass = 12,
        Composites = 13,
        Kek = 14,

        // Non-storable items must be last because number is used for texture coordinates in inventory displays
        Crop = 100,
        Fruit = 101,
        Water = 200
    }
}

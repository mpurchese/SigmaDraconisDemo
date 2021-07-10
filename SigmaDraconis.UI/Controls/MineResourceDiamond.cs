namespace SigmaDraconis.UI
{
    using Draconis.Shared;
    using Draconis.UI;
    using Shared;
    using WorldInterfaces;

    public class MineResourceDiamond : UIElementBase
    {
        private readonly MineResourceDiamondCell[] cells = new MineResourceDiamondCell[9];
        private IMine mine;

        public MineResourceDiamond(IUIElement parent, int x, int y)
            : base(parent, x, y, Scale(196), Scale(148))
        {
            this.IsInteractive = false;

            this.cells[0] = new MineResourceDiamondCell(this, Direction.E, 128, 48);
            this.cells[1] = new MineResourceDiamondCell(this, Direction.W, 0, 48);
            this.cells[2] = new MineResourceDiamondCell(this, Direction.S, 64, 96);
            this.cells[3] = new MineResourceDiamondCell(this, Direction.N, 64, 0);
            this.cells[4] = new MineResourceDiamondCell(this, Direction.NE, 96, 24);
            this.cells[5] = new MineResourceDiamondCell(this, Direction.SE, 96, 72);
            this.cells[6] = new MineResourceDiamondCell(this, Direction.SW, 32, 72);
            this.cells[7] = new MineResourceDiamondCell(this, Direction.NW, 32, 24);
            this.cells[8] = new MineResourceDiamondCell(this, Direction.None, 64, 48);

            for (int i = 0; i < 9; i++)
            {
                this.AddChild(this.cells[i]);
                this.cells[i].MouseLeftClick += this.OnCellClick;
            }
        }

        public void SetMine(IMine mine)
        {
            this.mine = mine;
            for (int i = 0; i < 9; i++)
            {
                var tile = (i == 8) ? mine.MainTile : mine.MainTile.GetTileToDirection((Direction)i);
                var canSelect = tile != null && tile.MineResourceMineId.GetValueOrDefault(mine.Id) == mine.Id;
                this.cells[i].SetResources(tile?.GetResources(), mine.TileSelections[i], tile?.MineResourceExtrationProgress ?? 0, canSelect);
            }
        }

        private void OnCellClick(object sender, MouseEventArgs e)
        {
            var cell = sender as MineResourceDiamondCell;
            if (cell.IsMouseOver && cell.CanSelect)
            {
                cell.IsSelected = !cell.IsSelected;
                if (this.mine != null)
                {
                    this.mine.TileSelections[(int)cell.Direction] = cell.IsSelected;
                    var tile = cell.Direction == Direction.None ? this.mine.MainTile : this.mine.MainTile.GetTileToDirection(cell.Direction);
                    if (tile != null) tile.SetMineResourceMineId(cell.IsSelected ? mine.Id : (int?)null);
                }
            }
        }
    }
}

namespace SigmaDraconis.AI
{
    using ProtoBuf;
    using System.Linq;
    using Shared;
    using World;
    using World.Projects;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionRelax : ActionBase
    {
        [ProtoMember(1)]
        private int? tableId;

        // Deserialisation ctor
        protected ActionRelax() { }

        public ActionRelax(IColonist colonist, ITable table) : base(colonist)
        {
            if (colonist.CarriedItemTypeArms == ItemType.Kek)
            {
                colonist.DrinkingKekFrame = 1;
                if (table != null)
                {
                    table.AddKek(DirectionHelper.Reverse(colonist.FacingDirection));
                    this.tableId = table.Id;
                    colonist.CarriedItemTypeArms = ItemType.None;
                    EventManager.EnqueueWorldPropertyChangeEvent(this.Colonist.Id, nameof(IColonist.CarriedItemTypeArms), this.Colonist.MainTile.Row, ThingType.Colonist);
                }
            }
        }

        public override void Update()
        {
            this.Colonist.IsWorking = false;
            this.Colonist.IsRelaxing = true;

            if (this.Colonist.DrinkingKekFrame > 0)
            {
                this.Colonist.DrinkingKekFrame++;
                if (this.Colonist.DrinkingKekFrame > Constants.ColonistFramesToDrinkKek)
                {
                    this.Colonist.DrinkingKekFrame = 0;
                    if (ProjectManager.GetDefinition(12)?.IsDone == true)
                    {
                        this.Colonist.KekHappinessTimer = Constants.ColonistFramesKekHappiness;
                        EventManager.EnqueueColonistEvent(ColonistEventType.ImprovedKek, this.Colonist.Id);
                    }

                    if (this.Colonist.CarriedItemTypeArms != ItemType.None)
                    {
                        this.Colonist.CarriedItemTypeArms = ItemType.None;
                        EventManager.EnqueueWorldPropertyChangeEvent(this.Colonist.Id, nameof(IColonist.CarriedItemTypeArms), this.Colonist.MainTile.Row, ThingType.Colonist);
                    }
                    if (this.tableId.HasValue && World.GetThing(this.tableId.Value) is ITable table)
                    {
                        table.RemoveKek(DirectionHelper.Reverse(this.Colonist.FacingDirection));
                        this.Colonist.ActivityType = ColonistActivityType.Relax;
                    }
                    else this.IsFinished = true;
                }
                else if (this.tableId.HasValue)
                {
                    var table = World.GetThing(this.tableId.Value) as ITable;
                    if (table == null || !table.IsReady)
                    {
                        // Pick up kek - table is gone or being deconstructed 
                        if (table != null) table.RemoveKek(DirectionHelper.Reverse(this.Colonist.FacingDirection));
                        this.tableId = null;
                        this.Colonist.CarriedItemTypeArms = ItemType.Kek;
                        EventManager.EnqueueWorldPropertyChangeEvent(this.Colonist.Id, nameof(IColonist.CarriedItemTypeArms), this.Colonist.MainTile.Row, ThingType.Colonist);
                    }
                }
                else
                {
                    var facingTile = this.Colonist.MainTile.GetTileToDirection(this.Colonist.FacingDirection);
                    if (facingTile != null 
                        && facingTile.ThingsPrimary.FirstOrDefault(t => t.ThingType == ThingType.TableMetal || t.ThingType == ThingType.TableStone) is ITable table
                        && table.IsReady)
                    {
                        // Put down kek
                        table.AddKek(DirectionHelper.Reverse(this.Colonist.FacingDirection));
                        this.tableId = table.Id;
                        this.Colonist.CarriedItemTypeArms = ItemType.None;
                        EventManager.EnqueueWorldPropertyChangeEvent(this.Colonist.Id, nameof(IColonist.CarriedItemTypeArms), this.Colonist.MainTile.Row, ThingType.Colonist);
                    }
                }
            }

             base.Update();
        }
    }
}

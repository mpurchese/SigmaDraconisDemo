namespace SigmaDraconis.AI
{
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using World;
    using WorldControllers;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionGeology : ActionBase
    {
        // Deserialisation ctor
        protected ActionGeology() { }

        public ActionGeology(IColonist colonist) : base(colonist)
        {
            this.ApplyTileBlock();
            this.RemoveJob();
        }

        public override void AfterDeserialization()
        {
            base.AfterDeserialization();
            this.ApplyTileBlock();
            this.RemoveJob();
        }

        private void RemoveJob()
        {
            if (GeologyController.TilesToSurvey.Contains(this.Colonist.MainTile.Index))
            {
                GeologyController.Toggle(this.Colonist.MainTile);
                EventManager.EnqueueWorldPropertyChangeEvent(this.Colonist.MainTile.Index, "TilesToSurvey", this.Colonist.MainTile.Row);
            }
        }

        public override void Update()
        {
            this.Colonist.IsWorking = this.Colonist.MainTile?.IsMineResourceVisible == false;

            if (this.Colonist.RaisedArmsFrame == 18)
            {
                var tile = this.Colonist.MainTile;
                if (tile?.IsMineResourceVisible == false)
                {
                    if (tile.InrementResourceSurveyProgress(1.0 / Constants.FramesToSurveyTileResources))
                    {
                        World.LastTileSurveyedByGeologist = tile.Index;
                        this.Colonist.IsWorking = false;
                        this.Colonist.LastResourceFound = tile.MineResourceType;
                        this.Colonist.LastResourceDensityFound = tile.MineResourceDensity;

                        if (tile.MineResourceType != ItemType.None)
                        {
                            if (tile.AdjacentTiles8.All(t => !t.IsMineResourceVisible || t.MineResourceType != tile.MineResourceType))
                            {
                                EventManager.EnqueueColonistEvent(ColonistEventType.NewResourceFound, this.Colonist.Id);
                            }
                            else if (tile.AdjacentTiles8.All(t => !t.IsMineResourceVisible || t.MineResourceType != tile.MineResourceType || t.MineResourceDensity < tile.MineResourceDensity))
                            {
                                EventManager.EnqueueColonistEvent(ColonistEventType.ImprovedResourceFound, this.Colonist.Id);
                            }
                        }

                        return;
                    }
                }
                else
                {
                    this.Colonist.IsWorking = false;
                    return;
                }
            }

            if (!this.Colonist.IsWorking && this.Colonist.RaisedArmsFrame == 0)
            {
                this.IsFinished = true;
                this.ReleaseTileBlock();
            }

            base.Update();
        }
    }
}

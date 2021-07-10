namespace SigmaDraconis.AI
{
    using Draconis.Shared;
    using ProtoBuf;
    using Shared;
    using World;
    using WorldInterfaces;

    [ProtoContract]
    public class ActionResearch : ActionBase
    {
        [ProtoMember(1)]
        private int counter = 0;

        public ILab Lab { get; private set; }

        [ProtoMember(2)]
        private readonly int labId;

        // Deserialisation ctor
        protected ActionResearch() { }

        public ActionResearch(IColonist colonist, ILab lab) : base(colonist)
        {
            this.Lab = lab;
            this.labId = lab.Id;
        }

        public override void AfterDeserialization()
        {
            this.Lab = World.GetThing(this.labId) as ILab;
            base.AfterDeserialization();
        }

        public override void Update()
        {
            this.OpenDoorIfExists();
            counter++;

            if (this.Lab?.IsReady != true || !this.Lab.LabStatus.In(LabStatus.WaitingForColonist, LabStatus.InProgress) || counter >= 300 || DoJob())
            {
                // Job done, aborted, or 300 frames passed (so we want to check if we want to keep working)
                this.IsFinished = true;
                return;
            }
            else
            {
                this.Colonist.IsWorking = true;
                this.Colonist.LastLabProjectId = this.Lab.SelectedProjectTypeId;
            }

            base.Update();
        }

        private bool DoJob()
        {
            var workRate = this.Colonist.GetWorkRate(out var effects);
            return this.Lab.DoJob(workRate, effects);
        }
    }
}

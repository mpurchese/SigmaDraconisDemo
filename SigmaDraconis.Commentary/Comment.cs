namespace SigmaDraconis.Commentary
{
    using ProtoBuf;
    using Shared;
    using Context;

    [ProtoContract]
    public class Comment
    {
        [ProtoMember(1)]
        public int DefintionId { get; }

        [ProtoMember(2)]
        public int ColonistId { get; }

        [ProtoMember(3)]
        public string ColonistName { get; set; }

        [ProtoMember(4)]
        public SkillType ColonistSkillType { get; set; }

        [ProtoMember(5)]
        public string Text { get; set; }

        [ProtoMember(6)]
        public long FrameDisplayed { get; set; }

        [ProtoMember(7)]
        public long FrameHidden { get; set; }

        [ProtoMember(8)]
        public bool IsImportant { get; set; }

        [ProtoMember(9)]
        public bool IsUrgent { get; set; }

        public Comment() { }

        internal Comment(CommentDefinition def, ColonistProxy colonist, ColonistProxy otherColonist)
        {
            this.DefintionId = def.Id;
            this.ColonistId = colonist.Id;
            this.ColonistName = colonist.Name;
            this.ColonistSkillType = colonist.SkillType;
            this.Text = def.GetText(colonist, otherColonist);
            this.IsImportant = def.IsImportant;
            this.IsUrgent = def.IsUrgent;
            this.FrameDisplayed = CommentaryContext.FrameNumber;
        }
    }
}

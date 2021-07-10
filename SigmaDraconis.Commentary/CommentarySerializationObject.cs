namespace SigmaDraconis.Commentary
{
    using System.Collections.Generic;
    using ProtoBuf;

    [ProtoContract]
    public class CommentarySerializationObject
    {
        [ProtoMember(1)]
        public List<Comment> CommentsHistory { get; set; }

        [ProtoMember(2)]
        public Comment CurrentComment { get; set; }

        [ProtoMember(3)]
        public Dictionary<int, long> DefinitionFramesLastUsed { get; set; }

        [ProtoMember(4)]
        public long LastUpdateFrame { get; set; }

        [ProtoMember(6)]
        public List<Comment> CommentsQueueNormal { get; set; }

        [ProtoMember(7)]
        public List<Comment> CommentsQueueImportant { get; set; }

        [ProtoMember(8)]
        public List<Comment> CommentsQueueUrgent { get; set; }
    }
}

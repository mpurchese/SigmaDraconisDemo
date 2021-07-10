namespace SigmaDraconis.CheckList
{
    using System.Collections.Generic;
    using ProtoBuf;

    [ProtoContract]
    public class CheckListSerializationObject
    {
        [ProtoMember(1)]
        public long LastUpdateFrame { get; set; }

        [ProtoMember(2)]
        public List<int> ItemsStarted { get; set; }

        [ProtoMember(3)]
        public List<int> ItemsCompleted { get; set; }

        [ProtoMember(4)]
        public List<int> ItemsRead { get; set; }

        [ProtoMember(5)]
        public int ActiveItemId { get; set; }
    }
}

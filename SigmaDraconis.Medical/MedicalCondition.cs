namespace SigmaDraconis.Medical
{
    using ProtoBuf;

    [ProtoContract]
    public class MedicalCondition
    {
        [ProtoMember(1)]
        public MedicalConditionType Type { get; }

        [ProtoMember(3)]
        public double Severity { get; set; }

        protected MedicalCondition() { }

        public MedicalCondition(MedicalConditionType type)
        {
            this.Type = type;
            this.Severity = 0;
        }
    }
}

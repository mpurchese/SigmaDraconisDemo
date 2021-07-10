namespace SigmaDraconis.Shared
{
    public class ColonistEvent
    {
        public ColonistEvent(ColonistEventType eventType, int colonistId, int? otherColonistId)
        {
            this.EventType = eventType;
            this.ColonistId = colonistId;
            this.OtherColonistId = otherColonistId;
        }

        public int ColonistId { get; set; }
        public int? OtherColonistId { get; set; }
        public ColonistEventType EventType { get; set; }
    }
}

namespace SigmaDraconis.Shared
{
    public class WarningMessage
    {
        public WarningMessage(string message, WarningType type)
        {
            this.Message = message;
            this.Type = type;
        }

        public string Message { get; set; }
        public WarningType Type { get; set; }
    }
}

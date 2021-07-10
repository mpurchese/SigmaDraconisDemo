namespace SigmaDraconis.Shared
{
    using Draconis.Shared;

    public class RoutePlanSection
    {
        public Vector2f Start { get; set; }
        public Vector2f End { get; set; }

        public RoutePlanSection(Vector2f start, Vector2f end)
        {
            this.Start = start;
            this.End = end;
        }

        public float Length()
        {
            return (this.End - this.Start).Length();
        }
    }
}

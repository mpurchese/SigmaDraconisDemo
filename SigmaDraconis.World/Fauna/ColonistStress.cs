namespace SigmaDraconis.World.Fauna
{
    using Draconis.Shared;
    using System.Diagnostics.CodeAnalysis;
    using ProtoBuf;
    using Shared;

    [ProtoContract]
    internal class ColonistStress
    {
        [ProtoMember(8)]
        public double Value { get; private set; }

        [ProtoMember(9)]
        public double RateOfChange { get; private set; }

        [ProtoMember(10)]
        private int currentRating;   // Old way 0 - 120, 100 = 14 hour work day.  Now use DisplayRating (0 - 100) for display.

        [ProtoMember(11)]
        public StressLevel CurrentLevel { get; private set; }

        [ProtoMember(12)]
        public int FramesSinceWaking { get; private set; }

        [ProtoAfterDeserialization]
        [SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Called by ProtoBuf")]
        private void AfterDeserialization()
        {
            if (this.currentRating != 0) this.Value = (this.currentRating / 1.2).Clamp(0.0, 100.0);
            this.currentRating = 0;
        }

        public void Update(bool isAwake, bool isWorking, bool isWorkaholic, bool isDrinkingKek, double stressRateModifier)
        {
            if (isAwake && isWorking)
            {
                this.RateOfChange = (stressRateModifier + (isWorkaholic ? Constants.ColonistStressRateWorkingWorkaholic : Constants.ColonistStressRateWorking)) / 3600.0;
                this.FramesSinceWaking++;
            }
            else if (isAwake)
            {
                this.RateOfChange = (stressRateModifier + (isDrinkingKek ? Constants.ColonistStressRateDrinkingKek : Constants.ColonistStressRateNotWorking)) / 3600.0;
                this.FramesSinceWaking++;
            }
            else
            {
                this.RateOfChange = 0;
                this.FramesSinceWaking = 0;
                return;
            }

            this.RateOfChange = this.RateOfChange.Clamp(0.0 - this.Value, 100.0 - this.Value);
            this.Value += this.RateOfChange;

            var level = StressLevel.Low;
            if (this.Value >= 90) level = StressLevel.Extreme;
            else if (this.Value >= 80) level = StressLevel.High;
            else if (this.Value >= 60) level = StressLevel.Moderate;

            this.CurrentLevel = level;
        }
    }
}

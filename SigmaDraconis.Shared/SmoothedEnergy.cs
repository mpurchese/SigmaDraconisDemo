namespace SigmaDraconis.Shared
{
    using System.Collections.Generic;
    using ProtoBuf;

    [ProtoContract]
    public class SmoothedEnergy
    {
        // For smoothed display
        private Queue<Energy> prevValues;
        private long prevValuesTotal;
        private long prevValuesAverage;

        public double KWh
        {
            get
            {
                return this.prevValuesAverage / 3600000.0;
            }
        }

        public SmoothedEnergy()
        {
            this.prevValues = new Queue<Energy>();
        }

        public void SetValue(long joules)
        {
            this.prevValues.Enqueue(joules);
            this.prevValuesTotal += joules;
            if (this.prevValues.Count > 60)
            {
                this.prevValuesTotal -= this.prevValues.Dequeue();
            }

            this.prevValuesAverage = this.prevValuesTotal / this.prevValues.Count;
        }

        public override string ToString()
        {
            if (this.KWh >= 1.0)
            {
                return $"{this.KWh:F1} kW";
            }
            else
            {
                return $"{this.KWh * 1000:F0} W";
            }
        }
    }
}

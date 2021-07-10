namespace SigmaDraconis.Shared
{
    using ProtoBuf;

    [ProtoContract]
    public class WorldTime
    {
        public const int TimeCompressionFactor = 60;
        public const int MillisecondsPerUpdate = 1000;
        public const int HoursInDay = 192;
        public const int MinutesInHour = 60;
        public const int SecondsInMinute = 60;

        [ProtoMember(1)]
        public int Day { get; private set; }

        [ProtoMember(2)]
        public int Hour { get; private set; }

        [ProtoMember(3)]
        public int Minute { get; private set; }

        [ProtoMember(4)]
        public int Second { get; private set; }

        [ProtoMember(5)]
        public int Millisecond { get; private set; }

        [ProtoMember(6)]
        public long TotalSeconds { get; private set; }

        [ProtoMember(7)]
        public long FrameNumber { get; private set; }

        [ProtoMember(8)]
        public int TotalHoursPassed { get; private set; }

        public float DayFraction { get; private set; }

        public WorldTime()
        {
        }

        [ProtoAfterDeserialization]
        public void UpdateDayFraction()
        {
            this.DayFraction = (this.Hour + (this.Minute / (float)MinutesInHour)) / HoursInDay;
        }

        /// <summary>
        /// Returns seconds (frames) between two times
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static long operator -(WorldTime left, WorldTime right)
        {
            return (HoursInDay * MinutesInHour * SecondsInMinute * (left.Day - right.Day))
                + (MinutesInHour * SecondsInMinute * (left.Hour - right.Hour))
                + (SecondsInMinute * (left.Minute - right.Minute))
                + (left.Second - right.Second);
        }

        public void Reset()
        {
            
            this.Day = 1;
            this.Hour = Constants.StartHour;
            this.Minute = 0;
            this.Second = 0;
            this.Millisecond = 0;
            this.TotalSeconds = 0;
            this.FrameNumber = 0;
            this.TotalHoursPassed = 0;
            this.UpdateDayFraction();
        }

        public void Increment()
        {
            var secondUpdated = 0;

            this.Millisecond += MillisecondsPerUpdate;
            this.FrameNumber++;
            while (this.Millisecond > 1000)
            {
                this.Millisecond -= 1000;
                this.Second++;
                this.TotalSeconds++;
                secondUpdated++;
            }

            while (this.Second > SecondsInMinute)
            {
                this.Second -= SecondsInMinute;
                this.Minute++;
            }

            while (this.Minute > MinutesInHour)
            {
                this.Minute -= MinutesInHour;
                this.Hour++;
                this.TotalHoursPassed++;
            }

            while (this.Hour > HoursInDay)
            {
                this.Hour -= HoursInDay;
                this.Day += 1;
            }

            this.UpdateDayFraction();

            for (int i = 0; i < secondUpdated; ++i) EventManager.RaiseEvent(EventType.Timer1Second, null);
        }

        public WorldTime Clone()
        {
            return new WorldTime
            {
                Day = this.Day,
                Hour = this.Hour,
                Minute = this.Minute,
                Second = this.Second,
                Millisecond = this.Millisecond,
                DayFraction = this.DayFraction,
                FrameNumber = this.FrameNumber,
                TotalHoursPassed = this.TotalHoursPassed,
                TotalSeconds = this.TotalSeconds
            };
        }

        public override string ToString()
        {
            return $"D{this.Day:D3} {this.Hour:D3}:{this.Minute:D2}:{this.Second:D2}";
        }
    }
}
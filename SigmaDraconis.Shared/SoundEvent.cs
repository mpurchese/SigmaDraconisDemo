namespace SigmaDraconis.Shared
{
    public class SoundEvent
    {
        public static SoundEvent CreateAddEvent(int thingId, string soundName, bool isRepeat)
        {
            return new SoundEvent { SoundEventType = SoundEventType.Add, ThingId = thingId, SoundName = soundName, IsRepeat = isRepeat };
        }

        public static SoundEvent CreateUpdateEvent(int thingId, bool isPaused, float volume, float altitude, bool updatePosition, float pitch)
        {
            return new SoundEvent { SoundEventType = SoundEventType.Update, ThingId = thingId, IsPaused = isPaused, Volume = volume, Altitude = altitude, UpdatePosition = updatePosition, Pitch = pitch };
        }

        public static SoundEvent CreateRemoveEvent(int thingId)
        {
            return new SoundEvent { SoundEventType = SoundEventType.Remove, ThingId = thingId };
        }

        public int ThingId { get; set; }
        public SoundEventType SoundEventType { get; set; }
        public string SoundName { get; set; }
        public bool IsPaused { get; set; }
        public bool IsRepeat { get; set; }
        public float Volume { get; set; }
        public float Pitch { get; set; }
        public float Altitude { get; set; }
        public bool UpdatePosition { get; set; }
    }
}

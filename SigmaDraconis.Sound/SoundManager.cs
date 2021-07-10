namespace SigmaDraconis.Sound
{
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Draconis.UI;
    using Shared;
    using World;

    public static class SoundManager
    {
        
        private static float globalFade = 1f;

        private static readonly Dictionary<int, SoundInstance> soundInstancesByThing = new Dictionary<int, SoundInstance>();

        private static Vector2f screenScrollPos = new Vector2f();
        private static float screenZoom;
        private static float screenWidth;
        private static float screenHeight;

        public static float GlobalVolume { get; set; } = 1f;

        public static void Reset()
        {
            foreach (var id in soundInstancesByThing.Keys.ToList()) Remove(id);
            soundInstancesByThing.Clear();
            globalFade = 0f;
        }

        public static void Update(bool isPaused, Vector2f scrollPos, float zoom, float width, float height)
        {
            if (width == 0 || height == 0) return;

            screenScrollPos = scrollPos;
            screenZoom = zoom;
            screenWidth = width;
            screenHeight = height;

            while (EventManager.HasSoundEvent)
            {
                var e = EventManager.DequeueSoundEvent();
                switch(e.SoundEventType)
                {
                    case SoundEventType.Add:
                        Add(e.ThingId, e.SoundName, e.IsRepeat);
                        break;
                    case SoundEventType.Update:
                        if (e.UpdatePosition) UpdatePosition(e.ThingId);
                        UpdateInstanceParams(e.ThingId, e.IsPaused, e.Volume, e.Altitude, e.Pitch);
                        break;
                    case (SoundEventType.Remove):
                        Remove(e.ThingId);
                        break;
                }
            }

            if (isPaused && globalFade > 0f) globalFade -= 0.02f;
            else if (!isPaused && globalFade < 1f) globalFade += 0.02f;

            var zoomEffect = zoom / 16f;
            foreach (var instance in soundInstancesByThing.Values)
            {
                instance.UpdateEffect(globalFade * GlobalVolume * zoomEffect);
            }
        }

        public static void Add(int thingId, string soundName, bool repeat)
        {
            // Get terrain position
            var thing = World.GetThing(thingId);
            if (thing == null || thing.MainTile == null) return;

            if (soundInstancesByThing.ContainsKey(thingId))
            {
                // Only one sound per thing for now, but the position may have changed
                soundInstancesByThing[thingId].WorldPos = thing.MainTile.CentrePosition;
                return;   
            }

            var si = new SoundInstance(thing.MainTile.CentrePosition, soundName, repeat);
            soundInstancesByThing.Add(thingId, si);
        }

        public static void UpdatePosition(int thingId)
        {
            if (!soundInstancesByThing.ContainsKey(thingId)) return;   // Only one sound per thing for now

            // Get terrain position
            var thing = World.GetThing(thingId);
            if (thing == null || thing.MainTile == null) return;

            soundInstancesByThing[thingId].WorldPos = thing.MainTile.CentrePosition;
        }

        public static void Remove(int thingId)
        {
            if (!soundInstancesByThing.ContainsKey(thingId)) return;

            var si = soundInstancesByThing[thingId];
            si.Stop();
            soundInstancesByThing.Remove(thingId);
        }

        public static void UpdateInstanceParams(int thingId, bool isPaused, float volume, float altitude, float pitch)
        {
            if (!soundInstancesByThing.ContainsKey(thingId)) return;

            var instance = soundInstancesByThing[thingId];
            if (isPaused)
            {
                instance.UpdateParams(0f, 0f, 0f);
                return;
            }

            var x = instance.WorldPos.X;
            var y = instance.WorldPos.Y - altitude;
            var screenPos = CoordinateHelper.GetScreenPosition(UIStatics.Graphics, screenScrollPos, screenZoom, x, y);
            var fracX = screenPos.X / screenWidth;
            var fracY = screenPos.Y / screenHeight;

            // Tail off volume if just off screen
            var volMod = 0f;
            if (fracX > -0.1f && fracY > -0.1f && fracX < 1.1f && fracY < 1.1f)
            {
                volMod = 1f;

                if (fracX < 0.1f) volMod *= 5f * (fracX + 0.1f);
                else if (fracX > 0.9f) volMod *= 5f * (1.1f - fracX);

                if (fracY < 0.1f) volMod *= 5f * (fracY + 0.1f);
                else if (fracY > 0.9f) volMod *= 5f * (1.1f - fracY);
            }

            volume *= volMod;
            instance.UpdateParams(volume, volume > 0 ? (2f * (fracX - 0.5f)).Clamp(-1f, 1f) : 0f, pitch);
        }
    }
}

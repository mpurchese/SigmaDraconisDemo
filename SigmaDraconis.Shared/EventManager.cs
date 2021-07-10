namespace SigmaDraconis.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;

    public class EventManager
    {
        private static readonly Queue<PropertyChangeEvent> worldPropertyChangeEvents = new Queue<PropertyChangeEvent>();
        private static readonly Queue<RoomLightChangeEvent> roomLightChangeEvents = new Queue<RoomLightChangeEvent>();
        private static readonly Queue<ColonistEvent> colonistEvents = new Queue<ColonistEvent>();
        private static readonly Queue<SoundEvent> soundEvents = new Queue<SoundEvent>();

        public static void EnqueueWorldPropertyChangeEvent(int thingId, string propertyName, object previousValue, object newValue, int? terrainRow = null, ThingType? thingType = null)
        {
            worldPropertyChangeEvents.Enqueue(new PropertyChangeEvent(thingId, propertyName, previousValue, newValue, terrainRow, thingType));
        }

        public static void EnqueueWorldPropertyChangeEvent(int thingId, string propertyName, int? terrainRow = null, ThingType? thingType = null)
        {
            worldPropertyChangeEvents.Enqueue(new PropertyChangeEvent(thingId, propertyName, null, null, terrainRow, thingType));
        }

        public static bool HasWorldPropertyChangeEvent => worldPropertyChangeEvents.Any();
        public static PropertyChangeEvent DequeueWorldPropertyChangeEvent()
        {
            return worldPropertyChangeEvents.Dequeue();
        }

        public static IEnumerable<PropertyChangeEvent> WorldPropertyChangeEvents => worldPropertyChangeEvents.AsEnumerable();

        public static void EnqueueColonistEvent(ColonistEventType eventType, int colonistId, int? otherColonistId = null)
        {
            colonistEvents.Enqueue(new ColonistEvent(eventType, colonistId, otherColonistId));
        }

        public static void EnqueueRoomLightChangeEvent(int terrainRow, ThingType thingType)
        {
            roomLightChangeEvents.Enqueue(new RoomLightChangeEvent(terrainRow, thingType));
        }

        public static void EnqueueSoundAddEvent(int thingId, string soundName, bool isRepeat = true)
        {
            soundEvents.Enqueue(SoundEvent.CreateAddEvent(thingId, soundName, isRepeat));
        }

        public static void EnqueueSoundUpdateEvent(int thingId, bool isPaused, float volume, float altitude = 0f, bool updatePosition = false, float pitch = 0f)
        {
            soundEvents.Enqueue(SoundEvent.CreateUpdateEvent(thingId, isPaused, volume, altitude, updatePosition, pitch));
        }

        public static void EnqueueSoundRemoveEvent(int thingId)
        {
            soundEvents.Enqueue(SoundEvent.CreateRemoveEvent(thingId));
        }

        public static bool HasSoundEvent => soundEvents.Any();
        public static SoundEvent DequeueSoundEvent()
        {
            return soundEvents.Dequeue();
        }

        public static bool HasRoomLightChangeEvent => roomLightChangeEvents.Any();
        public static RoomLightChangeEvent DequeueRoomLightChangeEvent()
        {
            return roomLightChangeEvents.Dequeue();
        }

        public static bool HasColonistEvent => colonistEvents.Any();
        public static ColonistEvent DequeueColonistEvent()
        {
            return colonistEvents.Dequeue();
        }

        public static bool IsTemperatureOverlayInvalidated { get; set; }

        #region Old System
        private static readonly Dictionary<EventType, Dictionary<object, Action<object>>> actions = new Dictionary<EventType, Dictionary<object, Action<object>>>();
        private static readonly TwoKeyDictionary<EventType, EventSubType, Dictionary<object, Action<object>>> actionsWithSubType = new TwoKeyDictionary<EventType, EventSubType, Dictionary<object, Action<object>>>();

        // Optimisations to reduce events
        public static HashSet<int> MovedAnimals { get; } = new HashSet<int>();
        public static HashSet<int> MovedBugs { get; } = new HashSet<int>();
        public static HashSet<int> MovedFlyingInsects { get; } = new HashSet<int>();
        public static HashSet<int> MovedColonists { get; } = new HashSet<int>();
        public static HashSet<int> InvalidatedRoofRendererRows { get; } = new HashSet<int>();

        public static void Init()
        {
            if (actions.Any()) return;

            foreach (var eventType in Enum.GetValues(typeof(EventType)).Cast<EventType>())
            {
                actions.Add(eventType, new Dictionary<object, Action<object>>());
                foreach (var subType in Enum.GetValues(typeof(EventSubType)).Cast<EventSubType>())
                {
                    actionsWithSubType.Add(eventType, subType, new Dictionary<object, Action<object>>());
                }
            }
        }

        public static void RaiseEvent(EventType eventType, object obj)
        {
            foreach (var action in actions[eventType].Values)
            {
                action.Invoke(obj);
            }
        }

        public static void RaiseEvent(EventType eventType, EventSubType subType, object obj)
        {
            foreach (var action in actions[eventType].Values.Union(actionsWithSubType[eventType, subType].Values).ToList())
            {
                action.Invoke(obj);
            }
        }

        public static void Subscribe(EventType eventType, Action<object> action)
        {
            Unsubscribe(eventType, action.Target);
            actions[eventType].Add(action.Target, action);
        }

        public static void Subscribe(EventType eventType, EventSubType subType, Action<object> action)
        {
            Unsubscribe(eventType, subType, action.Target);
            actionsWithSubType[eventType, subType].Add(action.Target, action);
        }

        public static void Unsubscribe(EventType eventType, object sender)
        {
            if (actions[eventType].ContainsKey(sender))
            {
                actions[eventType].Remove(sender);
            }
        }

        public static void Unsubscribe(EventType eventType, EventSubType subType, object sender)
        {
            if (actionsWithSubType[eventType, subType].ContainsKey(sender))
            {
                actionsWithSubType[eventType, subType].Remove(sender);
            }
        }

        public static void UnsubscribeAll(object sender)
        {
            foreach (var action in actions.Values)
            {
                action.Remove(sender);
            }

            foreach (var action in actionsWithSubType.Values)
            {
                action.Remove(sender);
            }
        }
        #endregion Old System
    }
}

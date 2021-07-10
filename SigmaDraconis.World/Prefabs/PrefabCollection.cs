namespace SigmaDraconis.World.Prefabs
{
    using System.Collections.Generic;
    using ProtoBuf;
    using Shared;

    [ProtoContract]
    public class PrefabCollection
    {
        [ProtoMember(1)]
        private readonly Dictionary<ThingType, int> prefabCountsByType;

        public PrefabCollection()
        {
            if (this.prefabCountsByType == null) this.prefabCountsByType = new Dictionary<ThingType, int>();
        }

        public void Add(ThingType type)
        {
            if (this.prefabCountsByType.ContainsKey(type)) this.prefabCountsByType[type]++;
            else this.prefabCountsByType.Add(type, 1);
        }

        public int Count(ThingType type)
        {
            return this.prefabCountsByType.ContainsKey(type) ? this.prefabCountsByType[type] : 0;
        }

        public void Remove(ThingType type)
        {
            if (this.prefabCountsByType.ContainsKey(type))
            {
                this.prefabCountsByType[type]--;
                if (this.prefabCountsByType[type] <= 0) this.prefabCountsByType.Remove(type);
            }
        }
    }
}

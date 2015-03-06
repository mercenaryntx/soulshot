using System;
using System.Collections;
using System.Linq;

namespace Neurotoxin.Soulshot
{
    public struct EntityKey
    {
        public int Key { get; private set; }

        public EntityKey(object key) : this()
        {
            var enumerable = key as IEnumerable;
            Key = enumerable != null
                ? enumerable.Cast<object>().Aggregate(0x2D2816FE, (current, item) => current*31 + (item == null ? 0 : item.GetHashCode()))
                : (key is int ? (int) key : key.GetHashCode());
        }

        public bool Equals(EntityKey other)
        {
            return Key == other.Key;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is EntityKey && Equals((EntityKey) obj);
        }

        public override int GetHashCode()
        {
            return Key;
        }
    }
}
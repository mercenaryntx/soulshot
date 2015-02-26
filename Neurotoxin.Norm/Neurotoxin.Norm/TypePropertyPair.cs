using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neurotoxin.Norm
{
    public struct TypePropertyPair
    {
        public Type Type { get; private set; }
        public string Property { get; private set; }

        public TypePropertyPair(Type type, string property) : this()
        {
            Type = type;
            Property = property;
        }

        public bool Equals(TypePropertyPair other)
        {
            return Type == other.Type && string.Equals(Property, other.Property);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is TypePropertyPair && Equals((TypePropertyPair) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Type != null ? Type.GetHashCode() : 0)*397) ^ (Property != null ? Property.GetHashCode() : 0);
            }
        }
    }
}

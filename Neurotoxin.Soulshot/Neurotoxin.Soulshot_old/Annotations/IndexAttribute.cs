using System;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot.Annotations
{
    public class IndexAttribute : Attribute
    {
        public IndexType Type { get; private set; }

        public IndexAttribute(IndexType type = IndexType.Default)
        {
            Type = type;
        }
    }
}
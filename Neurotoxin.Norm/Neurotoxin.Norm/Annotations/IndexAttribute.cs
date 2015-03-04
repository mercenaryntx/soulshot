using System;
using Neurotoxin.Norm.Query;

namespace Neurotoxin.Norm.Annotations
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
﻿using System.Reflection.Emit;

namespace Neurotoxin.Soulshot
{
    public struct PropertyFieldPair
    {
        public PropertyBuilder Property { get; set; }
        public FieldBuilder BackingField { get; set; }

        public PropertyFieldPair(PropertyBuilder property, FieldBuilder backingField) : this()
        {
            Property = property;
            BackingField = backingField;
        }
    }
}

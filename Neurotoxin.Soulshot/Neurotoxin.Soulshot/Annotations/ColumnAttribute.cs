using System;

namespace Neurotoxin.Soulshot.Annotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class ColumnAttribute : Attribute
    {
        public string Name { get; set; }
        protected string Type { get; set; }

        protected int? Length { get; set; }
        protected bool MaxLength { get; set; }
        protected int? Scale { get; set; }

        protected ColumnAttribute(string type)
        {
            Type = type;
        }

        public override string ToString()
        {
            if (MaxLength) return $"{Type}(max)";
            if (Length.HasValue && Scale.HasValue) return $"{Type}({Length},{Scale})";
            if (Length.HasValue) return $"{Type}({Length})";
            return Type;
        }
    }
}
using System;

namespace Neurotoxin.Norm.Annotations
{
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class ColumnTypeAttribute : Attribute
    {
        protected string Type { get; set; }

        protected int? Length { get; set; }
        protected bool MaxLength { get; set; }
        protected int? Scale { get; set; }

        protected ColumnTypeAttribute(string type)
        {
            Type = type;
        }

        public override string ToString()
        {
            if (MaxLength) return string.Format("{0}(max)", Type);
            if (Length.HasValue && Scale.HasValue) return string.Format("{0}({1},{2})", Type, Length, Scale);
            if (Length.HasValue) return string.Format("{0}({1})", Type, Length);
            return Type;
        }

    }
}
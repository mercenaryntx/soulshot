namespace Neurotoxin.Soulshot.Annotations
{
    public class NumericAttribute : ColumnAttribute
    {
        public NumericAttribute(int precision) : base("numeric")
        {
            Length = precision;
        }

        public NumericAttribute(int precision, int scale) : this(precision)
        {
            Scale = scale;
        }
    }
}
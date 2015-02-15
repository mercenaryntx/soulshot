namespace Neurotoxin.Norm.Annotations
{
    public class DecimalAttribute : ColumnTypeAttribute
    {
        public DecimalAttribute() : base("decimal")
        {
        }

        public DecimalAttribute(int precision) : this()
        {
            Length = precision;
        }

        public DecimalAttribute(int precision, int scale) : this(precision)
        {
            Scale = scale;
        }
    }
}
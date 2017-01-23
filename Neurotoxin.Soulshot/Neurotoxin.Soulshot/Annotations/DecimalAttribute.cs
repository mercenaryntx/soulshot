namespace Neurotoxin.Soulshot.Annotations
{
    public class DecimalAttribute : ColumnAttribute
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
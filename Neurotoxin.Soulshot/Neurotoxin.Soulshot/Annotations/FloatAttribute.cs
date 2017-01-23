namespace Neurotoxin.Soulshot.Annotations
{
    public class FloatAttribute : ColumnAttribute
    {
        public FloatAttribute() : base("float")
        {
        }

        public FloatAttribute(int mantissa) : this()
        {
            Length = mantissa;
        }
    }
}
namespace Neurotoxin.Soulshot.Annotations
{
    public class FloatAttribute : ColumnTypeAttribute
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
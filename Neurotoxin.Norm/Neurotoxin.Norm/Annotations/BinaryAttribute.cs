namespace Neurotoxin.Norm.Annotations
{
    public class BinaryAttribute : ColumnTypeAttribute
    {
        public BinaryAttribute() : base("binary")
        {
        }

        public BinaryAttribute(int? length) : this()
        {
            Length = length;
        }
    }
}
namespace Neurotoxin.Soulshot.Annotations
{
    public class BinaryAttribute : ColumnAttribute
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
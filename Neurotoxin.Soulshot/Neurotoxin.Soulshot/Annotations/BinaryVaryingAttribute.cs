namespace Neurotoxin.Soulshot.Annotations
{
    public class BinaryVaryingAttribute : ColumnAttribute
    {
        public BinaryVaryingAttribute() : base("varbinary")
        {
        }

        public BinaryVaryingAttribute(int? length) : this()
        {
            Length = length;
        }

        public BinaryVaryingAttribute(bool maxLength) : this()
        {
            MaxLength = maxLength;
        }
    }
}
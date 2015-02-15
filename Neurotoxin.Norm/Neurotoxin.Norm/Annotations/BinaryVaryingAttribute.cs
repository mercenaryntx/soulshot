namespace Neurotoxin.Norm.Annotations
{
    public class BinaryVaryingAttribute : ColumnTypeAttribute
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
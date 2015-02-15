namespace Neurotoxin.Norm.Annotations
{
    public class UnicodeCharacterVaryingAttribute : ColumnTypeAttribute
    {
        public UnicodeCharacterVaryingAttribute() : base("nvarchar")
        {
        }

        public UnicodeCharacterVaryingAttribute(int? length) : this()
        {
            Length = length;
        }

        public UnicodeCharacterVaryingAttribute(bool maxLength) : this()
        {
            MaxLength = maxLength;
        }
    }
}
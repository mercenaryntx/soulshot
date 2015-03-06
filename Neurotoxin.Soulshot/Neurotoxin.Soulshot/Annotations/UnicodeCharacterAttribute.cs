namespace Neurotoxin.Soulshot.Annotations
{
    public class UnicodeCharacterAttribute : ColumnTypeAttribute
    {
        public UnicodeCharacterAttribute() : base("nchar")
        {
        }

        public UnicodeCharacterAttribute(int? length) : this()
        {
            Length = length;
        }
    }
}
namespace Neurotoxin.Soulshot.Annotations
{
    public class UnicodeCharacterAttribute : ColumnAttribute
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
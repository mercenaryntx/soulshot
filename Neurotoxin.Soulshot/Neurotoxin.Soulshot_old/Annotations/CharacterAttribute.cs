namespace Neurotoxin.Soulshot.Annotations
{
    public class CharacterAttribute : ColumnTypeAttribute
    {
        public CharacterAttribute() : base("char")
        {
        }

        public CharacterAttribute(int? length) : this()
        {
            Length = length;
        }
    }
}
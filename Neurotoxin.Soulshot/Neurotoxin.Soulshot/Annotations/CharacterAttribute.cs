namespace Neurotoxin.Soulshot.Annotations
{
    public class CharacterAttribute : ColumnAttribute
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
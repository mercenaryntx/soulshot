namespace Neurotoxin.Soulshot.Annotations
{
    public class CharacterVaryingAttribute : ColumnTypeAttribute
    {
        public CharacterVaryingAttribute() : base("varchar")
        {
        }

        public CharacterVaryingAttribute(int? length) : this()
        {
            Length = length;
        }

        public CharacterVaryingAttribute(bool maxLength) : this()
        {
            MaxLength = maxLength;
        }
    }
}
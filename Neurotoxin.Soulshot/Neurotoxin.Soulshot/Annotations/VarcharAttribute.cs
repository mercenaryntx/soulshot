namespace Neurotoxin.Soulshot.Annotations
{
    public class VarcharAttribute : CharacterVaryingAttribute
    {
        public VarcharAttribute()
        {
        }

        public VarcharAttribute(int? length) : base(length)
        {
        }

        public VarcharAttribute(bool maxLength) : base(maxLength)
        {
        }
    }
}
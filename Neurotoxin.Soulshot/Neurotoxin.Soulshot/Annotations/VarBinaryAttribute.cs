namespace Neurotoxin.Soulshot.Annotations
{
    public class VarbinaryAttribute : BinaryVaryingAttribute
    {
        public VarbinaryAttribute()
        {
        }

        public VarbinaryAttribute(int? length) : base(length)
        {
        }

        public VarbinaryAttribute(bool maxLength) : base(maxLength)
        {
        }
    }
}
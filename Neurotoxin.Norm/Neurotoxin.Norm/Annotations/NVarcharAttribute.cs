namespace Neurotoxin.Norm.Annotations
{
    public class NVarcharAttribute : UnicodeCharacterVaryingAttribute
    {
        public NVarcharAttribute()
        {
        }

        public NVarcharAttribute(int? length) : base(length)
        {
        }

        public NVarcharAttribute(bool maxLength) : base(maxLength)
        {
        }
    }
}
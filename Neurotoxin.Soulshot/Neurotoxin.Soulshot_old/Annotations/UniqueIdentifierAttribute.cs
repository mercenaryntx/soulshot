namespace Neurotoxin.Soulshot.Annotations
{
    public class UniqueIdentifierAttribute : ColumnTypeAttribute
    {
        public UniqueIdentifierAttribute() : base("uniqueidentifier")
        {
        }
    }
}
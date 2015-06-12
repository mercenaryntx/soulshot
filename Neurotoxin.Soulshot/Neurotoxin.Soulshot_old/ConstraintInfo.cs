namespace Neurotoxin.Soulshot
{
    public class ConstraintInfo
    {
        public string TableName { get; set; }
        public string TableSchema { get; set; }
        public string ConstraintName { get; set; }
        public string TargetColumn { get; set; }
        public string SourceColumn { get; set; }
    }
}
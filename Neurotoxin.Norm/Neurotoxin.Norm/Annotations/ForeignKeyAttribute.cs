using System.Reflection;

namespace Neurotoxin.Norm.Annotations
{
    public class ForeignKeyAttribute : ColumnTypeAttribute
    {
        public ForeignKeyAttribute(string type) : base(type) { }
    }

    public class CrossTableReferenceAttribute : ColumnTypeAttribute
    {
        public CrossTableReferenceAttribute() : base("void")
        {
        }
    }
}
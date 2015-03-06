using System.Reflection;

namespace Neurotoxin.Soulshot.Annotations
{
    public class ForeignKeyAttribute : ColumnTypeAttribute
    {
        public IDbSet DbSet { get; private set; }

        public ForeignKeyAttribute(string type, IDbSet dbSet) : base(type)
        {
            DbSet = dbSet;
        }
    }

    public class CrossTableReferenceAttribute : ColumnTypeAttribute
    {
        public CrossTableReferenceAttribute() : base("void")
        {
        }
    }
}
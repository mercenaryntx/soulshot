using System;

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
        public Type EntityType { get; private set; }

        public CrossTableReferenceAttribute(Type entityType) : base(null)
        {
            EntityType = entityType;
        }
    }
}
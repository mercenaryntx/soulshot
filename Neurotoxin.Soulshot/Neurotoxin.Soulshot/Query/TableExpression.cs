using System;
using System.Linq.Expressions;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Query
{
    public class TableExpression : Expression
    {
        public Type EntityType { get; }
        public TableAttribute Table { get; }
        public string Alias { get; }
        public TableHint TableHint { get; set; }

        public override ExpressionType NodeType => ExpressionType.Constant;
        public override Type Type => typeof(bool);

        public TableExpression(Type entityType, TableAttribute table, string @alias = null)
        {
            EntityType = entityType;
            Table = table;
            Alias = alias;
        }

        protected bool Equals(TableExpression other)
        {
            return Equals(Table, other.Table) && string.Equals(Alias, other.Alias);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TableExpression) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Table?.GetHashCode() ?? 0)*397) ^ (Alias?.GetHashCode() ?? 0);
            }
        }
    }
}
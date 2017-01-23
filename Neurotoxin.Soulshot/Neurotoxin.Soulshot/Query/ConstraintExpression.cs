using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class ConstraintExpression : Expression, IHasColumnsExpression
    {
        public Expression Name { get; set; }
        public ConstraintType ConstraintType { get; set; }
        public IndexType IndexType { get; set; }
        public Expression Columns { get; set; }
        public TableExpression ReferenceTable { get; set; }
        public Expression ReferenceColumn { get; set; }

        public ConstraintExpression(string name, ExpressionType nodeType) : base(nodeType, null)
        {
            Name = new ObjectNameExpression(name);
        }

        public void AddName(ObjectNameExpression name)
        {
            Name = Name == null ? (Expression)name : new ListingExpression(Name, name);
        }
    }
}
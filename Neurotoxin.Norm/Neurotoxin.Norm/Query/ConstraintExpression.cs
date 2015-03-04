using System.Linq.Expressions;

namespace Neurotoxin.Norm.Query
{
    public class ConstraintExpression : Expression, IHasColumnsExpression
    {
        public Expression Name { get; set; }
        public ConstraintType Type { get; set; }
        public Expression Columns { get; set; }

        public ConstraintExpression(Expression name, ConstraintType type, ExpressionType nodeType) : base(nodeType, null)
        {
            Name = name;
            Type = type;
        }
    }
}
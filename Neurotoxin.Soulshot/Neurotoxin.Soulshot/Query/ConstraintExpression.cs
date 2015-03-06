using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class ConstraintExpression : Expression, IHasColumnsExpression
    {
        public string Name { get; set; }
        public ConstraintType ConstraintType { get; set; }
        public IndexType IndexType { get; set; }
        public Expression Columns { get; set; }

        public ConstraintExpression(string name, ExpressionType nodeType) : base(nodeType, null)
        {
            Name = name;
        }
    }
}
using System;
using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class WhereExpression : Expression
    {
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }
        public override ExpressionType NodeType => ExpressionType.And;
        public override Type Type => typeof(object);

        public WhereExpression(Expression left, Expression right)
        {
            Left = CheckForShortLogicalExpression(left);
            Right = CheckForShortLogicalExpression(right);
        }

        private Expression CheckForShortLogicalExpression(Expression node)
        {
            var columnExpression = node as ColumnExpression;
            return columnExpression != null ? MakeBinary(ExpressionType.Equal, columnExpression, Constant(true)) : node;
        }

        public override string ToString()
        {
            var right = Right != null ? $" {NodeType} [{Right}]" : string.Empty;
            return $"[{Left}]{right}";
        }
    }
}
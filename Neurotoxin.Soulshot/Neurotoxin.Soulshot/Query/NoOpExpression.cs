using System;
using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class NoOpExpression : Expression
    {
        public override ExpressionType NodeType => ExpressionType.Default;
        public override Type Type { get; }

        public NoOpExpression(Type type)
        {
            Type = type;
        }
    }
}
using System;
using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class ParameterExpression : Expression
    {
        public int Index { get; }
        public override ExpressionType NodeType => ExpressionType.Default;
        public override Type Type { get; }

        public ParameterExpression(int index, Type type)
        {
            Index = index;
            Type = type;
        }
    }
}
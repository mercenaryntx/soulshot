using System;
using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class NestedQueryExpression : Expression
    {
        private ExpressionType _nodeType;
        private Type _type;
        public override ExpressionType NodeType => _nodeType;
        public override Type Type => _type;
        public Expression Select { get; private set; }

        public NestedQueryExpression(Expression node)
        {
            _nodeType = node.NodeType;
            _type = node.Type;
            Select = node;
        }

        public override string ToString()
        {
            return $"{Select}";
        }
    }
}
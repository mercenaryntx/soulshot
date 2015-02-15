using System.Linq.Expressions;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Query
{
    public class TableExpression : Expression
    {
        public TableAttribute Table { get; set; }
        public string Alias { get; set; }

        public TableExpression(TableAttribute table, string @alias = null) : base(ExpressionType.Constant, typeof(bool))
        {
            Table = table;
            Alias = alias;
        }
    }
}
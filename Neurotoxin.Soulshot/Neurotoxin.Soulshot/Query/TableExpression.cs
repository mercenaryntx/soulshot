using System.Linq.Expressions;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Query
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
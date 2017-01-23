using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class AsteriskExpression : Expression
    {
        public TableExpression Table { get; set; }

        public AsteriskExpression()
        {
        }

        public AsteriskExpression(TableExpression table)
        {
            Table = table;
        }
    }
}
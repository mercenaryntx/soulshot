using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class JoinExpression : Expression
    {
        public TableExpression Table { get; set; }
        public JoinType JoinType { get; private set; }
        public Expression Condition { get; set; }

        public JoinExpression(JoinType joinType, TableExpression table = null)
        {
            JoinType = joinType;
            Table = table;
        }
    }
}
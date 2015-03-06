using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class DropTableExpression : Expression
    {
        public TableExpression Table { get; set; }

        public DropTableExpression(TableExpression table)
        {
            Table = table;
        }
    }
}
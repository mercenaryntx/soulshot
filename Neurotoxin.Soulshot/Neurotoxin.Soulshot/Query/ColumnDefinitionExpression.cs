using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class ColumnDefinitionExpression : Expression
    {
        public ColumnInfo Column { get; set; }

        public ColumnDefinitionExpression(ColumnInfo column)
        {
            Column = column;
        }
    }
}
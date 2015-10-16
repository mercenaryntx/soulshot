using System;
using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class ColumnExpression : Expression
    {
        public ObjectNameExpression ColumnName { get; set; }
        public TableExpression Table { get; set; }
        public string As { get; set; }

        public ColumnExpression(string columnName)
        {
            ColumnName = new ObjectNameExpression(columnName);
        }

        public ColumnExpression(string columnName, TableExpression table, Type type) : base(ExpressionType.Constant, type)
        {
            ColumnName = new ObjectNameExpression(columnName);
            Table = table;
        }

        public override string ToString()
        {
            return string.Format("[{0}]", ColumnName.Name);
        }
    }
}
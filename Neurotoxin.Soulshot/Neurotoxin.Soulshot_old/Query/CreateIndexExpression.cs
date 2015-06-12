using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class CreateIndexExpression : Expression, IHasColumnsExpression
    {
        public string Name { get; set; }
        public TableExpression Table { get; set; }
        public Expression Columns { get; set; }
        public IndexType IndexType { get; set; }

        public CreateIndexExpression(string name, TableExpression table)
        {
            Name = name;
            Table = table;
        }
    }
}
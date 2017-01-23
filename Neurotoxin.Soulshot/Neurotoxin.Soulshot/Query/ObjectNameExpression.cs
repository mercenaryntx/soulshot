using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class ObjectNameExpression : Expression
    {
        public string Name { get; set; }

        public ObjectNameExpression(string name)
        {
            Name = name;
        }
    }
}
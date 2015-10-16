namespace Neurotoxin.Soulshot
{
    public class SqlParameter
    {
        public string Name { get; private set; }
        public object Value { get; private set; }

        public SqlParameter(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}
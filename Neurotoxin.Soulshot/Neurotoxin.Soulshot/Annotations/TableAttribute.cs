using System;

namespace Neurotoxin.Soulshot.Annotations
{
    public class TableAttribute : Attribute
    {
        public MappingStrategy MappingStrategy { get; set; }

        public string Name { get; }

        private string _schema;
        public string Schema
        {
            get { return string.IsNullOrEmpty(_schema) ? "dbo" : _schema; }
            set { _schema = value; }
        }

        public string FullName => StringFormat("{0}.{1}");
        public string FullNameWithBrackets => StringFormat("[{0}].[{1}]");

        public TableAttribute(string name, string schema = null)
        {
            if (name.Contains("."))
            {
                var p = name.Split('.');
                name = p[1];
                schema = p[0];
            }
            Name = name;
            Schema = schema;
        }

        private string StringFormat(string format)
        {
            return string.Format(format, Schema, Name);
        }

        public override string ToString()
        {
            return FullNameWithBrackets;
        }

        protected bool Equals(TableAttribute other)
        {
            return base.Equals(other) && 
                   string.Equals(Name, other.Name) && 
                   string.Equals(Schema, other.Schema);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((TableAttribute)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode()*397) ^ (Name?.GetHashCode() ?? 0);
            }
        }
    }
}
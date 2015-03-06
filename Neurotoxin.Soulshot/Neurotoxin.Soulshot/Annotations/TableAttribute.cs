using System;

namespace Neurotoxin.Soulshot.Annotations
{
    public class TableAttribute : Attribute
    {
        public string Name { get; set; }

        private string _schema;

        public string Schema
        {
            get { return string.IsNullOrEmpty(_schema) ? "dbo" : _schema; }
            set { _schema = value; }
        }

        public string FullName
        {
            get { return StringFormat("{0}.{1}"); }
        }

        public string FullNameWithBrackets
        {
            get { return StringFormat("[{0}].[{1}]"); }
        }

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

    }
}
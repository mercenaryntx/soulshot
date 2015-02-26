﻿using System;
using System.Linq.Expressions;

namespace Neurotoxin.Norm.Query
{
    public class ColumnExpression : Expression
    {
        public string ColumnName { get; set; }
        public string Alias { get; set; }

        public ColumnExpression(string columnName, string @alias, Type type) : base(ExpressionType.Constant, type)
        {
            ColumnName = columnName;
            Alias = alias;
        }

    }
}
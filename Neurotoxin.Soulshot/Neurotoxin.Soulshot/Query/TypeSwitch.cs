using System;
using System.Collections.Generic;

namespace Neurotoxin.Soulshot.Query
{
    public class TypeSwitch<TBase>
    {
        private Func<TBase, TBase> _default;
        private readonly Dictionary<Type, Func<TBase, TBase>> _matches = new Dictionary<Type, Func<TBase, TBase>>();

        public TypeSwitch<TBase> Case<T>(Func<T, TBase> func) where T : TBase
        {
            var type = typeof(T);
            _matches.Add(type, x => func((T)x)); return this;
        }

        public TypeSwitch<TBase> Default(Func<TBase, TBase> func)
        {
            _default = func;
            return this;
        }

        public TBase Switch(TBase x)
        {
            if (x != null)
            {
                var type = x.GetType();
                if (_matches.ContainsKey(type)) return _matches[type](x);
            }
            if (_default == null) throw new Exception("Default case is not defined.");
            return _default(x);
        }
    }
}
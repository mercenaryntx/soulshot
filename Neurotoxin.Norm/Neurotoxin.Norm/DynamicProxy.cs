using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using Neurotoxin.Norm.Extensions;

namespace Neurotoxin.Norm
{
    internal class DynamicProxy
    {
        private readonly ModuleBuilder _moduleBuilder;
        private readonly Dictionary<Type, Type> _cache = new Dictionary<Type, Type>(); 

        private static DynamicProxy _instance;
        internal static DynamicProxy Instance
        {
            get { return _instance ?? (_instance = new DynamicProxy("FooBar")); }
        }

        internal DynamicProxy(string assemblyName)
        {
            var name = new AssemblyName(assemblyName);
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
            _moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName, string.Format("{0}.dll", assemblyName));
        }

        internal Type GetProxyType(Type baseType)
        {
            if (_cache.ContainsKey(baseType)) return _cache[baseType];
            var typeBuilder = CreateTypeBuilder(baseType);
            var dirtyProperties = typeBuilder.CreateField<HashSet<string>>("_dirtyProperties");
            var state = typeBuilder.CreateProperty<EntityState>("State", f => StateGetter(typeBuilder, f, dirtyProperties), f => StateSetter(typeBuilder, f));

            foreach (var property in baseType.GetProperties())
            {
                var getter = PropertyGetter(typeBuilder, property);
                var setter = PropertySetter(typeBuilder, property, dirtyProperties);
                typeBuilder.CreateProperty(property.PropertyType, property.Name, f => getter, f => setter);
            }

            typeBuilder.CreateDefaultConstructor(il =>
            {
                il.SetFieldDefault(dirtyProperties);
                il.Emit(OpCodes.Ret);
            });
            var proxyType = typeBuilder.CreateType();
            _cache[baseType] = proxyType;
            return proxyType;
        }

        internal TBase Create<TBase>() //where TBase : class
        {
            var proxyType = GetProxyType(typeof(TBase));
            return (TBase)Activator.CreateInstance(proxyType);
        }

        internal TBase Wrap<TBase>(TBase wrappee) //where TBase : class
        {
            var baseType = typeof(TBase);
            var proxy = Create<TBase>();
            foreach (var property in baseType.GetProperties())
            {
                if (!property.CanWrite) continue;
                property.SetValue(proxy, property.GetValue(wrappee));
            }
            return proxy;
        }

        private TypeBuilder CreateTypeBuilder(Type baseType)
        {
            var typeBuilder = _moduleBuilder.DefineType(string.Format("{0}_Proxy", baseType.FullName), TypeAttributes.Class | TypeAttributes.Public, baseType, new[] { typeof(IProxy) });

            if (baseType.IsGenericType)
            {
                var genericArguments = baseType.GetGenericArguments();
                var genericArgumentNames = genericArguments.Select(ga => ga.Name).ToArray();
                var genericTypeParameterBuilder = typeBuilder.DefineGenericParameters(genericArgumentNames);
                typeBuilder.MakeGenericType(genericTypeParameterBuilder);
            }

            return typeBuilder;
        }

        private MethodBuilder PropertyGetter(TypeBuilder typeBuilder, PropertyInfo property)
        {
            var baseGetter = property.GetGetMethod(true);
            if (baseGetter == null) return null;

            const MethodAttributes methodAttributes = MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;
            var methodAccess = baseGetter.Attributes & MethodAttributes.MemberAccessMask;
            var getter = typeBuilder.DefineMethod("get_" + property.Name, methodAccess | methodAttributes, property.PropertyType, Type.EmptyTypes);
            var il = getter.GetILGenerator();

            //Generates the code of return base.get_Property();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, baseGetter);
            il.Emit(OpCodes.Ret);
            return getter;
        }

        private MethodBuilder PropertySetter(TypeBuilder typeBuilder, PropertyInfo property, FieldBuilder dirtyProperties)
        {
            var baseSetter = property.GetSetMethod(true);
            if (baseSetter == null) return null;

            const MethodAttributes methodAttributes = MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;
            var methodAccess = baseSetter.Attributes & MethodAttributes.MemberAccessMask;
            var setter = typeBuilder.DefineMethod("set_" + property.Name, methodAccess | methodAttributes, typeof(void), new[] { property.PropertyType });
            var il = setter.GetILGenerator();

            //Generates the code of base.set_Property(value); _dirtyProperties.Add("Property");
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, baseSetter);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, dirtyProperties);
            il.Emit(OpCodes.Ldstr, property.Name);
            il.Emit(OpCodes.Callvirt, dirtyProperties.FieldType.GetMethod("Add"));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);

            return setter;
        }

        private MethodBuilder StateGetter(TypeBuilder typeBuilder, FieldBuilder field, FieldBuilder dirtyProperties)
        {
            var getter = typeBuilder.DefineMethod("get_State", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual, typeof(EntityState), Type.EmptyTypes);
            typeBuilder.DefineMethodOverride(getter, typeof(IProxy).GetMethod("get_State"));
            var il = getter.GetILGenerator();

            var changed = il.DefineLabel();
            var endOfBlock = il.DefineLabel();

            //Generates the code of return _state == EntityState.Unchanged && _dirtyProperties.Count > 0 ? EntityState.Changed : _state;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Brtrue, endOfBlock);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, dirtyProperties);
            il.Emit(OpCodes.Callvirt, dirtyProperties.FieldType.GetMethod("get_Count"));
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Bgt_S, changed);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Br_S, endOfBlock);
            il.MarkLabel(changed);
            il.Emit(OpCodes.Ldc_I4, (int)EntityState.Changed);
            il.MarkLabel(endOfBlock);
            il.Emit(OpCodes.Ret);

            //il.Emit(OpCodes.Ldarg_0);
            //il.Emit(OpCodes.Ldfld, field);
            //il.Emit(OpCodes.Ret);

            return getter;
        }

        private MethodBuilder StateSetter(TypeBuilder typeBuilder, FieldBuilder field)
        {
            var setter = typeBuilder.DefineMethod("set_State", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual, typeof(void), new[] {typeof(EntityState)});
            typeBuilder.DefineMethodOverride(setter, typeof(IProxy).GetMethod("set_State"));
            var il = setter.GetILGenerator();

            //Generates the code of this._state = value;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);

            return setter;
        }
    }
}
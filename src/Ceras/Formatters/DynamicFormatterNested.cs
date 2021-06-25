
// ReSharper disable ArgumentsStyleOther
// ReSharper disable ArgumentsStyleNamedExpression
namespace Ceras.Formatters
{
	using Helpers;
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using static System.Linq.Expressions.Expression;

	public class DynamicFormatterNested
	{
		static ConcurrentDictionary<string, FieldInfo> fields = new ConcurrentDictionary<string, FieldInfo>();

		public static void Inject()
		{
			DynamicFormatter.SerializerNestedInjector = InjectToSerializer;
			DynamicFormatter.DeserializerNestedInjector = InjectToDeserializer;
		}

		static Expression HasInnerFieldData(ParameterExpression value, MemberInfo pi)
		{
			var key = pi.ToString();
			var field = fields.GetOrAdd(key, _ => GetInnerFieldInfo(value.Type, pi));
			if (field == null)
				return Expression.Constant(true);
			return Expression.NotEqual(Expression.MakeMemberAccess(value, field), Expression.Constant(null, typeof(object)));
		}

		static FieldInfo GetInnerFieldInfo(Type type, MemberInfo pi)
		{
			var fieldName = "_" + pi.Name.Substring(0, 1).ToLower() + pi.Name.Substring(1);
			var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
			return field;
		}

		internal static void InjectToSerializer(List<Expression> body, SchemaMember member, ParameterExpression valueArg, MethodCallExpression serializeCall, MethodCallExpression serializeNullCall)
		{
			if (RequireNestedCheck(member))
			{
				var checkNested = HasInnerFieldData(valueArg, member.MemberInfo);
				body.Add(IfThenElse(checkNested, serializeCall, serializeNullCall));
			}
			else
				body.Add(serializeCall);
		}

		static bool RequireNestedCheck(SchemaMember member)
		{
			var propertyInfo = member.MemberInfo as PropertyInfo;

			var memberType = GetTypeArgument(member.MemberType);

			return propertyInfo != null && !memberType.FullName.StartsWith("System") && !memberType.FullName.StartsWith("Microsoft");
		}

		static Type GetTypeArgument(Type memberType)
		{
			if (memberType.IsGenericType)
				memberType = memberType.GetGenericArguments()[0];
			return memberType;
		}

		internal static void InjectToDeserializer(List<Expression> body, SchemaMember m, ParameterExpression refValueArg, ParameterExpression local)
		{
			if (RequireNestedCheck(m))
			{
				var checkNested = HasInnerFieldData(refValueArg, m.MemberInfo);
				body.Add(IfThenElse(checkNested
					, Assign(local, MakeMemberAccess(refValueArg, m.MemberInfo))
					, Assign(local, Constant(GetDefaultValue(m.MemberType), m.MemberType))));
			}
			else
				body.Add(Assign(local, MakeMemberAccess(refValueArg, m.MemberInfo)));

		}

		static object GetDefaultValue(Type t)
		{
			if (t.IsValueType)
				return Activator.CreateInstance(t);

			return null;
		}

	}
}

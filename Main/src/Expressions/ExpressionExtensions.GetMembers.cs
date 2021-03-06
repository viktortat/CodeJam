﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

namespace CodeJam.Expressions
{
	using Reflection;

	/// <summary>
	/// <see cref="Expression"/> Extensions.
	/// </summary>
	[PublicAPI]
	public static partial class ExpressionExtensions
	{
		/// <summary>
		/// Gets the <see cref="MemberInfo"/>.
		/// </summary>
		/// <param name="expression">The expression to analyze.</param>
		/// <returns>
		/// The <see cref="MemberInfo"/> instance.
		/// </returns>
		[NotNull, Pure]
		public static MemberExpression GetMemberExpression(this LambdaExpression expression)
		{
			var body = expression.Body;
			return body is UnaryExpression unary
				? (MemberExpression)unary.Operand
				: (MemberExpression)body;
		}

		/// <summary>
		/// Gets the <see cref="MemberInfo"/>.
		/// </summary>
		/// <param name="expression">The expression to analyze.</param>
		/// <returns>
		/// The <see cref="MemberInfo"/> instance.
		/// </returns>
		[NotNull, Pure]
		public static MemberExpression GetMemberExpression(this Expression expression)
		{
			var body = expression is LambdaExpression lambda
				? lambda.Body
				: expression;

			return body is UnaryExpression unary
				? (MemberExpression)unary.Operand
				: (MemberExpression)body;
		}

		/// <summary>
		/// Gets the <see cref="MemberInfo"/>.
		/// </summary>
		/// <param name="expression">The expression to analyze.</param>
		/// <returns>
		/// The <see cref="MemberInfo"/> instance.
		/// </returns>
		[NotNull, Pure]
		public static MemberInfo GetMemberInfo([NotNull] this LambdaExpression expression)
		{
			Code.NotNull(expression, nameof(expression));

			var body = expression.Body;
			if (body is UnaryExpression unary)
				body = unary.Operand;

			switch (body.NodeType)
			{
				case ExpressionType.MemberAccess:
					return ((MemberExpression)body).Member;
				case ExpressionType.Call:
					return ((MethodCallExpression)body).Method;
				default:
					return ((NewExpression)body).Constructor;
			}
		}

		/// <summary>
		/// Returns the property.
		/// </summary>
		/// <param name="expression">The expression to analyze.</param>
		/// <returns>
		/// The <see cref="PropertyInfo"/> instance.
		/// </returns>
		[NotNull, Pure]
		public static PropertyInfo GetProperty([NotNull] this LambdaExpression expression) =>
			(PropertyInfo)GetMemberExpression(expression).Member;

		/// <summary>
		/// Returns the field.
		/// </summary>
		/// <param name="expression">The expression to analyze.</param>
		/// <returns>
		/// The <see cref="FieldInfo"/> instance.
		/// </returns>
		[NotNull, Pure]
		public static FieldInfo GetField([NotNull] this LambdaExpression expression) =>
			(FieldInfo)GetMemberExpression(expression).Member;

		/// <summary>
		/// Returns the constructor.
		/// </summary>
		/// <param name="expression">The expression to analyze.</param>
		/// <returns>
		/// The <see cref="ConstructorInfo"/> instance.
		/// </returns>
		[NotNull, Pure]
		public static ConstructorInfo GetConstructor([NotNull] this LambdaExpression expression) =>
			(ConstructorInfo)GetMemberInfo(expression);

		/// <summary>
		/// Returns the method.
		/// </summary>
		/// <param name="expression">The expression to analyze.</param>
		/// <returns>
		/// The <see cref="MethodInfo"/> instance.
		/// </returns>
		[NotNull, Pure]
		public static MethodInfo GetMethod([NotNull] this LambdaExpression expression)
		{
			var info = GetMemberInfo(expression);
			var propertyInfo = info as PropertyInfo;
			return
				propertyInfo != null
					? propertyInfo.GetGetMethod(true)
					: (MethodInfo)info;
		}

		/// <summary>
		/// Returns a name of the property.
		/// </summary>
		/// <param name="expression">The expression to analyze.</param>
		/// <returns>
		/// A name of the property.
		/// </returns>
		[NotNull, Pure]
		public static string GetPropertyName([NotNull] this LambdaExpression expression) =>
			GetMemberExpression(expression).Member.Name;

		/// <summary>
		/// Returns a composited name of the property.
		/// </summary>
		/// <param name="expression">The expression to analyze.</param>
		/// <returns>
		/// A composited name of the property.
		/// </returns>
		[NotNull, Pure]
		public static string GetFullPropertyName([NotNull] this LambdaExpression expression) =>
			GetFullPropertyNameImpl(GetMemberExpression(expression));

		/// <summary>
		/// Returns a name of the method.
		/// </summary>
		/// <param name="expression">The expression to analyze.</param>
		/// <returns>
		/// A name of the method.
		/// </returns>
		[NotNull, Pure]
		public static string GetMethodName([NotNull] this LambdaExpression expression) =>
			GetMethod(expression).Name;

		private static string GetFullPropertyNameImpl(MemberExpression expression)
		{
			var name = expression.Member.Name;
			while ((expression = expression.Expression as MemberExpression) != null)
				name = expression.Member.Name + "." + name;

			return name;
		}

		/// <summary>
		/// Gets the <see cref="MemberInfo"/>.
		/// </summary>
		/// <param name="expression">The expression to analyze.</param>
		/// <returns>
		/// The <see cref="MemberInfo"/> instance.
		/// </returns>
		[NotNull, Pure]
		public static MemberInfo[] GetMembersInfo([NotNull] this LambdaExpression expression)
		{
			Code.NotNull(expression, nameof(expression));

			var body = expression.Body;
			if (body is UnaryExpression unary)
				body = unary.Operand;

			return GetMembers(body).Reverse().ToArray();
		}

		private static IEnumerable<MemberInfo> GetMembers(Expression expression, bool passIndexer = true)
		{
			MemberInfo lastMember = null;

			for (; ; )
			{
				switch (expression.NodeType)
				{
					case ExpressionType.Parameter:
						if (lastMember == null)
							goto default;
						yield break;

					case ExpressionType.Call:
						{
							if (lastMember == null)
								goto default;

							var cexpr = (MethodCallExpression)expression;
							var expr = cexpr.Object;

							if (expr == null)
							{
								if (cexpr.Arguments.Count == 0)
									goto default;

								expr = cexpr.Arguments[0];
							}

							if (expr.NodeType != ExpressionType.MemberAccess)
								goto default;

							var member = ((MemberExpression)expr).Member;
							var mtype = member.GetMemberType();

							if (lastMember.ReflectedType != mtype.GetItemType())
								goto default;

							expression = expr;

							break;
						}

					case ExpressionType.MemberAccess:
						{
							var mexpr = (MemberExpression)expression;
							var member = lastMember = mexpr.Member;

							yield return member;

							expression = mexpr.Expression;

							break;
						}

					case ExpressionType.ArrayIndex:
						{
							if (passIndexer)
							{
								expression = ((BinaryExpression)expression).Left;
								break;
							}

							goto default;
						}

					default:
						throw new InvalidOperationException($"Expression '{expression}' is not an association.");
				}
			}
		}


	}
}
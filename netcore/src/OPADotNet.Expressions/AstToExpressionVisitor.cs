﻿/*
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using Microsoft.Extensions.Logging;
using OPADotNet.Expressions.Ast;
using OPADotNet.Expressions.Ast.Models;
using OPADotNet.Expressions.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace OPADotNet.Expressions
{
    internal class AstToExpressionVisitor : ExpressionAstVisitor<Expression>
    {
        private static readonly MethodInfo anyMethod = GetAnyMethod();

        private static MethodInfo GetAnyMethod()
        {
            var methods = typeof(Enumerable).GetMethods().Where(x => x.Name == "Any" && x.IsGenericMethod && x.GetParameters().Length == 2);
            return methods.FirstOrDefault();
        }

        private readonly Type _rootType;
        private readonly Expression _rootParameter;
        private readonly Dictionary<string, ParameterExpression> _parameters = new Dictionary<string, ParameterExpression>();
        private readonly ILogger _logger;
        private readonly ExpressionConversionOptions _options;

        public AstToExpressionVisitor(Expression parameterExpression, Type rootType, ILogger logger, ExpressionConversionOptions options)
        {
            _rootType = rootType;
            _rootParameter = parameterExpression;
            _logger = logger;
            _options = options;
        }

        public override Expression VisitStringLiteral(StringLiteral stringLiteral)
        {
            return Expression.Constant(stringLiteral.Value);
        }

        public override Expression VisitNumericLiteral(NumericLiteral numericLiteral)
        {
            return Expression.Constant(numericLiteral.Value);
        }

        public override Expression VisitBoolLiteral(BoolLiteral boolLiteral)
        {
            return Expression.Constant(boolLiteral.Value);
        }

        public override Expression VisitNullLiteral(NullLiteral nullLiteral)
        {
            return Expression.Constant(null);
        }

        public override Expression VisitQuery(Query query)
        {
            if (query.AndExpressions.Count == 0)
            {
                return Expression.Constant(true);
            }

            Expression expr = Visit(query.AndExpressions.First());

            for (int i = 1; i < query.AndExpressions.Count; i++)
            {
                var result = Visit(query.AndExpressions[i]);

                if (result is ConstantExpression constantExpression && constantExpression.Value.Equals(true))
                {
                    continue;
                }

                expr = Expression.AndAlso(expr, result);
            }
            return expr;
        }

        public override Expression VisitQueries(Queries queries)
        {
            Expression expr = null;
            //Find first working expression
            int i = 0;
            for (; i < queries.OrQueries.Count; i++)
            {
                try
                {
                    expr = Visit(queries.OrQueries[i]);
                    break;
                }
                catch(Exception e)
                {
                    //On exceptions we go to the next query
                    _logger.LogWarning(e, "Error converting an OR query, skipping it.");
                }
            }

            if (expr == null)
            {
                return Expression.Constant(false);
            }

            // If it is just a constant true, return directly since it will always be true.
            if (expr is ConstantExpression exprConstantExpression && exprConstantExpression.Value.Equals(true))
            {
                return exprConstantExpression;
            }

            i++;
            for (; i < queries.OrQueries.Count; i++)
            {
                try
                {
                    var otherExpr = Visit(queries.OrQueries[i]);

                    //If there is a true, in an OR, just return true
                    if (otherExpr is ConstantExpression otherExprConstantExpression && otherExprConstantExpression.Value.Equals(true))
                    {
                        return otherExprConstantExpression;
                    }

                    expr = Expression.OrElse(expr, otherExpr);
                }
                catch(Exception e)
                {
                    //On exception skip the query
                    _logger.LogWarning(e, "Error converting an OR query, skipping it.");
                }
            }

            return expr;
        }

        public override Expression VisitAnyCall(AnyCall anyCall)
        {
            var member = GetMember(anyCall.Property);
            var elementType = GetArrayElementType(member.Type);

            //Create parameter before visiting children
            var lambdaParameter = Expression.Parameter(elementType);
            _parameters.Add(anyCall.ParameterName, lambdaParameter);

            var method = anyMethod.MakeGenericMethod(elementType);

            var andExpressions = Visit(anyCall.AndExpressions);

            Expression first = andExpressions.First();

            for (int i = 1; i < andExpressions.Count; i++)
            {
                first = Expression.AndAlso(first, andExpressions[i]);
            }

            var lambda = Expression.Lambda(first, lambdaParameter);
            return Expression.Call(method, member, lambda);
        }

        public override Expression VisitReference(Reference reference)
        {
            return GetMember(reference);
        }

        public override Expression VisitBooleanComparisonExpression(BooleanComparisonExpression booleanComparisonExpression)
        {
            if (_options.IgnoreNotNullReferenceChecks && booleanComparisonExpression.IsReferenceNullCheck)
            {
                return Expression.Constant(true);
            }

            var left = Visit(booleanComparisonExpression.Left);
            var right = Visit(booleanComparisonExpression.Right);
            return PredicateUtils.CreateComparisonExpression(left, right, booleanComparisonExpression.Type);
        }

        private Expression GetMember(Reference reference)
        {
            if (reference.References.Count == 0)
            {
                return _rootParameter;
            }
            if (reference.References.FirstOrDefault()?.Type == ReferenceType.Parameter)
            {
                if (!_parameters.TryGetValue(reference.References.First().Value, out var parameter))
                {
                    throw new InvalidOperationException();
                }
                Expression memberAccess = parameter;
                for (int i = 1; i < reference.References.Count; i++)
                {
                    memberAccess = Expression.MakeMemberAccess(memberAccess, GetTypeProperty(memberAccess.Type, reference.References[i].Value));
                }
                return memberAccess;
            }
            else
            {
                var first = reference.References.First();
                var property = GetTypeProperty(_rootType, first.Value);
                Expression memberAccess = Expression.MakeMemberAccess(_rootParameter, property);

                for (int i = 1; i < reference.References.Count; i++)
                {
                    memberAccess = Expression.MakeMemberAccess(memberAccess, GetTypeProperty(memberAccess.Type, reference.References[i].Value));
                }
                return memberAccess;
            }
        }

        private static PropertyInfo GetTypeProperty(Type type, string property)
        {
            var propertyInfo = type.GetTypeInfo().GetProperty(property, BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Public);
            return propertyInfo;
        }

        private static Type GetArrayElementType(Type type)
        {
            //From: https://stackoverflow.com/questions/906499/getting-type-t-from-ienumerablet

            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];

            var enumType = type.GetInterfaces()
                                    .Where(t => t.IsGenericType &&
                                           t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                    .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();

            return enumType ?? type;
        }
    }
}

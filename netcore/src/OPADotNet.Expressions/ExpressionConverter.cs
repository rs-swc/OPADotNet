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
using OPADotNet.Ast.Models;
using OPADotNet.Expressions.Ast;
using OPADotNet.Expressions.Ast.Conversion;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OPADotNet.Expressions
{
    public class ExpressionConverter
    {
        private readonly ILogger<ExpressionConverter> _logger;
        private static ExpressionConversionOptions defaultOptions = new ExpressionConversionOptions()
        {
            IgnoreNotNullReferenceChecks = false
        };

        public ExpressionConverter(ILogger<ExpressionConverter> logger)
        {
            _logger = logger;
        }

        public async Task<Expression<Func<object, bool>>> ToExpression(AstQueries partialQueries, string unknown, Type queryType, ExpressionConversionOptions options = null)
        {
            if (options == null)
            {
                options = defaultOptions;
            }

            var convertor = new PartialToAstVisitor();
            var ast = convertor.Convert(partialQueries);
            CleanupVisitor cleanupVisitor = new CleanupVisitor(unknown, _logger);
            cleanupVisitor.Visit(ast);

            ParameterExpression parameterExpression = Expression.Parameter(typeof(object));
            var convertedParameter = Expression.Convert(parameterExpression, queryType);

            AstToExpressionVisitor astToExpressionVisitor = new AstToExpressionVisitor(convertedParameter, queryType, _logger, options);
            var expression = astToExpressionVisitor.Visit(ast);
            return Expression.Lambda(expression, parameterExpression) as Expression<Func<object, bool>>;
        }

        public async Task<Expression> ToExpression(AstQueries partialQueries, string unknown, ParameterExpression parameterExpression, ExpressionConversionOptions options = null)
        {
            if (options == null)
            {
                options = defaultOptions;
            }

            var convertor = new PartialToAstVisitor();
            var ast = convertor.Convert(partialQueries);
            CleanupVisitor cleanupVisitor = new CleanupVisitor(unknown, _logger);
            cleanupVisitor.Visit(ast);
            AstToExpressionVisitor astToExpressionVisitor = new AstToExpressionVisitor(parameterExpression, parameterExpression.Type, _logger, options);
            return astToExpressionVisitor.Visit(ast);
        }
    }
}

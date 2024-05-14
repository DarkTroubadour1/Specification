using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;

namespace Specification.Caching
{
    /// <summary>
    /// Encapsulates methods that allow the generation of a unique string representing a function expression.
    /// Adapted from work By Joakim aka Nertip <see cref="http://pastebin.com/DhHi0Cs2"/>
    /// and Peter Montgomery <see cref="http://petemontgomery.wordpress.com/2008/08/07/caching-the-results-of-linq-queries/ "/>
    /// See: https://github.com/JimBobSquarePants/EFBootstrap/tree/master/EFBootstrap/Caching
    /// </summary>
    public static class KeyFromExpression
    {
        /// <summary>
        /// A prefix for adding to the cache key to allow easy removal of cached linq queries.
        /// </summary>
        private const string CachedQueryPrefix = "CACHED_QUERY";

        /// <summary>
        /// Gets the prefix used for cached queries.
        /// </summary>
        public static string Prefix
        {
            get { return CachedQueryPrefix; }
        }

        /// <summary>
        /// Gets a value indicating whether the expression can be evaluated locally.
        /// </summary>
        /// <value><see langword="true"/> if the expression can be evaluated locally; otherwise, <see langword="false"/>.</value>
        private static Func<Expression, bool> CanBeEvaluatedLocally
        {
            get
            {
                return expression =>
                {
                    // Don't evaluate new instances.
                    if (expression.NodeType == ExpressionType.New)
                    {
                        return false;
                    }

                    // Don't evaluate parameters
                    if (expression.NodeType == ExpressionType.Parameter)
                    {
                        return false;
                    }

                    // Can't evaluate queries
                    if (typeof(IQueryable).IsAssignableFrom(expression.Type))
                    {
                        return false;
                    }

                    return true;
                };
            }
        }

        /// <summary>
        /// Returns a unique cache key for the given expression.
        /// </summary>
        /// <param name="expression">
        /// A strongly typed lambda expression as a data structure in the form of an expression tree.
        /// </param>
        /// <returns>A single instance of the given type</returns>
        /// <typeparam name="T">The type of entity for which to provide the method.</typeparam>
        public static string GetCacheKey<T>(this Expression<Func<T, bool>> expression) where T : class
        {
            // Convert the expression type to an object.
            Expression<Func<T, object>> converted = AddBox<T, bool, object>(expression);

            return EvaluateExpression(converted);
        }

        /// <summary>
        /// Returns a unique cache key for the given expression.
        /// </summary>
        /// <param name="expression">
        /// A strongly typed lambda expression as a data structure in the form of an expression tree.
        /// </param>
        /// <returns>A single instance of the given type</returns>
        /// <typeparam name="T">The type of entity for which to provide the method.</typeparam>
        public static string GetCacheKey<T>(this Expression<Func<T, object>> expression) where T : class
        {
            return EvaluateExpression(expression);
        }

        /// <summary>
        /// Converts a Linq Expression from one type to another.
        /// <see cref="http://stackoverflow.com/questions/729295/how-to-cast-expressionfunct-datetime-to-expressionfunct-object"/>
        /// </summary>
        /// <typeparam name="TModel">The type of entity for which to provide the method.</typeparam>
        /// <typeparam name="TFromProperty">The type to convert from.</typeparam>
        /// <typeparam name="TToProperty">The type to convert to.</typeparam>
        /// <param name="expression">The expression to convert.</param>
        /// <returns>The strongly typed lambda expression</returns>
        private static Expression<Func<TModel, TToProperty>> AddBox<TModel, TFromProperty, TToProperty>(Expression<Func<TModel, TFromProperty>> expression)
        {
            Expression converted = Expression.Convert(expression.Body, typeof(TToProperty));

            return Expression.Lambda<Func<TModel, TToProperty>>(converted, expression.Parameters);
        }

        /// <summary>
        /// Returns a unique cache key for the given expression.
        /// </summary>
        /// <param name="expression">
        /// A strongly typed lambda expression as a date structure
        /// in the form of an expression tree.
        /// </param>
        /// <returns>A single instance of the given type</returns>
        /// <typeparam name="T">The type of entity for which to provide the method.</typeparam>
        private static string EvaluateExpression<T>(Expression<Func<T, object>> expression)
        {
            // Locally evaluate as much of the query as possible
            Expression predicate = Evaluator.PartialEval(expression, CanBeEvaluatedLocally);

            // Handles local expressions.
            predicate = LocalCollectionExpander.Rewrite(predicate);

            // Use the string representation of the expression for the cache key
            string key = predicate.ToString();
            string typeName = typeof(T).Name;

            // Loop through and replace any parent parameters.
            return expression.Parameters.Select(param => param.Name)
                                       .Aggregate(key, (current, name) => current.Replace(name + ".", typeName + "."));
        }

        /// <summary>
        /// Creates a doubly linked list from the <see cref="T:System.Collections.Generic.IEnumerable`1"/>.
        /// </summary>
        /// <param name="source">The <see cref="T:System.Collections.Generic.IEnumerable`1"/> to produce the doubly linked list from.</param>
        /// <returns>A doubly linked list from the <see cref="T:System.Collections.Generic.IEnumerable`1"/>.</returns>
        /// <typeparam name="T">The type of object that is enumerated.</typeparam>
        public static LinkedList<T> ToLinkedList<T>(this IEnumerable<T> source)
        {
            return new LinkedList<T>(source);
        }

        /// <summary>
        /// Returns a concatenated string separated by the given separator from the
        /// given IEnumerable.
        /// </summary>
        /// <param name="source">The <see cref="T:System.Collections.Generic.IEnumerable`1"/> to parse.</param>
        /// <param name="selector">The function expression to add to the String.</param>
        /// <param name="separator">The separator that defines separate function expressions.</param>
        /// <returns>A a concatenated string separated by the given separator from the given <see cref="T:System.Collections.Generic.IEnumerable`1"/>.</returns>
        /// <typeparam name="T">The type of object that is enumerated.</typeparam>
        public static string ToConcatenatedString<T>(this IEnumerable<T> source, Func<T, string> selector, string separator)
        {
            StringBuilder stringBuilder = new StringBuilder();
            bool needSeparator = false;

            foreach (T item in source)
            {
                if (needSeparator)
                {
                    stringBuilder.Append(separator);
                }

                stringBuilder.Append(selector(item));
                needSeparator = true;
            }

            return stringBuilder.ToString();
        }

        // --------------------------------------------------------------------------------------------------------------------
        // <copyright file="Evaluator.cs" company="James South">
        //   Copyright (c) James South
        //   Licensed under GNU LGPL v3.
        // </copyright>
        // <summary>
        //   Enables the partial evaluation of queries.
        // </summary>
        // --------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Enables the partial evaluation of queries.
        /// </summary>
        /// <remarks>
        /// <see cref="http://msdn.microsoft.com/en-us/library/bb546158.aspx"/>
        /// <see cref="http://petemontgomery.wordpress.com/2008/08/07/caching-the-results-of-linq-queries/ "/>
        /// Copyright notice  <see cref="http://msdn.microsoft.com/en-gb/cc300389.aspx#O"/>
        /// </remarks>
        private static class Evaluator
        {
            /// <summary>
            /// Performs evaluation and replacement of independent sub-trees.
            /// </summary>
            /// <param name="expression">The root of the expression tree.</param>
            /// <param name="functionCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
            /// <returns>A new tree with sub-trees evaluated and replaced.</returns>
            public static Expression PartialEval(Expression expression, Func<Expression, bool> functionCanBeEvaluated)
            {
                return new SubtreeEvaluator(new Nominator(functionCanBeEvaluated).Nominate(expression)).Eval(expression);
            }

            /// <summary>
            /// Evaluates and replaces sub-trees when first candidate is reached (top-down)
            /// </summary>
            private sealed class SubtreeEvaluator : ExpressionVisitor
            {
                #region Fields
                /// <summary>
                /// The candidate expressions.
                /// </summary>
                private readonly HashSet<Expression> candidates;
                #endregion

                #region Constructors
                /// <summary>
                /// Initializes a new instance of the <see cref="T:EFBootstrap.Caching.Evaluator.SubtreeEvaluator"/> class.
                /// </summary>
                /// <param name="candidates">
                /// The candidates for evaluation.
                /// </param>
                internal SubtreeEvaluator(HashSet<Expression> candidates)
                {
                    this.candidates = candidates;
                }
                #endregion

                #region Methods
                /// <summary>
                /// Dispatches the expression to one of the more specialized visit methods in this
                /// class.
                /// </summary>
                /// <param name="node">The expression to visit.</param>
                /// <returns>
                /// The modified expression, if it or any sub-expression was modified; otherwise,
                /// returns the original expression.
                /// </returns>
                public override Expression Visit(Expression node)
                {
                    if (node == null)
                    {
                        return null;
                    }

                    if (this.candidates.Contains(node))
                    {
                        return Evaluate(node);
                    }

                    return base.Visit(node);
                }

                /// <summary>
                /// Returns an evaluated Linq.Expression.
                /// </summary>
                /// <param name="expression">The expression to evaluate.</param>
                /// <returns>
                /// The modified expression, if it or any sub-expression was modified; otherwise,
                /// returns the original expression.
                /// </returns>
                internal Expression Eval(Expression expression)
                {
                    return this.Visit(expression);
                }

                /// <summary>
                /// Returns an evaluated Linq.Expression.
                /// </summary>
                /// <param name="expression">The expression to evaluate.</param>
                /// <returns>
                /// The evaluated expression
                /// </returns>
                private static Expression Evaluate(Expression expression)
                {
                    if (expression.NodeType == ExpressionType.Constant)
                    {
                        return expression;
                    }

                    LambdaExpression lambda = Expression.Lambda(expression);
                    Delegate function = lambda.Compile();
                    return Expression.Constant(function.DynamicInvoke(null), expression.Type);
                }
            }
            #endregion

            /// <summary>
            /// Performs bottom-up analysis to determine which nodes can possibly
            /// be part of an evaluated sub-tree.
            /// </summary>
            private sealed class Nominator : ExpressionVisitor
            {
                #region Fields
                /// <summary>
                /// A function that decides whether a given expression node can be part of the local function.
                /// </summary>
                private readonly Func<Expression, bool> functionCanBeEvaluated;

                /// <summary>
                /// The candidates for evaluation.
                /// </summary>
                private HashSet<Expression> candidates;

                /// <summary>
                /// Whether the function can be evaluated.
                /// </summary>
                private bool cannotBeEvaluated;
                #endregion

                #region Constructors
                /// <summary>
                /// Initializes a new instance of the <see cref="T:EFBootstrap.Caching.Evaluator.Nominator">Nominator</see> class.
                /// </summary>
                /// <param name="functionCanBeEvaluated">A function that decides whether a given expression node can be part of the local function.</param>
                internal Nominator(Func<Expression, bool> functionCanBeEvaluated)
                {
                    this.functionCanBeEvaluated = functionCanBeEvaluated;
                }
                #endregion

                #region Methods
                /// <summary>
                /// Dispatches the expression to one of the more specialized visit methods in this
                /// class.
                /// </summary>
                /// <param name="node">The expression to visit.</param>
                /// <returns>
                /// The modified expression, if it or any sub-expression was modified; otherwise,
                /// returns the original expression.
                /// </returns>
                public override Expression Visit(Expression node)
                {
                    if (node != null)
                    {
                        bool saveCannotBeEvaluated = this.cannotBeEvaluated;
                        this.cannotBeEvaluated = false;
                        base.Visit(node);
                        if (!this.cannotBeEvaluated)
                        {
                            if (this.functionCanBeEvaluated(node))
                            {
                                this.candidates.Add(node);
                            }
                            else
                            {
                                this.cannotBeEvaluated = true;
                            }
                        }

                        this.cannotBeEvaluated |= saveCannotBeEvaluated;
                    }

                    return node;
                }

                /// <summary>
                /// Returns an adjusted collection of expressions nominated for evaluation.
                /// </summary>
                /// <param name="expression">The expression to visit.</param>
                /// <returns>A an adjusted collection of expressions nominated for evaluation.</returns>
                internal HashSet<Expression> Nominate(Expression expression)
                {
                    this.candidates = new HashSet<Expression>();
                    this.Visit(expression);
                    return this.candidates;
                }
                #endregion
            }
        }

        /// <summary>
        /// Enables cache key support for local collection values.
        /// Based on the work by Peter Montgomery
        /// <see cref="http://petemontgomery.wordpress.com"/>
        /// <see cref="http://petemontgomery.wordpress.com/2008/08/07/caching-the-results-of-linq-queries/ "/>
        /// </summary>
        public class LocalCollectionExpander : ExpressionVisitor
        {
            #region Methods
            /// <summary>
            /// Returns a re-written Expression.
            /// </summary>
            /// <param name="expression">The expression to rewrite.</param>
            /// <returns>A re-written Expression. </returns>
            public static Expression Rewrite(Expression expression)
            {
                return new LocalCollectionExpander().Visit(expression);
            }

            /// <summary>
            /// Visits the children of the <see cref="T:System.Linq.Expressions.MethodCallExpression" />.
            /// </summary>
            /// <param name="node">The expression to visit.</param>
            /// <returns>
            /// The modified expression, if it or any sub expression was modified; otherwise,
            /// returns the original expression.
            /// </returns>
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                // Pair the method's parameter types with its arguments
                var map = node.Method.GetParameters()
                    .Zip(node.Arguments, (p, a) => new { Param = p.ParameterType, Arg = a })
                    .ToLinkedList();

                // Deal with instance methods
                Type instanceType = node.Object == null ? null : node.Object.Type;
                map.AddFirst(new { Param = instanceType, Arg = node.Object });

                // For any local collection parameters in the method, make a
                // replacement argument which will print its elements
                var replacements = (from x in map
                                    where x.Param != null && x.Param.IsGenericType
                                    let g = x.Param.GetGenericTypeDefinition()
                                    where g == typeof(IEnumerable<>) || g == typeof(List<>)
                                    where x.Arg.NodeType == ExpressionType.Constant
                                    let elementType = x.Param.GetGenericArguments().Single()
                                    let printer = MakePrinter((ConstantExpression)x.Arg, elementType)
                                    select new { x.Arg, Replacement = printer }).ToList();

                if (replacements.Any())
                {
                    List<Expression> args = map.Select(x => replacements.Where(r => r.Arg == x.Arg).Select(r => r.Replacement).SingleOrDefault() ?? x.Arg).ToList();

                    node = node.Update(args.First(), args.Skip(1));
                }

                return base.VisitMethodCall(node);
            }

            /// <summary>
            /// Creates a <see cref="T:System.Linq.Expressions.ConstantExpression">ConstantExpression</see> that has the
            /// ConstantExpression.Value property set to the specified value.
            /// </summary>
            /// <param name="enumerable">The ConstantExpression to manipulate.</param>
            /// <param name="elementType">The element type to create a ConstantExpression for.</param>
            /// <returns>A <see cref="T:System.Linq.Expressions.ConstantExpression">ConstantExpression</see> that has the
            /// ConstantExpression.Value property set to the specified value.</returns>
            private static ConstantExpression MakePrinter(ConstantExpression enumerable, Type elementType)
            {
                IEnumerable value = (IEnumerable)enumerable.Value;
                Type printerType = typeof(Printer<>).MakeGenericType(elementType);
                object printer = Activator.CreateInstance(printerType, value);

                return Expression.Constant(printer);
            }
            #endregion

            /// <summary>
            /// Overrides ToString to print each element of a collection.
            /// </summary>
            /// <remarks>
            /// Inherits List in order to support List.Contains instance method as well
            /// as standard Enumerable.Contains/Any extension methods.
            /// </remarks>
            /// <typeparam name="T">The type of object to provide the methods for.</typeparam>
            private sealed class Printer<T> : List<T>
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="T:EFBootstrap.Caching.LocalCollectionExpander.Printer`1">Printer</see> class.
                /// </summary>
                /// <param name="collection">The collection of objects to print.</param>
                //TODO: [IsNotDeadCode]
                public Printer(IEnumerable collection)
                {
                    this.AddRange(collection.Cast<T>());
                }

                /// <summary>
                /// Returns a <see cref="T:System.String">String</see> that represents the current <see cref="T:System.Object">Object.</see>
                /// </summary>
                /// <returns>
                /// A <see cref="T:System.String">String</see> that represents the current <see cref="T:System.Object">Object.</see>.
                /// </returns>
                /// <filterpriority>2</filterpriority>
                public override string ToString()
                {
                    return string.Format(CultureInfo.InvariantCulture, "{{{0}}}", this.ToConcatenatedString(t => t.ToString(), "|"));
                }
            }
        }
    }
}
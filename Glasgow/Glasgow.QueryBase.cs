/*
 * Copyright 2025 Nathanne Isip
 * This file is part of Glasgow (https://github.com/nthnn/Glasgow)
 * This code is licensed under MIT license (see LICENSE for details)
 */

namespace Glasgow
{
    /// <summary>
    /// Represents an abstract base class designed to facilitate query-building operations over tabular data structures.
    /// This class provides core functionality for defining and applying filter conditions to a collection of rows,
    /// where each row is represented as a dictionary of column names and corresponding values.
    /// </summary>
    /// <remarks>
    /// Classes inheriting from <see cref="QueryBase"/> can leverage its flexible filtering logic to build complex
    /// queries without directly dealing with predicate composition or row traversal. It is suitable for in-memory
    /// querying of tabular datasets such as CSV, database result sets, or other structured row-oriented formats.
    /// </remarks>
    public abstract class QueryBase
    {
        /// <summary>
        /// Holds a list of filter predicates to be applied to each row.
        /// Each predicate is a function that takes a row (dictionary of column→value)
        /// and returns a boolean indicating whether the row satisfies the filter.
        /// </summary>
        protected List<Func<Dictionary<string, object>, bool>> _filters;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryBase"/> class and sets up
        /// an empty list of row filter predicates.
        /// </summary>
        protected QueryBase()
        {
            _filters = [];
        }

        /// <summary>
        /// Determines whether a given <see cref="Type"/> object represents a numeric data type.
        /// Supported types include: <see cref="Int32"/>, <see cref="Int64"/>, <see cref="Double"/>,
        /// <see cref="Single"/>, <see cref="Decimal"/>, <see cref="Int16"/>, and <see cref="Byte"/>.
        /// </summary>
        /// <param name="Type">The type to evaluate.</param>
        /// <returns>
        /// <c>true</c> if the specified <paramref name="Type"/> is one of the recognized numeric types; otherwise, <c>false</c>.
        /// </returns>
        protected static bool IsNumericType(Type Type)
        {
            return Type == typeof(int) ||
                Type == typeof(long) ||
                Type == typeof(double) ||
                Type == typeof(float) ||
                Type == typeof(decimal) ||
                Type == typeof(short) ||
                Type == typeof(byte);
        }

        /// <summary>
        /// Performs a case-insensitive pattern match between the input string and a specified wildcard pattern.
        /// The pattern can contain the '*' character to represent zero or more characters.
        /// </summary>
        /// <param name="Text">The input string to evaluate.</param>
        /// <param name="Pattern">
        /// A wildcard string where '*' acts as a wildcard character matching any number of characters.
        /// For example: "Jo*" matches "John", "Joanna", but not "Alan".
        /// </param>
        /// <returns>
        /// <c>true</c> if the <paramref name="Text"/> matches the <paramref name="Pattern"/>; otherwise, <c>false</c>.
        /// </returns>
        protected static bool WildcardMatch(string Text, string Pattern)
        {
            string regexPattern = "^" + System.Text.RegularExpressions.Regex
                .Escape(Pattern)
                .Replace("\\*", ".*") + "$";

            return System.Text.RegularExpressions.Regex.IsMatch(
                Text,
                regexPattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }

        /// <summary>
        /// Adds a filter condition to this query based on a comparison operator, column name, and value.
        /// Supports comparisons for numeric types, strings (including wildcard, contains, starts/ends-with),
        /// DateTime, boolean, and byte array equality.
        /// </summary>
        /// <typeparam name="T">
        /// The concrete query type that inherits from <see cref="QueryBase"/>. 
        /// This allows fluent chaining by returning the derived type.
        /// </typeparam>
        /// <param name="Operation">
        /// The comparison operator to apply (e.g., EQUALS, NOT_EQUALS, LESS_THAN, etc.).
        /// </param>
        /// <param name="ColumnName">The name of the column on which to apply the filter condition.</param>
        /// <param name="Value">
        /// The value to compare against. Its type must match the data type stored in the column.
        /// </param>
        /// <returns>
        /// The current query instance cast to <typeparamref name="T"/>, enabling method chaining.
        /// </returns>
        /// <remarks>
        /// Supports filtering on various data types including:
        /// <list type="bullet">
        ///   <item><description>Numeric types: performs mathematical comparisons.</description></item>
        ///   <item><description>Strings: supports equality, wildcard, substring, prefix/suffix match.</description></item>
        ///   <item><description>DateTime: supports chronological comparisons.</description></item>
        ///   <item><description>Booleans and byte arrays: supports equality operations.</description></item>
        /// </list>
        /// If either the row value or the provided value is <c>null</c>, they are only considered equal if both are <c>null</c>.
        /// </remarks>
        public T Where<T>(
            Operator Operation,
            string ColumnName,
            object Value
        ) where T : QueryBase
        {
            _filters.Add(row =>
            {
                if (!row.TryGetValue(ColumnName, out object? rowValue))
                    return false;
                if (rowValue == null && Value == null)
                    return true;

                if (rowValue == null || Value == null)
                    return false;

                Type rowType = rowValue.GetType();
                Type valueType = Value.GetType();

                if (IsNumericType(rowType) && IsNumericType(valueType))
                {
                    double rowDouble = Convert.ToDouble(rowValue);
                    double valueDouble = Convert.ToDouble(Value);

                    return Operation switch
                    {
                        Operator.EQUALS => rowDouble == valueDouble,
                        Operator.NOT_EQUALS => rowDouble != valueDouble,
                        Operator.LESS_THAN => rowDouble < valueDouble,
                        Operator.GREATER_THAN => rowDouble > valueDouble,
                        Operator.LESS_THAN_OR_EQUAL => rowDouble <= valueDouble,
                        Operator.GREATER_THAN_OR_EQUAL => rowDouble >= valueDouble,
                        _ => false
                    };
                }
                else if (rowType == typeof(string) && valueType == typeof(string))
                {
                    string rowString = (string)rowValue;
                    string valueString = (string)Value;

                    return Operation switch
                    {
                        Operator.EQUALS => valueString.Contains("*") ?
                            WildcardMatch(rowString, valueString) :
                            string.Equals(
                                rowString,
                                valueString,
                                StringComparison.OrdinalIgnoreCase
                            ),

                        Operator.NOT_EQUALS => valueString.Contains("*") ?
                            !WildcardMatch(rowString, valueString) :
                            !string.Equals(
                                rowString,
                                valueString,
                                StringComparison.OrdinalIgnoreCase
                            ),

                        Operator.CONTAINS => rowString.IndexOf(
                            valueString,
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0,

                        Operator.STARTS_WITH => rowString.StartsWith(
                            valueString,
                            StringComparison.OrdinalIgnoreCase
                        ),

                        Operator.ENDS_WITH => rowString.EndsWith(
                            valueString,
                            StringComparison.OrdinalIgnoreCase
                        ),

                        _ => false
                    };
                }
                else if (rowType == typeof(DateTime) && valueType == typeof(DateTime))
                {
                    DateTime rowDateTime = (DateTime)rowValue;
                    DateTime valueDateTime = (DateTime)Value;

                    return Operation switch
                    {
                        Operator.EQUALS => rowDateTime == valueDateTime,
                        Operator.NOT_EQUALS => rowDateTime != valueDateTime,
                        Operator.LESS_THAN => rowDateTime < valueDateTime,
                        Operator.GREATER_THAN => rowDateTime > valueDateTime,
                        Operator.LESS_THAN_OR_EQUAL => rowDateTime <= valueDateTime,
                        Operator.GREATER_THAN_OR_EQUAL => rowDateTime >= valueDateTime,
                        _ => false
                    };
                }
                else if (rowType == typeof(bool) && valueType == typeof(bool))
                {
                    bool rowBool = (bool)rowValue;
                    bool valueBool = (bool)Value;

                    return Operation switch
                    {
                        Operator.EQUALS => rowBool == valueBool,
                        Operator.NOT_EQUALS => rowBool != valueBool,

                        _ => false
                    };
                }
                else if (rowType == typeof(byte[]) && valueType == typeof(byte[]))
                {
                    byte[] rowBytes = (byte[])rowValue;
                    byte[] valueBytes = (byte[])Value;

                    return Operation switch
                    {
                        Operator.EQUALS => rowBytes.SequenceEqual(valueBytes),
                        Operator.NOT_EQUALS => !rowBytes.SequenceEqual(valueBytes),

                        _ => false
                    };
                }

                return false;
            });

            return (T)this;
        }

        /// <summary>
        /// Applies all filter conditions accumulated via <see cref="Where{T}"/> to a given sequence of rows.
        /// Only rows that satisfy every predicate in the internal filter list are included in the result.
        /// </summary>
        /// <param name="Rows">The collection of rows to be filtered, where each row is a dictionary of column values.</param>
        /// <returns>
        /// A filtered <see cref="IEnumerable{T}"/> containing only the rows that match all defined filter predicates.
        /// </returns>
        protected IEnumerable<Dictionary<string, object>> ApplyFilters(
            IEnumerable<Dictionary<string, object>> Rows
        )
        {
            return _filters.Aggregate(
                Rows,
                (current, filter) => current.Where(filter)
            );
        }
    }
}

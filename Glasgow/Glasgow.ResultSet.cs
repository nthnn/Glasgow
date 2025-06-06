/*
 * Copyright 2025 Nathanne Isip
 * This file is part of Glasgow (https://github.com/nthnn/Glasgow)
 * This code is licensed under MIT license (see LICENSE for details)
 */

namespace Glasgow
{
    /// <summary>
    /// Represents the result of a SELECT query operation within GlasgowDB.
    /// 
    /// The <see cref="ResultSet"/> class encapsulates a filtered, ordered subset of data rows retrieved
    /// from the underlying GlasgowDB in-memory database. Each row is represented as a dictionary 
    /// mapping column names to their corresponding values. This class supports fluent method chaining 
    /// for applying WHERE clauses, ordering, and union operations to shape and refine query results.
    ///
    /// This class inherits from <see cref="QueryBase"/> and utilizes internal logic for comparing, filtering, 
    /// and manipulating rows based on column values, enabling dynamic, composable queries.
    /// </summary>
    public class ResultSet : QueryBase
    {
        /// <summary>
        /// Internal storage for the current set of rows in the result.
        /// Each row is a dictionary of column-value pairs, where the key is the column name
        /// and the value is the associated data.
        /// </summary>
        private List<Dictionary<string, object>> _currentRows;

        /// <summary>
        /// The list of column names selected for projection from the database.
        /// Only these columns will be included when rendering or exporting the result.
        /// </summary>
        private List<string> _selectedColumns;

        /// <summary>
        /// Reference to the originating <see cref="GlasgowDB"/> instance that produced this result set.
        /// This reference allows for additional operations that require access to global database state.
        /// </summary>
        private GlasgowDB _GlasgowDBInstance;

        /// <summary>
        /// Indicates the direction (ascending or descending) in which the result set will be ordered
        /// when sorting is applied using the <see cref="OrderBy(string, SortDirection)"/> method.
        /// Defaults to <see cref="SortDirection.Ascending"/>.
        /// </summary>
        private SortDirection _orderByDirection;

        /// <summary>
        /// The name of the column by which the result set should be ordered.
        /// If null, no ordering will be applied unless explicitly set.
        /// </summary>
        private string? _orderByColumn;

        /// <summary>
        /// Constructs a new instance of the <see cref="ResultSet"/> class using the provided
        /// collection of rows, selected columns, and a reference to the source database.
        /// </summary>
        /// <param name="Rows">
        /// The initial collection of rows (each a dictionary of column→value) for this result set.
        /// </param>
        /// <param name="SelectedColumns">
        /// The list of column names to include in the final output of this result set.
        /// </param>
        /// <param name="Database">
        /// The <see cref="GlasgowDB"/> instance from which this result set originates.
        /// </param>
        public ResultSet(
            List<Dictionary<string, object>> Rows,
            List<string> SelectedColumns,
            GlasgowDB Database
        )
        {
            _currentRows = Rows;
            _selectedColumns = SelectedColumns;
            _GlasgowDBInstance = Database;
            _orderByColumn = null;
            _orderByDirection = SortDirection.Ascending;
        }

        /// <summary>
        /// Filters the result set to retain only those rows that satisfy the given comparison condition.
        /// 
        /// This method evaluates each row using the specified operator against a target column and value.
        /// Type compatibility is enforced and various data types are supported, including strings (with wildcard),
        /// numbers, booleans, DateTime, and binary arrays.
        /// 
        /// Wildcard matching using '*' is supported for strings via case-insensitive regular expression translation.
        /// </summary>
        /// <param name="Operation">
        /// The comparison operator to apply (e.g., EQUALS, NOT_EQUALS, LESS_THAN, etc.).
        /// </param>
        /// <param name="ColumnName">The name of the column on which to apply the filter.</param>
        /// <param name="Value">
        /// The value to compare against. Its type must match the actual data type in the column.
        /// </param>
        /// <returns>
        /// The current <see cref="ResultSet"/> instance, enabling method chaining for multiple WHERE clauses.
        /// </returns>
        public ResultSet Where(
            Operator Operation,
            string ColumnName,
            object Value
        )
        {
            var filteredRows = new List<Dictionary<string, object>>();
            foreach (var row in _currentRows)
            {
                if (row.ContainsKey(ColumnName))
                {
                    object rowValue = row[ColumnName];
                    bool matches = false;

                    if (rowValue == null && Value == null)
                        matches = true;
                    else if (rowValue == null || Value == null)
                        matches = false;
                    else
                    {
                        Type rowType = rowValue.GetType();
                        Type valueType = Value.GetType();

                        if (IsNumericType(rowType) && IsNumericType(valueType))
                        {
                            double rowDouble = Convert.ToDouble(rowValue);
                            double valueDouble = Convert.ToDouble(Value);

                            switch (Operation)
                            {
                                case Operator.EQUALS:
                                    matches = rowDouble == valueDouble;
                                    break;

                                case Operator.NOT_EQUALS:
                                    matches = rowDouble != valueDouble;
                                    break;

                                case Operator.LESS_THAN:
                                    matches = rowDouble < valueDouble;
                                    break;

                                case Operator.GREATER_THAN:
                                    matches = rowDouble > valueDouble;
                                    break;

                                case Operator.LESS_THAN_OR_EQUAL:
                                    matches = rowDouble <= valueDouble;
                                    break;

                                case Operator.GREATER_THAN_OR_EQUAL:
                                    matches = rowDouble >= valueDouble;
                                    break;
                            }
                        }
                        else if (rowType == typeof(string) && valueType == typeof(string))
                        {
                            string rowString = (string)rowValue;
                            string valueString = (string)Value;

                            switch (Operation)
                            {
                                case Operator.EQUALS:
                                    if (valueString.Contains("*"))
                                        matches = WildcardMatch(rowString, valueString);
                                    else matches = string.Equals(
                                        rowString,
                                        valueString,
                                        StringComparison.OrdinalIgnoreCase
                                    );
                                    break;

                                case Operator.NOT_EQUALS:
                                    if (valueString.Contains("*"))
                                        matches = !WildcardMatch(rowString, valueString);
                                    else
                                        matches = !string.Equals(
                                            rowString,
                                            valueString,
                                            StringComparison.OrdinalIgnoreCase
                                        );
                                    break;

                                case Operator.CONTAINS:
                                    matches = rowString.IndexOf(
                                        valueString,
                                        StringComparison.OrdinalIgnoreCase
                                    ) >= 0;
                                    break;

                                case Operator.STARTS_WITH: matches = rowString.StartsWith(
                                    valueString,
                                    StringComparison.OrdinalIgnoreCase
                                ); break;

                                case Operator.ENDS_WITH: matches = rowString.EndsWith(
                                    valueString,
                                    StringComparison.OrdinalIgnoreCase
                                ); break;
                            }
                        }
                        else if (rowType == typeof(DateTime) && valueType == typeof(DateTime))
                        {
                            DateTime rowDateTime = (DateTime)rowValue;
                            DateTime valueDateTime = (DateTime)Value;

                            switch (Operation)
                            {
                                case Operator.EQUALS:
                                    matches = rowDateTime == valueDateTime;
                                    break;

                                case Operator.NOT_EQUALS:
                                    matches = rowDateTime != valueDateTime;
                                    break;

                                case Operator.LESS_THAN:
                                    matches = rowDateTime < valueDateTime;
                                    break;

                                case Operator.GREATER_THAN:
                                    matches = rowDateTime > valueDateTime;
                                    break;

                                case Operator.LESS_THAN_OR_EQUAL:
                                    matches = rowDateTime <= valueDateTime;
                                    break;

                                case Operator.GREATER_THAN_OR_EQUAL:
                                    matches = rowDateTime >= valueDateTime;
                                    break;
                            }
                        }
                        else if (rowType == typeof(bool) && valueType == typeof(bool))
                        {
                            bool rowBool = (bool)rowValue;
                            bool valueBool = (bool)Value;

                            switch (Operation)
                            {
                                case Operator.EQUALS:
                                    matches = rowBool == valueBool;
                                    break;

                                case Operator.NOT_EQUALS:
                                    matches = rowBool != valueBool;
                                    break;
                            }
                        }
                        else if (rowType == typeof(byte[]) && valueType == typeof(byte[]))
                        {
                            byte[] rowBytes = (byte[])rowValue;
                            byte[] valueBytes = (byte[])Value;

                            switch (Operation)
                            {
                                case Operator.EQUALS:
                                    matches = rowBytes.SequenceEqual(valueBytes);
                                    break;

                                case Operator.NOT_EQUALS:
                                    matches = !rowBytes.SequenceEqual(valueBytes);
                                    break;
                            }
                        }
                    }

                    if (matches)
                        filteredRows.Add(row);
                }
            }

            _currentRows = filteredRows;
            return this;
        }

        /// <summary>
        /// Performs case-insensitive wildcard matching on a given text and pattern.
        /// This is a private override for use within the <see cref="ResultSet"/> filtering process.
        /// Converts '*' in patterns to regex equivalents for matching arbitrary substrings.
        /// </summary>
        /// <param name="Text">The input text to match against.</param>
        /// <param name="Pattern">
        /// The wildcard pattern, where '*' stands for zero or more characters.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="Text"/> matches the wildcard <paramref name="Pattern"/>; otherwise, <c>false</c>.
        /// </returns>
        private static new bool WildcardMatch(string Text, string Pattern)
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
        /// Determines whether the given <see cref="Type"/> is a recognized numeric type
        /// (int, long, double, float, decimal, short, or byte).
        /// Private overload of <see cref="QueryBase.IsNumericType(Type)"/>.
        /// </summary>
        /// <param name="Type">The type to check.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="Type"/> is a numeric type; otherwise, <c>false</c>.
        /// </returns>
        private static new bool IsNumericType(Type Type)
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
        /// Specifies that the current result set should be ordered in ascending order based on the given column.
        /// This is a shorthand for calling <see cref="OrderBy(string, SortDirection)"/> with <see cref="SortDirection.Ascending"/>.
        /// </summary>>
        /// <param name="ColumnName">The column name to order by in ascending order.</param>
        /// <returns>The current <see cref="ResultSet"/> instance for further chaining.</returns>
        public ResultSet Ascending(string ColumnName)
        {
            return OrderBy(ColumnName, SortDirection.Ascending);
        }

        /// <summary>
        /// Specifies that the current result set should be ordered in descending order based on the given column.
        /// This is a shorthand for calling <see cref="OrderBy(string, SortDirection)"/> with <see cref="SortDirection.Descending"/>.
        /// </summary>
        /// <param name="ColumnName">The column name to order by in descending order.</param>
        /// <returns>The current <see cref="ResultSet"/> instance for further chaining.</returns>
        public ResultSet Descending(string ColumnName)
        {
            return OrderBy(ColumnName, SortDirection.Descending);
        }

        /// <summary>
        /// Sets the ordering for the result set using the specified column and sort direction.
        /// The actual sort will be applied when materializing the results (e.g., via <c>ToList</c> or equivalent).
        /// </summary>
        /// <param name="ColumnName">The column name to order by.</param>
        /// <param name="Direction">
        /// The <see cref="SortDirection"/> indicating ascending or descending order.
        /// </param>
        /// <returns>The current <see cref="ResultSet"/> instance for method chaining.</returns>
        public ResultSet OrderBy(string ColumnName, SortDirection Direction)
        {
            _orderByColumn = ColumnName;
            _orderByDirection = Direction;

            return this;
        }

        /// <summary>
        /// Merges this current <see cref="ResultSet"/> with another result set, combining their rows into a single unified result.
        /// 
        /// Duplicate rows (i.e., rows with identical values for all selected columns) are eliminated using a set-based comparison.
        /// This method is useful for combining results from multiple queries that share the same column schema.
        /// </summary>
        /// <param name="other">Another <see cref="ResultSet"/> to union with this instance.</param>
        /// <returns>
        /// A new <see cref="ResultSet"/> containing the combined rows and the same selected columns.
        /// </returns>
        public ResultSet Union(ResultSet OtherResultSet)
        {
            List<Dictionary<string, object>> combinedRows = _currentRows;
            foreach (var otherRow in OtherResultSet._currentRows)
            {
                var newRow = new Dictionary<string, object>();
                foreach (var colName in _selectedColumns)
                    if (otherRow.ContainsKey(colName))
                        newRow[colName] = otherRow[colName];
                    else newRow[colName] = Null.GetNullObject();

                combinedRows.Add(newRow);
            }

            return new ResultSet(
                combinedRows,
                _selectedColumns,
                _GlasgowDBInstance
            );
        }

        /// <summary>
        /// Finalizes the query and returns the filtered, optionally ordered list of rows.
        /// 
        /// Only columns specified in the SELECT clause are included in each result row.
        /// If an ordering has been defined using <see cref="OrderBy"/> or its variants, the results are sorted accordingly.
        /// </summary>
        /// <returns>
        /// A <see cref="List{Dictionary}"/> representing the ordered and projected rows in this result set.
        /// Columns not present in a row are filled with a null-representing object.
        /// </returns>
        public List<Dictionary<string, object>> ToList()
        {
            var result = new List<Dictionary<string, object>>();

            IEnumerable<Dictionary<string, object>> orderedRows = _currentRows;
            if (_orderByColumn != null && _currentRows.Any())
            {
                if (_currentRows.First().ContainsKey(_orderByColumn))
                {
                    orderedRows = _currentRows.OrderBy(row =>
                    {
                        if (row.TryGetValue(_orderByColumn, out var value))
                        {
                            if (value is IComparable comparableValue)
                                return comparableValue;
                            return (string)value;
                        }

                        return Null.GetNullObject();
                    }, new SortComparer(_orderByDirection == SortDirection.Ascending));
                }
            }

            foreach (var row in orderedRows)
            {
                var newRow = new Dictionary<string, object>();
                foreach (var colName in _selectedColumns)
                    if (row.ContainsKey(colName))
                        newRow[colName] = row[colName];
                    else newRow[colName] = Null.GetNullObject();

                result.Add(newRow);
            }

            return result;
        }
    }
}

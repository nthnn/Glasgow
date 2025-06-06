/*
 * Copyright 2025 Nathanne Isip
 * This file is part of Glasgow (https://github.com/nthnn/Glasgow)
 * This code is licensed under MIT license (see LICENSE for details)
 */

namespace Glasgow
{
    /// <summary>
    /// Constructs and runs a DELETE operation against a specified table in a <see cref="GlasgowDB"/> instance.
    /// <para>
    /// This class leverages the filtering functionality inherited from <see cref="QueryBase"/> to enable
    /// <c>WHERE</c> clauses that refine which rows to remove. Each <see cref="Where"/> call adds an additional
    /// predicate: all predicates are combined with logical AND semantics.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// int deletedCount = db.Delete("Users")
    ///     .Where(Operator.EQUALS, "IsActive", false)
    ///     .Where(Operator.LESS_THAN, "LastLogin", someDate)
    ///     .Execute();
    /// </code>
    /// This example deletes all rows in the "Users" table that are inactive and have a last login earlier than <c>someDate</c>.
    /// </para>
    /// <para>
    /// If the specified table does not exist in the database, <see cref="Execute"/> returns zero without throwing.
    /// </para>
    /// </summary>
    /// <param name="Instance">
    /// The <see cref="GlasgowDB"/> instance against which this delete operation will be performed.
    /// All modifications occur directly in this instance's in-memory table store.
    /// </param>
    /// <param name="Table">
    /// The name of the table from which rows should be deleted. Must be a non-null, non-empty string;
    /// if the table does not exist, no action is taken.
    /// </param>
    public class DeleteCommand(GlasgowDB Instance, string Table) : QueryBase
    {
        /// <summary>
        /// A reference to the database instance that holds the target table. All deletes apply to this instance.
        /// </summary>
        private readonly GlasgowDB DbInstance = Instance;

        /// <summary>
        /// The name of the target table from which rows will be deleted.
        /// </summary>
        private readonly string TableName = Table;

        /// <summary>
        /// Adds a filtering condition (a <c>WHERE</c> clause) to the delete operation. 
        /// Each call refines the deletion scope: only rows satisfying <paramref name="Operation"/> on
        /// <paramref name="ColumnName"/> against <paramref name="Value"/> will be considered. 
        /// Multiple calls combine predicates using logical AND.
        /// </summary>
        /// <param name="Operation">
        /// The comparison operator to use when evaluating each row’s column value against <paramref name="Value"/>.
        /// Supported operators include:
        /// <list type="bullet">
        ///   <item><see cref="Operator.EQUALS"/>: Checks for equality (numeric, string, boolean, DateTime, or byte[]).</item>
        ///   <item><see cref="Operator.NOT_EQUALS"/>: Checks for inequality.</item>
        ///   <item><see cref="Operator.LESS_THAN"/>, <see cref="Operator.GREATER_THAN"/>,
        ///         <see cref="Operator.LESS_THAN_OR_EQUAL"/>, <see cref="Operator.GREATER_THAN_OR_EQUAL"/>:
        ///         Numeric or <see cref="DateTime"/> comparisons.</item>
        ///   <item><see cref="Operator.CONTAINS"/>, <see cref="Operator.STARTS_WITH"/>, <see cref="Operator.ENDS_WITH"/>:
        ///         String comparisons (case-insensitive; <c>CONTAINS</c> checks substring, etc.).</item>
        /// </list>
        /// </param>
        /// <param name="ColumnName">
        /// The name of the column in each row to evaluate. If a row does not contain this column, that row is excluded.
        /// Column name comparison is case-sensitive and must match one of the table’s schema columns.
        /// </param>
        /// <param name="Value">
        /// The value to compare against. Its runtime type must match the column’s data type (e.g., <c>int</c>, <c>string</c>,
        /// <c>DateTime</c>, <c>bool</c>, or <c>byte[]</c>). For string equality/inequality, wildcard patterns using '*' are supported.
        /// </param>
        /// <returns>
        /// The same <see cref="DeleteCommand"/> instance, allowing subsequent <see cref="Where"/> calls to further refine the filter.
        /// </returns>
        /// <remarks>
        /// Internally, this method calls the generic <see cref="QueryBase.Where{T}"/> implementation, which appends a
        /// predicate delegate into the inherited <c>_filters</c> list. When <see cref="Execute"/> is invoked, only rows 
        /// satisfying all accumulated predicates are deleted.
        /// </remarks>
        public DeleteCommand Where(
            Operator Operation,
            string ColumnName,
            object Value
        ) {
            return Where<DeleteCommand>(Operation, ColumnName, Value);
        }

        /// <summary>
        /// Executes the DELETE operation: all rows in the specified table matching every filter predicate
        /// added via <see cref="Where"/> calls will be removed. The deletion occurs in two phases for safety:
        /// <list type="number">
        ///   <item>Identify and collect all matching rows without modifying the original row list.</item>
        ///   <item>Iterate through the collected rows, removing each from the table and incrementing a counter.</item>
        /// </list>
        /// </summary>
        /// <returns>
        /// The total number of rows successfully deleted. If the table does not exist, returns 0.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// This method does not throw on missing table—but if filters or table data are corrupted,
        /// unexpected exceptions may bubble up (e.g., <see cref="NullReferenceException"/> if internal state is invalid).
        /// </exception>
        /// <remarks>
        /// <para>
        /// <strong>Filtering logic:</strong> For each row in the target table, each predicate in <c>_filters</c> 
        /// (inherited from <see cref="QueryBase"/>) is invoked. Only rows for which every predicate returns <c>true</c>
        /// are slated for deletion.
        /// </para>
        /// <para>
        /// <strong>Deletion logic:</strong> By first collecting matching rows into a separate list, this method avoids
        /// modifying the underlying collection while enumerating it. Removal is then performed in a second pass,
        /// ensuring thread-safety concerns are minimized (though <see cref="GlasgowDB"/> itself is not inherently thread-safe).
        /// </para>
        /// <para>
        /// After deletion, the table’s <see cref="Table.Rows"/> list is permanently altered. There is no rollback
        /// mechanism: once <see cref="Execute"/> returns, deleted rows cannot be recovered unless an external copy
        /// or snapshot was maintained.
        /// </para>
        /// <para>
        /// If multiple <see cref="DeleteCommand"/> objects operate concurrently on the same table from different
        /// threads without synchronization, the result may be non-deterministic due to race conditions on <c>List.Remove</c>.
        /// </para>
        /// </remarks>
        public int Execute()
        {
            if (!DbInstance.Database.ContainsKey(TableName))
                return 0;

            var table = DbInstance.Database[TableName];
            var rowsToDelete = new List<Dictionary<string, object>>();

            foreach (var row in table.Rows)
            {
                bool matchesAllFilters = true;
                foreach (var filter in _filters)
                {
                    if (!filter(row))
                    {
                        matchesAllFilters = false;
                        break;
                    }
                }

                if (matchesAllFilters)
                    rowsToDelete.Add(row);
            }

            int deletedCount = 0;
            foreach (var row in rowsToDelete)
                if (table.Rows.Remove(row))
                    deletedCount++;

            return deletedCount;
        }
    }
}

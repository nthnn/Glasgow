/*
 * Copyright 2025 Nathanne Isip
 * This file is part of Glasgow (https://github.com/nthnn/Glasgow)
 * This code is licensed under MIT license (see LICENSE for details)
 */

namespace Glasgow
{
    /// <summary>
    /// Constructs and runs an UPDATE operation against a specified table in a <see cref="GlasgowDB"/> instance.
    /// <para>
    /// This class leverages the filtering functionality inherited from <see cref="QueryBase"/> to enable
    /// <c>WHERE</c> clauses that refine which rows to update. Each <see cref="Where"/> call adds an additional
    /// predicate: all predicates are combined with logical AND semantics.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// int rowsAffected = db.Update("Users")
    ///                       .Set("IsActive", false)
    ///                       .Where(Operator.LESS_THAN, "LastLogin", someDate)
    ///                       .Execute();
    /// </code>
    /// This example sets the <c>IsActive</c> column to <c>false</c> for all rows in the "Users" table
    /// whose <c>LastLogin</c> is earlier than <c>someDate</c>.
    /// </para>
    /// <para>
    /// If the specified table does not exist in the database, <see cref="Execute"/> returns zero without throwing.
    /// </para>
    /// </summary>
    /// <param name="Instance">
    /// The <see cref="GlasgowDB"/> instance against which this update operation will be performed.
    /// All modifications occur directly in this instance's in-memory table store.
    /// </param>
    /// <param name="Table">
    /// The name of the table whose rows will be updated. Must be a non-null, non-empty string;
    /// if the table does not exist, no action is taken.
    /// </param>
    public class UpdateCommand(GlasgowDB Instance, string Table) : QueryBase
    {
        /// <summary>
        /// A reference to the database instance that holds the target table. All updates apply to this instance.
        /// </summary>
        private readonly GlasgowDB DbInstance = Instance;

        /// <summary>
        /// The name of the target table whose rows will be updated.
        /// </summary>
        private readonly string TableName = Table;

        /// <summary>
        /// Holds the mapping from column names to their new values for the update operation.
        /// When <see cref="Execute"/> is called, each matching row will have these columns overwritten.
        /// </summary>
        private readonly Dictionary<string, object> Updates = [];

        /// <summary>
        /// Specifies a column and its new value to be applied to rows matching the <c>WHERE</c> filters.
        /// Multiple <see cref="Set"/> calls can be chained to update multiple columns at once.
        /// </summary>
        /// <param name="ColumnName">
        /// The name of the column to update. If this column does not already exist in a particular row,
        /// it will be added to that row and to <see cref="Table.ColumnNames"/>. Column names are case-sensitive.
        /// </param>
        /// <param name="Value">
        /// The new value to assign to <paramref name="ColumnName"/>. Its runtime type should match
        /// the data type expected for that column (e.g., <c>int</c>, <c>string</c>, <c>DateTime</c>, etc.).
        /// </param>
        /// <returns>
        /// The same <see cref="UpdateCommand"/> instance, allowing additional <see cref="Set"/> or <see cref="Where"/>
        /// calls to be chained before executing.
        /// </returns>
        /// <remarks>
        /// Each call to <see cref="Set"/> replaces or adds a key/value pair in the internal <c>Updates</c> dictionary.
        /// If the same column is <see cref="Set"/> more than once, the last value provided will be used.
        /// </remarks>
        public UpdateCommand Set(string ColumnName, object Value)
        {
            Updates[ColumnName] = Value;
            return this;
        }

        /// <summary>
        /// Adds a filtering condition (<c>WHERE</c> clause) to this update command, matching only rows where
        /// the specified column satisfies the comparison operator against the provided value.
        /// </summary>
        /// <param name="Operation">
        /// The comparison operator to apply (e.g., <see cref="Operator.EQUALS"/>, <see cref="Operator.NOT_EQUALS"/>,
        /// <see cref="Operator.LESS_THAN"/>, etc.). See <see cref="Operator"/> for all supported types.
        /// </param>
        /// <param name="ColumnName">The name of the column to compare. If a row lacks this column, that row is excluded.</param>
        /// <param name="Value">
        /// The value to compare against. Must be of a compatible type for the column
        /// (e.g., numeric comparisons for numeric columns, string comparisons for text columns, etc.).
        /// </param>
        /// <returns>
        /// The same <see cref="UpdateCommand"/> instance, allowing further chaining of <see cref="Where"/> clauses.
        /// </returns>
        /// <remarks>
        /// Internally, this method calls the generic <see cref="QueryBase.Where{T}"/> implementation, which appends
        /// a predicate delegate into the inherited <c>_filters</c> list. When <see cref="Execute"/> is called,
        /// only rows satisfying all accumulated predicates are updated.
        /// </remarks>
        public UpdateCommand Where(
            Operator Operation,
            string ColumnName,
            object Value
        )
        {
            return Where<UpdateCommand>(Operation, ColumnName, Value);
        }

        /// <summary>
        /// Executes the UPDATE operation against the underlying table, modifying all rows that satisfy
        /// the accumulated filter predicates. If the table does not exist, no rows are updated and zero is returned.
        /// </summary>
        /// <returns>
        /// The number of rows successfully updated in the table.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <para>
        /// This method does not throw if the table is missing. However, if the internal state of the table or filters
        /// is corrupted (e.g., <c>Table.Rows</c> is null), unexpected exceptions (such as <see cref="NullReferenceException"/>) may propagate.
        /// </para>
        /// </exception>
        /// <remarks>
        /// <para>
        /// <strong>Algorithm:</strong>
        /// <list type="number">
        ///   <item>
        ///     Verify that the target table exists in <c>DbInstance.Database</c>. If not, return 0 immediately.
        ///   </item>
        ///   <item>
        ///     Iterate through each <c>row</c> in <c>table.Rows</c>. For each row, evaluate all predicates
        ///     in <c>_filters</c> (inherited from <see cref="QueryBase"/>). Only if every predicate returns <c>true</c>
        ///     is the row considered for updating.
        ///   </item>
        ///   <item>
        ///     For each matching row, iterate through all key/value pairs in <c>Updates</c>. 
        ///     Assign the new value to <c>row[update.Key]</c>. If the column does not exist in the table schema,
        ///     add <c>update.Key</c> to <c>table.ColumnNames</c> so that future operations recognize it.
        ///   </item>
        ///   <item>
        ///     Increment <c>updatedCount</c> for each row modified.
        ///   </item>
        ///   <item>
        ///     Return <c>updatedCount</c> once all rows have been processed.
        ///   </item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Notes on Type Handling:</strong>
        /// <list type="bullet">
        ///   <item>
        ///     Numeric comparators (<c>&lt;</c>, <c>&gt;</c>, etc.) only apply if both the existing row value
        ///     and <paramref name="Value"/> are numeric types (int, long, double, float, decimal, short, or byte).
        ///   </item>
        ///   <item>
        ///     String comparisons (e.g., <c>Operator.CONTAINS</c>, <c>Operator.STARTS_WITH</c>, <c>Operator.ENDS_WITH</c>)
        ///     perform case-insensitive matches. Wildcards (using '*') are also supported when <c>Operator.EQUALS</c>
        ///     or <c>Operator.NOT_EQUALS</c> is used with a pattern containing '*'.
        ///   </item>
        ///   <item>
        ///     DateTime comparisons use <see cref="DateTime"/> comparison operators directly.
        ///   </item>
        ///   <item>
        ///     Boolean comparisons only support <c>Operator.EQUALS</c> and <c>Operator.NOT_EQUALS</c>.
        ///   </item>
        ///   <item>
        ///     Byte-array comparisons (<c>byte[]</c>) only support <c>Operator.EQUALS</c> and
        ///     <c>Operator.NOT_EQUALS</c>, using sequence equality.
        ///   </item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Thread Safety:</strong> 
        /// <c>GlasgowDB</c> is not inherently thread-safe. If multiple threads perform <c>UpdateCommand</c> on
        /// the same table concurrently without external synchronization, the result may be unpredictable
        /// due to race conditions on <c>List&lt;Dictionary&lt;string, object&gt;&gt;.Remove</c> and
        /// direct assignment to <c>row[column]</c>.
        /// </para>
        /// <para>
        /// <strong>Side Effects:</strong>
        /// Once <see cref="Execute"/> is called, the in-memory table is modified immediately. There is no built-in
        /// rollback or versioning; if an update should be undone, the caller must manually revert changes or use a backup/copy.
        /// </para>
        /// </remarks>
        public int Execute()
        {
            if (!DbInstance.Database.ContainsKey(TableName))
                return 0;

            var table = DbInstance.Database[TableName];
            int updatedCount = 0;

            foreach (var row in table.Rows)
            {
                bool matchesAllFilters = true;
                foreach (var filter in _filters)
                    if (!filter(row))
                    {
                        matchesAllFilters = false;
                        break;
                    }

                if (matchesAllFilters)
                {
                    foreach (var update in Updates)
                    {
                        row[update.Key] = update.Value;
                        if (!table.ColumnNames.Contains(update.Key))
                            table.ColumnNames.Add(update.Key);
                    }

                    updatedCount++;
                }
            }

            return updatedCount;
        }
    }
}

/*
 * Copyright 2025 Nathanne Isip
 * This file is part of Glasgow (https://github.com/nthnn/Glasgow)
 * This code is licensed under MIT license (see LICENSE for details)
 */

namespace Glasgow
{
    /// <summary>
    /// Represents a database table, containing a name, a collection of column names, and a collection of rows.
    /// Each row is stored as a dictionary mapping column names to their corresponding values.
    /// </summary>
    public sealed class Table
    {
        /// <summary>
        /// Gets or sets the name of this table.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the list of column names defined for this table.
        /// </summary>
        public List<string> ColumnNames { get; set; }

        /// <summary>
        /// Gets or sets the collection of rows in this table.
        /// Each row is represented as a dictionary where keys are column names and values are the data entries.
        /// </summary>
        public List<Dictionary<string, object>> Rows { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Table"/> class with the specified name and optional initial columns.
        /// </summary>
        /// <param name="TableName">The name to assign to this table.</param>
        /// <param name="Cols">
        /// An optional enumerable of column names to initialize the table schema.
        /// If <c>null</c>, the table starts with an empty list of columns.
        /// </param>
        public Table(string TableName, IEnumerable<string>? Cols = null)
        {
            Name = TableName;
            Rows = [];
            ColumnNames = Cols != null ?
                Cols.ToList() :
                [];
        }
    }
}

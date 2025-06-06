/*
 * Copyright 2025 Nathanne Isip
 * This file is part of Glasgow (https://github.com/nthnn/Glasgow)
 * This code is licensed under MIT license (see LICENSE for details)
 */

namespace Glasgow
{
    /// <summary>
    /// Represents an in-memory database that holds named tables and provides basic 
    /// relational operations (CREATE, DROP, INSERT, SELECT, DELETE, UPDATE).
    /// <para>
    /// All data is stored in memory as <see cref="Table"/> objects, each identified by a unique string name.
    /// This class is not thread-safe: concurrent modifications (e.g., two threads inserting into the same table)
    /// can lead to inconsistent state. If thread safety is required, external synchronization must be applied.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// // Create a new database
    /// var db = new GlasgowDB();
    /// // Create a table "Users" with columns "Id", "Name", "Email"
    /// db.CreateTable("Users", "Id", "Name", "Email");
    /// // Insert a row into "Users"
    /// db.InsertInto(
    ///     "Users",
    ///     new[] { "Id", "Name", "Email" },
    ///     new object[] { 1, "Alice", "alice@example.com" }
    /// );
    /// // Query the "Users" table
    /// var results = db.Select("Users", "Id", "Name")
    ///     .Where(Operator.EQUALS, "Name", "Alice")
    ///     .ToList();
    /// foreach (var row in results)
    /// {
    ///     Console.WriteLine($"Id: {row["Id"]}, Name: {row["Name"]}");
    /// }
    /// // Delete inactive users
    /// int deleted = db.Delete("Users")
    ///     .Where(Operator.EQUALS, "IsActive", false)
    ///     .Execute();
    /// // Drop the "Users" table entirely
    /// bool dropped = db.DropTable("Users");
    /// </code>
    /// </para>
    /// </summary>
    public class GlasgowDB
    {
        /// <summary>
        /// A dictionary of tables in this database, keyed by table name. 
        /// Each key corresponds to a <see cref="Table"/> instance that contains its own schema and rows.
        /// </summary>
        /// <remarks>
        /// - Table names are case-sensitive; two names that differ only by case are considered distinct.
        /// - Modifications (Insert/Delete/Update) operate directly on the <see cref="Table.Rows"/> list.
        /// - To check if a table exists, use <c>Database.ContainsKey(tableName)</c>.
        /// </remarks>
        public Dictionary<string, Table> Database { get; set; }

        /// <summary>
        /// Initializes a new, empty <see cref="GlasgowDB"/> instance with no tables.
        /// </summary>
        /// <remarks>
        /// Internally, this constructor allocates a new <see cref="Dictionary{String, Table}"/> 
        /// to hold tables by name. Subsequent operations (CreateTable, InsertInto, etc.) will populate this dictionary.
        /// </remarks>
        public GlasgowDB()
        {
            Database = [];
        }

        /// <summary>
        /// Creates a new table in the database with the specified name and initial columns.
        /// </summary>
        /// <param name="TableName">
        /// The name for the new table. Must be a non-null, non-empty string. 
        /// If a table with this name already exists, creation fails and no changes are made.
        /// </param>
        /// <param name="ColumnNames">
        /// A variable-length array of column names to define the table’s schema. 
        /// Each column name must be unique within this table and non-null/non-empty. 
        /// If <paramref name="ColumnNames"/> is empty, the new table will initially have zero columns; 
        /// columns can be added later upon insertion.
        /// </param>
        /// <returns>
        /// <c>true</c> if the table was created successfully; <c>false</c> if a table with the same name already exists.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method does not validate that <paramref name="ColumnNames"/> are unique at the call site; 
        /// however, duplicate names in the same array will result in multiple entries in <c>Table.ColumnNames</c>.
        /// It is the caller’s responsibility to avoid passing duplicate column names.
        /// </para>
        /// <para>
        /// After creation, the new table is accessible via <c>Database[TableName]</c>.
        /// Subsequent <see cref="InsertInto"/> calls on this table will add rows to it.
        /// </para>
        /// <para>
        /// The return value <c>false</c> indicates that no exception was thrown, but the operation was a no-op 
        /// because the table already existed. Use <see cref="DropTable"/> followed by <see cref="CreateTable"/> 
        /// if you need to re-create a table with the same name.
        /// </para>
        /// </remarks>
        public bool CreateTable(
            string TableName,
            params string[] ColumnNames
        )
        {
            if (Database.ContainsKey(TableName))
                return false;

            Database.Add(TableName, new Table(TableName, ColumnNames));
            return true;
        }

        /// <summary>
        /// Deletes (drops) a table and all its contents from the database.
        /// </summary>
        /// <param name="TableName">
        /// The name of the table to remove. Must be non-null and case-sensitive. 
        /// If no such table exists, no exception is thrown and the method returns <c>false</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the table was found and removed; <c>false</c> if the table did not exist.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Once a table is dropped, all schema information (column names) and row data are lost. 
        /// There is no built-in undo/restore; callers should handle backups if necessary.
        /// </para>
        /// <para>
        /// Example:
        /// <code>
        /// bool removed = db.DropTable("Orders");
        /// if (!removed)
        ///     Console.WriteLine("No table named 'Orders' existed.");
        /// </code>
        /// </para>
        /// </remarks>
        public bool DropTable(string TableName)
        {
            if (!Database.ContainsKey(TableName))
                return false;

            Database.Remove(TableName);
            return true;
        }

        /// <summary>
        /// Retrieves a <see cref="ResultSet"/> containing rows from the specified table,
        /// projected onto the specified columns. Allows further filtering, ordering, and transformation.
        /// </summary>
        /// <param name="TableName">
        /// The name of the table to select from. If the table does not exist, an empty <see cref="ResultSet"/> is returned.
        /// </param>
        /// <param name="ColumnNames">
        /// A variable-length list of column names to include in the result. Columns that do not exist in the table 
        /// schema are ignored. If no valid column names are specified, the returned <see cref="ResultSet"/> 
        /// will have zero columns and, if the table exists, all rows will appear with null placeholders.
        /// </param>
        /// <returns>
        /// A <see cref="ResultSet"/> containing:
        /// <list type="bullet">
        ///   <item>
        ///     <term>Rows</term>
        ///     <description>If the table exists, its internal row list (<c>Table.Rows</c>) is used; otherwise, an empty list.</description>
        ///   </item>
        ///   <item>
        ///     <term>Columns</term>
        ///     <description>The subset of <paramref name="ColumnNames"/> that exist in the table schema.</description>
        ///   </item>
        ///   <item>
        ///     <term>Database reference</term>
        ///     <description>A reference to this <see cref="GlasgowDB"/> is stored for potential further operations.</description>
        ///   </item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// <para>
        /// The returned <see cref="ResultSet"/> enables fluent operations:
        /// <list type="bullet">
        ///   <item><c>Where(...)</c> to filter rows.</item>
        ///   <item><c>OrderBy(...)</c> to sort.</item>
        ///   <item><c>Union(...)</c> to combine with other sets.</item>
        ///   <item><c>ToList()</c> to materialize the final list of dictionaries.</item>
        /// </list>
        /// </para>
        /// <para>
        /// If the table does not exist, this method returns a <see cref="ResultSet"/> initialized with:
        /// <list type="bullet">
        ///   <item><c>Rows</c> = an empty <c>List&lt;Dictionary&lt;string, object&gt;&gt;</c></item>
        ///   <item><c>SelectedColumns</c> = all requested <paramref name="ColumnNames"/> (converted to a <c>List&lt;string&gt;</c>)</item>
        ///   <item><c>Database</c> = this <see cref="GlasgowDB"/> instance</item>
        /// </list>
        /// This allows callers to chain <c>Where</c> or <c>OrderBy</c> calls safely, but <c>ToList()</c> will return an empty list.
        /// </para>
        /// <para>
        /// <strong>Performance:</strong> The <c>Select</c> method does not copy row data; it simply passes a reference to the table’s internal
        /// <c>List&lt;Dictionary&lt;string, object&gt;&gt;</c>. Subsequent filtering and ordering operations work on that reference,
        /// so if the underlying table is modified after <c>Select</c> is called but before <c>ToList()</c>, results may vary.
        /// </para>
        /// </remarks>
        public ResultSet Select(
            string TableName,
            params string[] ColumnNames
        )
        {
            if (!Database.ContainsKey(TableName))
                return new ResultSet(
                    [],
                    [.. ColumnNames],
                    this
                );

            var table = Database[TableName];
            var validColumns = ColumnNames.Where(
                c => table.ColumnNames.Contains(c)
            ).ToList();

            return new ResultSet(table.Rows, validColumns, this);
        }

        /// <summary>
        /// Inserts a new row into the specified table. If the table does not yet exist, it is automatically created
        /// with the provided columns. If new columns appear in <paramref name="ColumnNames"/> that are not in the
        /// existing table schema, they will be added dynamically.
        /// </summary>
        /// <param name="TableName">
        /// The name of the table into which to insert the new row. Must be non-null and non-empty.
        /// </param>
        /// <param name="ColumnNames">
        /// An array of column names corresponding one-to-one with <paramref name="Values"/>. 
        /// If the table does not exist, these column names define its initial schema. 
        /// If the table exists, any column in this array that is not already in the table’s schema 
        /// will be appended to <c>Table.ColumnNames</c>.
        /// </param>
        /// <param name="Values">
        /// An array of objects representing the data for the new row. 
        /// The length of this array must match the length of <paramref name="ColumnNames"/>. 
        /// Supported value types include: <c>null</c> (serialized as <see cref="Null"/>), 
        /// <c>int</c>, <c>long</c>, <c>double</c>, <c>float</c>, <c>decimal</c>, 
        /// <c>string</c>, <c>bool</c>, <c>DateTime</c>, and <c>byte[]</c>. Other types will cause an exception when saving to disk.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when:
        /// <list type="bullet">
        ///   <item><c>TableName</c> is null or empty.</item>
        ///   <item><paramref name="ColumnNames"/> and <paramref name="Values"/> have different lengths.</item>
        ///   <item>A <paramref name="ColumnNames"/> entry is null or empty.</item>
        /// </list>
        /// </exception>
        /// <remarks>
        /// <para>
        /// Internally, this method performs these steps:
        /// <list type="number">
        ///   <item>
        ///     If <paramref name="TableName"/> does not exist in <c>Database</c>, calls 
        ///     <see cref="CreateTable(string, string[])"/> with the provided columns.
        ///   </item>
        ///   <item>
        ///     For each column name in <paramref name="ColumnNames"/>, if that column is not 
        ///     already present in <c>table.ColumnNames</c>, add it to the schema.
        ///   </item>
        ///   <item>
        ///     Validates that <paramref name="ColumnNames"/>.Length == <paramref name="Values"/>.Length. 
        ///     If not, throws <see cref="ArgumentException"/>.
        ///   </item>
        ///   <item>
        ///     Constructs a new <c>Dictionary&lt;string, object&gt;</c> mapping each column name 
        ///     to its corresponding value. Null values should be represented as <see cref="Null.GetNullObject()"/>.
        ///   </item>
        ///   <item>
        ///     Appends the new dictionary to <c>table.Rows</c>.
        ///   </item>
        /// </list>
        /// </para>
        /// <para>
        /// After insertion, the new row is immediately visible to subsequent <c>Select</c> operations. 
        /// There is no validation of value types against an earlier schema (e.g., you could store an integer 
        /// in a column that previously held strings). It is the caller’s responsibility to ensure type consistency.
        /// </para>
        /// </remarks>
        public void InsertInto(
            string TableName,
            string[] ColumnNames,
            object[] Values)
        {
            if (!Database.ContainsKey(TableName))
                CreateTable(TableName, ColumnNames);

            var table = Database[TableName];
            foreach (var colName in ColumnNames)
                if (!table.ColumnNames.Contains(colName))
                    table.ColumnNames.Add(colName);

            if (ColumnNames.Length != Values.Length)
                throw new ArgumentException(
                    "Number of column names must match number of values for insertion."
                );

            var newRow = new Dictionary<string, object>();
            for (int i = 0; i < ColumnNames.Length; i++)
                newRow[ColumnNames[i]] = Values[i];
            table.Rows.Add(newRow);
        }

        /// <summary>
        /// Initiates a <see cref="DeleteCommand"/> builder for performing row deletions on a specific table.
        /// </summary>
        /// <param name="tableName">
        /// The name of the table from which rows will be deleted. Must be non-null and non-empty. 
        /// If no table exists with this name, the resulting <see cref="DeleteCommand.Execute"/> call will return 0.
        /// </param>
        /// <returns>
        /// A new <see cref="DeleteCommand"/> instance tied to this database and the specified table name.
        /// The returned command supports method chaining for <c>Where</c> filters followed by <c>Execute</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Example usage:
        /// <code>
        /// int count = db.Delete("Products")
        ///     .Where(Operator.LESS_THAN, "Quantity", 10)
        ///     .Execute();
        /// </code>
        /// This will delete all rows from "Products" where the "Quantity" column is less than 10.
        /// </para>
        /// <para>
        /// Internally, <see cref="DeleteCommand"/> holds a reference to this <see cref="GlasgowDB"/> instance
        /// and the target table name. All <c>Where</c> filters are stored until <c>Execute</c> is called.
        /// </para>
        /// </remarks>
        public DeleteCommand Delete(string tableName)
        {
            return new DeleteCommand(this, tableName);
        }
    }
}

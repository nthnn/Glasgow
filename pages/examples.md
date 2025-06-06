## Example Usage

### Loading and Saving Database to File

These examples demonstrate how to persist a GlasgowDB instance to disk and retrieve it later. The database is encrypted using a password for secure storage. This allows you to save the entire in-memory database state and reload it later without reinitializing or repopulating data manually.

```csharp
GlasgowDB db = new GlasgowDB(); // Create database instance
DatabaseIO.SaveToFile(          // Save the database instance to file
    db,
    "database.glasgow",
    "this_is_your_password"
);
```

```csharp
// Load database from file with specified file name and password
GlasgowDB db = DatabaseIO.LoadFromFile(
    "database.glasgow",
    "this_is_your_password"
);
```

### Creating Database

This snippet shows how to initialize a new GlasgowDB instance and define tables within it. Each table is defined with a name and a list of columns. Tables are created dynamically and are ready to store and manipulate rows of data once defined.

```csharp
GlasgowDB db = new GlasgowDB(); // Create database instance

db.CreateTable(                 // Create new table on the instance
    "users",                    // Name the table "users"
    "id",                       // These are column names ...
    "username",                 //   |
    "email",                    //   |
    "age",                      //   |
    "is_active",                //   |
    "created_at",               //   |
    "profile_pic"               //   |
);
db.CreateTable(                 // Create another table
    "products",                 // Name this table "products"
    "product_id",               // Here's the column names ...
    "name",                     //   |
    "price",                    //   |
    "stock_count"               //   |
);
```

### Inserting Row to Table

After creating tables, you can insert rows by specifying both the column names and the corresponding values in the correct order. The data can include strings, numbers, booleans, `DateTime` values, and even binary data such as images. The inserted values will be stored and indexed automatically.

```csharp
db.InsertInto(
    // Define the table name where the row will be added
    "users",
    // Row name array
    [
        "id",
        "username",
        "email",
        "age",
        "is_active",
        "created_at",
        "profile_pic"
    ],
    // Values that will be put with the same
    // arrangement as row names above
    [
        1, "alice_test_user", "alice@example.com", 30, true,
        DateTime.UtcNow.AddDays(-10),
        Encoding.UTF8.GetBytes("some_image_data_alice")
    ]
);

db.InsertInto(
    // Define the table name where the row will be added
    "products",
    // Row name array
    [
        "product_id",
        "name",
        "price",
        "stock_count"
    ],
    // Values that will be put with the same
    // arrangement as row names above
    [ 101, "Laptop", 1200.50, 10 ]
);
```

### Query and Data Fetching

GlasgowDB includes a fluent query API for filtering, sorting, and selecting records. The following example demonstrates how to select users whose usernames start with `"bob"` and whose age is less than 30. Results are sorted by age in ascending order, and each row can be accessed like a dictionary for reading individual column values.

```csharp
// Select the following rows from the "users" table
var users = db.Select(
        "users",
        "id",
        "username",
        "email",
        "age",
        "created_at"
    )
    .Where(Operator.STARTS_WITH, "username", "bob")
    .Where(Operator.LESS_THAN, "age", 30)
    .Ascending("age")
    .ToList();

// Iterate on the fetched rows
foreach (var user in users)
    Console.WriteLine(
        $"ID: {user["id"]}, Username: {user["username"]}, " +
        $"Email: {user["email"]}, Age: {user["age"]}, " +
        $"Created: {user["created_at"]}"
    );
```

using Glasgow;
using System.Text;

namespace GlasgowExample
{
    internal class Program
    {
        public static void Main()
        {
            string dbFilePath = "database.glasgow";
            string passkey = "MySuperSecretPasskey456!";

            if (File.Exists(dbFilePath))
                File.Delete(dbFilePath);

            Console.WriteLine("--- Creating Tables ---");
            GlasgowDB db = new();

            db.CreateTable(
                "users",
                "id",
                "username",
                "email",
                "age",
                "is_active",
                "created_at",
                "profile_pic"
            );
            db.CreateTable(
                "products",
                "product_id",
                "name",
                "price",
                "stock_count"
            );

            DatabaseIO.SaveToFile(db, dbFilePath, passkey);
            Console.WriteLine("Database tables created and saved to file.");

            Console.WriteLine("--- Inserting Data ---");
            db.InsertInto(
                "users",
                [
                    "id",
                    "username",
                    "email",
                    "age",
                    "is_active",
                    "created_at",
                    "profile_pic"
                ],
                [
                    1, "alice_test_user", "alice@example.com", 30, true,
                    DateTime.UtcNow.AddDays(-10),
                    Encoding.UTF8.GetBytes("some_image_data_alice")
                ]
            );
            db.InsertInto(
                "users",
                [
                    "id",
                    "username",
                    "email",
                    "age",
                    "is_active",
                    "created_at",
                    "profile_pic"
                ],
                [
                    2, "bob_test_user", "bob@example.com", 25, true,
                    DateTime.UtcNow.AddDays(-5),
                    Encoding.UTF8.GetBytes("some_image_data_bob")
                ]
            );
            db.InsertInto(
                "users",
                [
                    "id",
                    "username",
                    "email",
                    "age",
                    "is_active",
                    "created_at"
                ],
                [
                    3, "charlie_user", "charlie@example.com", 35, false,
                    DateTime.UtcNow.AddDays(-1)
                ]
            );
            db.InsertInto(
                "users",
                [
                    "id",
                    "username",
                    "email",
                    "age",
                    "is_active",
                    "created_at"
                ],
                [
                    4, "david_user", "david@example.com", 28, true,
                    DateTime.UtcNow.AddDays(-15)
                ]
            );

            db.InsertInto(
                "products",
                [
                    "product_id",
                    "name",
                    "price",
                    "stock_count"
                ],
                [ 101, "Laptop", 1200.50, 10 ]
            );
            db.InsertInto(
                "products",
                [
                    "product_id",
                    "name",
                    "price",
                    "stock_count"
                ],
                [ 102, "Mouse", 25.99, 150 ]
            );
            db.InsertInto(
                "products",
                [
                    "product_id",
                    "name",
                    "price"
                ],
                [ 103, "Keyboard", 75.00 ]
            );

            db.Delete("products").Where(
                Operator.EQUALS,
                "product_id",
                101
            ).Execute();

            DatabaseIO.SaveToFile(db, dbFilePath, passkey);
            Console.WriteLine("Data inserted and saved to file.");

            Console.WriteLine("--- Selecting Data with Ordering ---");
            var loadedDb = DatabaseIO.LoadFromFile(dbFilePath, passkey);
            var users = loadedDb.Select(
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

            Console.WriteLine(
                "\nUsers matching conditions (name starts " +
                "with 'bob', age < 30, ordered by age ASC):"
            );

            foreach (var user in users)
                Console.WriteLine(
                    $"ID: {user["id"]}, Username: {user["username"]}, " +
                    $"Email: {user["email"]}, Age: {user["age"]}, " +
                    $"Created: {user["created_at"]}"
                );

            var allUsersOrderedDesc = loadedDb.Select(
                "users",
                "id",
                "username",
                "created_at"
            ).Descending("created_at").ToList();

            Console.WriteLine("\nAll Users (ordered by created_at DESC):");
            foreach (var user in allUsersOrderedDesc)
                Console.WriteLine(
                    $"ID: {user["id"]}, " +
                    $"Username: {user["username"]}, " +
                    $"Created: {user["created_at"]}"
                );

            var productsOrderedAsc = loadedDb.Select(
                "products",
                "name",
                "price",
                "stock_count"
            ).Ascending("price").ToList();

            Console.WriteLine("\nAll Products (ordered by price ASC):");
            foreach (var product in productsOrderedAsc)
                Console.WriteLine(
                    $"Name: {product["name"]}, " + 
                    $"Price: {product["price"]}, " +
                    $"Stock: {product["stock_count"]}"
                );

            Console.WriteLine("\n--- Dropping a Table ---");
            loadedDb.DropTable("products");
            DatabaseIO.SaveToFile(loadedDb, dbFilePath, passkey);

            var reloadedDb = DatabaseIO.LoadFromFile(dbFilePath, passkey);
            reloadedDb.Select("products", "name").ToList();
        }
    }
}
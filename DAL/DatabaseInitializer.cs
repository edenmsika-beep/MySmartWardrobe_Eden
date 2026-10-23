using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace DAL
{
    public static class DatabaseInitializer
    {
        private const string DatabaseName = "MySmartWardrobe";

        public static async Task InitializeAsync(string connectionString)
        {
            await CreateDatabaseAsync(connectionString);
            await CreateTablesAsync(connectionString);
            await SeedRolesAsync(connectionString);
            await SeedUsersAsync(connectionString);
        }

        // יצירת מסד הנתונים אם הוא לא קיים
        private static async Task CreateDatabaseAsync(string connectionString)
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            builder.Database = string.Empty;

            await using var connection = new MySqlConnection(
                builder.ConnectionString);

            await connection.OpenAsync();

            string sql = $@"
                CREATE DATABASE IF NOT EXISTS `{DatabaseName}`
                CHARACTER SET utf8mb4
                COLLATE utf8mb4_unicode_ci;";

            await using var command = new MySqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        // יצירת שבע הטבלאות
        private static async Task CreateTablesAsync(string connectionString)
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            builder.Database = DatabaseName;

            await using var connection = new MySqlConnection(
                builder.ConnectionString);

            await connection.OpenAsync();

            string[] tables =
            {
                // 1. Roles
                @"
                CREATE TABLE IF NOT EXISTS Roles
                (
                    RoleId INT AUTO_INCREMENT PRIMARY KEY,
                    RoleName VARCHAR(50) NOT NULL
                );",

                // 2. Users
                @"
                CREATE TABLE IF NOT EXISTS Users
                (
                    UserId INT AUTO_INCREMENT PRIMARY KEY,
                    Username VARCHAR(100) NOT NULL,
                    Email VARCHAR(255) NOT NULL,
                    PasswordHash VARCHAR(255) NOT NULL,
                    RoleId INT NOT NULL,
                    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
                    FOREIGN KEY (RoleId)
                        REFERENCES Roles(RoleId)
                );",

                // 3. Categories
                @"
                CREATE TABLE IF NOT EXISTS Categories
                (
                    CategoryId INT AUTO_INCREMENT PRIMARY KEY,
                    CategoryName VARCHAR(100) NOT NULL
                );",

                // 4. ClothesItems
                @"
                CREATE TABLE IF NOT EXISTS ClothesItems
                (
                    ItemId INT AUTO_INCREMENT PRIMARY KEY,
                    UserId INT NOT NULL,
                    CategoryId INT NOT NULL,
                    Color VARCHAR(50) NOT NULL,
                    Season VARCHAR(50) NOT NULL,
                    ImageData LONGBLOB NOT NULL,
                    ImageMimeType VARCHAR(100) NOT NULL,

                    FOREIGN KEY (UserId)
                        REFERENCES Users(UserId),

                    FOREIGN KEY (CategoryId)
                        REFERENCES Categories(CategoryId)
                );",

                // 5. Outfits
                @"
                CREATE TABLE IF NOT EXISTS Outfits
                (
                    OutfitId INT AUTO_INCREMENT PRIMARY KEY,
                    UserId INT NOT NULL,
                    OutfitName VARCHAR(100) NOT NULL,
                    Rating INT NOT NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

                    FOREIGN KEY (UserId)
                        REFERENCES Users(UserId),

                    CHECK (Rating BETWEEN 1 AND 5)
                );",

                // 6. OutfitItems
                @"
                CREATE TABLE IF NOT EXISTS OutfitItems
                (
                    OutfitId INT NOT NULL,
                    ItemId INT NOT NULL,

                    PRIMARY KEY (OutfitId, ItemId),

                    FOREIGN KEY (OutfitId)
                        REFERENCES Outfits(OutfitId),

                    FOREIGN KEY (ItemId)
                        REFERENCES ClothesItems(ItemId)
                );",

                // 7. InspirationImages
                @"
                CREATE TABLE IF NOT EXISTS InspirationImages
                (
                    InspirationId INT AUTO_INCREMENT PRIMARY KEY,
                    UserId INT NOT NULL,
                    ImageData LONGBLOB NOT NULL,
                    TargetCategory VARCHAR(100) NOT NULL,
                    TargetColor VARCHAR(50) NOT NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

                    FOREIGN KEY (UserId)
                        REFERENCES Users(UserId)
                );"
            };

            foreach (string sql in tables)
            {
                await using var command = new MySqlCommand(sql, connection);
                await command.ExecuteNonQueryAsync();
            }
        }

        // הכנסת התפקידים הבסיסיים רק אם הטבלה ריקה
        private static async Task SeedRolesAsync(string connectionString)
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            builder.Database = DatabaseName;

            await using var connection = new MySqlConnection(
                builder.ConnectionString);

            await connection.OpenAsync();

            string checkSql = "SELECT COUNT(*) FROM Roles;";

            await using var checkCommand =
                new MySqlCommand(checkSql, connection);

            object? result = await checkCommand.ExecuteScalarAsync();
            long count = Convert.ToInt64(result);

            if (count == 0)
            {
                string insertSql = @"
                    INSERT INTO Roles (RoleName)
                    VALUES (@admin), (@user);";

                await using var insertCommand =
                    new MySqlCommand(insertSql, connection);

                insertCommand.Parameters.AddWithValue("@admin", "Admin");
                insertCommand.Parameters.AddWithValue("@user", "User");

                await insertCommand.ExecuteNonQueryAsync();
            }
        }

        // הכנסת שני מנהלים ושמונה משתמשים רק אם טבלת המשתמשים ריקה
        private static async Task SeedUsersAsync(string connectionString)
        {
            var builder = new MySqlConnectionStringBuilder(connectionString);
            builder.Database = DatabaseName;

            await using var connection = new MySqlConnection(
                builder.ConnectionString);

            await connection.OpenAsync();

            string checkSql = "SELECT COUNT(*) FROM Users;";

            await using var checkCommand =
                new MySqlCommand(checkSql, connection);

            object? result = await checkCommand.ExecuteScalarAsync();
            long count = Convert.ToInt64(result);

            if (count == 0)
            {
                // שליפת ה-RoleId של Admin ושל User מהמסד
                string getRoleIdSql = "SELECT RoleId FROM Roles WHERE RoleName = @roleName;";

                await using var getAdminRoleCmd = new MySqlCommand(getRoleIdSql, connection);
                getAdminRoleCmd.Parameters.AddWithValue("@roleName", "Admin");
                int adminRoleId = Convert.ToInt32(await getAdminRoleCmd.ExecuteScalarAsync());

                await using var getUserRoleCmd = new MySqlCommand(getRoleIdSql, connection);
                getUserRoleCmd.Parameters.AddWithValue("@roleName", "User");
                int userRoleId = Convert.ToInt32(await getUserRoleCmd.ExecuteScalarAsync());

                // שאילתת ההכנסה של 2 מנהלים ו-8 משתמשים
                string insertUsersSql = @"
                    INSERT INTO Users (Username, Email, PasswordHash, RoleId, IsActive)
                    VALUES 
                    -- מנהלים (RoleId = Admin)
                    ('admin1', 'admin1@smartwardrobe.com', 'AdminPass123!', @adminRoleId, TRUE),
                    ('admin2', 'admin2@smartwardrobe.com', 'AdminPass123!', @adminRoleId, TRUE),

                    -- משתמשים רגילים (RoleId = User)
                    ('dana_c', 'dana@example.com', 'UserPass123!', @userRoleId, TRUE),
                    ('yossi_k', 'yossi@example.com', 'UserPass123!', @userRoleId, TRUE),
                    ('noa_m', 'noa@example.com', 'UserPass123!', @userRoleId, TRUE),
                    ('ron_b', 'ron@example.com', 'UserPass123!', @userRoleId, TRUE),
                    ('maya_s', 'maya@example.com', 'UserPass123!', @userRoleId, TRUE),
                    ('omer_d', 'omer@example.com', 'UserPass123!', @userRoleId, TRUE),
                    ('shira_l', 'shira@example.com', 'UserPass123!', @userRoleId, TRUE),
                    ('guy_p', 'guy@example.com', 'UserPass123!', @userRoleId, TRUE);";

                await using var insertCommand = new MySqlCommand(insertUsersSql, connection);
                insertCommand.Parameters.AddWithValue("@adminRoleId", adminRoleId);
                insertCommand.Parameters.AddWithValue("@userRoleId", userRoleId);

                await insertCommand.ExecuteNonQueryAsync();
            }
        }
    }
}
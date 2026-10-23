using Model;
using MySql.Data.MySqlClient;
using System.Data.Common;
namespace DAL
{
    public class UserDB
    {
        private readonly string connectionString;

        // שליפה משותפת של משתמש יחד עם שם התפקיד שלו
        private const string SelectUserSql = @"
            SELECT u.UserId, u.Username, u.Email,
                   u.PasswordHash, u.RoleId, r.RoleName, u.IsActive
            FROM Users u
            INNER JOIN Roles r ON u.RoleId = r.RoleId";

        public UserDB(string connectionString)
        {
            this.connectionString = connectionString;
        }

        // הוספת משתמש חדש
        public async Task<int> AddUserAsync(User user, string roleName = "User")
        {
            string sql = @"
                INSERT INTO Users (Username, Email, PasswordHash, RoleId)
                SELECT @Username, @Email, @Password, RoleId
                FROM Roles
                WHERE RoleName = @RoleName;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@Password", user.PasswordHash);
            command.Parameters.AddWithValue("@RoleName", roleName);

            int rows = await command.ExecuteNonQueryAsync();

            if (rows == 0)
                throw new InvalidOperationException($"Role '{roleName}' not found.");

            user.Id = (int)command.LastInsertedId;
            return user.Id;
        }

        // בדיקה אם שם המשתמש כבר קיים
        public async Task<bool> UsernameExistsAsync(string username)
        {
            string sql = "SELECT EXISTS(SELECT 1 FROM Users WHERE Username = @Username);";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", username);

            return Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
        }

        // בדיקה אם האימייל כבר קיים
        public async Task<bool> EmailExistsAsync(string email)
        {
            string sql = "SELECT EXISTS(SELECT 1 FROM Users WHERE Email = @Email);";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Email", email);

            return Convert.ToInt64(await command.ExecuteScalarAsync()) == 1;
        }

        // בדיקת התחברות (בודק גם שהמשתמש פעיל)
        public async Task<User?> CheckLoginAsync(string username, string password)
        {
            string sql = SelectUserSql + " WHERE u.Username = @Username AND u.IsActive = TRUE;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Username", username);

            using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            User user = ReadUser(reader);

            return user.PasswordHash == password ? user : null;
        }

        // שליפת משתמש לפי מזהה
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            string sql = SelectUserSql + " WHERE u.UserId = @UserId;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();

            return await reader.ReadAsync() ? ReadUser(reader) : null;
        }

        // שליפת כל המשתמשים
        public async Task<List<User>> GetAllUsersAsync()
        {
            string sql = SelectUserSql + " ORDER BY u.Username;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            var users = new List<User>();

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                users.Add(ReadUser(reader));

            return users;
        }

        // ספירת משתמשים לפי תפקיד
        public async Task<Dictionary<string, int>> CountUsersByRoleAsync()
        {
            string sql = @"
                SELECT r.RoleName, COUNT(u.UserId) AS UserCount
                FROM Roles r
                LEFT JOIN Users u ON u.RoleId = r.RoleId
                GROUP BY r.RoleId, r.RoleName
                ORDER BY r.RoleId;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);

            var counts = new Dictionary<string, int>();

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                counts[reader.GetString(reader.GetOrdinal("RoleName"))] =
                    Convert.ToInt32(reader["UserCount"]);
            }

            return counts;
        }

        // שינוי תפקיד של משתמש
        public async Task<bool> UpdateUserRoleAsync(int userId, string roleName)
        {
            string sql = @"
                UPDATE Users u
                INNER JOIN Roles r ON r.RoleName = @RoleName
                SET u.RoleId = r.RoleId
                WHERE u.UserId = @UserId;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@RoleName", roleName);
            command.Parameters.AddWithValue("@UserId", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        // עדכון סיסמה
        public async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
        {
            string sql = @"
                UPDATE Users
                SET PasswordHash = @Password
                WHERE UserId = @UserId;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Password", newPassword);
            command.Parameters.AddWithValue("@UserId", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        // עדכון אימייל
        public async Task<bool> UpdateEmailAsync(int userId, string newEmail)
        {
            string sql = @"
                UPDATE Users
                SET Email = @Email
                WHERE UserId = @UserId;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Email", newEmail);
            command.Parameters.AddWithValue("@UserId", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        // מחיקת משתמש
        public async Task<bool> DeleteUserAsync(int userId)
        {
            string sql = "DELETE FROM Users WHERE UserId = @UserId;";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        // המרת שורה מהמסד לאובייקט User
        private static User ReadUser(DbDataReader reader)
        {
            return new User(
                reader.GetInt32(reader.GetOrdinal("UserId")),
                reader.GetString(reader.GetOrdinal("Username")),
                reader.GetString(reader.GetOrdinal("Email")),
                reader.GetString(reader.GetOrdinal("PasswordHash")),
                reader.GetInt32(reader.GetOrdinal("RoleId")),
                reader.GetString(reader.GetOrdinal("RoleName")),
                reader.GetBoolean(reader.GetOrdinal("IsActive"))
            );
        }
    }
}

// ============================================================
// Models/DbHelper.cs
// A small helper that gives us a MySQL connection anywhere.
// We pass the connection string from appsettings.json.
// ============================================================

using MySql.Data.MySqlClient;

namespace OnlineClearance.Models
{
    public static class DbHelper
    {
        // Call this whenever you need a DB connection.
        // Always wrap usage in a using() block so it auto-closes.
        public static MySqlConnection GetConnection(IConfiguration config)
        {
            string connStr = config.GetConnectionString("DefaultConnection")!;
            return new MySqlConnection(connStr);
        }
    }
}

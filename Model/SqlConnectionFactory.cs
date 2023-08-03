using System;
using System.Data.SqlClient;

namespace WorkerService.Model
{
    public interface IDbConnectionFactory
    {
        SqlConnection GetConnection();
    }

    public class SqlConnectionFactory : IDbConnectionFactory
    {
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        Log logger = new Logger();

        public string connectionString 
        { 
            get
            {
                return $"Data Source={ServerName};Initial Catalog={DatabaseName};User ID={UserName};Password={Password};";
            }
        }

        public SqlConnection GetConnection()
        {
            try
            {
                SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to connect to database server: {ServerName} - database: {DatabaseName} - user: {UserName}", ex);
                throw;
            }
        }
    }
}

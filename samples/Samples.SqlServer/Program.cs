using System;
using System.Data.SqlClient;
using System.Linq;

namespace Samples.SqlServer
{
    internal class Program
    {
        private static void Main()
        {
            var connectionString = GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DROP TABLE IF EXISTS Employees; CREATE TABLE Employees (Id int PRIMARY KEY CLUSTERED, Name nvarchar(100));";
                    int records = command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.Parameters.AddWithValue("Id", 1);
                    command.Parameters.AddWithValue("Name", "Name1");
                    command.CommandText = "INSERT INTO Employees (Id, Name) VALUES (@Id, @Name);";
                    int records = command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.Parameters.AddWithValue("Id", 1);
                    command.CommandText = "SELECT Name FROM Employees WHERE Id=@Id;";
                    var name = command.ExecuteScalar() as string;
                }

                using (var command = connection.CreateCommand())
                {
                    command.Parameters.AddWithValue("Name", "Name2");
                    command.Parameters.AddWithValue("Id", 1);
                    command.CommandText = "UPDATE Employees SET Name=@Name WHERE Id=@Id;";
                    int records = command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.Parameters.AddWithValue("Id", 1);
                    command.CommandText = "SELECT * FROM Employees WHERE Id=@Id;";

                    using (var reader = command.ExecuteReader())
                    {
                        var employees = reader.AsDataRecords()
                                              .Select(
                                                   r => new
                                                   {
                                                       Id = (int)r["Id"],
                                                       Name = (string)r["Name"]
                                                   })
                                              .ToList();
                    }
                }

                using (var command = connection.CreateCommand())
                {
                    command.Parameters.AddWithValue("Id", 1);
                    command.CommandText = "DELETE FROM Employees WHERE Id=@Id;";
                    int records = command.ExecuteNonQuery();
                }
            }
        }

        private static string GetConnectionString()
        {
            return Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING") ??
                   @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;";
        }
    }
}

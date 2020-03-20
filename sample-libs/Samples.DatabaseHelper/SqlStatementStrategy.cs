namespace Samples.DatabaseHelper
{
    public class SqlStatementStrategy
    {
        public virtual string DropCommandText => "DROP TABLE IF EXISTS Employees; CREATE TABLE Employees (Id int PRIMARY KEY, Name varchar(100));";

        public virtual string InsertCommandText => "INSERT INTO Employees (Id, Name) VALUES (@Id, @Name);";

        public virtual string SelectOneCommandText => "SELECT Name FROM Employees WHERE Id=@Id;";

        public virtual string UpdateCommandText => "UPDATE Employees SET Name=@Name WHERE Id=@Id;";

        public virtual string SelectManyCommandText => "SELECT * FROM Employees WHERE Id=@Id;";

        public virtual string DeleteCommandText => "DELETE FROM Employees WHERE Id=@Id;";
    }
}

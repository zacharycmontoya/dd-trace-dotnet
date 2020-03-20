namespace Samples.DatabaseHelper
{
    public class OracleSqlStatementStrategy : SqlStatementStrategy
    {
        public override string DropCommandText => "BEGIN BEGIN EXECUTE IMMEDIATE 'DROP TABLE Employees'; EXCEPTION WHEN OTHERS THEN NULL; END; EXECUTE IMMEDIATE 'CREATE TABLE Employees (Id int PRIMARY KEY, Name varchar(100))'; END;";

        public override string InsertCommandText => "INSERT INTO Employees (Id,Name) VALUES(:Id,:Name)";

        public override string SelectOneCommandText => "SELECT Name FROM Employees WHERE Id=:Id";

        public override string UpdateCommandText => "UPDATE Employees SET Name=:Name WHERE Id=:Id";

        public override string SelectManyCommandText => "SELECT * FROM Employees WHERE Id=:Id";

        public override string DeleteCommandText => "DELETE FROM Employees WHERE Id=:Id";
    }
}

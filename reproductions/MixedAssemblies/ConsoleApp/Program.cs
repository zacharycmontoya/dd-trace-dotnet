namespace ConsoleApp
{
    public static class Program
    {
        public static void Main()
        {
            CppCliLibrary.ConsoleHelper.WriteToConsole("foo");
            CppCliLibrary.ConsoleHelper.TryWriteToConsole("bar");
        }
    }
}

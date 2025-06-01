internal class Program
{
    static bool hadError = false;

    private static void Main(string[] args)
    {
        if (args.Length > 1)
        {
            Console.WriteLine("Usage: dotnet run [<path-to-file>]");
            Environment.Exit(64);
        }
        else if (args.Length == 1)
        {
            runFile(args[0]);
        }
        else
        {
            runPrompt();
        }
    }

    private static void runFile(string path)
    {
        var fileContent = File.ReadAllText(path);
        run(fileContent);

        if (hadError)
            System.Environment.Exit(65);
    }

    private static void runPrompt()
    {
        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
            if (line is null)
                break;
            run(line);
            hadError = false;
        }
    }

    private static void run(string source)
    {
        // Console.WriteLine(source);
        var scanner = new Scanner(source);
        var tokens = scanner.scanTokens();

        foreach (var token in tokens)
        {
            Console.WriteLine(token);
        }
    }

    internal static void error(int line, string message)
    {
        report(line, "", message);
    }

    private static void report(int line, string where, string message)
    {
        Console.WriteLine($"[line {line}] Error {where}: {message}");
        hadError = true;
    }
}

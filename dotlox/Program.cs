internal class Program
{
    static bool hadError = false;
    static bool hadRuntimeError = false;

    private static void Main(string[] args)
    {
        if (args.Length > 1)
        {
            Console.WriteLine("Usage: dotnet run [<path-to-file>]");
            Environment.Exit(64);
        }
        else if (args.Length == 1)
        {
            RunFile(args[0]);
        }
        else
        {
            RunPrompt();
        }
    }

    private static void RunFile(string path)
    {
        var fileContent = File.ReadAllText(path);
        Run(fileContent);

        if (hadError) Environment.Exit(65);
        if (hadRuntimeError) Environment.Exit(70);

    }

    private static void RunPrompt()
    {
        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();
            if (line is null)
                break;
            Run(line);
            hadError = false;
        }
    }

    private static void Run(string source)
    {
        var scanner = new Scanner(source);
        var tokens = scanner.ScanTokens();

        var parser = new Parser(tokens);
        var stmts = parser.Parse();

        if (hadError) return;


        var interpreter = new Interpreter();
        var resolver = new Resolver(interpreter);

        resolver.Resolve(stmts);
        if (hadError) return;

        interpreter.Interpret(stmts);
    }

    private static void Report(int line, string where, string message)
    {
        Console.WriteLine($"[line {line}] Error {where}: {message}");
        hadError = true;
    }


    internal static void Error(int line, string message)
    {
        Report(line, "", message);
    }


    internal static void Error(Token token, string message)
    {
        if (token.type == TokenType.EOF)
        {
            Report(token.line, " at end", message);
        }
        else
        {
            Report(token.line, " at '" + token.lexeme + "'", message);
        }
    }


    internal static void RuntimeError(RuntimeError error)
    {
        Console.WriteLine($"{error.Message} \n[line {error.token.line}]");
        hadRuntimeError = true;
    }
}

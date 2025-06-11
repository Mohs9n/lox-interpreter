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

        if (hadError) Environment.Exit(65);
        if (hadRuntimeError) Environment.Exit(70);

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
        var scanner = new Scanner(source);
        var tokens = scanner.scanTokens();

        var parser = new Parser(tokens);
        var stmts = parser.Parse();

        if (hadError) return;


        var interpreter = new Interpreter();
        var resolver = new Resolver(interpreter);

        resolver.Resolve(stmts);
        if (hadError) return;

        interpreter.Interpret(stmts);
    }

    private static void report(int line, string where, string message)
    {
        Console.WriteLine($"[line {line}] Error {where}: {message}");
        hadError = true;
    }


    internal static void error(int line, string message)
    {
        report(line, "", message);
    }


    internal static void error(Token token, String message)
    {
        if (token.type == TokenType.EOF)
        {
            report(token.line, " at end", message);
        }
        else
        {
            report(token.line, " at '" + token.lexeme + "'", message);
        }
    }


    internal static void RuntimeError(RuntimeError error)
    {
        Console.WriteLine($"{error.Message} \n[line {error.token.line}]");
        hadRuntimeError = true;
    }
}

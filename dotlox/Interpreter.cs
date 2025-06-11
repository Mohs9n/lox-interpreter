using static TokenType;

internal class Interpreter
{
    private readonly LoxEnvironment globals = new();
    private LoxEnvironment environment;
    private readonly Dictionary<Expr, int> locals = [];

    public Interpreter()
    {
        environment = globals;
        globals.Define("clock", new ClockFunction());
    }

    public void Interpret(List<Stmt> stmts)
    {
        try
        {
            foreach (var stmt in stmts)
            {
                Execute(stmt);
            }
        }
        catch (RuntimeError error)
        {
            Program.RuntimeError(error);
        }
    }

    private void Execute(Stmt stmt)
    {
        switch (stmt)
        {
            case Print(var expr):
                ExecutePrint(expr);
                break;
            case Expression(var expr):
                ExecuteExpression(expr);
                break;
            case Var(var name, var initializer):
                ExecuteVarDecl(name, initializer);
                break;
            case Block(var stmts):
                ExecuteBlock(stmts, new LoxEnvironment(environment));
                break;
            case If(var cond, var thenBranch, var elseBranch):
                ExecuteIf(Evaluate(cond), thenBranch, elseBranch);
                break;
            case While(var cond, var body):
                ExecuteWhile(cond, body);
                break;
            case Function functionStmt:
                var fun = new LoxFunction(functionStmt, environment);
                environment.Define(functionStmt.Name.lexeme, fun);
                break;
            case Return(var keyword, var value):
                object? val = null;
                if (value is not null)
                    val = Evaluate(value);
                throw new ReturnControl(val);
            default:
                throw new Exception("Unkown statement.");
        }
    }


    public object? Evaluate(Expr expr) => expr switch
    {
        Literal(var value) => value,
        Grouping(var expression) => Evaluate(expression),
        Unary(var op, var right) => EvaluateUnary(op, Evaluate(right)),
        Binary(var left, var op, var right) => EvaluateBinary(op, Evaluate(left), Evaluate(right)),
        Variable varExpr => EvaluateVariable(varExpr),
        Assign assignExpr => EvaluateAssign(assignExpr),
        Logical(var left, var op, var right) => EvaluateLogical(op, left, right),
        Call(var calle, var paren, var arguments) => EvaluateCall(calle, paren, arguments),
        _ => throw new Exception("Unknown expression")
    };

    public void Resolve(Expr expr, int depth)
    {
      locals[expr] = depth;
    }

    private void ExecuteWhile(Expr cond, Stmt body)
    {
      while (IsTruthy(Evaluate(cond)))
        Execute(body);
    }

    private void ExecuteIf(object? cond, Stmt thenBranch, Stmt? elseBranch)
    {
      if (IsTruthy(cond))
        Execute(thenBranch);
      else if (elseBranch is not null)
        Execute(elseBranch);
    }

    public void ExecuteBlock(List<Stmt> stmts, LoxEnvironment environment)
    {
        var previous = this.environment;
        try
        {
            this.environment = environment;

            foreach (var stmt in stmts)
            {
                Execute(stmt);
            }
        }
        finally
        {
            this.environment = previous;
        }
    }

    private void ExecuteVarDecl(Token name, Expr? initializer)
    {
        object? value = null;
        if (initializer is not null)
            value = Evaluate(initializer);
        environment.Define(name.lexeme, value);
    }

    private void ExecuteExpression(Expr expr)
    {
        Evaluate(expr);
    }

    private void ExecutePrint(Expr expr)
    {
        var val = Evaluate(expr);
        Console.WriteLine(Stringify(val));
    }


    private object? EvaluateCall(Expr calle, Token paren, List<Expr> arguments)
    {
        var calleVal = Evaluate(calle);

        List<object?> args = [];
        foreach (var arg in arguments)
        {
            args.Add(Evaluate(arg));
        }

        if (calleVal is not ILoxCallable fn)
        {
            throw new RuntimeError(paren, "Can only call functions and classes.");
        }


        if (args.Count != fn.Arity())
        {
            throw new RuntimeError(paren, $"Expected {fn.Arity()} arguments but got {args.Count}.");
        }

        return fn.Call(this, args);
    }

    private object? EvaluateLogical(Token op, Expr left, Expr right)
    {
      var l = Evaluate(left);

      if (op.type == OR)
      {
        if (IsTruthy(l)) return l;
      } else {
        if (!IsTruthy(l)) return l;
      }

      return Evaluate(right);
    }

    private object? EvaluateAssign(Assign expr)
    {
      var value = Evaluate(expr.Value);

      var found = locals.TryGetValue(expr, out var distance);
      if (found)
        environment.AssignAt(distance, expr.Name, value);
      else
        globals.Assign(expr.Name, value);

      return value;
    }

    private object? EvaluateVariable(Variable expr)
    {
        return LookUpVariable(expr.Name, expr);
    }

    private object? LookUpVariable(Token name, Expr expr)
    {
      var found = locals.TryGetValue(expr, out var distance);
      if (found)
        return environment.GetAt(distance, name.lexeme);
      else
        return globals.Get(name);
    }


    private object? EvaluateUnary(Token? op, object? right)
    {
        ArgumentNullException.ThrowIfNull(op);
        // ArgumentNullException.ThrowIfNull(right);

        return op.type switch
        {
            BANG => !IsTruthy(right),
            MINUS => -RequireNumber(right, op),
            _ => null,
        };
    }

    private object? EvaluateBinary(Token? op, object? left, object? right)
    {
        ArgumentNullException.ThrowIfNull(op);

        switch (op.type)
        {
            case MINUS:
                {
                    var (l, r) = RequireNumbers(left, right, op);
                    return l - r;
                }
            case STAR:
                {
                    var (l, r) = RequireNumbers(left, right, op);
                    return l * r;
                }
            case SLASH:
                {
                    var (l, r) = RequireNumbers(left, right, op);
                    return l / r;
                }
            case PLUS:
                return left switch
                {
                    double l when right is double r => l + r,
                    string l when right is string r => l + r,
                    _ => throw new RuntimeError(op, "Operands must be two numbers or two strings.")
                };
            case GREATER:
                {
                    var (l, r) = RequireNumbers(left, right, op);
                    return l > r;
                }
            case GREATER_EQUAL:
                {
                    var (l, r) = RequireNumbers(left, right, op);
                    return l >= r;
                }
            case LESS:
                {
                    var (l, r) = RequireNumbers(left, right, op);
                    return l < r;
                }
            case LESS_EQUAL:
                {
                    var (l, r) = RequireNumbers(left, right, op);
                    return l <= r;
                }
            case BANG_EQUAL: return !IsEqual(left, right);
            case EQUAL_EQUAL: return IsEqual(left, right);
            default:
                return null;
        }
        ;
    }


    private bool IsTruthy(object? val)
    {
        if (val == null) return false;
        if (val is bool v) return v;
        return true;
    }


    private bool IsEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null) return false;

        return a.Equals(b);
    }


    private static double RequireNumber(object? operand, Token op) =>
        operand is double d
            ? d
            : throw new RuntimeError(op, "Operand must be a number.");


    private static (double Left, double Right) RequireNumbers(object? left, object? right, Token op) =>
        (left, right) switch
        {
            (double l, double r) => (l, r),
            _ => throw new RuntimeError(op, "Operands must be numbers.")
        };


    private string Stringify(object? val)
    {
        if (val == null) return "nil";

        if (val is double num)
        {
            string text = num.ToString();
            if (text.EndsWith(".0"))
            {
                text = text.Substring(0, text.Length - 2);
            }
            return text;
        }

        return val.ToString()!;
    }
}

internal class RuntimeError(Token token, string message) : Exception(message)
{
    public readonly Token token = token;
}

internal interface ILoxCallable
{
    int Arity();
    object? Call(Interpreter interpreter, List<object?> arguments);
}


internal class ClockFunction : ILoxCallable
{
    public int Arity() => 0;

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
    }

    public override string ToString() => "<native fn>";
}

internal class LoxFunction(Function declaration, LoxEnvironment closure) : ILoxCallable
{
    private readonly Function declaration = declaration;

    private readonly LoxEnvironment closure = closure;
    public int Arity()
    {
        return declaration.Parameters.Count;
    }

    public object? Call(Interpreter interpreter, List<object?> arguments)
    {
        var environment = new LoxEnvironment(closure);
        for (int i = 0; i < declaration.Parameters.Count; i++)
        {
            environment.Define(declaration.Parameters[i].lexeme, arguments[i]);
        }

        try
        {
            interpreter.ExecuteBlock(declaration.Body, environment);
        }
        catch (ReturnControl returnVal)
        {
            return returnVal.Value;
        }

        return null;
    }

    public override string ToString() => $"<fn {declaration.Name.lexeme}>";
}


public class ReturnControl : Exception
{
    public object? Value { get; }

    public ReturnControl(object? value) : base(null)
    {
        Value = value;
    }

    public override string StackTrace => "";
}

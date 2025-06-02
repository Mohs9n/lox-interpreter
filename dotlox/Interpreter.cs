using static TokenType;

internal class Interpreter
{

    public void Interpret(Expr expression)
    {
        try
        {
            var value = Evaluate(expression);
            Console.WriteLine(Stringify(value));
        }
        catch (RuntimeError error)
        {
            Program.RuntimeError(error);
        }
    }

    public object? Evaluate(Expr expr) => expr switch
    {
        Literal(var value) => value,
        Grouping(var expression) => Evaluate(expression),
        Unary(var op, var right) => EvaluateUnary(op, Evaluate(right)),
        Binary(var left, var op, var right) => EvaluateBinary(op, Evaluate(left), Evaluate(right)),
        _ => throw new Exception("Unknown expression")
    };


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
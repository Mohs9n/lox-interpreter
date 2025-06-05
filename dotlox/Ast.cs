internal abstract record Expr;

internal record Binary(Expr Left, Token Operator, Expr Right) : Expr;
internal record Grouping(Expr Expression) : Expr;
internal record Literal(object? Value) : Expr;
internal record Unary(Token Operator, Expr Right) : Expr;
internal record Variable(Token Name) : Expr;
internal record Assign(Token Name, Expr Value) : Expr;
internal record Logical(Expr Left, Token Operator, Expr Right) : Expr;

internal abstract record Stmt;
internal record Expression(Expr Expr) : Stmt;
internal record Print(Expr Expr) : Stmt;
internal record Var(Token Name, Expr? Initializer) : Stmt;
internal record Block(List<Stmt> Statements) : Stmt;
internal record If(Expr condition, Stmt ThenBranch, Stmt? ElseBranch) : Stmt;
internal record While(Expr condition, Stmt body) : Stmt;



internal class AstPrinter
{
    public string Print(Expr expr) => expr switch
    {
        Binary(var left, var op, var right) =>
            Parenthesize(op.lexeme, left, right),

        Unary(var op, var right) =>
            Parenthesize(op.lexeme, right),

        Grouping(var expression) =>
            Parenthesize("group", expression),

        Literal(var value) =>
            value?.ToString() ?? "nil",

        _ => throw new Exception("Unknown expression")
    };

    private string Parenthesize(string name, params Expr[] expressions)
    {
        var parts = expressions.Select(Print);
        return $"({name} {string.Join(" ", parts)})";
    }
}

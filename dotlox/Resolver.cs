internal class Resolver(Interpreter interpreter)
{
  private readonly Interpreter interpreter = interpreter;
  private readonly List<Dictionary<string, bool>> scopes = [];
  private FunctionType currentFn = FunctionType.NONE;

  private void ScopesPush(Dictionary<string, bool> n) => scopes.Add(n);
  public Dictionary<string, bool> ScopesPop()
  {
    var top = scopes[^1];
    scopes.RemoveAt(scopes.Count - 1);
    return top;
  }
  private Dictionary<string, bool> ScopesPeek() => scopes[^1];

  public void Resolve(List<Stmt> stmts)
  {
    foreach (var stmt in stmts)
      Resolve(stmt);
  }

  private void Resolve(Stmt stmt)
  {
    switch (stmt)
    {
      case Print(var expr):
        Resolve(expr);
        break;
      case Expression(var expr):
        Resolve(expr);
        break;
      case Var(var name, var initializer):
        Declare(name);
        if (initializer is not null)
          Resolve(initializer);
        Define(name);
        break;
      case Block(var stmts):
        BeginScope();
        Resolve(stmts);
        EndScope();
        break;
      case If(var cond, var thenBranch, var elseBranch):
        Resolve(cond);
        Resolve(thenBranch);
        if (elseBranch is not null)
          Resolve(elseBranch);
        break;
      case While(var cond, var body):
        Resolve(cond);
        Resolve(body);
        break;
      case Function functionStmt:
        Declare(functionStmt.Name);
        Define(functionStmt.Name);
        ResolveFunction(functionStmt, FunctionType.FUNCTION);
        break;
      case Return(var keyword, var value):
        if (currentFn == FunctionType.NONE)
          Program.error(keyword, "Can't return from top-level code.");
        if (value is not null)
          Resolve(value);
        break;
      default:
        throw new Exception("Resolver:: Unkown statement.");
    }
  }

  private void Resolve(Expr expr)
  {
    switch (expr)
    {
      case Variable varExpr:
        if (scopes.Count != 0 &&
            ScopesPeek().TryGetValue(varExpr.Name.lexeme, out var defined) &&
            defined == false)
          Program.error(varExpr.Name, "Can't read local variable in it's own initializer.");
        ResolveLocal(varExpr, varExpr.Name);
        break;
      case Assign assignExpr:
        Resolve(assignExpr.Value);
        ResolveLocal(assignExpr, assignExpr.Name);
        break;
      case Binary(var left, var _, var right):
        Resolve(left);
        Resolve(right);
        break;
      case Call(var calle, var _, var arguments):
        Resolve(calle);
        foreach (var arg in arguments)
          Resolve(arg);
        break;
      case Grouping(var gExpr):
        Resolve(gExpr);
        break;
      case Literal(var lExpr):
        break;
      case Logical(var left, var _, var right):
        Resolve(left);
        Resolve(right);
        break;
      case Unary(var op, var right):
        Resolve(right);
        break;
      default:
        throw new Exception("Resolver:: Unkown expression.");
    }
  }

  private void ResolveFunction(Function function, FunctionType type)
  {
    var enclosingFn = currentFn;
    currentFn = type;

    BeginScope();
    foreach (var param in function.Parameters)
    {
      Declare(param);
      Define(param);
    }
    Resolve(function.Body);
    EndScope();

    currentFn = enclosingFn;
  }

  private void ResolveLocal(Expr expr, Token name)
  {
    for (int i = scopes.Count - 1; i >= 0; i--)
    {
      if (scopes[i].ContainsKey(name.lexeme))
      {
        interpreter.Resolve(expr, scopes.Count - 1 - i);
        return;
      }
    }
  }

  private void BeginScope()
  {
    ScopesPush([]);
  }

  private void EndScope()
  {
    ScopesPop();
  }

  private void Declare(Token name)
  {
    if (scopes.Count is 0) return;
    var scope = ScopesPeek();
    if (scope.ContainsKey(name.lexeme))
      Program.error(name, "Already a variable with this name in this scope.");
    scope[name.lexeme] = false;
  }

  private void Define(Token name)
  {
    if (scopes.Count is 0) return;
    ScopesPeek()[name.lexeme] = true;
  }

  private enum FunctionType
  {
    NONE,
    FUNCTION
  }
}

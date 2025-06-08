using static TokenType;

internal class Parser(List<Token> tokens)
{
    private readonly List<Token> tokens = tokens;
    private int current = 0;


    public List<Stmt> Parse()
    {
        List<Stmt> statements = [];
        while (!IsAtEnd())
        {
            statements.Add(Declaration());
        }

        return statements;
    }

    private Stmt Declaration()
    {
        try
        {
            if (Match(VAR)) return VarDeclaration();
            if (Match(FUN)) return Function("function");

            return Statement();
        }
        catch (ParseError e)
        {
            Synchronize();
            return null;
        }
    }

    private Stmt Statement()
    {
        if (Match(FOR)) return ForStatement();
        if (Match(PRINT)) return PrintStatement();
        if (Match(LEFT_BRACE)) return new Block(Block());
        if (Match(IF)) return IfStatement();
        if (Match(WHILE)) return WhileStatement();
        if (Match(RETURN)) return ReturnStatement();

        return ExpressionStatement();
    }

    private Stmt Function(string kind)
    {
      var name = Consume(IDENTIFIER, $"Expect {kind} name.");
      Consume(LEFT_PAREN, $"Expect '(' after {kind} name.");
      List<Token> parameters = [];
      if (!Check(RIGHT_PAREN))
      {
        do
        {
          if (parameters.Count >= 255)
          {
            Error(Peek(), "Can't have more than 255 parameters.");
          }

          parameters.Add(Consume(IDENTIFIER, "Expect parameter name."));
        } while (Match(COMMA));
      }
      Consume(RIGHT_PAREN, "Expect ')' after parameters");

      Consume(LEFT_BRACE, $"Expect '{{' before {kind} block.");
      var body = Block();

      return new Function(name, parameters, body);
    }

    private Stmt ReturnStatement()
    {
      var keyword = Previous();
      Expr? value = null;
      if (!Check(SEMICOLON))
      {
        value = Expression();
      }

      Consume(SEMICOLON, "Expect ';' after return value.");

      return new Return(keyword, value);
    }

    private Stmt ForStatement()
    {
      Consume(LEFT_PAREN, "Expect '(' after 'for'");

      Stmt? initializer = null;
      if (Match(SEMICOLON))
        initializer = null;
      else if (Match(VAR))
        initializer = VarDeclaration();
      else
        initializer = ExpressionStatement();

      Expr? cond = null;
      if (!Check(SEMICOLON))
        cond = Expression();
      Consume(SEMICOLON, "Expect ';' after loop condition.");

      Expr? increment = null;
      if (!Check(RIGHT_PAREN))
        increment = Expression();
      Consume(RIGHT_PAREN, "Expect ')' after for clauses.");

      var body = Statement();

      if (increment is not null)
      {
        body = new Block([
            body,
            new Expression(increment),
        ]);
      }

      if (cond is null)
        cond = new Literal(true);
      body = new While(cond, body);

      if (initializer is not null)
        body = new Block([initializer, body]);

      return body;
    }

    private Stmt WhileStatement()
    {
      Consume(LEFT_PAREN, "Expect '(' after an 'while'.");
      var condition = Expression();
      Consume(RIGHT_PAREN, "Expect ')' after condition.");

      var body = Statement();
      return new While(condition, body);
    }

    private Stmt IfStatement()
    {
      Consume(LEFT_PAREN, "Expect '(' after an 'if'.");
      var condition = Expression();
      Consume(RIGHT_PAREN, "Expect ')' after an if condition.");

      var thenBranch = Statement();
      Stmt? elseBranch = null;
      if (Match(ELSE))
        elseBranch = Statement();

      return new If(condition, thenBranch, elseBranch);
    }

    private List<Stmt> Block()
    {
        List<Stmt> stmts = [];

        while (!Check(RIGHT_BRACE) && !IsAtEnd())
        {
            stmts.Add(Declaration());
        }

        Consume(RIGHT_BRACE, "Expect '}' after block.");
        return stmts;
    }

    private Stmt VarDeclaration()
    {
        var name = Consume(IDENTIFIER, "Expect variable name.");

        Expr? initializer = null;
        if (Match(EQUAL))
        {
            initializer = Expression();
        }

        Consume(SEMICOLON, "Expect ';' after variable declaration.");
        return new Var(name, initializer);
    }

    private Stmt PrintStatement()
    {
        var value = Expression();
        Consume(SEMICOLON, "Expect ';' after value.");
        return new Print(value);
    }

    private Stmt ExpressionStatement()
    {
        var expr = Expression();
        Consume(SEMICOLON, "Expect ';' after expression.");

        return new Expression(expr);
    }

    private Expr Expression()
    {
        return Assignment();
    }

    private Expr Assignment()
    {
        var expr = Or();

        if (Match(EQUAL))
        {
            Token equals = Previous();
            Expr value = Assignment();

            if (expr is Variable v)
            {
                var name = v.Name;
                return new Assign(name, value);
            }

            Error(equals, "Invalid assignment target.");
        }
        return expr;
    }

    private Expr Or()
    {
      var expr = And();

      while (Match(OR))
      {
        var op = Previous();
        var right = And();
        expr = new Logical(expr, op, right);
      }

      return expr;
    }

    private Expr And()
    {
      var expr = Equality();

      while (Match(AND))
      {
        var op = Previous();
        var right = Equality();
        expr = new Logical(expr, op, right);
      }

      return expr;
    }

    private Expr Equality()
    {
        var expr = Comparison();

        while (Match(BANG_EQUAL, EQUAL_EQUAL))
        {
            var op = Previous();
            var right = Comparison();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }


    private Expr Comparison()
    {
        var expr = Term();

        while (Match(GREATER, GREATER_EQUAL, LESS, LESS_EQUAL))
        {
            var op = Previous();
            var right = Term();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }

    private Expr Term()
    {

        var expr = Factor();

        while (Match(MINUS, PLUS))
        {
            var op = Previous();
            var right = Factor();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }


    private Expr Factor()
    {
        Expr expr = Unary();

        while (Match(SLASH, STAR))
        {
            var op = Previous();
            Expr right = Unary();
            expr = new Binary(expr, op, right);
        }

        return expr;
    }


    private Expr Unary()
    {
        if (Match(BANG, MINUS))
        {
            var op = Previous();
            var right = Unary();
            return new Unary(op, right);
        }

        return Call();
    }

    private Expr Call()
    {
      var expr = Primary();

      while(true) {
        if (Match(LEFT_PAREN)) {
          expr = FinishCall(expr);
        } else {
          break;
        }
      }

      return expr;
    }

    private Expr FinishCall(Expr calle)
    {
      List<Expr> arguments = [];
      if (!Check(RIGHT_PAREN)) {
        do {
          if (arguments.Count >= 255) {
            Error(Peek(), "Can't have more than 255 arguments");
          }
          arguments.Add(Expression());
        } while (Match(COMMA));
      }

      var paren = Consume(RIGHT_PAREN, "Expect ')' after arguments.");

      return new Call(calle, paren, arguments);
    }


    private Expr Primary()
    {
        if (Match(FALSE)) return new Literal(false);
        if (Match(TRUE)) return new Literal(true);
        if (Match(NIL)) return new Literal(null);

        if (Match(NUMBER, STRING))
        {
            return new Literal(Previous().literal);
        }

        if (Match(IDENTIFIER))
        {
            return new Variable(Previous());
        }

        if (Match(LEFT_PAREN))
        {
            var expr = Expression();
            Consume(RIGHT_PAREN, "Expect ')' after expression.");
            return new Grouping(expr);
        }

        throw Error(Peek(), "Expect expression.");
    }

    private Token Consume(TokenType type, String message)
    {
        if (Check(type)) return Advance();

        throw Error(Peek(), message);
    }


    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            if (Previous().type == SEMICOLON) return;

            switch (Peek().type)
            {
                case CLASS:
                case FUN:
                case VAR:
                case FOR:
                case IF:
                case WHILE:
                case PRINT:
                case RETURN:
                    return;
            }

            Advance();
        }
    }


    private ParseError Error(Token token, String message)
    {
        Program.error(token, message);
        return new ParseError();
    }


    private bool Match(params List<TokenType> types)
    {
        foreach (TokenType type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }

        return false;
    }


    private bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Peek().type == type;
    }


    private Token Advance()
    {
        if (!IsAtEnd()) current++;
        return Previous();
    }

    private bool IsAtEnd()
    {
        return Peek().type == EOF;
    }

    private Token Peek()
    {
        return tokens[current];
    }

    private Token Previous()
    {
        return tokens[current - 1];
    }


    private class ParseError : Exception;
}

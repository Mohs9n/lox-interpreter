using static TokenType;

internal class Parser(List<Token> tokens)
{
    private readonly List<Token> tokens = tokens;
    private int current = 0;
    

    
      public Expr? Parse() {
        try {
          return Expression();
        } catch (ParseError)
        {
          return null;
        }
      }

    private Expr Expression()
    {
        return Equality();
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

        return Primary();
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
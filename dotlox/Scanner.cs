using static TokenType;

class Scanner(string source)
{
  private readonly string source = source;
  private readonly List<Token> tokens = [];

  private int start = 0;
  private int current = 0;
  private int line = 1;
  private bool isAtEnd => current >= source.Length;
  private int subStringLength => current - start;

  internal List<Token> scanTokens()
  {
    while (!isAtEnd)
    {
      start = current;
      scanToken();
    }

    tokens.Add(new(EOF, "", null, line));
    return tokens;
  }

  private void scanToken() {
    char c = advance();
    switch (c) {
      case '(': addToken(LEFT_PAREN); break;
      case ')': addToken(RIGHT_PAREN); break;
      case '{': addToken(LEFT_BRACE); break;
      case '}': addToken(RIGHT_BRACE); break;
      case ',': addToken(COMMA); break;
      case '.': addToken(DOT); break;
      case '-': addToken(MINUS); break;
      case '+': addToken(PLUS); break;
      case ';': addToken(SEMICOLON); break;
      case '*': addToken(STAR); break; 
      case '!':
          addToken(match('=')? BANG_EQUAL: BANG);
          break;
      case '=':
          addToken(match('=')? EQUAL_EQUAL : EQUAL);
          break;
      case '<':
          addToken(match('=')? TokenType.LESS_EQUAL : LESS);
          break;
      case '>':
          addToken(match('=')? GREATER_EQUAL : GREATER);
          break;
      case '/':
          if (match('/'))
          {
            while(peek() != '\n' && !isAtEnd)
              advance();
          } else {
            addToken(SLASH);
          }
          break;
      case ' ':
      case '\r':
      case '\t':
        break;
      case '\n':
        line++;
        break;
      case '"':
        consume_string();
        break;
      default:
        if (isDigit(c)) {
          consume_number();
        } else if (isAlpha(c)) {
          identifier();
        } else {
          Program.error(line, "Unexpected character");
        }
        break;
    }
  }

  private void identifier() {
    while (isAlphaNumeric(peek())) advance();

    string text = source.Substring(start, subStringLength);
    var found = keywords.TryGetValue(text, out var type);
    if (!found)
      type = IDENTIFIER;

    addToken(type);
  }


  private void consume_number() {
    while(isDigit(peek())) advance();

    if (peek() == '.' && isDigit(peekNext())) {
      advance();
      while(isDigit(peek())) advance();
    }

    addToken(NUMBER, double.Parse(source.Substring(start, subStringLength)));
  }

  private void consume_string() {
    while (peek() != '"' && !isAtEnd)
    {
      if (peek() == '\n') line++;
      advance();
    }

    if (isAtEnd) {
      Program.error(line, "Unterminated string.");
      return;
    }

    advance();

    string value = source.Substring(start + 1, subStringLength - 2);
    addToken(STRING, value);
  }

  private bool isAlphaNumeric(char c) {
    return isAlpha(c) || isDigit(c);
  }

  private bool isAlpha(char c) {
    return (c >= 'a' && c <= 'z') ||
           (c >= 'A' && c <= 'Z') ||
            c == '_';
  }

  private bool isDigit(char c) {
    return c >= '0' && c <= '9';
  } 

  private bool match(char expected)
  {
    if (isAtEnd) return false;
    if (source[current] != expected) return false;

    current++;
    return true;
  }

  private char peek() {
    if (isAtEnd) return '\0';
    return source[current];
  }

  private char peekNext() {
    if (current + 1 >= source.Length) return '\0';

    return source[current+1];
  }


  private char advance()
  {
    return source[current++];
  }


  private void addToken(TokenType type) {
    addToken(type, null);
  }

  private void addToken(TokenType type, object? literal) {
    string text = source.Substring(start, subStringLength); //TODO: check
    tokens.Add(new(type, text, literal, line));
  }

  public static readonly Dictionary<string, TokenType> keywords = new()
  {
      { "and",    AND },
      { "class",  CLASS },
      { "else",   ELSE },
      { "false",  FALSE },
      { "for",    FOR },
      { "fun",    FUN },
      { "if",     IF },
      { "nil",    NIL },
      { "or",     OR },
      { "print",  PRINT },
      { "return", RETURN },
      { "super",  SUPER },
      { "this",   THIS },
      { "true",   TRUE },
      { "var",    VAR },
      { "while",  WHILE }
  };
}

class Token(TokenType type, string lexeme, object? literal, int line)
{
  readonly TokenType type = type;
  readonly string lexeme = lexeme;
  readonly object? literal = literal;
  readonly int line = line;

  public override string ToString()
  {
    return $"{type} {lexeme} {literal}";
  }
}

enum TokenType {
  // Single-character tokens.
  LEFT_PAREN, RIGHT_PAREN, LEFT_BRACE, RIGHT_BRACE,
  COMMA, DOT, MINUS, PLUS, SEMICOLON, SLASH, STAR,

  // One or two character tokens.
  BANG, BANG_EQUAL,
  EQUAL, EQUAL_EQUAL,
  GREATER, GREATER_EQUAL,
  LESS, LESS_EQUAL,

  // Literals.
  IDENTIFIER, STRING, NUMBER,

  // Keywords.
  AND, CLASS, ELSE, FALSE, FUN, FOR, IF, NIL, OR,
  PRINT, RETURN, SUPER, THIS, TRUE, VAR, WHILE,

  EOF
}

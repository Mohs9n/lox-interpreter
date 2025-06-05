internal class LoxEnvironment(LoxEnvironment? enclosing = null)
{
    private readonly LoxEnvironment? enclosing = enclosing;

    private readonly Dictionary<string, object?> values = [];

    public object? Get(Token name)
    {
        var found = values.TryGetValue(name.lexeme, out var val);
        if (found)
            return val;

        if (enclosing is not null)
            return enclosing.Get(name);
        
        throw new RuntimeError(name, $"Undefined variable {name.lexeme}.");
    }
    public void Define(string name, object? value)
    {
        // values.Add(name, value);
        values[name] = value;
    }

    public void Assign(Token name, object? value)
    {
        if (values.ContainsKey(name.lexeme))
        {
            values[name.lexeme] = value;
            return;
        }

        if (enclosing is not null)
        {
            enclosing.Assign(name, value);
            return;
        }

        throw new RuntimeError(name, $"Undefined variable {name.lexeme}.");
    }
}

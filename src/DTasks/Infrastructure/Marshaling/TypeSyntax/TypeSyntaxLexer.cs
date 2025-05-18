using System.Collections.Frozen;

namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

internal ref struct TypeSyntaxLexer
{
    private static readonly FrozenSet<char> s_nonWordChars = ".,= `[]-+*&@\0".ToFrozenSet();
    
    private readonly string _text;
    private int _position;
    private int _length;

    public TypeSyntaxLexer(string text)
    {
        _text = text;
        _position = _text.Length;
        _length = 0;
        Kind = TypeSyntaxTokenKind.EndOfText;
    }
    
    public TypeSyntaxTokenKind Kind { get; private set; }
    
    public ReadOnlySpan<char> Value => _text.AsSpan(_position, _length);

    public bool ValueMatch(string value) => Value.SequenceEqual(value);

    private char Current
    {
        get
        {
            int index = _position - _length;
            if (index < 0)
                return '\0';
            
            return _text[index];
        }
    }
    
    public bool MoveNext()
    {
        if (Kind is TypeSyntaxTokenKind.StartOfText)
            return false;
        
        _length = 0;
        _position--;

        if (_position == -1)
        {
            _length = 0;
            Kind = TypeSyntaxTokenKind.StartOfText;
            return true;
        }
        
        char current = Current;
        _length++;

        return current switch
        {
            '.' => Token(TypeSyntaxTokenKind.Dot),
            ',' => Token(TypeSyntaxTokenKind.Comma),
            '=' => Token(TypeSyntaxTokenKind.Equal),
            ' ' => Token(TypeSyntaxTokenKind.Space),
            '`' => Token(TypeSyntaxTokenKind.Backtick),
            '[' => Token(TypeSyntaxTokenKind.LeftBracket),
            ']' => Token(TypeSyntaxTokenKind.RightBracket),
            '-' => Token(TypeSyntaxTokenKind.Minus),
            '+' => Token(TypeSyntaxTokenKind.Plus),
            '*' => Token(TypeSyntaxTokenKind.Star),
            '&' => Token(TypeSyntaxTokenKind.Ampersand),
            '@' => Token(TypeSyntaxTokenKind.At),
            '\0' => Token(TypeSyntaxTokenKind.Error),
            _ => Word()
        };
    }

    private bool Word()
    {
        EatWhile(IsWordChar);
        
        return Token(TypeSyntaxTokenKind.Word);
    }

    private bool Token(TypeSyntaxTokenKind kind)
    {
        _position -= _length - 1;
        Kind = kind;
        
        return true;
    }

    private void EatWhile(Predicate<char> predicate)
    {
        while (predicate(Current))
        {
            _length++;
        }
    }

    private static bool IsWordChar(char c) => !s_nonWordChars.Contains(c);
}

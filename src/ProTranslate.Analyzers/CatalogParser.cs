using System;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ProTranslate.Analyzers;

internal static class CatalogParser
{
    public static CatalogFile Parse(string path, SourceText text)
    {
        if (!CatalogFileNames.IsJsonCatalogFile(path))
        {
            return ParseTextCatalog(path, text);
        }

        string content = text.ToString();
        int firstNonWhitespace = 0;
        while (firstNonWhitespace < content.Length && char.IsWhiteSpace(content[firstNonWhitespace]))
        {
            firstNonWhitespace++;
        }

        if (firstNonWhitespace >= content.Length || content[firstNonWhitespace] != '{')
        {
            return new CatalogFile(
                path,
                text,
                CatalogFileNames.TryGetCultureName(path),
                ImmutableArray<KeyEntry>.Empty,
                ImmutableArray.Create(CreateInvalidCatalogDiagnostic(path, text, firstNonWhitespace, "Expected root object.")));
        }

        return JsonCatalogParser.Parse(path, text);
    }

    private static CatalogFile ParseTextCatalog(string path, SourceText text)
    {
        var keys = ImmutableArray.CreateBuilder<KeyEntry>();

        foreach (TextLine line in text.Lines)
        {
            string lineText = line.ToString();
            string key = lineText.Trim();
            if (key.Length == 0 || key.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            int offset = lineText.IndexOf(key, StringComparison.Ordinal);
            int position = line.Start + Math.Max(0, offset);
            keys.Add(new KeyEntry(key, value: null, CreateLocation(path, text, position, key.Length)));
        }

        return new CatalogFile(path, text, cultureName: null, keys.ToImmutable(), ImmutableArray<Diagnostic>.Empty);
    }

    private static Diagnostic CreateInvalidCatalogDiagnostic(string path, SourceText text, int position, string message)
    {
        return Diagnostic.Create(
            DiagnosticDescriptors.InvalidCatalog,
            CreateLocation(path, text, Math.Min(position, text.Length), 0),
            System.IO.Path.GetFileName(path),
            message);
    }

    private static Location CreateLocation(string path, SourceText text, int position, int length)
    {
        var span = new TextSpan(Math.Min(position, text.Length), Math.Min(length, Math.Max(0, text.Length - position)));
        return Location.Create(path, span, text.Lines.GetLinePositionSpan(span));
    }

    private sealed class JsonCatalogParser
    {
        private readonly string _path;
        private readonly SourceText _text;
        private readonly string _content;
        private readonly ImmutableArray<KeyEntry>.Builder _keys = ImmutableArray.CreateBuilder<KeyEntry>();
        private readonly ImmutableArray<Diagnostic>.Builder _diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        private int _position;

        private JsonCatalogParser(string path, SourceText text)
        {
            _path = path;
            _text = text;
            _content = text.ToString();
        }

        public static CatalogFile Parse(string path, SourceText text)
        {
            var parser = new JsonCatalogParser(path, text);
            parser.ParseRoot();
            return new CatalogFile(
                path,
                text,
                CatalogFileNames.TryGetCultureName(path),
                parser._keys.ToImmutable(),
                parser._diagnostics.ToImmutable());
        }

        private bool IsEnd => _position >= _content.Length;

        private char Current => IsEnd ? '\0' : _content[_position];

        private void ParseRoot()
        {
            SkipWhitespace();
            if (!TryConsume('{'))
            {
                AddInvalidCatalog("Expected root object.");
                return;
            }

            ParseObjectMembers(prefix: string.Empty);
            if (_diagnostics.Count > 0)
            {
                return;
            }

            SkipWhitespace();
            if (!IsEnd)
            {
                AddInvalidCatalog("Unexpected content after root object.");
            }
        }

        private void ParseObjectMembers(string prefix)
        {
            SkipWhitespace();
            if (TryConsume('}'))
            {
                return;
            }

            while (!IsEnd)
            {
                SkipWhitespace();
                int nameStart = _position;
                if (!TryParseString(out string propertyName))
                {
                    AddInvalidCatalog("Expected property name.");
                    return;
                }

                SkipWhitespace();
                if (!TryConsume(':'))
                {
                    AddInvalidCatalog("Expected ':' after property name.");
                    return;
                }

                string key = prefix.Length == 0 ? propertyName : prefix + "." + propertyName;
                SkipWhitespace();

                if (TryConsume('{'))
                {
                    ParseObjectMembers(key);
                }
                else if (Current == '"')
                {
                    int valueStart = _position;
                    if (!TryParseString(out string value))
                    {
                        return;
                    }

                    _keys.Add(new KeyEntry(key, value, CreateLocation(_path, _text, nameStart, Math.Max(1, valueStart - nameStart))));
                }
                else if (!TrySkipValue())
                {
                    return;
                }

                SkipWhitespace();
                if (TryConsume('}'))
                {
                    return;
                }

                if (!TryConsume(','))
                {
                    AddInvalidCatalog("Expected ',' or '}' after property value.");
                    return;
                }
            }

            AddInvalidCatalog("Unterminated object.");
        }

        private bool TryParseString(out string value)
        {
            value = string.Empty;
            if (!TryConsume('"'))
            {
                return false;
            }

            var builder = new StringBuilder();
            while (!IsEnd)
            {
                char character = _content[_position++];
                if (character == '"')
                {
                    value = builder.ToString();
                    return true;
                }

                if (character != '\\')
                {
                    builder.Append(character);
                    continue;
                }

                if (IsEnd)
                {
                    AddInvalidCatalog("Unterminated escape sequence.");
                    return false;
                }

                char escaped = _content[_position++];
                switch (escaped)
                {
                    case '"':
                    case '\\':
                    case '/':
                        builder.Append(escaped);
                        break;
                    case 'b':
                        builder.Append('\b');
                        break;
                    case 'f':
                        builder.Append('\f');
                        break;
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    case 'u':
                        if (!TryReadUnicodeEscape(out char unicode))
                        {
                            return false;
                        }

                        builder.Append(unicode);
                        break;
                    default:
                        AddInvalidCatalog("Invalid escape sequence.");
                        return false;
                }
            }

            AddInvalidCatalog("Unterminated string.");
            return false;
        }

        private bool TryReadUnicodeEscape(out char value)
        {
            value = '\0';
            if (_position + 4 > _content.Length)
            {
                AddInvalidCatalog("Incomplete unicode escape sequence.");
                return false;
            }

            int code = 0;
            for (int i = 0; i < 4; i++)
            {
                int digit = HexValue(_content[_position++]);
                if (digit < 0)
                {
                    AddInvalidCatalog("Invalid unicode escape sequence.");
                    return false;
                }

                code = (code << 4) + digit;
            }

            value = (char)code;
            return true;
        }

        private bool TrySkipValue()
        {
            switch (Current)
            {
                case '[':
                    return TrySkipArray();
                case 't':
                    return TryConsumeLiteral("true");
                case 'f':
                    return TryConsumeLiteral("false");
                case 'n':
                    return TryConsumeLiteral("null");
                default:
                    if (Current == '-' || char.IsDigit(Current))
                    {
                        return TrySkipNumber();
                    }

                    AddInvalidCatalog("Expected JSON value.");
                    return false;
            }
        }

        private bool TrySkipArray()
        {
            if (!TryConsume('['))
            {
                return false;
            }

            SkipWhitespace();
            if (TryConsume(']'))
            {
                return true;
            }

            while (!IsEnd)
            {
                SkipWhitespace();
                if (Current == '"')
                {
                    if (!TryParseString(out _))
                    {
                        return false;
                    }
                }
                else if (Current == '{')
                {
                    if (!TrySkipObject())
                    {
                        return false;
                    }
                }
                else if (Current == '[')
                {
                    if (!TrySkipArray())
                    {
                        return false;
                    }
                }
                else if (!TrySkipValue())
                {
                    return false;
                }

                SkipWhitespace();
                if (TryConsume(']'))
                {
                    return true;
                }

                if (!TryConsume(','))
                {
                    AddInvalidCatalog("Expected ',' or ']' after array value.");
                    return false;
                }
            }

            AddInvalidCatalog("Unterminated array.");
            return false;
        }

        private bool TrySkipObject()
        {
            if (!TryConsume('{'))
            {
                return false;
            }

            SkipWhitespace();
            if (TryConsume('}'))
            {
                return true;
            }

            while (!IsEnd)
            {
                SkipWhitespace();
                if (!TryParseString(out _))
                {
                    AddInvalidCatalog("Expected property name.");
                    return false;
                }

                SkipWhitespace();
                if (!TryConsume(':'))
                {
                    AddInvalidCatalog("Expected ':' after property name.");
                    return false;
                }

                SkipWhitespace();
                if (Current == '"')
                {
                    if (!TryParseString(out _))
                    {
                        return false;
                    }
                }
                else if (Current == '{')
                {
                    if (!TrySkipObject())
                    {
                        return false;
                    }
                }
                else if (!TrySkipValue())
                {
                    return false;
                }

                SkipWhitespace();
                if (TryConsume('}'))
                {
                    return true;
                }

                if (!TryConsume(','))
                {
                    AddInvalidCatalog("Expected ',' or '}' after property value.");
                    return false;
                }
            }

            AddInvalidCatalog("Unterminated object.");
            return false;
        }

        private bool TrySkipNumber()
        {
            if (Current == '-')
            {
                _position++;
            }

            if (IsEnd || !char.IsDigit(Current))
            {
                AddInvalidCatalog("Invalid number.");
                return false;
            }

            while (!IsEnd && char.IsDigit(Current))
            {
                _position++;
            }

            if (!IsEnd && Current == '.')
            {
                _position++;
                if (IsEnd || !char.IsDigit(Current))
                {
                    AddInvalidCatalog("Invalid number.");
                    return false;
                }

                while (!IsEnd && char.IsDigit(Current))
                {
                    _position++;
                }
            }

            if (!IsEnd && (Current == 'e' || Current == 'E'))
            {
                _position++;
                if (!IsEnd && (Current == '+' || Current == '-'))
                {
                    _position++;
                }

                if (IsEnd || !char.IsDigit(Current))
                {
                    AddInvalidCatalog("Invalid number.");
                    return false;
                }

                while (!IsEnd && char.IsDigit(Current))
                {
                    _position++;
                }
            }

            return true;
        }

        private bool TryConsumeLiteral(string literal)
        {
            if (_position + literal.Length > _content.Length)
            {
                AddInvalidCatalog("Expected JSON value.");
                return false;
            }

            for (int i = 0; i < literal.Length; i++)
            {
                if (_content[_position + i] != literal[i])
                {
                    AddInvalidCatalog("Expected JSON value.");
                    return false;
                }
            }

            _position += literal.Length;
            return true;
        }

        private void SkipWhitespace()
        {
            while (!IsEnd && char.IsWhiteSpace(Current))
            {
                _position++;
            }
        }

        private bool TryConsume(char expected)
        {
            if (Current != expected)
            {
                return false;
            }

            _position++;
            return true;
        }

        private void AddInvalidCatalog(string message)
        {
            _diagnostics.Add(CreateInvalidCatalogDiagnostic(_path, _text, _position, message));
        }

        private static int HexValue(char character)
        {
            if (character >= '0' && character <= '9')
            {
                return character - '0';
            }

            if (character >= 'a' && character <= 'f')
            {
                return character - 'a' + 10;
            }

            if (character >= 'A' && character <= 'F')
            {
                return character - 'A' + 10;
            }

            return -1;
        }
    }
}

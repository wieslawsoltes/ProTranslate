using System;
using System.Collections.Generic;
using System.Text;

namespace ProTranslate.Analyzers;

internal static class KeyIdentifierHelpers
{
    public static Dictionary<string, string> CreateGeneratedAccessorMap(IEnumerable<string> keys)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var identifiers = new List<KeyValuePair<string, string>>();

        foreach (string key in keys)
        {
            identifiers.Add(new KeyValuePair<string, string>(key, CreateIdentifier(key)));
        }

        var collisions = new HashSet<string>(StringComparer.Ordinal);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> identifier in identifiers)
        {
            counts.TryGetValue(identifier.Value, out int count);
            counts[identifier.Value] = count + 1;
        }

        foreach (KeyValuePair<string, int> count in counts)
        {
            if (count.Value > 1)
            {
                collisions.Add(count.Key);
            }
        }

        foreach (KeyValuePair<string, string> identifier in identifiers)
        {
            string name = identifier.Value;
            if (collisions.Contains(name))
            {
                name = name + "_" + ComputeStableHash(identifier.Key);
            }

            while (!usedNames.Add(name))
            {
                name = name + "_Key";
            }

            map["Get_" + name] = identifier.Key;
            map["Value_" + name] = identifier.Key;
            map["Format_" + name] = identifier.Key;
            map["Observe_" + name] = identifier.Key;
        }

        return map;
    }

    private static string CreateIdentifier(string key)
    {
        var builder = new StringBuilder(key.Length);
        bool capitalizeNext = true;

        foreach (char character in key)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(capitalizeNext ? char.ToUpperInvariant(character) : character);
                capitalizeNext = false;
            }
            else
            {
                capitalizeNext = true;
            }
        }

        if (builder.Length == 0 || char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }

        string identifier = builder.ToString();
        return IsCSharpKeyword(identifier) ? "_" + identifier : identifier;
    }

    private static string ComputeStableHash(string value)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        uint hash = offsetBasis;
        foreach (char character in value)
        {
            hash ^= character;
            hash *= prime;
        }

        return hash.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool IsCSharpKeyword(string value)
    {
        switch (value)
        {
            case "abstract":
            case "as":
            case "base":
            case "bool":
            case "break":
            case "byte":
            case "case":
            case "catch":
            case "char":
            case "checked":
            case "class":
            case "const":
            case "continue":
            case "decimal":
            case "default":
            case "delegate":
            case "do":
            case "double":
            case "else":
            case "enum":
            case "event":
            case "explicit":
            case "extern":
            case "false":
            case "finally":
            case "fixed":
            case "float":
            case "for":
            case "foreach":
            case "goto":
            case "if":
            case "implicit":
            case "in":
            case "int":
            case "interface":
            case "internal":
            case "is":
            case "lock":
            case "long":
            case "namespace":
            case "new":
            case "null":
            case "object":
            case "operator":
            case "out":
            case "override":
            case "params":
            case "private":
            case "protected":
            case "public":
            case "readonly":
            case "ref":
            case "return":
            case "sbyte":
            case "sealed":
            case "short":
            case "sizeof":
            case "stackalloc":
            case "static":
            case "string":
            case "struct":
            case "switch":
            case "this":
            case "throw":
            case "true":
            case "try":
            case "typeof":
            case "uint":
            case "ulong":
            case "unchecked":
            case "unsafe":
            case "ushort":
            case "using":
            case "virtual":
            case "void":
            case "volatile":
            case "while":
                return true;
            default:
                return false;
        }
    }
}

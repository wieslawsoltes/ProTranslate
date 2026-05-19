using System;

namespace ProTranslate.Analyzers;

internal static class FormatPlaceholderCounter
{
    public static bool TryGetRequiredArgumentCount(string format, out int count)
    {
        int maxIndex = -1;

        for (int i = 0; i < format.Length; i++)
        {
            char character = format[i];
            if (character == '{')
            {
                if (i + 1 < format.Length && format[i + 1] == '{')
                {
                    i++;
                    continue;
                }

                i++;
                if (i >= format.Length || !char.IsDigit(format[i]))
                {
                    count = 0;
                    return false;
                }

                int index = 0;
                while (i < format.Length && char.IsDigit(format[i]))
                {
                    checked
                    {
                        index = (index * 10) + (format[i] - '0');
                    }

                    i++;
                }

                maxIndex = Math.Max(maxIndex, index);

                while (i < format.Length && char.IsWhiteSpace(format[i]))
                {
                    i++;
                }

                if (i < format.Length && format[i] == ',')
                {
                    i++;
                    while (i < format.Length && char.IsWhiteSpace(format[i]))
                    {
                        i++;
                    }

                    if (i < format.Length && (format[i] == '-' || format[i] == '+'))
                    {
                        i++;
                    }

                    if (i >= format.Length || !char.IsDigit(format[i]))
                    {
                        count = 0;
                        return false;
                    }

                    while (i < format.Length && char.IsDigit(format[i]))
                    {
                        i++;
                    }
                }

                if (i < format.Length && format[i] == ':')
                {
                    i++;
                    while (i < format.Length)
                    {
                        if (format[i] == '{')
                        {
                            if (i + 1 < format.Length && format[i + 1] == '{')
                            {
                                i += 2;
                                continue;
                            }

                            count = 0;
                            return false;
                        }

                        if (format[i] == '}')
                        {
                            break;
                        }

                        i++;
                    }
                }

                if (i >= format.Length || format[i] != '}')
                {
                    count = 0;
                    return false;
                }
            }
            else if (character == '}')
            {
                if (i + 1 < format.Length && format[i + 1] == '}')
                {
                    i++;
                    continue;
                }

                count = 0;
                return false;
            }
        }

        count = maxIndex + 1;
        return true;
    }
}

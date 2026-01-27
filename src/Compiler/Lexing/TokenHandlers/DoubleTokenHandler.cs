using System.Text;
using Tutel.Core.Compiler.Lexing.Abstractions;
using Tutel.Core.Compiler.Lexing.Exceptions;
using Tutel.Core.Compiler.Lexing.Models.Enums;
using Tutel.Core.Compiler.Lexing.Models.Tokens;

namespace Tutel.Compiler.Lexing.TokenHandlers;

public class DoubleTokenHandler : TokenHandlerBase
{
    public override Token? Handle(ISourceReader reader)
    {
        // Проверяем, начинается ли с цифры или точки
        if (!char.IsDigit(reader.Current) && reader.Current != '.')
        {
            return Next?.Handle(reader);
        }

        int startLine = reader.Line;
        int startColumn = reader.Column;
        var builder = new StringBuilder();

        bool hasDot = false;
        bool hasExponent = false;
        bool hasDigits = false;

        // Обрабатываем начальную часть (до точки или экспоненты)
        if (reader.Current == '.')
        {
            builder.Append(reader.Current);
            hasDot = true;
            reader.MoveNext();
        }
        else
        {
            // Обрабатываем целую часть
            while (char.IsDigit(reader.Current))
            {
                builder.Append(reader.Current);
                hasDigits = true;
                if (!char.IsDigit(reader.Peek()))
                    break;
                reader.MoveNext();
            }

            // Проверяем точку
            if (reader.Peek() == '.')
            {
                reader.MoveNext();
                builder.Append(reader.Current);
                hasDot = true;
            }
        }

        // Обрабатываем дробную часть
        if (hasDot)
        {
            while (char.IsDigit(reader.Peek()))
            {
                reader.MoveNext();
                builder.Append(reader.Current);
                hasDigits = true;
            }
        }

        // Проверяем экспоненту (e или E)
        if (reader.Peek() == 'e' || reader.Peek() == 'E')
        {
            reader.MoveNext();
            builder.Append(reader.Current);
            hasExponent = true;

            // Знак экспоненты
            if (reader.Peek() == '+' || reader.Peek() == '-')
            {
                reader.MoveNext();
                builder.Append(reader.Current);
            }

            // Цифры экспоненты
            if (!char.IsDigit(reader.Peek()))
            {
                throw new LexicalException($"Invalid exponent in double literal at {startLine}:{startColumn}");
            }

            while (char.IsDigit(reader.Peek()))
            {
                reader.MoveNext();
                builder.Append(reader.Current);
            }
        }

        string numberStr = builder.ToString();

        if (!hasDot && !hasExponent)
        {
            // Это целое число. Возвращаем IntegerType здесь же, чтобы не требовать "отката" reader.
            if (numberStr.Length > 20)
                throw new LexicalException($"Number too large at {startLine}:{startColumn}");

            if (!long.TryParse(numberStr, out _))
                throw new LexicalException($"Invalid integer literal at {startLine}:{startColumn}");

            return new Token(
                TokenType.IntegerType,
                numberStr,
                startLine,
                startColumn);
        }

        // Проверяем, что есть хотя бы одна цифра
        if (!hasDigits)
        {
            throw new LexicalException($"Invalid double literal at {startLine}:{startColumn}");
        }

        // Пытаемся распарсить как double
        if (!double.TryParse(numberStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _))
        {
            throw new LexicalException($"Invalid double literal at {startLine}:{startColumn}");
        }

        return new Token(
            TokenType.DoubleLiteral,
            numberStr,
            startLine,
            startColumn);
    }
}

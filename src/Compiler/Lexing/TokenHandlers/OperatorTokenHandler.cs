using Tutel.Core.Compiler.Lexing.Abstractions;
using Tutel.Core.Compiler.Lexing.Models.Enums;
using Tutel.Core.Compiler.Lexing.Models.Tokens;

namespace Tutel.Compiler.Lexing.TokenHandlers;

public class OperatorTokenHandler : TokenHandlerBase
{
    private readonly HashSet<char> _singleCharOperators =
    [
        '+', '-', '*', '/', '!', '<', '>', '=', '%', '|', '&'
    ];

    private readonly HashSet<string> _twoCharOperators =
    [
        "==", "!=", "<=", ">=", "&&", "||"
    ];

    private readonly IEnumerable<char> _allOperatorsFirstLetters;

    public OperatorTokenHandler()
    {
        _allOperatorsFirstLetters = new HashSet<char>(_singleCharOperators)
            .Concat(_twoCharOperators.Select(op => op[0]));
    }

    public override Token? Handle(ISourceReader reader)
    {
        if (!_allOperatorsFirstLetters.Contains(reader.Current))
            return Next?.Handle(reader);

        int startLine = reader.Line;
        int startColumn = reader.Column;
        string op;

        if (!reader.IsEndOfFile)
        {
            char secondChar = reader.Peek();
            string potentialTwoChar = $"{reader.Current}{secondChar}";

            if (_twoCharOperators.Contains(potentialTwoChar))
            {
                op = potentialTwoChar;
                reader.MoveNext();
                return new Token(
                    TokenType.Operator,
                    op,
                    startLine,
                    startColumn);
            }
        }

        if (!_singleCharOperators.Contains(reader.Current)) return Next?.Handle(reader);

        op = reader.Current.ToString();
        return new Token(
            TokenType.Operator,
            op,
            startLine,
            startColumn);
    }
}
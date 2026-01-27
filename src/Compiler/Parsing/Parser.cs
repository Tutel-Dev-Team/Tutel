using System.Collections.ObjectModel;
using Tutel.Core.Compiler.AST;
using Tutel.Core.Compiler.AST.Declarations;
using Tutel.Core.Compiler.AST.Expressions;
using Tutel.Core.Compiler.AST.Expressions.Literals;
using Tutel.Core.Compiler.AST.Statements;
using Tutel.Core.Compiler.AST.Types;
using Tutel.Core.Compiler.Lexing.Models.Enums;
using Tutel.Core.Compiler.Lexing.Models.Tokens;
using Tutel.Core.Compiler.Parsing.Exception;
using Tutel.Core.Compiler.Parsing.Models;

namespace Tutel.Compiler.Parsing;

public class Parser
{
    private readonly ParseContext _context;

    public Parser(ParseContext context)
    {
        _context = context;
    }

    public ProgramAst ParseProgram()
    {
        var declarations = new List<DeclarationAst>();

        while (!_context.IsAtEnd)
        {
            declarations.Add(ParseDeclaration());
        }

        return new ProgramAst()
        {
            Declarations = declarations,
        };
    }

    private DeclarationAst ParseDeclaration()
    {
        if (_context.Match("func"))
        {
            return ParseFunctionDeclaration();
        }
        else if (_context.Match("int") || _context.Match("double"))
        {
            return ParseVariableDeclaration();
        }

        throw new ParseException("Unknown token", _context.Current.Line, _context.Current.Column);
    }

    private FunctionDeclaration ParseFunctionDeclaration()
    {
        _context.Advance();
        TypeNode returnType = ParseType();
        string name = _context.Consume(TokenType.Identifier, "Expected function name").Value;

        _context.Consume("(", "Expected '('");

        var parameters = new List<Parameter>();
        if (!_context.Match(")"))
        {
            do
            {
                parameters.Add(ParseParameter());
            }
            while (_context.TryConsume(","));
        }

        _context.Consume(")", "Expected ')'");

        BlockStatement body = ParseBlockStatement();

        return new FunctionDeclaration(name, body, returnType)
        {
            Parameters = parameters,
            Line = _context.Current.Line,
            Column = _context.Current.Column,
        };
    }

    private GlobalVariableDeclaration ParseVariableDeclaration()
    {
        TypeNode type = ParseType();
        string name = _context.Consume(TokenType.Identifier, "Expected variable name").Value;

        int line = _context.Current.Line;
        int column = _context.Current.Column;

        ExpressionAst? initializer = null;

        if (_context.TryConsume("="))
        {
            initializer = ParseExpression();
        }

        _context.Consume(";", "Expected ';'");

        return new GlobalVariableDeclaration(name, type, initializer)
        {
            Line = line,
            Column = column,
        };
    }

    private Parameter ParseParameter()
    {
        TypeNode type = ParseType();
        string name = _context.Consume(TokenType.Identifier, "Expected parameter name").Value;

        return new Parameter(name, type);
    }

    private TypeNode ParseType()
    {
        if (_context.TryConsume("int"))
        {
            if (_context.TryConsume("["))
            {
                _context.Consume("]", "Expected ']' after '['");

                if (_context.TryConsume("["))
                {
                    throw new ParseException(
                        "Multi-dimensional arrays are not supported. Use only one pair of [].",
                        _context.Current.Line,
                        _context.Current.Column);
                }

                return new ArrayType(new IntType());
            }

            return new IntType();
        }
        else if (_context.TryConsume("double"))
        {
            if (_context.TryConsume("["))
            {
                _context.Consume("]", "Expected ']' after '['");

                if (_context.TryConsume("["))
                {
                    throw new ParseException(
                        "Multi-dimensional arrays are not supported. Use only one pair of [].",
                        _context.Current.Line,
                        _context.Current.Column);
                }

                return new ArrayType(new DoubleType());
            }

            return new DoubleType();
        }
        else if (_context.TryConsume("void"))
        {
            if (_context.TryConsume("["))
            {
                throw new ParseException(
                    "Cannot create array of void",
                    _context.Current.Line,
                    _context.Current.Column);
            }

            return new VoidType();
        }
        else
        {
            throw new ParseException("Wrong type name", _context.Current.Line, _context.Current.Column);
        }
    }

    private ExpressionAst ParseExpression()
    {
        return ParseAssignment();
    }

    private ExpressionAst ParseAssignment()
    {
        ExpressionAst expr = ParseLogicalOr();

        if (_context.TryConsume("="))
        {
            Token operatorToken = _context.Previous;

            if (expr is IdentifierExpression identifier)
            {
                ExpressionAst value = ParseAssignment();
                return new AssignmentExpression(
                    identifier,
                    operatorToken,
                    value)
                {
                    Line = _context.Current.Line,
                    Column = _context.Current.Column,
                };
            }
            else if (expr is ArrayAccessExpression arrayAccess)
            {
                ExpressionAst value = ParseAssignment();
                return new ArrayAssignmentExpression(
                    arrayAccess,
                    operatorToken,
                    value)
                {
                    Line = _context.Current.Line,
                    Column = _context.Current.Column,
                };
            }

            throw new ParseException(
                "Left side of assignment must be a variable or array element",
                _context.Current.Line,
                _context.Current.Column);
        }

        return expr;
    }

    private ExpressionAst ParseLogicalOr()
    {
        ExpressionAst expr = ParseLogicalAnd();

        while (_context.TryConsume("||"))
        {
            Token op = _context.Previous;
            ExpressionAst right = ParseLogicalAnd();
            expr = new BinaryExpression(expr, op, right)
            {
                Line = _context.Current.Line,
                Column = _context.Current.Column,
            };
        }

        return expr;
    }

    private ExpressionAst ParseLogicalAnd()
    {
        ExpressionAst expr = ParseBitwiseOr();

        while (_context.TryConsume("&&"))
        {
            Token op = _context.Previous;
            ExpressionAst right = ParseBitwiseOr();
            expr = new BinaryExpression(expr, op, right)
            {
                Line = _context.Current.Line,
                Column = _context.Current.Column,
            };
        }

        return expr;
    }

    private ExpressionAst ParseBitwiseOr()
    {
        ExpressionAst expr = ParseBitwiseAnd();

        while (_context.TryConsume("|"))
        {
            Token op = _context.Previous;
            ExpressionAst right = ParseBitwiseAnd();
            expr = new BinaryExpression(expr, op, right)
            {
                Line = _context.Current.Line,
                Column = _context.Current.Column,
            };
        }

        return expr;
    }

    private ExpressionAst ParseBitwiseAnd()
    {
        ExpressionAst expr = ParseEquality();

        while (_context.TryConsume("&"))
        {
            Token op = _context.Previous;
            ExpressionAst right = ParseEquality();
            expr = new BinaryExpression(expr, op, right)
            {
                Line = _context.Current.Line,
                Column = _context.Current.Column,
            };
        }

        return expr;
    }

    private ExpressionAst ParseEquality()
    {
        ExpressionAst expr = ParseRelational();

        while (_context.Current.Value is "==" or "!=")
        {
            Token op = _context.Advance();
            ExpressionAst right = ParseRelational();
            expr = new BinaryExpression(expr, op, right)
            {
                Line = _context.Current.Line,
                Column = _context.Current.Column,
            };
        }

        return expr;
    }

    private ExpressionAst ParseRelational()
    {
        ExpressionAst expr = ParseAdditive();

        while (_context.Current.Value is "<" or "<=" or ">" or ">=")
        {
            Token op = _context.Advance();
            ExpressionAst right = ParseAdditive();
            expr = new BinaryExpression(expr, op, right)
            {
                Line = _context.Current.Line,
                Column = _context.Current.Column,
            };
        }

        return expr;
    }

    private ExpressionAst ParseAdditive()
    {
        ExpressionAst expr = ParseMultiplicative();

        while (_context.Current.Value is "+" or "-")
        {
            Token op = _context.Advance();
            ExpressionAst right = ParseMultiplicative();
            expr = new BinaryExpression(expr, op, right)
            {
                Line = _context.Current.Line,
                Column = _context.Current.Column,
            };
        }

        return expr;
    }

    private ExpressionAst ParseMultiplicative()
    {
        ExpressionAst expr = ParseUnary();

        while (_context.Current.Value is "*" or "/" or "%")
        {
            Token op = _context.Advance();
            ExpressionAst right = ParseUnary();
            expr = new BinaryExpression(expr, op, right)
            {
                Line = _context.Current.Line,
                Column = _context.Current.Column,
            };
        }

        return expr;
    }

    private ExpressionAst ParseUnary()
    {
        if (_context.Current.Value is "!" or "-")
        {
            Token op = _context.Advance();
            ExpressionAst operand = ParseUnary();
            return new UnaryExpression(op, operand)
            {
                Line = _context.Current.Line,
                Column = _context.Current.Column,
            };
        }

        ExpressionAst primary = ParsePrimary();
        return ParsePostfix(primary);
    }

    private ExpressionAst ParsePrimary()
    {
        if (_context.Current.Value == "read")
        {
            _context.Advance();
            _context.Consume("(", "Expected ( after read");
            _context.Consume(")", "Expected () after read");

            return new ReadExpression
            {
                Line = _context.Current.Line,
                Column = _context.Current.Column,
            };
        }

        if (_context.Match(TokenType.IntegerType))
        {
            Token token = _context.Advance();
            return new IntegerLiteral { Value = long.Parse(token.Value) };
        }

        if (_context.Match(TokenType.DoubleLiteral))
        {
            Token token = _context.Advance();
            return new DoubleLiteral { Value = double.Parse(token.Value, System.Globalization.CultureInfo.InvariantCulture) };
        }

        if (_context.TryConsume("new"))
        {
            return ParseArrayCreation();
        }

        if (_context.TryConsume("len"))
        {
            return ParseLengthExpression();
        }

        if (_context.TryConsume("["))
        {
            return ParseArrayLiteral();
        }

        if (_context.TryConsume("("))
        {
            ExpressionAst expr = ParseExpression();
            _context.Consume(")", "Expected )");
            return expr;
        }

        if (_context.Match(TokenType.Identifier))
        {
            string identifier = _context.Consume(TokenType.Identifier, "Expected identifier").Value;
            ExpressionAst expr = new IdentifierExpression(identifier)
            {
                Line = _context.Current.Line,
                Column = _context.Current.Column,
            };

            if (_context.TryConsume("("))
            {
                return ParseFunctionCall(identifier);
            }

            return expr;
        }

        throw new ParseException(
            "Expected expression or statement",
            _context.Current.Line,
            _context.Current.Column);
    }

    private ExpressionAst ParsePostfix(ExpressionAst expr)
    {
        while (true)
        {
            if (_context.TryConsume("["))
            {
                ExpressionAst index = ParseExpression();
                _context.Consume("]", "Expected ]");

                expr = new ArrayAccessExpression(
                    expr,
                    index)
                {
                    Line = _context.Previous.Line,
                    Column = _context.Previous.Column,
                };

                continue;
            }

            break;
        }

        return expr;
    }

    private ArrayCreationExpression ParseArrayCreation()
    {
        Token newToken = _context.Previous;

        TypeNode elementType;
        if (_context.TryConsume("int"))
        {
            elementType = new IntType();
        }
        else if (_context.TryConsume("double"))
        {
            elementType = new DoubleType();
        }
        else
        {
            throw new ParseException(
                "Expected 'int' or 'double' after 'new'",
                _context.Current.Line,
                _context.Current.Column);
        }

        if (!_context.TryConsume("["))
        {
            throw new ParseException(
                "Expected '[' after type in array creation",
                _context.Current.Line,
                _context.Current.Column);
        }

        ExpressionAst size = ParseExpression();
        _context.Consume("]", "Expected ']' after array size");
        var arrayType = new ArrayType(elementType);

        return new ArrayCreationExpression(elementType, arrayType, size)
        {
            Line = newToken.Line,
            Column = newToken.Column,
        };
    }

    private LengthExpression ParseLengthExpression()
    {
        _context.Consume("(", "Expected (");
        ExpressionAst target = ParseExpression();
        _context.Consume(")", "Expected )");

        return new LengthExpression(target)
        {
            Line = _context.Current.Line,
            Column = _context.Current.Column,
        };
    }

    private ArrayLiteralExpression ParseArrayLiteral()
    {
        var elements = new List<ExpressionAst>();

        if (!_context.TryConsume("]"))
        {
            do
            {
                elements.Add(ParseExpression());
            }
            while (_context.TryConsume(","));

            _context.Consume("]", "Expected ]");
        }

        return new ArrayLiteralExpression
        {
            Elements = elements,
            Line = _context.Current.Line,
            Column = _context.Current.Column,
        };
    }

    private FunctionCallExpression ParseFunctionCall(string functionName)
    {
        var arguments = new List<ExpressionAst>();

        if (!_context.TryConsume(")"))
        {
            do
            {
                arguments.Add(ParseExpression());
            }
            while (_context.TryConsume(","));

            _context.Consume(")", "Expected )");
        }

        return new FunctionCallExpression(
            functionName,
            arguments)
        {
            Line = _context.Current.Line,
            Column = _context.Current.Column,
        };
    }

    private StatementAst ParseStatement()
    {
        if (_context.Match("{"))
        {
            return ParseBlockStatement();
        }
        else if (_context.Match("int") || _context.Match("double"))
        {
            return ParseLocalVariableDeclaration();
        }
        else if (_context.Match("if"))
        {
            return ParseIfStatement();
        }
        else if (_context.Match("while"))
        {
            return ParseWhileStatement();
        }
        else if (_context.Match("for"))
        {
            return ParseForStatement();
        }
        else if (_context.Match("return"))
        {
            return ParseReturnStatement();
        }
        else if (_context.Match("break"))
        {
            return ParseBreakStatement();
        }
        else if (_context.Match("continue"))
        {
            return ParseContinueStatement();
        }
        else if (_context.Match("print"))
        {
            return ParsePrintStatement();
        }
        else
        {
            return ParseExpressionStatement();
        }
    }

    private BlockStatement ParseBlockStatement()
    {
        _context.Consume("{", "Expected {");
        var statements = new List<StatementAst>();

        while (!_context.TryConsume("}"))
        {
            if (_context.IsAtEnd)
            {
                throw new ParseException(
                    "Unclosed block statement",
                    _context.Current.Line,
                    _context.Current.Column);
            }

            statements.Add(ParseStatement());
        }

        return new BlockStatement
        {
            Statements = statements,
            Line = _context.Current.Line,
            Column = _context.Current.Column,
        };
    }

    private VariableDeclarationStatement ParseLocalVariableDeclaration()
    {
        TypeNode type = ParseType();
        string name = _context.Consume(TokenType.Identifier, "Expected local variable name").Value;
        ExpressionAst? initializer = null;

        if (_context.TryConsume("="))
        {
            initializer = ParseExpression();
        }

        _context.Consume(";", "Expected ;");

        return new VariableDeclarationStatement(name, type, initializer)
        {
            Line = _context.Current.Line,
            Column = _context.Current.Column,
        };
    }

    private IfStatement ParseIfStatement()
    {
        Token ifToken = _context.Advance();
        _context.Consume("(",  "Expected (");
        ExpressionAst condition = ParseExpression();
        _context.Consume(")", "Expected )");
        StatementAst thenBranch = ParseStatement();
        StatementAst? elseBranch = null;
        if (_context.TryConsume("else"))
        {
            elseBranch = ParseStatement();
        }

        return new IfStatement(condition, thenBranch, elseBranch)
        {
            Line = ifToken.Line,
            Column = ifToken.Column,
        };
    }

    private WhileStatement ParseWhileStatement()
    {
        Token whileToken = _context.Advance();
        _context.Consume("(",  "Expected (");
        ExpressionAst condition = ParseExpression();
        _context.Consume(")", "Expected )");

        StatementAst body = ParseStatement();

        return new WhileStatement(condition, body)
        {
            Line = whileToken.Line,
            Column = whileToken.Column,
        };
    }

    private BreakStatement ParseBreakStatement()
    {
        Token breakToken = _context.Advance();
        _context.Consume(";",  "Expected ;");

        return new BreakStatement
        {
            Line = breakToken.Line,
            Column = breakToken.Column,
        };
    }

    private ContinueStatement ParseContinueStatement()
    {
        Token continueToken = _context.Advance();
        _context.Consume(";",  "Expected ;");

        return new ContinueStatement
        {
            Line = continueToken.Line,
            Column = continueToken.Column,
        };
    }

    private ForStatement ParseForStatement()
    {
        Token forToken = _context.Advance();
        _context.Consume("(", "Expected (");
        StatementAst? initialization = null;

        if (!_context.TryConsume(";"))
        {
            if (_context.Match("int") || _context.Match("double"))
            {
                initialization = ParseLocalVariableDeclaration();
            }
            else
            {
                initialization = ParseExpressionStatement();
            }
        }

        ExpressionAst? condition = null;
        if (!_context.TryConsume(";"))
        {
            condition = ParseExpression();
            _context.Consume(";", "Expected ;");
        }

        ExpressionAst? increment = null;
        if (!_context.TryConsume(")"))
        {
            increment = ParseExpression();
            _context.Consume(")", "Expected )");
        }

        StatementAst body = ParseStatement();

        return new ForStatement(body)
        {
            Initializer = initialization,
            Condition = condition,
            Increment = increment,
            Line = forToken.Line,
            Column = forToken.Column,
        };
    }

    private ReturnStatement ParseReturnStatement()
    {
        Token returnToken = _context.Advance();
        ExpressionAst? value = null;

        if (!_context.TryConsume(";"))
        {
            value = ParseExpression();
            _context.Consume(";", "Expected ;");
        }

        return new ReturnStatement
        {
            Value = value,
            Line = returnToken.Line,
            Column = returnToken.Column,
        };
    }

    private PrintStatement ParsePrintStatement()
    {
        _context.Advance();
        _context.Consume("(",  "Expected (");

        var expressions = new Collection<ExpressionAst> { ParseExpression() };

        while (_context.TryConsume(","))
        {
            expressions.Add(ParseExpression());
        }

        _context.Consume(")", "Expected )");
        _context.Consume(";", "Expected ;");
        return new PrintStatement(expressions)
        {
            Line = _context.Current.Line,
            Column = _context.Current.Column,
        };
    }

    private ExpressionStatement ParseExpressionStatement()
    {
        ExpressionAst expression = ParseExpression();
        _context.Consume(";", "Expected ;");

        return new ExpressionStatement(expression)
        {
            Line = _context.Current.Line,
            Column = _context.Current.Column,
        };
    }
}

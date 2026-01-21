using Tutel.Core.Compiler.AST;
using Tutel.Core.Compiler.AST.Abstractions;
using Tutel.Core.Compiler.AST.Declarations;
using Tutel.Core.Compiler.AST.Expressions;
using Tutel.Core.Compiler.AST.Expressions.Literals;
using Tutel.Core.Compiler.AST.Statements;
using Tutel.Core.Compiler.AST.Types;
using Tutel.Core.Compiler.SemanticAnalysis.Models;

namespace Tutel.Compiler.SemanticAnalysis;

public class SymbolTableBuilder : IAstVisitor<object?>
{
    private readonly SymbolTable _symbolTable = new SymbolTable();
    private readonly Stack<Dictionary<string, VariableSymbol>> _scopes = new();
    private FunctionSymbol? _currentFunction = null;
    private bool _inLoop = false;

    public SymbolTable Build(ProgramAst program)
    {
        foreach (DeclarationAst decl in program.Declarations)
        {
            if (decl is GlobalVariableDeclaration globalVar)
            {
                ProcessGlobalVariable(globalVar);
            }
        }

        foreach (DeclarationAst decl in program.Declarations)
        {
            if (decl is FunctionDeclaration func)
            {
                ProcessFunctionDeclaration(func);
            }
        }

        foreach (DeclarationAst decl in program.Declarations)
        {
            if (decl is FunctionDeclaration func)
            {
                ProcessFunctionBody(func);
            }
        }

        ValidateMainFunction();

        return _symbolTable;
    }

    public object? Visit(ProgramAst programAst)
    {
        return null;
    }

    public object? Visit(PrintStatement stmt)
    {
        if (_currentFunction == null)
        {
            AddError("Оператор 'print' может использоваться только внутри функции", stmt.Line);
            return null;
        }

        if (stmt.Expressions.Count == 0)
        {
            AddError("Оператор 'print' ожидает хотя бы одно выражение", stmt.Line);
            return null;
        }

        foreach (ExpressionAst expr in stmt.Expressions)
        {
            expr.Accept(this);
        }

        return null;
    }

    public object? Visit(FunctionDeclaration decl)
    {
        return null;
    }

    public object? Visit(GlobalVariableDeclaration decl)
    {
        return null;
    }

    public object? Visit(ReadExpression expr)
    {
        return null;
    }

    public object? Visit(BlockStatement stmt)
    {
        EnterScope();

        foreach (StatementAst statement in stmt.Statements)
        {
            statement.Accept(this);
        }

        ExitScope();
        return null;
    }

    public object? Visit(VariableDeclarationStatement stmt)
    {
        if (_currentFunction == null)
        {
            AddError($"Локальная переменная '{stmt.Name}' вне функции", stmt.Line);
            return null;
        }

        if (_scopes.Peek().ContainsKey(stmt.Name))
        {
            AddError($"Переменная '{stmt.Name}' уже объявлена в этой области", stmt.Line);
            return null;
        }

        var variable = new VariableSymbol(
            stmt.Name,
            stmt.Type,
            _currentFunction.Parameters.Count + _currentFunction.Locals.Count);

        _scopes.Peek()[stmt.Name] = variable;
        _currentFunction.Locals.Add(variable);

        stmt.InitValue?.Accept(this);

        return null;
    }

    public object? Visit(IfStatement stmt)
    {
        stmt.Condition.Accept(this);

        EnterScope();
        stmt.ThenBranch.Accept(this);
        ExitScope();

        if (stmt.ElseBranch != null)
        {
            EnterScope();
            stmt.ElseBranch.Accept(this);
            ExitScope();
        }

        return null;
    }

    public object? Visit(WhileStatement stmt)
    {
        bool oldInLoop = _inLoop;
        _inLoop = true;

        stmt.Condition.Accept(this);

        EnterScope();
        stmt.Body.Accept(this);
        ExitScope();

        _inLoop = oldInLoop;
        return null;
    }

    public object? Visit(ForStatement stmt)
    {
        bool oldInLoop = _inLoop;
        _inLoop = true;

        stmt.Initializer?.Accept(this);
        stmt.Condition?.Accept(this);

        EnterScope();
        stmt.Body.Accept(this);
        ExitScope();

        stmt.Increment?.Accept(this);

        _inLoop = oldInLoop;
        return null;
    }

    public object? Visit(ReturnStatement stmt)
    {
        if (_currentFunction == null)
        {
            AddError("Оператор 'return' вне функции", stmt.Line);
        }
        else
        {
            stmt.Value?.Accept(this);
        }

        return null;
    }

    public object? Visit(BreakStatement stmt)
    {
        return null;
    }

    public object? Visit(ContinueStatement stmt)
    {
        return null;
    }

    public object? Visit(ExpressionStatement stmt)
    {
        stmt.Expression.Accept(this);
        return null;
    }

    public object? Visit(IdentifierExpression expr)
    {
        VariableSymbol? variable = FindVariable(expr.Name);
        if (variable == null)
        {
            AddError($"Неизвестный идентификатор '{expr.Name}'", expr.Line);
        }

        return null;
    }

    public object? Visit(FunctionCallExpression expr)
    {
        FunctionSymbol? func = _symbolTable.FindFunction(expr.FunctionName);
        if (func == null)
        {
            AddError($"Неизвестная функция '{expr.FunctionName}'", expr.Line);
        }
        else
        {
            if (expr.Arguments.Count != func.Parameters.Count)
            {
                AddError(
                    $"Функция '{expr.FunctionName}' ожидает {func.Parameters.Count} аргументов, " +
                    $"получено {expr.Arguments.Count}",
                    expr.Line);
            }
        }

        foreach (ExpressionAst arg in expr.Arguments)
        {
            arg.Accept(this);
        }

        return null;
    }

    public object? Visit(BinaryExpression expr)
    {
        expr.Left.Accept(this);
        expr.Right.Accept(this);
        return null;
    }

    public object? Visit(UnaryExpression expr)
    {
        expr.Operand.Accept(this);
        return null;
    }

    public object? Visit(ArrayCreationExpression expr)
    {
        expr.Size.Accept(this);
        return null;
    }

    public object? Visit(ArrayLiteralExpression expr)
    {
        foreach (ExpressionAst element in expr.Elements)
        {
            element.Accept(this);
        }

        return null;
    }

    public object? Visit(ArrayAccessExpression expr)
    {
        expr.Array.Accept(this);
        expr.Index.Accept(this);
        return null;
    }

    public object? Visit(ArrayAssignmentExpression expr)
    {
        expr.Target.Accept(this);
        expr.Value.Accept(this);
        return null;
    }

    public object? Visit(AssignmentExpression expr)
    {
        VariableSymbol? variable = FindVariable(expr.Target.Name);
        if (variable == null)
        {
            AddError($"Неизвестный идентификатор '{expr.Target.Name}'", expr.Line);
        }

        expr.Value.Accept(this);
        return null;
    }

    public object? Visit(LengthExpression expr)
    {
        expr.Array.Accept(this);
        return null;
    }

    public object? Visit(IntegerLiteral expr)
    {
        return null;
    }

    public object? Visit(IntType type)
    {
        return null;
    }

    public object? Visit(ArrayType type)
    {
        return null;
    }

    public object? Visit(VoidType type)
    {
        return null;
    }

    private void ProcessGlobalVariable(GlobalVariableDeclaration globalVar)
    {
        if (_symbolTable.FindGlobal(globalVar.Name) != null)
        {
            AddError($"Глобальная переменная '{globalVar.Name}' уже объявлена", globalVar.Line);
            return;
        }

        var symbol = new VariableSymbol(
            globalVar.Name,
            globalVar.Type,
            _symbolTable.Globals.Count)
        {
            Initializer = globalVar.InitValue,
        };

        _symbolTable.AddGlobal(symbol);
    }

    private void ProcessFunctionDeclaration(FunctionDeclaration func)
    {
        if (_symbolTable.FindFunction(func.Name) != null)
        {
            AddError($"Функция '{func.Name}' уже объявлена", func.Line);
            return;
        }

        var symbol = new FunctionSymbol
        {
            Name = func.Name,
            ReturnType = func.ReturnType,
            Index = _symbolTable.Functions.Count,
        };

        int paramIndex = 0;
        foreach (Parameter param in func.Parameters)
        {
            symbol.Parameters.Add(new VariableSymbol(
                param.Name,
                param.Type,
                paramIndex++));
        }

        _symbolTable.AddFunction(symbol);
    }

    private void ProcessFunctionBody(FunctionDeclaration func)
    {
        FunctionSymbol? funcSymbol = _symbolTable.FindFunction(func.Name);
        if (funcSymbol == null) return;

        _currentFunction = funcSymbol;
        _scopes.Clear();

        EnterScope();

        foreach (VariableSymbol param in funcSymbol.Parameters)
        {
            _scopes.Peek()[param.Name] = param;
        }

        func.Body.Accept(this);

        ExitScope();
        _currentFunction = null;
    }

    private void EnterScope()
    {
        _scopes.Push(new Dictionary<string, VariableSymbol>());
    }

    private void ExitScope()
    {
        _scopes.Pop();
    }

    private VariableSymbol? FindVariable(string name)
    {
        foreach (Dictionary<string, VariableSymbol> scope in _scopes.Reverse())
        {
            if (scope.TryGetValue(name, out VariableSymbol? variable))
            {
                return variable;
            }
        }

        return _symbolTable.FindGlobal(name);
    }

    private void ValidateMainFunction()
    {
        FunctionSymbol? mainFunc = _symbolTable.FindFunction("main");

        if (mainFunc == null)
        {
            AddError("Программа должна содержать функцию 'main'", -1);
            return;
        }

        if (mainFunc.Parameters.Count > 0)
        {
            AddError("Функция 'main' не должна иметь параметров", -1);
        }

        if (mainFunc.ReturnType is not IntType)
        {
            AddError("Функция main должна возвращать int", -1);
        }
    }

    private void AddError(string message, int line)
    {
        _symbolTable.Errors.Add(new SemanticError(message, line));
    }
}

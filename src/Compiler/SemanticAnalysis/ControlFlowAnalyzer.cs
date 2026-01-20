using Tutel.Core.Compiler.AST;
using Tutel.Core.Compiler.AST.Abstractions;
using Tutel.Core.Compiler.AST.Declarations;
using Tutel.Core.Compiler.AST.Expressions;
using Tutel.Core.Compiler.AST.Expressions.Literals;
using Tutel.Core.Compiler.AST.Statements;
using Tutel.Core.Compiler.AST.Types;
using Tutel.Core.Compiler.SemanticAnalysis.Models;

namespace Tutel.Compiler.SemanticAnalysis;

public class ControlFlowAnalyzer : IAstVisitor<ControlFlowAnalyzer.ControlFlowInfo>
{
    private readonly SymbolTable _symbols;
    private FunctionSymbol? _currentFunction;
    private bool _inLoop = false;
    private bool _isReachable = true;

    public class ControlFlowInfo
    {
        public bool AlwaysReturns { get; set; }

        public bool AlwaysBreaks { get; set; }

        public bool AlwaysContinues { get; set; }

        public bool IsTerminal { get; set; }
    }

    public ControlFlowAnalyzer(SymbolTable symbols)
    {
        _symbols = symbols;
    }

    public void Analyze(ProgramAst program)
    {
        foreach (DeclarationAst decl in program.Declarations)
        {
            switch (decl)
            {
                case GlobalVariableDeclaration global:
                    AnalyzeGlobal(global);
                    break;
                case FunctionDeclaration func:
                    AnalyzeFunction(func);
                    break;
            }
        }
    }

    public ControlFlowInfo Visit(ReadExpression expr)
    {
        return new ControlFlowInfo();
    }

    public ControlFlowInfo Visit(BlockStatement stmt)
    {
        var info = new ControlFlowInfo();
        bool blockReachable = _isReachable;

        foreach (StatementAst statement in stmt.Statements)
        {
            if (!_isReachable)
            {
                AddError("Недостижимый код", statement.Line);
                break;
            }

            ControlFlowInfo stmtInfo = statement.Accept(this);

            if (stmtInfo.IsTerminal)
            {
                _isReachable = false;
            }

            info.AlwaysReturns |= stmtInfo.AlwaysReturns;
            info.AlwaysBreaks |= stmtInfo.AlwaysBreaks;
            info.AlwaysContinues |= stmtInfo.AlwaysContinues;
        }

        _isReachable = blockReachable;
        return info;
    }

    public ControlFlowInfo Visit(VariableDeclarationStatement stmt)
    {
        return new ControlFlowInfo();
    }

    public ControlFlowInfo Visit(IfStatement stmt)
    {
        stmt.Condition.Accept(this);

        ControlFlowInfo thenInfo = stmt.ThenBranch.Accept(this);
        ControlFlowInfo elseInfo = stmt.ElseBranch?.Accept(this) ?? new ControlFlowInfo();

        return new ControlFlowInfo
        {
            AlwaysReturns = thenInfo.AlwaysReturns && elseInfo.AlwaysReturns,
            AlwaysBreaks = thenInfo.AlwaysBreaks && elseInfo.AlwaysBreaks,
            AlwaysContinues = thenInfo.AlwaysContinues && elseInfo.AlwaysContinues,
        };
    }

    public ControlFlowInfo Visit(WhileStatement stmt)
    {
        bool oldInLoop = _inLoop;
        _inLoop = true;

        stmt.Condition.Accept(this);
        ControlFlowInfo bodyInfo = stmt.Body.Accept(this);

        _inLoop = oldInLoop;

        return new ControlFlowInfo
        {
            AlwaysReturns = false,
        };
    }

    public ControlFlowInfo Visit(ForStatement stmt)
    {
        bool oldInLoop = _inLoop;
        _inLoop = true;

        stmt.Initializer?.Accept(this);
        stmt.Condition?.Accept(this);

        ControlFlowInfo bodyInfo = stmt.Body.Accept(this);

        stmt.Increment?.Accept(this);

        _inLoop = oldInLoop;

        return new ControlFlowInfo
        {
            AlwaysReturns = false,
        };
    }

    public ControlFlowInfo Visit(ReturnStatement stmt)
    {
        stmt.Value?.Accept(this);

        return new ControlFlowInfo
        {
            AlwaysReturns = true,
            IsTerminal = true,
        };
    }

    public ControlFlowInfo Visit(BreakStatement stmt)
    {
        if (!_inLoop)
        {
            AddError("Оператор break вне цикла", stmt.Line);
        }

        return new ControlFlowInfo
        {
            AlwaysBreaks = true,
            IsTerminal = true,
        };
    }

    public ControlFlowInfo Visit(ContinueStatement stmt)
    {
        if (!_inLoop)
        {
            AddError("Оператор continue вне цикла", stmt.Line);
        }

        return new ControlFlowInfo
        {
            AlwaysContinues = true,
            IsTerminal = true,
        };
    }

    public ControlFlowInfo Visit(ExpressionStatement stmt)
    {
        stmt.Expression.Accept(this);
        return new ControlFlowInfo();
    }

    public ControlFlowInfo Visit(PrintStatement stmt)
    {
        if (!_isReachable)
        {
            AddError("Недостижимый код", stmt.Line);
        }

        foreach (ExpressionAst expr in stmt.Expressions)
        {
            expr.Accept(this);
        }

        return new ControlFlowInfo
        {
            AlwaysReturns = false,
            AlwaysBreaks = false,
            AlwaysContinues = false,
            IsTerminal = false,
        };
    }

    public ControlFlowInfo Visit(IntegerLiteral expr) => new();

    public ControlFlowInfo Visit(IdentifierExpression expr) => new();

    public ControlFlowInfo Visit(BinaryExpression expr)
    {
        expr.Left.Accept(this);
        expr.Right.Accept(this);
        return new();
    }

    public ControlFlowInfo Visit(UnaryExpression expr)
    {
        expr.Operand.Accept(this);
        return new();
    }

    public ControlFlowInfo Visit(FunctionCallExpression expr)
    {
        foreach (ExpressionAst arg in expr.Arguments)
        {
            arg.Accept(this);
        }

        return new();
    }

    public ControlFlowInfo Visit(ArrayAccessExpression expr)
    {
        expr.Array.Accept(this);
        expr.Index.Accept(this);
        return new();
    }

    public ControlFlowInfo Visit(ArrayCreationExpression expr)
    {
        expr.Size.Accept(this);
        return new();
    }

    public ControlFlowInfo Visit(ArrayLiteralExpression expr)
    {
        foreach (ExpressionAst element in expr.Elements)
        {
            element.Accept(this);
        }

        return new();
    }

    public ControlFlowInfo Visit(AssignmentExpression expr)
    {
        expr.Value.Accept(this);
        return new();
    }

    public ControlFlowInfo Visit(ArrayAssignmentExpression expr)
    {
        expr.Value.Accept(this);
        return new();
    }

    public ControlFlowInfo Visit(LengthExpression expr)
    {
        expr.Array.Accept(this);
        return new();
    }

    public ControlFlowInfo Visit(FunctionDeclaration decl) => new();

    public ControlFlowInfo Visit(GlobalVariableDeclaration decl)
    {
        decl.InitValue?.Accept(this);
        return new ControlFlowInfo();
    }

    public ControlFlowInfo Visit(IntType type) => new();

    public ControlFlowInfo Visit(ArrayType type) => new();

    public ControlFlowInfo Visit(VoidType type) => new();

    public ControlFlowInfo Visit(ProgramAst programAst) => new();

    private void AnalyzeFunction(FunctionDeclaration func)
    {
        _currentFunction = _symbols.FindFunction(func.Name);
        if (_currentFunction == null) return;

        _isReachable = true;

        ControlFlowInfo info = func.Body.Accept(this);

        if (_currentFunction.ReturnType is not VoidType && !info.AlwaysReturns)
        {
            AddError($"Функция '{func.Name}' может не вернуть значение", func.Line);
        }

        _currentFunction = null;
    }

    private void AnalyzeGlobal(GlobalVariableDeclaration global)
    {
        global.Accept(this);
    }

    private void AddError(string message, int line)
    {
        _symbols.Errors.Add(new SemanticError(message, line));
    }
}
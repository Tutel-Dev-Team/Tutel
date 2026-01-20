using Tutel.Compiler.SemanticAnalysis;
using Tutel.Core.Compiler.AST;
using Tutel.Core.Compiler.AST.Abstractions;
using Tutel.Core.Compiler.AST.Declarations;
using Tutel.Core.Compiler.AST.Expressions;
using Tutel.Core.Compiler.AST.Expressions.Literals;
using Tutel.Core.Compiler.AST.Statements;
using Tutel.Core.Compiler.AST.Types;
using Tutel.Core.Compiler.Bytecode.Enums;
using Tutel.Core.Compiler.Bytecode.Models;
using Tutel.Core.Compiler.SemanticAnalysis.Models;

namespace Tutel.Compiler.Bytecode;

public class BytecodeGenerator : IAstVisitor<object?>
{
    private readonly SymbolTable _symbols;
    private readonly TutelBytecode _bytecode = new();
    private readonly CodeEmitter _emitter = new();
    private readonly Stack<string> _loopStartLabels = new();
    private readonly Stack<string> _loopEndLabels = new();
    private FunctionSymbol? _currentFunction;
    private int _labelCounter = 0;

    public BytecodeGenerator(SymbolTable symbols)
    {
        _symbols = symbols;
    }

    public TutelBytecode Generate(ProgramAst program)
    {
        FunctionSymbol? mainFunction = _symbols.Functions.FirstOrDefault(f => f.Name == "main");
        if (mainFunction != null)
        {
            _bytecode.EntryFunctionIndex = (ushort)_symbols.Functions.IndexOf(mainFunction);
        }

        foreach (VariableSymbol unused in _symbols.Globals)
        {
            _bytecode.Globals.Add(0);
        }

        bool hasGlobalInitializers = _symbols.Globals.Any(g => g.Initializer != null);
        if (hasGlobalInitializers && _symbols.FindFunction("__init__") == null)
        {
            var initSymbol = new FunctionSymbol
            {
                Name = "__init__",
                ReturnType = new VoidType(),
                Index = _symbols.Functions.Count,
            };

            _symbols.AddFunction(initSymbol);
        }

        foreach (FunctionSymbol funcSymbol in _symbols.Functions)
        {
            GenerateFunction(funcSymbol, program);
        }

        return _bytecode;
    }

    public object? Visit(IntegerLiteral expr)
    {
        _emitter.Emit(OpCode.PUSH_INT);
        _emitter.EmitInt64(expr.Value);
        return null;
    }

    public object? Visit(IdentifierExpression expr)
    {
        VariableSymbol? variable = FindVariable(expr.Name, out bool isGlobal);
        if (variable == null)
        {
            throw new InvalidOperationException($"Неизвестная переменная: {expr.Name}");
        }

        if (isGlobal)
        {
            _emitter.Emit(OpCode.LOAD_GLOBAL);
            _emitter.EmitUInt16((ushort)variable.Index);
        }
        else
        {
            _emitter.Emit(OpCode.LOAD_LOCAL);
            _emitter.EmitByte((byte)variable.Index);
        }

        return null;
    }

    public object? Visit(BinaryExpression expr)
    {
        switch (expr.Operator.Value)
        {
            case "&&":
                return GenerateLogicalAnd(expr);
            case "||":
                return GenerateLogicalOr(expr);
        }

        expr.Left.Accept(this);
        expr.Right.Accept(this);

        switch (expr.Operator.Value)
        {
            case "+":
                _emitter.Emit(OpCode.ADD);
                break;
            case "-":
                _emitter.Emit(OpCode.SUB);
                break;
            case "*":
                _emitter.Emit(OpCode.MUL);
                break;
            case "/":
                _emitter.Emit(OpCode.DIV);
                break;
            case "%":
                _emitter.Emit(OpCode.MOD);
                break;
            case "==":
                _emitter.Emit(OpCode.CMP_EQ);
                break;
            case "!=":
                _emitter.Emit(OpCode.CMP_NE);
                break;
            case "<":
                _emitter.Emit(OpCode.CMP_LT);
                break;
            case "<=":
                _emitter.Emit(OpCode.CMP_LE);
                break;
            case ">":
                _emitter.Emit(OpCode.CMP_GT);
                break;
            case ">=":
                _emitter.Emit(OpCode.CMP_GE);
                break;
            default:
                throw new InvalidOperationException($"Неподдерживаемый бинарный оператор: {expr.Operator.Type}");
        }

        return null;
    }

    public object? Visit(UnaryExpression expr)
    {
        if (expr.Operator.Value == "!")
        {
            return GenerateLogicalNot(expr);
        }

        expr.Operand.Accept(this);

        switch (expr.Operator.Value)
        {
            case "-":
                _emitter.Emit(OpCode.NEG);
                break;
            default:
                throw new InvalidOperationException($"Неподдерживаемый унарный оператор: {expr.Operator.Type}");
        }

        return null;
    }

    public object? Visit(FunctionCallExpression expr)
    {
        foreach (ExpressionAst t in expr.Arguments)
        {
            t.Accept(this);
        }

        FunctionSymbol? func = _symbols.Functions.FirstOrDefault(f => f.Name == expr.FunctionName);
        if (func == null)
        {
            throw new InvalidOperationException($"Неизвестная функция: {expr.FunctionName}");
        }

        int funcIndex = _symbols.Functions.IndexOf(func);
        _emitter.Emit(OpCode.CALL);
        _emitter.EmitUInt16((ushort)funcIndex);

        return null;
    }

    public object? Visit(ArrayAccessExpression expr)
    {
        expr.Array.Accept(this);
        expr.Index.Accept(this);

        _emitter.Emit(OpCode.ARRAY_LOAD);

        return null;
    }

    public object? Visit(ArrayCreationExpression expr)
    {
        expr.Size.Accept(this);

        _emitter.Emit(OpCode.ARRAY_NEW);

        return null;
    }

    public object? Visit(ArrayLiteralExpression expr)
    {
        _emitter.Emit(OpCode.PUSH_INT);
        _emitter.EmitInt64(expr.Elements.Count);
        _emitter.Emit(OpCode.ARRAY_NEW);

        _emitter.Emit(OpCode.DUP);

        for (int i = 0; i < expr.Elements.Count; i++)
        {
            if (i > 0)
            {
                _emitter.Emit(OpCode.DUP);
            }

            _emitter.Emit(OpCode.PUSH_INT);
            _emitter.EmitInt64(i);

            expr.Elements[i].Accept(this);

            _emitter.Emit(OpCode.ARRAY_STORE);
        }

        return null;
    }

    public object? Visit(LengthExpression expr)
    {
        expr.Array.Accept(this);
        _emitter.Emit(OpCode.ARRAY_LEN);
        return null;
    }

    public object? Visit(AssignmentExpression expr)
    {
        expr.Value.Accept(this);

        VariableSymbol? variable = FindVariable(expr.Target.Name, out bool isGlobal);
        if (variable == null)
        {
            throw new InvalidOperationException($"Неизвестная переменная: {expr.Target.Name}");
        }

        if (isGlobal)
        {
            _emitter.Emit(OpCode.STORE_GLOBAL);
            _emitter.EmitUInt16((ushort)variable.Index);
        }
        else
        {
            _emitter.Emit(OpCode.STORE_LOCAL);
            _emitter.EmitByte((byte)variable.Index);
        }

        return null;
    }

    public object? Visit(ArrayAssignmentExpression expr)
    {
        expr.Target.Array.Accept(this);
        expr.Target.Index.Accept(this);
        expr.Value.Accept(this);

        _emitter.Emit(OpCode.ARRAY_STORE);

        return null;
    }

    public object? Visit(ReadExpression expr)
    {
        _emitter.Emit(OpCode.READ_INT);
        return null;
    }

    public object? Visit(ReturnStatement stmt)
    {
        if (stmt.Value != null)
        {
            stmt.Value.Accept(this);
        }
        else if (_currentFunction?.ReturnType is VoidType)
        {
            _emitter.Emit(OpCode.PUSH_INT);
            _emitter.EmitInt64(0);
        }

        _emitter.Emit(OpCode.RET);
        return null;
    }

    public object? Visit(ExpressionStatement stmt)
    {
        stmt.Expression.Accept(this);
        if (ShouldPopExpression(stmt.Expression))
        {
            _emitter.Emit(OpCode.POP);
        }

        return null;
    }

    public object? Visit(PrintStatement stmt)
    {
        foreach (ExpressionAst expr in stmt.Expressions)
        {
            expr.Accept(this);
            _emitter.Emit(OpCode.PRINT_INT);
        }

        return null;
    }

    public object? Visit(BlockStatement stmt)
    {
        foreach (StatementAst statement in stmt.Statements)
        {
            statement.Accept(this);
        }

        return null;
    }

    public object? Visit(VariableDeclarationStatement stmt)
    {
        if (stmt.InitValue != null)
        {
            stmt.InitValue.Accept(this);

            VariableSymbol? variable = FindVariable(stmt.Name, out bool isGlobal);
            if (variable == null)
            {
                throw new InvalidOperationException($"Переменная не найдена в таблице символов: {stmt.Name}");
            }

            if (isGlobal)
            {
                _emitter.Emit(OpCode.STORE_GLOBAL);
                _emitter.EmitUInt16((ushort)variable.Index);
            }
            else
            {
                _emitter.Emit(OpCode.STORE_LOCAL);
                _emitter.EmitByte((byte)variable.Index);
            }
        }

        return null;
    }

    public object? Visit(IfStatement stmt)
    {
        string elseLabel = NewLabel("else");
        string endLabel = NewLabel("endif");

        stmt.Condition.Accept(this);
        _emitter.EmitJump(OpCode.JZ, elseLabel);

        stmt.ThenBranch.Accept(this);

        if (stmt.ElseBranch != null)
        {
            _emitter.EmitJump(OpCode.JMP, endLabel);
            _emitter.DefineLabel(elseLabel);
            stmt.ElseBranch.Accept(this);
            _emitter.DefineLabel(endLabel);
        }
        else
        {
            _emitter.DefineLabel(elseLabel);
        }

        return null;
    }

    public object? Visit(WhileStatement stmt)
    {
        string startLabel = NewLabel("while_start");
        string endLabel = NewLabel("while_end");

        _loopStartLabels.Push(startLabel);
        _loopEndLabels.Push(endLabel);

        _emitter.DefineLabel(startLabel);

        stmt.Condition.Accept(this);

        _emitter.EmitJump(OpCode.JZ, endLabel);

        stmt.Body.Accept(this);

        _emitter.EmitJump(OpCode.JMP, startLabel);

        _emitter.DefineLabel(endLabel);

        _loopStartLabels.Pop();
        _loopEndLabels.Pop();

        return null;
    }

    public object? Visit(ForStatement stmt)
    {
        string startLabel = NewLabel("for_start");
        string incrementLabel = NewLabel("for_increment");
        string endLabel = NewLabel("for_end");

        stmt.Initializer?.Accept(this);

        _loopStartLabels.Push(incrementLabel);
        _loopEndLabels.Push(endLabel);

        _emitter.DefineLabel(startLabel);

        if (stmt.Condition != null)
        {
            stmt.Condition.Accept(this);
            _emitter.EmitJump(OpCode.JZ, endLabel);
        }

        stmt.Body.Accept(this);

        _emitter.DefineLabel(incrementLabel);

        stmt.Increment?.Accept(this);

        _emitter.EmitJump(OpCode.JMP, startLabel);

        _emitter.DefineLabel(endLabel);

        _loopStartLabels.Pop();
        _loopEndLabels.Pop();

        return null;
    }

    public object? Visit(BreakStatement stmt)
    {
        if (_loopEndLabels.Count == 0)
        {
            throw new InvalidOperationException("break вне цикла");
        }

        string endLabel = _loopEndLabels.Peek();
        _emitter.EmitJump(OpCode.JMP, endLabel);

        return null;
    }

    public object? Visit(ContinueStatement stmt)
    {
        if (_loopStartLabels.Count == 0)
        {
            throw new InvalidOperationException("continue вне цикла");
        }

        string startLabel = _loopStartLabels.Peek();
        _emitter.EmitJump(OpCode.JMP, startLabel);

        return null;
    }

    public object? Visit(FunctionDeclaration decl) => null;

    public object? Visit(GlobalVariableDeclaration decl) => null;

    public object? Visit(IntType type) => null;

    public object? Visit(ArrayType type) => null;

    public object? Visit(VoidType type) => null;

    public object? Visit(ProgramAst programAst)
    {
        return null;
    }

    private void GenerateFunction(FunctionSymbol funcSymbol, ProgramAst program)
    {
        _currentFunction = funcSymbol;
        _emitter.Clear();

        if (funcSymbol.Name == "__init__")
        {
            GenerateInitFunctionBody();
        }
        else
        {
            FunctionDeclaration? funcDecl = program.Declarations
                .OfType<FunctionDeclaration>()
                .FirstOrDefault(f => f.Name == funcSymbol.Name);

            if (funcDecl != null)
            {
                if (funcSymbol.Name == "main")
                {
                    FunctionSymbol? initFunc = _symbols.Functions.FirstOrDefault(f => f.Name == "__init__");
                    if (initFunc != null)
                    {
                        int initIndex = _symbols.Functions.IndexOf(initFunc);
                        _emitter.Emit(OpCode.CALL);
                        _emitter.EmitUInt16((ushort)initIndex);
                    }
                }

                funcDecl.Body.Accept(this);
                if (_emitter.Code.Count == 0 || _emitter.Code[^1] != (byte)OpCode.RET)
                {
                    if (funcSymbol.ReturnType is VoidType)
                    {
                        _emitter.Emit(OpCode.PUSH_INT);
                        _emitter.EmitInt64(0);
                    }

                    _emitter.Emit(OpCode.RET);
                }
            }
            else
            {
                if (funcSymbol.ReturnType is VoidType)
                {
                    _emitter.Emit(OpCode.PUSH_INT);
                    _emitter.EmitInt64(0);
                }

                _emitter.Emit(OpCode.RET);
            }
        }

        _emitter.ResolveFixups();

        _bytecode.Functions.Add(new FunctionCode(_emitter.Code)
        {
            Arity = (byte)funcSymbol.Parameters.Count,
            LocalsCount = (byte)funcSymbol.LocalSlotCount,
        });

        _currentFunction = null;
    }

    private void GenerateInitFunctionBody()
    {
        foreach (VariableSymbol global in _symbols.Globals)
        {
            ExpressionAst? initializer = global.Initializer;
            if (initializer is null)
            {
                continue;
            }

            initializer.Accept(this);
            _emitter.Emit(OpCode.STORE_GLOBAL);
            _emitter.EmitUInt16((ushort)global.Index);
        }

        _emitter.Emit(OpCode.PUSH_INT);
        _emitter.EmitInt64(0);
        _emitter.Emit(OpCode.RET);
    }

    private string NewLabel(string prefix) => $"{prefix}_{_labelCounter++}";

    private VariableSymbol? FindVariable(string name, out bool isGlobal)
    {
        isGlobal = false;

        if (_currentFunction != null)
        {
            VariableSymbol? param = _currentFunction.Parameters.FirstOrDefault(p => p.Name == name);
            if (param != null) return param;

            VariableSymbol? local = _currentFunction.Locals.FirstOrDefault(l => l.Name == name);
            if (local != null) return local;
        }

        VariableSymbol? global = _symbols.Globals.FirstOrDefault(g => g.Name == name);
        if (global != null)
        {
            isGlobal = true;
            return global;
        }

        return null;
    }

    private object? GenerateLogicalAnd(BinaryExpression expr)
    {
        string falseLabel = NewLabel("and_false");
        string endLabel = NewLabel("and_end");

        expr.Left.Accept(this);
        _emitter.EmitJump(OpCode.JZ, falseLabel);

        expr.Right.Accept(this);
        _emitter.EmitJump(OpCode.JMP, endLabel);

        _emitter.DefineLabel(falseLabel);
        _emitter.Emit(OpCode.PUSH_INT);
        _emitter.EmitInt64(0);

        _emitter.DefineLabel(endLabel);
        return null;
    }

    private object? GenerateLogicalOr(BinaryExpression expr)
    {
        string trueLabel = NewLabel("or_true");
        string endLabel = NewLabel("or_end");

        expr.Left.Accept(this);
        _emitter.EmitJump(OpCode.JNZ, trueLabel);

        expr.Right.Accept(this);
        _emitter.EmitJump(OpCode.JMP, endLabel);

        _emitter.DefineLabel(trueLabel);
        _emitter.Emit(OpCode.PUSH_INT);
        _emitter.EmitInt64(1);

        _emitter.DefineLabel(endLabel);
        return null;
    }

    private object? GenerateLogicalNot(UnaryExpression expr)
    {
        string falseLabel = NewLabel("not_false");
        string endLabel = NewLabel("not_end");

        expr.Operand.Accept(this);
        _emitter.EmitJump(OpCode.JZ, falseLabel);

        _emitter.Emit(OpCode.POP);
        _emitter.Emit(OpCode.PUSH_INT);
        _emitter.EmitInt64(0);
        _emitter.EmitJump(OpCode.JMP, endLabel);

        _emitter.DefineLabel(falseLabel);
        _emitter.Emit(OpCode.PUSH_INT);
        _emitter.EmitInt64(1);

        _emitter.DefineLabel(endLabel);
        return null;
    }

    private bool ShouldPopExpression(ExpressionAst expr)
    {
        return expr switch
        {
            AssignmentExpression or ArrayAssignmentExpression => false,
            _ => true,
        };
    }
}
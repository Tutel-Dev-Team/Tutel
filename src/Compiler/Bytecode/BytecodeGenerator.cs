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

    public object? Visit(DoubleLiteral expr)
    {
        long doubleAsLong = BitConverter.DoubleToInt64Bits(expr.Value);
        _emitter.Emit(OpCode.PUSH_DOUBLE);
        _emitter.EmitInt64(doubleAsLong);
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
        // short-circuit логика оставляем как есть
        if (expr.Operator.Value == "&&") return GenerateLogicalAnd(expr);
        if (expr.Operator.Value == "||") return GenerateLogicalOr(expr);

        TypeNode leftType = InferExprType(expr.Left);
        TypeNode rightType = InferExprType(expr.Right);

        bool isDouble = leftType is DoubleType || rightType is DoubleType;

        expr.Left.Accept(this);
        if (leftType is IntType && isDouble)
            _emitter.Emit(OpCode.I2D);

        expr.Right.Accept(this);
        if (rightType is IntType && isDouble)
            _emitter.Emit(OpCode.I2D);

        _emitter.Emit(GetBinaryOpCode(expr.Operator.Value, isDouble));
        return null;
    }

    public object? Visit(UnaryExpression expr)
    {
        if (expr.Operator.Value == "!")
        {
            return GenerateLogicalNot(expr);
        }

        TypeNode operandType = InferExprType(expr.Operand);
        expr.Operand.Accept(this);

        switch (expr.Operator.Value)
        {
            case "-":
                _emitter.Emit(operandType is DoubleType ? OpCode.DNEG : OpCode.NEG);
                break;
            default:
                throw new InvalidOperationException($"Неподдерживаемый унарный оператор: {expr.Operator.Type}");
        }

        return null;
    }

    public object? Visit(FunctionCallExpression expr)
    {
        // Встроенная sqrt(x): компилируем напрямую в опкод.
        if (expr.FunctionName == "sqrt" && expr.Arguments.Count == 1)
        {
            ExpressionAst argument = expr.Arguments[0];
            TypeNode argumentType = InferExprType(argument);

            argument.Accept(this);
            if (argumentType is IntType)
            {
                _emitter.Emit(OpCode.I2D);
            }

            _emitter.Emit(OpCode.DSQRT);
            return null;
        }

        FunctionSymbol? func = _symbols.Functions.FirstOrDefault(f => f.Name == expr.FunctionName);
        if (func == null)
        {
            throw new InvalidOperationException($"Неизвестная функция: {expr.FunctionName}");
        }

        for (int i = 0; i < expr.Arguments.Count; i++)
        {
            ExpressionAst argument = expr.Arguments[i];
            TypeNode argumentType = InferExprType(argument);
            TypeNode parameterType = func.Parameters[i].Type;

            argument.Accept(this);
            EmitNumericConversionIfNeeded(parameterType, argumentType);
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

        var elementTypes = expr.Elements.Select(InferExprType).ToList();
        bool arrayIsDouble = elementTypes.Any(t => t is DoubleType);

        for (int i = 0; i < expr.Elements.Count; i++)
        {
            if (i > 0)
            {
                _emitter.Emit(OpCode.DUP);
            }

            _emitter.Emit(OpCode.PUSH_INT);
            _emitter.EmitInt64(i);

            expr.Elements[i].Accept(this);
            if (arrayIsDouble && elementTypes[i] is IntType)
            {
                _emitter.Emit(OpCode.I2D);
            }

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
        VariableSymbol? variable = FindVariable(expr.Target.Name, out bool isGlobal);
        if (variable == null)
        {
            throw new InvalidOperationException($"Неизвестная переменная: {expr.Target.Name}");
        }

        TypeNode valueType = InferExprType(expr.Value);
        expr.Value.Accept(this);
        EmitNumericConversionIfNeeded(variable.Type, valueType);

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
        TypeNode arrayType = InferExprType(expr.Target.Array);
        TypeNode valueType = InferExprType(expr.Value);

        expr.Target.Array.Accept(this);
        expr.Target.Index.Accept(this);
        expr.Value.Accept(this);

        if (arrayType is ArrayType arrayTypeNode)
        {
            EmitNumericConversionIfNeeded(arrayTypeNode.ElementType, valueType);
        }

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
            TypeNode exprType = InferExprType(expr);
            expr.Accept(this);
            _emitter.Emit(exprType is DoubleType ? OpCode.PRINT_DOUBLE : OpCode.PRINT_INT);
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
            VariableSymbol? variable = FindVariable(stmt.Name, out bool isGlobal);
            if (variable == null)
            {
                throw new InvalidOperationException($"Переменная не найдена в таблице символов: {stmt.Name}");
            }

            TypeNode initType = InferExprType(stmt.InitValue);
            stmt.InitValue.Accept(this);
            EmitNumericConversionIfNeeded(variable.Type, initType);

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

    public object? Visit(DoubleType type) => null;

    public object? Visit(ArrayType type) => null;

    public object? Visit(VoidType type) => null;

    public object? Visit(ProgramAst programAst)
    {
        return null;
    }

    private static OpCode GetBinaryOpCode(string op, bool isDouble) => op switch
    {
        "+" => isDouble ? OpCode.DADD : OpCode.ADD,
        "-" => isDouble ? OpCode.DSUB : OpCode.SUB,
        "*" => isDouble ? OpCode.DMUL : OpCode.MUL,
        "/" => isDouble ? OpCode.DDIV : OpCode.DIV,
        "%" => isDouble ? OpCode.DMOD : OpCode.MOD,

        "==" => isDouble ? OpCode.DCMP_EQ : OpCode.CMP_EQ,
        "!=" => isDouble ? OpCode.DCMP_NE : OpCode.CMP_NE,
        "<" => isDouble ? OpCode.DCMP_LT : OpCode.CMP_LT,
        "<=" => isDouble ? OpCode.DCMP_LE : OpCode.CMP_LE,
        ">" => isDouble ? OpCode.DCMP_GT : OpCode.CMP_GT,
        ">=" => isDouble ? OpCode.DCMP_GE : OpCode.CMP_GE,

        _ => throw new InvalidOperationException($"Неподдерживаемый бинарный оператор: {op}"),
    };

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

            TypeNode initType = InferExprType(initializer);
            initializer.Accept(this);
            EmitNumericConversionIfNeeded(global.Type, initType);
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

    private void EmitNumericConversionIfNeeded(TypeNode targetType, TypeNode valueType)
    {
        if (targetType is DoubleType && valueType is IntType)
        {
            _emitter.Emit(OpCode.I2D);
        }
    }

    private TypeNode InferExprType(ExpressionAst expr)
    {
        return expr switch
        {
            IntegerLiteral => new IntType(),
            DoubleLiteral => new DoubleType(),
            IdentifierExpression id => FindVariable(id.Name, out _)?.Type ?? new ErrorType(),
            ReadExpression => new IntType(),
            LengthExpression => new IntType(),
            FunctionCallExpression call => InferFunctionCallType(call),
            ArrayAccessExpression access => InferExprType(access.Array) is ArrayType at
                ? at.ElementType
                : new ErrorType(),
            ArrayCreationExpression creation => creation.ArrayType,
            ArrayLiteralExpression lit => InferArrayLiteralType(lit),
            AssignmentExpression assign => InferExprType(assign.Value),
            ArrayAssignmentExpression aassign => InferExprType(aassign.Value),
            UnaryExpression un => InferExprType(un.Operand),
            BinaryExpression bin => InferBinaryType(bin),
            _ => new ErrorType(),
        };
    }

    private ArrayType InferArrayLiteralType(ArrayLiteralExpression literal)
    {
        if (literal.Elements.Count == 0)
        {
            return new ArrayType(new IntType());
        }

        bool hasDouble = literal.Elements.Select(InferExprType).Any(t => t is DoubleType);
        return new ArrayType(hasDouble ? new DoubleType() : new IntType());
    }

    private TypeNode InferFunctionCallType(FunctionCallExpression call)
    {
        if (call.FunctionName == "sqrt" && call.Arguments.Count == 1)
        {
            return new DoubleType();
        }

        return _symbols.FindFunction(call.FunctionName)?.ReturnType ?? new ErrorType();
    }

    private TypeNode InferBinaryType(BinaryExpression expr)
    {
        TypeNode left = InferExprType(expr.Left);
        TypeNode right = InferExprType(expr.Right);

        return expr.Operator.Value switch
        {
            "&&" or "||" => new IntType(),
            "+" or "-" or "*" or "/" or "%" => (left is DoubleType || right is DoubleType)
                ? new DoubleType()
                : new IntType(),
            "==" or "!=" or "<" or "<=" or ">" or ">=" => new IntType(),
            _ => new ErrorType(),
        };
    }
}

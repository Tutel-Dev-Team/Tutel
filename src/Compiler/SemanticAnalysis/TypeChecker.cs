using Tutel.Core.Compiler.AST;
using Tutel.Core.Compiler.AST.Abstractions;
using Tutel.Core.Compiler.AST.Declarations;
using Tutel.Core.Compiler.AST.Expressions;
using Tutel.Core.Compiler.AST.Expressions.Literals;
using Tutel.Core.Compiler.AST.Statements;
using Tutel.Core.Compiler.AST.Types;
using Tutel.Core.Compiler.SemanticAnalysis.Models;

namespace Tutel.Compiler.SemanticAnalysis;

public class TypeChecker : IAstVisitor<TypeNode>
{
    private readonly SymbolTable _symbols;
    private FunctionSymbol? _currentFunction = null;

    public TypeChecker(SymbolTable symbols)
    {
        _symbols = symbols;
    }

    public void Check(ProgramAst program)
    {
        foreach (DeclarationAst decl in program.Declarations)
        {
            if (decl is GlobalVariableDeclaration global)
            {
                global.Accept(this);
            }
        }

        foreach (DeclarationAst decl in program.Declarations)
        {
            if (decl is FunctionDeclaration func)
            {
                _currentFunction = _symbols.FindFunction(func.Name);
                func.Body.Accept(this);
            }
        }
    }

    public TypeNode Visit(IntegerLiteral expr)
    {
        return new IntType();
    }

    public TypeNode Visit(IdentifierExpression expr)
    {
        VariableSymbol? variable = null;

        if (_currentFunction != null)
        {
            variable = _currentFunction.Parameters.FirstOrDefault(p => p.Name == expr.Name) ??
                       _currentFunction.Locals.FirstOrDefault(l => l.Name == expr.Name);
        }

        variable ??= _symbols.Globals.FirstOrDefault(g => g.Name == expr.Name);

        if (variable == null)
        {
            AddError($"Идентификатор '{expr.Name}' не найден", expr.Line);
            return new ErrorType();
        }

        return variable.Type;
    }

    public TypeNode Visit(BinaryExpression expr)
    {
        TypeNode leftType = expr.Left.Accept(this);
        TypeNode rightType = expr.Right.Accept(this);

        if (leftType is not IntType || rightType is not IntType)
        {
            AddError("Бинарные операции поддерживаются только для типа int", expr.Line);
            return new ErrorType();
        }

        if (expr.Operator.Value == "+" ||
            expr.Operator.Value == "-" ||
            expr.Operator.Value == "*" ||
            expr.Operator.Value == "/" ||
            expr.Operator.Value == $"%")
        {
            return new IntType();
        }

        if (expr.Operator.Value == "==" ||
            expr.Operator.Value == "!=" ||
            expr.Operator.Value == "<" ||
            expr.Operator.Value == "<=" ||
            expr.Operator.Value == ">" ||
            expr.Operator.Value == ">=")
        {
            return new IntType();
        }

        if (expr.Operator.Value == "&&" ||
            expr.Operator.Value == "||")
        {
            return new IntType();
        }

        AddError($"Неподдерживаемый бинарный оператор: {expr.Operator.Type}", expr.Line);
        return new ErrorType();
    }

    public TypeNode Visit(UnaryExpression expr)
    {
        TypeNode operandType = expr.Operand.Accept(this);

        if (operandType is not IntType)
        {
            AddError("Унарные операции поддерживаются только для типа int", expr.Line);
            return new ErrorType();
        }

        return new IntType();
    }

    public TypeNode Visit(FunctionCallExpression expr)
    {
        FunctionSymbol? func = _symbols.FindFunction(expr.FunctionName);
        if (func == null)
        {
            AddError($"Функция '{expr.FunctionName}' не найдена", expr.Line);
            return new ErrorType();
        }

        if (expr.Arguments.Count != func.Parameters.Count)
        {
            AddError($"Функция '{expr.FunctionName}' ожидает {func.Parameters.Count} аргументов, получено {expr.Arguments.Count}", expr.Line);
            return new ErrorType();
        }

        for (int i = 0; i < expr.Arguments.Count; i++)
        {
            TypeNode argType = expr.Arguments[i].Accept(this);
            TypeNode paramType = func.Parameters[i].Type;

            if (!TypesEqual(argType, paramType))
            {
                AddError($"Тип аргумента {i + 1} не совпадает: ожидается {paramType}, получено {argType}", expr.Line);
            }
        }

        return func.ReturnType;
    }

    public TypeNode Visit(ArrayAccessExpression expr)
    {
        TypeNode arrayType = expr.Array.Accept(this);
        TypeNode indexType = expr.Index.Accept(this);

        if (arrayType is not ArrayType)
        {
            AddError("Индексация поддерживается только для массивов", expr.Line);
            return new ErrorType();
        }

        if (indexType is not IntType)
        {
            AddError("Индекс массива должен быть типа int", expr.Line);
            return new ErrorType();
        }

        return new IntType();
    }

    public TypeNode Visit(ArrayCreationExpression expr)
    {
        TypeNode sizeType = expr.Size.Accept(this);

        if (sizeType is not IntType)
        {
            AddError("Размер массива должен быть типа int", expr.Line);
            return new ErrorType();
        }

        return expr.ArrayType;
    }

    public TypeNode Visit(ArrayLiteralExpression expr)
    {
        if (expr.Elements.Count == 0)
        {
            return new ArrayType(new IntType());
        }

        TypeNode firstType = expr.Elements[0].Accept(this);
        if (firstType is not IntType)
        {
            AddError("Элементы массива должны быть типа int", expr.Line);
            return new ErrorType();
        }

        for (int i = 1; i < expr.Elements.Count; i++)
        {
            TypeNode elementType = expr.Elements[i].Accept(this);
            if (!TypesEqual(elementType, firstType))
            {
                AddError($"Элемент {i} массива имеет несовместимый тип: ожидается int, получено {elementType}", expr.Line);
            }
        }

        return new ArrayType(new IntType());
    }

    public TypeNode Visit(LengthExpression expr)
    {
        TypeNode arrayType = expr.Array.Accept(this);

        if (arrayType is not ArrayType)
        {
            AddError("Метод len() поддерживается только для массивов", expr.Line);
            return new ErrorType();
        }

        return new IntType();
    }

    public TypeNode Visit(AssignmentExpression expr)
    {
        TypeNode targetType = expr.Target.Accept(this);
        TypeNode valueType = expr.Value.Accept(this);

        if (!TypesEqual(targetType, valueType))
        {
            AddError($"Тип присваиваемого значения не совпадает: ожидается {targetType}, получено {valueType}", expr.Line);
            return new ErrorType();
        }

        return valueType;
    }

    public TypeNode Visit(ArrayAssignmentExpression expr)
    {
        TypeNode targetType = expr.Target.Accept(this);
        TypeNode valueType = expr.Value.Accept(this);

        // Проверяем, что присваивание в массив корректно
        if (targetType is not IntType)
        {
            AddError("Присваивание в массив должно возвращать тип int", expr.Line);
            return new ErrorType();
        }

        if (valueType is not IntType)
        {
            AddError("В массив можно присваивать только значения типа int", expr.Line);
            return new ErrorType();
        }

        return valueType;
    }

    public TypeNode Visit(ReadExpression expr)
    {
        return new IntType();
    }

    public TypeNode Visit(BlockStatement stmt)
    {
        TypeNode? lastType = null;

        foreach (StatementAst statement in stmt.Statements)
        {
            lastType = statement.Accept(this);
        }

        return new VoidType();
    }

    public TypeNode Visit(VariableDeclarationStatement stmt)
    {
        if (stmt.InitValue != null)
        {
            TypeNode initType = stmt.InitValue.Accept(this);

            if (!TypesEqual(stmt.Type, initType))
            {
                AddError($"Тип инициализатора не совпадает с типом переменной: ожидается {stmt.Type}, получено {initType}", stmt.Line);
            }
        }

        return new VoidType();
    }

    public TypeNode Visit(IfStatement stmt)
    {
        TypeNode conditionType = stmt.Condition.Accept(this);
        if (conditionType is not IntType)
        {
            AddError("Условие if должно быть типа int (в Tutel логический тип - это int)", stmt.Line);
        }

        stmt.ThenBranch.Accept(this);
        stmt.ElseBranch?.Accept(this);

        return new VoidType();
    }

    public TypeNode Visit(WhileStatement stmt)
    {
        TypeNode conditionType = stmt.Condition.Accept(this);
        if (conditionType is not IntType)
        {
            AddError("Условие while должно быть типа int (в Tutel логический тип - это int)", stmt.Line);
        }

        stmt.Body.Accept(this);

        return new VoidType();
    }

    public TypeNode Visit(ForStatement stmt)
    {
        stmt.Initializer?.Accept(this);

        if (stmt.Condition != null)
        {
            TypeNode conditionType = stmt.Condition.Accept(this);
            if (conditionType is not IntType)
            {
                AddError("Условие for должно быть типа int (в Tutel логический тип - это int)", stmt.Line);
            }
        }

        stmt.Body.Accept(this);
        stmt.Increment?.Accept(this);

        return new VoidType();
    }

    public TypeNode Visit(ReturnStatement stmt)
    {
        if (_currentFunction == null)
        {
            AddError("Оператор return вне функции", stmt.Line);
            return new VoidType();
        }

        TypeNode returnType = stmt.Value == null
            ? new VoidType()
            : stmt.Value.Accept(this);

        if (!TypesEqual(_currentFunction.ReturnType, returnType))
        {
            AddError($"Тип возвращаемого значения не совпадает: ожидается {_currentFunction.ReturnType}, получено {returnType}", stmt.Line);
        }

        return new VoidType();
    }

    public TypeNode Visit(BreakStatement stmt)
    {
        return new VoidType();
    }

    public TypeNode Visit(ContinueStatement stmt)
    {
        return new VoidType();
    }

    public TypeNode Visit(ExpressionStatement stmt)
    {
        stmt.Expression.Accept(this);
        return new VoidType();
    }

    public TypeNode Visit(PrintStatement stmt)
    {
        foreach (ExpressionAst expr in stmt.Expressions)
        {
            TypeNode exprType = expr.Accept(this);

            if (exprType is IntType)
            {
            }
            else if (exprType is ArrayType arrayType)
            {
                if (arrayType.ElementType is IntType)
                {
                }
                else
                {
                    AddError($"Print не поддерживает массивы типа {arrayType.ElementType}", expr.Line);
                }
            }
            else if (exprType is VoidType)
            {
                AddError("Нельзя выводить void выражение", expr.Line);
            }
            else
            {
                AddError($"Print не поддерживает тип {exprType}", expr.Line);
            }
        }

        return new VoidType();
    }

    public TypeNode Visit(FunctionDeclaration decl)
    {
        return new VoidType();
    }

    public TypeNode Visit(GlobalVariableDeclaration decl)
    {
        if (decl.InitValue != null)
        {
            TypeNode initType = decl.InitValue.Accept(this);

            if (!TypesEqual(decl.Type, initType))
            {
                AddError($"Тип инициализатора глобальной переменной не совпадает: ожидается {decl.Type}, получено {initType}", decl.Line);
            }
        }

        return new VoidType();
    }

    public TypeNode Visit(IntType type)
    {
        return type;
    }

    public TypeNode Visit(ArrayType type)
    {
        return type;
    }

    public TypeNode Visit(VoidType type)
    {
        return type;
    }

    public TypeNode Visit(ProgramAst programAst)
    {
        foreach (DeclarationAst decl in programAst.Declarations)
        {
            decl.Accept(this);
        }

        return new VoidType();
    }

    private bool TypesEqual(TypeNode? type1, TypeNode? type2)
    {
        if (type1 == null || type2 == null)
            return false;

        if (type1 is IntType && type2 is IntType)
            return true;

        if (type1 is VoidType && type2 is VoidType)
            return true;

        if (type1 is ArrayType arr1 && type2 is ArrayType arr2)
            return TypesEqual(arr1.ElementType, arr2.ElementType);

        if (type1 is ErrorType || type2 is ErrorType)
            return true;

        return false;
    }

    private void AddError(string message, int line)
    {
        _symbols.Errors.Add(new SemanticError(message, line));
    }
}
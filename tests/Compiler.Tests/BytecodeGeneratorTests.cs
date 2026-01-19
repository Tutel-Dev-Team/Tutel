using Tutel.Compiler.Bytecode;
using Tutel.Compiler.SemanticAnalysis;
using Tutel.Core.Compiler.AST;
using Tutel.Core.Compiler.AST.Declarations;
using Tutel.Core.Compiler.AST.Expressions;
using Tutel.Core.Compiler.AST.Expressions.Literals;
using Tutel.Core.Compiler.AST.Statements;
using Tutel.Core.Compiler.AST.Types;
using Tutel.Core.Compiler.Bytecode.Enums;
using Tutel.Core.Compiler.Lexing.Models.Enums;
using Tutel.Core.Compiler.Lexing.Models.Tokens;
using Xunit;

namespace Compiler.Tests;

public class BytecodeGeneratorTests
{
    private readonly SemanticAnalyzer _analyzer = new SemanticAnalyzer();

    [Fact]
    public void Generate_SimpleProgram_GeneratesBytecode()
    {
        // Arrange
        ProgramAst program = CreateProgramWithMain();
        SymbolTable symbolTable = _analyzer.Analyze(program);
        var generator = new BytecodeGenerator(symbolTable);

        // Act
        Tutel.Core.Compiler.Bytecode.Models.TutelBytecode bytecode = generator.Generate(program);

        // Assert
        Assert.NotNull(bytecode);
        Assert.Single(bytecode.Functions);
        Assert.Equal(0, bytecode.EntryFunctionIndex);
        Assert.Empty(bytecode.Globals);
    }

    [Fact]
    public void Generate_GlobalVariableWithInitializer_GeneratesInitFunction()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateGlobalVariable("x", new IntType(), new IntegerLiteral { Value = 42 }),
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral { Value = 0 },
                            },
                        ],
                    })
            ],
        };

        SymbolTable symbolTable = _analyzer.Analyze(program);
        var generator = new BytecodeGenerator(symbolTable);

        // Act
        Tutel.Core.Compiler.Bytecode.Models.TutelBytecode bytecode = generator.Generate(program);

        // Assert
        Assert.NotNull(bytecode);
        Assert.Equal(2, bytecode.Functions.Count);
        Assert.Equal(0, bytecode.EntryFunctionIndex);
        Assert.Single(bytecode.Globals);
        Assert.Contains("__init__", symbolTable.Functions.Select(f => f.Name));
    }

    [Fact]
    public void Generate_GlobalVariableWithoutInitializer_GeneratesBytecode()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateGlobalVariable("x", new IntType(), null),
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral { Value = 0 },
                            },
                        ],
                    })
            ],
        };

        SymbolTable symbolTable = _analyzer.Analyze(program);
        var generator = new BytecodeGenerator(symbolTable);

        // Act
        Tutel.Core.Compiler.Bytecode.Models.TutelBytecode bytecode = generator.Generate(program);

        // Assert
        Assert.NotNull(bytecode);
        Assert.Single(bytecode.Functions);
        Assert.Single(bytecode.Globals);
        Assert.DoesNotContain("__init__", symbolTable.Functions.Select(f => f.Name));
    }

    [Fact]
    public void Generate_FunctionWithParameters_GeneratesBytecode()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "add",
                    new IntType(),
                    [new Parameter("a", new IntType()), new Parameter("b", new IntType()),],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new ReturnStatement
                            {
                                Value = new BinaryExpression(
                                    new IdentifierExpression("a"),
                                    new Token(TokenType.Operator, "+", 1, 1),
                                    new IdentifierExpression("b")),
                            },
                        ],
                    }),
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new ReturnStatement
                            {
                                Value = new FunctionCallExpression(
                                    "add",
                                    [new IntegerLiteral { Value = 5 }, new IntegerLiteral { Value = 3 },]),
                            },
                        ],
                    })
            ],
        };

        SymbolTable symbolTable = _analyzer.Analyze(program);
        var generator = new BytecodeGenerator(symbolTable);

        // Act
        Tutel.Core.Compiler.Bytecode.Models.TutelBytecode bytecode = generator.Generate(program);

        // Assert
        Assert.NotNull(bytecode);
        Assert.Equal(2, bytecode.Functions.Count);
        Tutel.Core.Compiler.Bytecode.Models.FunctionCode addFunction = bytecode.Functions[0];
        Assert.Equal(2, addFunction.Arity); // два параметра
        Tutel.Core.Compiler.Bytecode.Models.FunctionCode mainFunction = bytecode.Functions[1];
        Assert.Contains((byte)OpCode.CALL, mainFunction.Code);
    }

    [Fact]
    public void Generate_ArrayOperations_GeneratesBytecode()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new VariableDeclarationStatement(
                                "arr",
                                new ArrayType(new IntType()),
                                new ArrayCreationExpression(
                                    new IntType(),
                                    new ArrayType(new IntType()),
                                    new IntegerLiteral { Value = 10 })),
                            new ExpressionStatement(
                                new ArrayAssignmentExpression(
                                    new ArrayAccessExpression(
                                        new IdentifierExpression("arr"),
                                        new IntegerLiteral { Value = 0 }),
                                    new Token(TokenType.Operator, "=", 1, 1),
                                    new IntegerLiteral { Value = 42 })),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral { Value = 0 },
                            },
                        ],
                    })
            ],
        };

        SymbolTable symbolTable = _analyzer.Analyze(program);
        var generator = new BytecodeGenerator(symbolTable);

        // Act
        Tutel.Core.Compiler.Bytecode.Models.TutelBytecode bytecode = generator.Generate(program);

        // Assert
        Assert.NotNull(bytecode);
        Tutel.Core.Compiler.Bytecode.Models.FunctionCode mainFunction = bytecode.Functions[0];
        Assert.Contains((byte)OpCode.ARRAY_NEW, mainFunction.Code);
        Assert.Contains((byte)OpCode.ARRAY_STORE, mainFunction.Code);
    }

    [Fact]
    public void Generate_ArrayLiteral_GeneratesBytecode()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new VariableDeclarationStatement(
                                "arr",
                                new ArrayType(new IntType()),
                                new ArrayLiteralExpression
                                {
                                    Elements =
                                    [
                                        new IntegerLiteral { Value = 1 },
                                        new IntegerLiteral { Value = 2 },
                                        new IntegerLiteral { Value = 3 },
                                    ],
                                }),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral { Value = 0 },
                            },
                        ],
                    })
            ],
        };

        SymbolTable symbolTable = _analyzer.Analyze(program);
        var generator = new BytecodeGenerator(symbolTable);

        // Act
        Tutel.Core.Compiler.Bytecode.Models.TutelBytecode bytecode = generator.Generate(program);

        // Assert
        Assert.NotNull(bytecode);
        Tutel.Core.Compiler.Bytecode.Models.FunctionCode mainFunction = bytecode.Functions[0];
        Assert.Contains((byte)OpCode.ARRAY_NEW, mainFunction.Code);
        Assert.Contains((byte)OpCode.ARRAY_STORE, mainFunction.Code);
        Assert.Contains((byte)OpCode.DUP, mainFunction.Code);
    }

    [Fact]
    public void Generate_LengthExpression_GeneratesBytecode()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new VariableDeclarationStatement(
                                "arr",
                                new ArrayType(new IntType()),
                                new ArrayCreationExpression(
                                    new IntType(),
                                    new ArrayType(new IntType()),
                                    new IntegerLiteral { Value = 5 })),
                            new VariableDeclarationStatement(
                                "len",
                                new IntType(),
                                new LengthExpression(new IdentifierExpression("arr"))),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral { Value = 0 },
                            },
                        ],
                    })
            ],
        };

        SymbolTable symbolTable = _analyzer.Analyze(program);
        var generator = new BytecodeGenerator(symbolTable);

        // Act
        Tutel.Core.Compiler.Bytecode.Models.TutelBytecode bytecode = generator.Generate(program);

        // Assert
        Assert.NotNull(bytecode);
        Tutel.Core.Compiler.Bytecode.Models.FunctionCode mainFunction = bytecode.Functions[0];
        Assert.Contains((byte)OpCode.ARRAY_LEN, mainFunction.Code);
    }

    [Fact]
    public void Generate_IfStatement_GeneratesBytecode()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new IfStatement(
                                new BinaryExpression(
                                    new IntegerLiteral { Value = 1 },
                                    new Token(TokenType.Operator, ">", 1, 1),
                                    new IntegerLiteral { Value = 0 }),
                                new ReturnStatement { Value = new IntegerLiteral { Value = 1 } },
                                new ReturnStatement { Value = new IntegerLiteral { Value = 0 } }),
                        ],
                    })
            ],
        };

        SymbolTable symbolTable = _analyzer.Analyze(program);
        var generator = new BytecodeGenerator(symbolTable);

        // Act
        Tutel.Core.Compiler.Bytecode.Models.TutelBytecode bytecode = generator.Generate(program);

        // Assert
        Assert.NotNull(bytecode);
        Tutel.Core.Compiler.Bytecode.Models.FunctionCode mainFunction = bytecode.Functions[0];
        Assert.Contains((byte)OpCode.JZ, mainFunction.Code);
        Assert.Contains((byte)OpCode.JMP, mainFunction.Code);
    }

    [Fact]
    public void Generate_WhileStatement_GeneratesBytecode()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new VariableDeclarationStatement("i", new IntType(), new IntegerLiteral { Value = 0 }),
                            new WhileStatement(
                                new BinaryExpression(
                                    new IdentifierExpression("i"),
                                    new Token(TokenType.Operator, "<", 1, 1),
                                    new IntegerLiteral { Value = 10 }),
                                new BlockStatement
                                {
                                    Statements =
                                    [
                                        new ExpressionStatement(
                                            new AssignmentExpression(
                                                new IdentifierExpression("i"),
                                                new Token(TokenType.Operator, "=", 1, 1),
                                                new BinaryExpression(
                                                    new IdentifierExpression("i"),
                                                    new Token(TokenType.Operator, "+", 1, 1),
                                                    new IntegerLiteral { Value = 1 }))),
                                    ],
                                }),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral { Value = 0 },
                            },
                        ],
                    })
            ],
        };

        SymbolTable symbolTable = _analyzer.Analyze(program);
        var generator = new BytecodeGenerator(symbolTable);

        // Act
        Tutel.Core.Compiler.Bytecode.Models.TutelBytecode bytecode = generator.Generate(program);

        // Assert
        Assert.NotNull(bytecode);
        Tutel.Core.Compiler.Bytecode.Models.FunctionCode mainFunction = bytecode.Functions[0];
        Assert.Contains((byte)OpCode.JZ, mainFunction.Code);
        Assert.Contains((byte)OpCode.JMP, mainFunction.Code);
    }

    [Fact]
    public void Generate_ForStatement_GeneratesBytecode()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new ForStatement(
                                new BlockStatement
                                {
                                    Statements =
                                    [
                                        new ReturnStatement
                                        {
                                            Value = new IntegerLiteral { Value = 0 },
                                        },
                                    ],
                                })
                            {
                                Initializer = new VariableDeclarationStatement(
                                    "i",
                                    new IntType(),
                                    new IntegerLiteral { Value = 0 }),
                                Condition = new BinaryExpression(
                                    new IdentifierExpression("i"),
                                    new Token(TokenType.Operator, "<", 1, 1),
                                    new IntegerLiteral { Value = 10 }),
                                Increment = new AssignmentExpression(
                                    new IdentifierExpression("i"),
                                    new Token(TokenType.Operator, "=", 1, 1),
                                    new BinaryExpression(
                                        new IdentifierExpression("i"),
                                        new Token(TokenType.Operator, "+", 1, 1),
                                        new IntegerLiteral { Value = 1 })),
                            },
                        ],
                    })
            ],
        };

        SymbolTable symbolTable = _analyzer.Analyze(program);
        var generator = new BytecodeGenerator(symbolTable);

        // Act
        Tutel.Core.Compiler.Bytecode.Models.TutelBytecode bytecode = generator.Generate(program);

        // Assert
        Assert.NotNull(bytecode);
        Tutel.Core.Compiler.Bytecode.Models.FunctionCode mainFunction = bytecode.Functions[0];

        Assert.Contains((byte)OpCode.JZ, mainFunction.Code);
        Assert.Contains((byte)OpCode.JMP, mainFunction.Code);
    }

    [Fact]
    public void Generate_PrintStatement_GeneratesBytecode()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new PrintStatement(
                                new System.Collections.ObjectModel.Collection<ExpressionAst>
                                {
                                    new IntegerLiteral { Value = 42 },
                                    new IntegerLiteral { Value = 100 },
                                }),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral { Value = 0 },
                            },
                        ],
                    })
            ],
        };

        SymbolTable symbolTable = _analyzer.Analyze(program);
        var generator = new BytecodeGenerator(symbolTable);

        // Act
        Tutel.Core.Compiler.Bytecode.Models.TutelBytecode bytecode = generator.Generate(program);

        // Assert
        Assert.NotNull(bytecode);
        Tutel.Core.Compiler.Bytecode.Models.FunctionCode mainFunction = bytecode.Functions[0];
        Assert.Contains((byte)OpCode.PRINT_INT, mainFunction.Code);
    }

    [Fact]
    public void Generate_ReadExpression_GeneratesBytecode()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new VariableDeclarationStatement(
                                "x",
                                new IntType(),
                                new ReadExpression()),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral { Value = 0 },
                            },
                        ],
                    })
            ],
        };

        SymbolTable symbolTable = _analyzer.Analyze(program);
        var generator = new BytecodeGenerator(symbolTable);

        // Act
        Tutel.Core.Compiler.Bytecode.Models.TutelBytecode bytecode = generator.Generate(program);

        // Assert
        Assert.NotNull(bytecode);
        Tutel.Core.Compiler.Bytecode.Models.FunctionCode mainFunction = bytecode.Functions[0];
        Assert.Contains((byte)OpCode.READ_INT, mainFunction.Code);
    }

    [Fact]
    public void Generate_LogicalOperators_GeneratesBytecode()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new VariableDeclarationStatement(
                                "result",
                                new IntType(),
                                new BinaryExpression(
                                    new BinaryExpression(
                                        new IntegerLiteral { Value = 1 },
                                        new Token(TokenType.Operator, ">", 1, 1),
                                        new IntegerLiteral { Value = 0 }),
                                    new Token(TokenType.Operator, "&&", 1, 1),
                                    new BinaryExpression(
                                        new IntegerLiteral { Value = 2 },
                                        new Token(TokenType.Operator, "<", 1, 1),
                                        new IntegerLiteral { Value = 3 }))),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral { Value = 0 },
                            },
                        ],
                    })
            ],
        };

        SymbolTable symbolTable = _analyzer.Analyze(program);
        var generator = new BytecodeGenerator(symbolTable);

        // Act
        Tutel.Core.Compiler.Bytecode.Models.TutelBytecode bytecode = generator.Generate(program);

        // Assert
        Assert.NotNull(bytecode);
        Tutel.Core.Compiler.Bytecode.Models.FunctionCode mainFunction = bytecode.Functions[0];

        Assert.Contains((byte)OpCode.JZ, mainFunction.Code);
        Assert.Contains((byte)OpCode.JMP, mainFunction.Code);
    }

    [Fact]
    public void Generate_NegativeNumber_GeneratesBytecode()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new VariableDeclarationStatement(
                                "x",
                                new IntType(),
                                new UnaryExpression(
                                    new Token(TokenType.Operator, "-", 1, 1),
                                    new IntegerLiteral { Value = 42 })),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral { Value = 0 },
                            },
                        ],
                    })
            ],
        };

        SymbolTable symbolTable = _analyzer.Analyze(program);
        var generator = new BytecodeGenerator(symbolTable);

        // Act
        Tutel.Core.Compiler.Bytecode.Models.TutelBytecode bytecode = generator.Generate(program);

        // Assert
        Assert.NotNull(bytecode);
        Tutel.Core.Compiler.Bytecode.Models.FunctionCode mainFunction = bytecode.Functions[0];
        Assert.Contains((byte)OpCode.NEG, mainFunction.Code);
    }

    [Fact]
    public void Generate_MultipleGlobalVariables_GeneratesCorrectInit()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateGlobalVariable("a", new IntType(), new IntegerLiteral { Value = 1 }),
                CreateGlobalVariable("b", new IntType(), new IntegerLiteral { Value = 2 }),
                CreateGlobalVariable("c", new IntType(), null),
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral { Value = 0 },
                            },
                        ],
                    })
            ],
        };

        SymbolTable symbolTable = _analyzer.Analyze(program);
        var generator = new BytecodeGenerator(symbolTable);

        // Act
        Tutel.Core.Compiler.Bytecode.Models.TutelBytecode bytecode = generator.Generate(program);

        // Assert
        Assert.NotNull(bytecode);
        Assert.Equal(3, bytecode.Globals.Count);
        Assert.Equal(2, bytecode.Functions.Count);

        Tutel.Core.Compiler.SemanticAnalysis.Models.FunctionSymbol initFunc = symbolTable.Functions.First(f => f.Name == "__init__");
        Assert.Equal(1, symbolTable.Functions.IndexOf(initFunc));
        Assert.Equal(0, bytecode.EntryFunctionIndex);
    }

    private GlobalVariableDeclaration CreateGlobalVariable(
        string name,
        TypeNode type,
        ExpressionAst? initValue = null)
    {
        return new GlobalVariableDeclaration(name, type, initValue)
        {
            Line = 1,
            Column = 1,
        };
    }

    private FunctionDeclaration CreateFunction(
        string name,
        TypeNode returnType,
        List<Parameter> parameters,
        BlockStatement body)
    {
        return new FunctionDeclaration(name, body, returnType)
        {
            Parameters = parameters,
            Line = 1,
            Column = 1,
        };
    }

    private ProgramAst CreateProgramWithMain()
    {
        return new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral { Value = 0 },
                            },
                        ],
                    })
            ],
        };
    }
}
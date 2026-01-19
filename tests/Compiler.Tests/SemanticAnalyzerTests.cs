using Tutel.Compiler.SemanticAnalysis;
using Tutel.Core.Compiler.AST;
using Tutel.Core.Compiler.AST.Declarations;
using Tutel.Core.Compiler.AST.Expressions;
using Tutel.Core.Compiler.AST.Expressions.Literals;
using Tutel.Core.Compiler.AST.Statements;
using Tutel.Core.Compiler.AST.Types;
using Tutel.Core.Compiler.Lexing.Models.Enums;
using Tutel.Core.Compiler.Lexing.Models.Tokens;
using Xunit;

namespace Compiler.Tests;

public class SemanticAnalyzerTests
{
    private readonly SemanticAnalyzer _analyzer = new SemanticAnalyzer();

    // Тесты для SymbolTableBuilder
    [Fact]
    public void Analyze_ValidProgramWithMain_NoErrors()
    {
        // Arrange
        ProgramAst program = CreateProgramWithMain();

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Empty(result.Errors);
        Assert.NotEmpty(result.Functions);
        Assert.Single(result.Functions);
        Assert.Equal("main", result.Functions[0].Name);
    }

    [Fact]
    public void Analyze_MissingMainFunction_ReportsError()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                new FunctionDeclaration(
                    "other",
                    new BlockStatement { Statements = new List<StatementAst>() },
                    new VoidType())
                {
                    Parameters = new List<Parameter>(),
                    Line = 1,
                    Column = 1,
                },
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Single(result.Errors);
        Assert.Contains(
            "Программа должна содержать функцию 'main'",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_MainFunctionWithParameters_ReportsError()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                new FunctionDeclaration(
                    "main",
                    new BlockStatement
                    {
                        Statements = new List<StatementAst>()
                        {
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 1,
                                },
                            },
                        },
                    },
                    new IntType())
                {
                    Parameters = new List<Parameter>
                    {
                        new Parameter("x", new IntType()),
                    },
                    Line = 1,
                    Column = 1,
                },
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Single(result.Errors);
        Assert.Contains(
            "Функция 'main' не должна иметь параметров",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_MainFunctionReturnsVoid_ReportsError()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                new FunctionDeclaration(
                    "main",
                    new BlockStatement { Statements = new List<StatementAst>() },
                    new VoidType())
                {
                    Parameters = new List<Parameter>(),
                    Line = 1,
                    Column = 1,
                },
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Single(result.Errors);
        Assert.Contains(
            "Функция main должна возвращать int",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_DuplicateGlobalVariable_ReportsError()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateGlobalVariable("x", new IntType()),
                CreateGlobalVariable("x", new IntType()),
                CreateProgramWithMain().Declarations[0],
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Single(result.Errors);
        Assert.Contains(
            "Глобальная переменная 'x' уже объявлена",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_DuplicateFunction_ReportsError()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "foo",
                    new VoidType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                            [],
                    }),
                CreateFunction(
                    "foo",
                    new VoidType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                            [],
                    }),
                CreateProgramWithMain().Declarations[0],
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Single(result.Errors);
        Assert.Contains(
            "Функция 'foo' уже объявлена",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_DuplicateLocalVariable_ReportsError()
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
                            new VariableDeclarationStatement("x", new IntType()),
                            new VariableDeclarationStatement("x", new IntType()),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 0,
                                },
                            },
                        ],
                    })
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Single(result.Errors);
        Assert.Contains(
            "Переменная 'x' уже объявлена в этой области",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_UndeclaredVariable_ReportsError()
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
                            new ExpressionStatement(new IdentifierExpression("undeclared")),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 0,
                                },
                            },
                        ],
                    })
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Single(result.Errors);
        Assert.Contains(
            "Неизвестный идентификатор 'undeclared'",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_TypeMismatchInBinaryOperation_ReportsError()
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
                                new BinaryExpression(
                                    new IntegerLiteral
                                    {
                                        Value = 5,
                                    },
                                    new Token(
                                        TokenType.Operator,
                                        "+",
                                        1,
                                        1),
                                    new IdentifierExpression("nonexistent"))),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 0,
                                },
                            },
                        ],
                    }),
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(
            "Неизвестный идентификатор 'nonexistent'",
            result.Errors.Select(e => e.Message));
    }

    [Fact]
    public void Analyze_FunctionCallWithWrongArgumentCount_ReportsError()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "helper",
                    new VoidType(),
                    [new Parameter("a", new IntType())],
                    new BlockStatement { Statements = new List<StatementAst>() }),
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new ExpressionStatement(
                                new FunctionCallExpression(
                                    "helper",
                                    new List<ExpressionAst>())),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 0,
                                },
                            },
                        ],
                    }),
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Single(result.Errors);
        Assert.Contains(
            "ожидает 1 аргументов",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_FunctionCallWithWrongArgumentType_ReportsError()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "helper",
                    new IntType(),
                    [new Parameter("a", new IntType())],
                    new BlockStatement { Statements = [], }),
                CreateFunction(
                    "main",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new ExpressionStatement(
                                new FunctionCallExpression(
                                    "helper",
                                    [new FunctionCallExpression("voidFunc", []),])),
                        ],
                    }),
                CreateFunction(
                    "voidFunc",
                    new VoidType(),
                    [],
                    new BlockStatement { Statements = [], }),
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Тип аргумента", result.Errors[0].Message, StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_ArrayAccessOnNonArray_ReportsError()
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
                                new IntegerLiteral
                                {
                                    Value = 5,
                                }),
                            new ExpressionStatement(
                                new ArrayAccessExpression(
                                    new IdentifierExpression("x"),
                                    new IntegerLiteral
                                    {
                                        Value = 0,
                                    })),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 0,
                                },
                            },
                        ],
                    })
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Single(result.Errors);
        Assert.Contains(
            "Индексация поддерживается только для массивов",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_AssignmentTypeMismatch_ReportsError()
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
                            new VariableDeclarationStatement("x", new IntType()),
                            new ExpressionStatement(
                                new AssignmentExpression(
                                    new IdentifierExpression("x"),
                                    new Token(TokenType.Operator, "=", 1, 1),
                                    new ArrayLiteralExpression())),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 0,
                                },
                            },
                        ],
                    })
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Single(result.Errors);
        Assert.Contains(
            "Тип присваиваемого значения не совпадает",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_ReturnTypeMismatch_ReportsError()
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
                            new ReturnStatement
                            {
                                Value = new ArrayLiteralExpression(),
                            },
                        ],
                    })
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Single(result.Errors);
        Assert.Contains(
            "Тип возвращаемого значения не совпадает",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_VoidFunctionReturnsValue_ReportsError()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "voidFunc",
                    new VoidType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 42,
                                },
                            },
                        ],
                    }),
                CreateProgramWithMain().Declarations[0],
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Single(result.Errors);
        Assert.Contains(
            "Тип возвращаемого значения не совпадает",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_ValidArrayOperations_NoErrors()
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
                                    new IntegerLiteral
                                    {
                                        Value = 10,
                                    })),
                            new ExpressionStatement(
                                new ArrayAssignmentExpression(
                                    new ArrayAccessExpression(
                                        new IdentifierExpression("arr"),
                                        new IntegerLiteral
                                        {
                                            Value = 0,
                                        }),
                                    new Token(TokenType.Operator, "=", 1, 1),
                                    new IntegerLiteral
                                    {
                                        Value = 42,
                                    })),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 0,
                                },
                            },
                        ],
                    })
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Empty(result.Errors);
    }

    // Тесты для ControlFlowAnalyzer
    [Fact]
    public void Analyze_BreakOutsideLoop_ReportsError()
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
                            new BreakStatement(),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 0,
                                },
                            },
                        ],
                    })
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Contains(
            "Оператор break вне цикла",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_ContinueOutsideLoop_ReportsError()
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
                            new ContinueStatement(),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 0,
                                },
                            },
                        ],
                    })
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Contains(
            "Оператор continue вне цикла",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_BreakInsideWhileLoop_NoError()
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
                            new WhileStatement(
                                new IntegerLiteral
                                {
                                    Value = 1,
                                },
                                new BlockStatement
                                {
                                    Statements =
                                    [
                                        new BreakStatement(),
                                    ],
                                }),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 0,
                                },
                            },
                        ],
                    })
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Analyze_MissingReturnInNonVoidFunction_ReportsError()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "noReturn",
                    new IntType(),
                    [],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new VariableDeclarationStatement(
                                "x",
                                new IntType(),
                                new IntegerLiteral
                                {
                                    Value = 42,
                                }),
                        ],
                    }),
                CreateProgramWithMain().Declarations[0],
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Single(result.Errors);
        Assert.Contains(
            "может не вернуть значение",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_AllPathsReturn_NoError()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "returnsInt",
                    new IntType(),
                    [new Parameter("x", new IntType())],
                    new BlockStatement
                    {
                        Statements =
                        [
                            new IfStatement(
                                new BinaryExpression(
                                    new IdentifierExpression("x"),
                                    new Token(TokenType.Operator, ">", 1, 1),
                                    new IntegerLiteral
                                    {
                                        Value = 0,
                                    }),
                                new ReturnStatement
                                {
                                    Value = new IntegerLiteral
                                    {
                                        Value = 1,
                                    },
                                },
                                new ReturnStatement
                                {
                                    Value = new IntegerLiteral
                                    {
                                        Value = 0,
                                    },
                                }),
                        ],
                    }),
                CreateProgramWithMain().Declarations[0],
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Analyze_UnreachableCodeAfterReturn_ReportsError()
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
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 42,
                                },
                            },
                            new ExpressionStatement(
                                new AssignmentExpression(
                                    new IdentifierExpression("x"),
                                    new Token(TokenType.Operator, "=", 1, 1),
                                    new IntegerLiteral
                                    {
                                        Value = 10,
                                    })),
                        ],
                    })
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains(
            "Недостижимый код",
            result.Errors[1].Message,
            StringComparison.CurrentCulture);
    }

    [Fact]
    public void Analyze_ValidVariableShadowing_NoError()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateGlobalVariable("x", new IntType()),
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
                                new IntegerLiteral
                                {
                                    Value = 10,
                                }),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 0,
                                },
                            },
                        ],
                    })
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Analyze_ValidFunctionWithParameters_NoError()
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
                                    [new IntegerLiteral { Value = 5, }, new IntegerLiteral { Value = 3, },]),
                            },
                        ],
                    })
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Analyze_ValidArrayLiteral_NoError()
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
                                        new IntegerLiteral
                                        {
                                            Value = 1,
                                        },
                                        new IntegerLiteral
                                        {
                                            Value = 2,
                                        },
                                        new IntegerLiteral
                                        {
                                            Value = 3,
                                        },
                                    ],
                                }),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 0,
                                },
                            },
                        ],
                    })
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Analyze_ArrayLiteralWithMixedTypes_ReportsError()
    {
        // Arrange
        var program = new ProgramAst
        {
            Declarations =
            [
                CreateFunction(
                    "voidFunc",
                    new VoidType(),
                    [],
                    new BlockStatement { Statements = [], }),
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
                                        new IntegerLiteral
                                        {
                                            Value = 1,
                                        },
                                        new FunctionCallExpression(
                                            "voidFunc",
                                            []),
                                    ],
                                }),
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 0,
                                },
                            },
                        ],
                    })
            ],
        };

        // Act
        SymbolTable result = _analyzer.Analyze(program);

        // Assert
        Assert.NotEmpty(result.Errors);
        Assert.Contains(
            "имеет несовместимый тип",
            result.Errors[0].Message,
            StringComparison.CurrentCulture);
    }

    private ProgramAst CreateProgramWithMain()
    {
        return new ProgramAst
        {
            Declarations =
            [
                new FunctionDeclaration(
                    "main",
                    new BlockStatement
                    {
                        Statements =
                        [
                            new ReturnStatement
                            {
                                Value = new IntegerLiteral
                                {
                                    Value = 0,
                                },
                            },
                        ],
                    },
                    new IntType())
                {
                    Parameters = new List<Parameter>(),
                    Line = 1,
                    Column = 1,
                },
            ],
        };
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
}
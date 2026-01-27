#pragma warning disable CA1707
#pragma warning disable CA1028

namespace Tutel.Core.Compiler.Bytecode.Enums;

public enum OpCode : byte
{
    // Константы и управление стеком
    NOP = 0x00,
    PUSH_INT = 0x01,
    PUSH_DOUBLE = 0x04,
    I2D = 0x05,
    POP = 0x02,
    DUP = 0x03,

    // Арифметические операции
    ADD = 0x10,
    SUB = 0x11,
    MUL = 0x12,
    DIV = 0x13,
    MOD = 0x14,
    NEG = 0x15,

    // Арифметические операции (double)
    DADD = 0x18,
    DSUB = 0x19,
    DMUL = 0x1A,
    DDIV = 0x1B,
    DMOD = 0x1C,
    DNEG = 0x1D,
    DSQRT = 0x1E,

    // Операции сравнения
    CMP_EQ = 0x20,
    CMP_NE = 0x21,
    CMP_LT = 0x22,
    CMP_LE = 0x23,
    CMP_GT = 0x24,
    CMP_GE = 0x25,

    // Операции сравнения (double) -> результат int (0/1)
    DCMP_EQ = 0x28,
    DCMP_NE = 0x29,
    DCMP_LT = 0x2A,
    DCMP_LE = 0x2B,
    DCMP_GT = 0x2C,
    DCMP_GE = 0x2D,

    // Управление потоком
    JMP = 0x30,
    JZ = 0x31,
    JNZ = 0x32,
    CALL = 0x33,
    RET = 0x34,
    HALT = 0xFF,

    // Работа с переменными
    LOAD_LOCAL = 0x40,
    STORE_LOCAL = 0x41,
    LOAD_GLOBAL = 0x50,
    STORE_GLOBAL = 0x51,

    // Работа с консолью
    PRINT_INT = 0x56,
    READ_INT = 0x57,
    PRINT_DOUBLE = 0x58,

    // Работа с массивами
    ARRAY_NEW = 0x60,
    ARRAY_LOAD = 0x61,
    ARRAY_STORE = 0x62,
    ARRAY_LEN = 0x63,
}

using System.Collections.ObjectModel;
using System.Text;
using Tutel.Compiler.SemanticAnalysis;
using Tutel.Core.Compiler.Bytecode.Enums;
using Tutel.Core.Compiler.Bytecode.Models;

namespace Tutel.Compiler.Bytecode;

public static class Disassembler
{
    private static readonly Dictionary<OpCode, int> OpCodeArgSizes = new()
    {
        { OpCode.NOP, 0 },
        { OpCode.PUSH_INT, 8 },
        { OpCode.PUSH_DOUBLE, 8 },
        { OpCode.I2D, 0 },
        { OpCode.POP, 0 },
        { OpCode.DUP, 0 },
        { OpCode.ADD, 0 },
        { OpCode.SUB, 0 },
        { OpCode.MUL, 0 },
        { OpCode.DIV, 0 },
        { OpCode.MOD, 0 },
        { OpCode.NEG, 0 },
        { OpCode.DADD, 0 },
        { OpCode.DSUB, 0 },
        { OpCode.DMUL, 0 },
        { OpCode.DDIV, 0 },
        { OpCode.DMOD, 0 },
        { OpCode.DNEG, 0 },
        { OpCode.DSQRT, 0 },
        { OpCode.CMP_EQ, 0 },
        { OpCode.CMP_NE, 0 },
        { OpCode.CMP_LT, 0 },
        { OpCode.CMP_LE, 0 },
        { OpCode.CMP_GT, 0 },
        { OpCode.CMP_GE, 0 },
        { OpCode.DCMP_EQ, 0 },
        { OpCode.DCMP_NE, 0 },
        { OpCode.DCMP_LT, 0 },
        { OpCode.DCMP_LE, 0 },
        { OpCode.DCMP_GT, 0 },
        { OpCode.DCMP_GE, 0 },
        { OpCode.LOAD_LOCAL, 1 },
        { OpCode.STORE_LOCAL, 1 },
        { OpCode.LOAD_GLOBAL, 2 },
        { OpCode.STORE_GLOBAL, 2 },
        { OpCode.CALL, 2 },
        { OpCode.RET, 0 },
        { OpCode.JMP, 4 },
        { OpCode.JZ, 4 },
        { OpCode.JNZ, 4 },
        { OpCode.ARRAY_NEW, 0 },
        { OpCode.ARRAY_LOAD, 0 },
        { OpCode.ARRAY_STORE, 0 },
        { OpCode.ARRAY_LEN, 0 },
        { OpCode.PRINT_INT, 0 },
        { OpCode.PRINT_DOUBLE, 0 },
        { OpCode.READ_INT, 0 },
    };

    public static string Disassemble(TutelBytecode bytecode, SymbolTable symbolTable)
    {
        var sb = new StringBuilder();

        sb.AppendLine("=== BYTECODE DISASSEMBLY ===");
        sb.AppendLine();

        sb.AppendLine($"Entry function index (main): {bytecode.EntryFunctionIndex}");
        sb.AppendLine($"Global variables: {bytecode.Globals.Count}");
        sb.AppendLine($"Functions: {bytecode.Functions.Count}");
        sb.AppendLine();

        sb.AppendLine("=== GLOBAL VARIABLES ===");
        for (int i = 0; i < bytecode.Globals.Count; i++)
        {
            sb.AppendLine($"  GLOBAL[{i}] = {bytecode.Globals[i]}");
        }

        sb.AppendLine();

        for (int funcIndex = 0; funcIndex < bytecode.Functions.Count; funcIndex++)
        {
            FunctionCode func = bytecode.Functions[funcIndex];
            sb.AppendLine($"=== FUNCTION #{funcIndex} ===");
            sb.AppendLine($"  Name: {GetFunctionName(bytecode, funcIndex, symbolTable)}");
            sb.AppendLine($"  Arity: {func.Arity}");
            sb.AppendLine($"  Locals count: {func.LocalsCount}");
            sb.AppendLine($"  Code size: {func.Code.Count} bytes");
            sb.AppendLine();

            sb.AppendLine("  Instructions:");
            sb.Append(DisassembleFunction(func.Code, symbolTable, bytecode));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string GetFunctionName(
        TutelBytecode bytecode,
        int index,
        SymbolTable symbolTable)
    {
        return index >= 0 && index < symbolTable.Functions.Count
            ? symbolTable.Functions[index].Name
            : $"func_{index}";
    }

    private static string DisassembleFunction(
        Collection<byte> code,
        SymbolTable symbolTable,
        TutelBytecode bytecode)
    {
        var sb = new StringBuilder();
        int offset = 0;

        while (offset < code.Count)
        {
            sb.Append($"    {offset.ToString("X4")}: ");

            var opcode = (OpCode)code[offset++];
            sb.Append($"{opcode.ToString().PadRight(12)}");

            if (!OpCodeArgSizes.TryGetValue(opcode, out int argSize))
            {
                sb.AppendLine($"[ERROR: Unknown opcode {opcode}]");
                break;
            }

            if (argSize > 0)
            {
                if (offset + argSize > code.Count)
                {
                    sb.AppendLine($"[ERROR: Insufficient bytes for opcode {opcode}]");
                    break;
                }

                byte[] argBytes = new byte[argSize];
                for (int i = 0; i < argSize; i++)
                {
                    argBytes[i] = code[offset + i];
                }

                switch (opcode)
                {
                    case OpCode.PUSH_INT:
                        long intValue = BitConverter.ToInt64(argBytes, 0);
                        sb.Append($" {intValue}");
                        break;
                    case OpCode.PUSH_DOUBLE:
                        long doubleBits = BitConverter.ToInt64(argBytes, 0);
                        double doubleValue = BitConverter.Int64BitsToDouble(doubleBits);
                        sb.Append($" {doubleValue:R}");
                        break;

                    case OpCode.LOAD_LOCAL:
                    case OpCode.STORE_LOCAL:
                        byte localIndex = argBytes[0];
                        sb.Append($" [{localIndex}]");
                        break;

                    case OpCode.LOAD_GLOBAL:
                    case OpCode.STORE_GLOBAL:
                        ushort globalIndex = BitConverter.ToUInt16(argBytes, 0);
                        sb.Append($" [{globalIndex}]");
                        break;

                    case OpCode.CALL:
                        ushort funcIndex = BitConverter.ToUInt16(argBytes, 0);
                        string funcName = GetFunctionName(bytecode, funcIndex, symbolTable);
                        sb.Append($" {funcName}(#{funcIndex})");
                        break;

                    case OpCode.JMP:
                    case OpCode.JZ:
                    case OpCode.JNZ:
                        int jumpOffset = BitConverter.ToInt32(argBytes, 0);
                        int target = offset + argSize + jumpOffset;
                        sb.Append($" -> 0x{target:X4} (offset: {jumpOffset})");
                        break;

                    default:
                        sb.Append($" [");
                        for (int i = 0; i < argSize; i++)
                        {
                            sb.Append($"{argBytes[i]:X2}");
                            if (i < argSize - 1) sb.Append(" ");
                        }

                        sb.Append("]");
                        break;
                }

                offset += argSize;
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}

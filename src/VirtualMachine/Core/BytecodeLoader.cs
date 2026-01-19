// Copyright (c) Tutel Team. All rights reserved.
// Licensed under the MIT License.

namespace Tutel.VirtualMachine.Core;

/// <summary>
/// Loads and parses .tbc bytecode files.
/// </summary>
public static class BytecodeLoader
{
    /// <summary>
    /// Loads a bytecode module from a file.
    /// </summary>
    /// <param name="filePath">Path to the .tbc file.</param>
    /// <returns>The loaded bytecode module.</returns>
    /// <exception cref="FileNotFoundException">Thrown when file not found.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown when file format is invalid.</exception>
    public static BytecodeModule LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Bytecode file not found: {filePath}", filePath);
        }

        byte[] data = File.ReadAllBytes(filePath);
        return LoadFromBytes(data);
    }

    /// <summary>
    /// Loads a bytecode module from a byte array.
    /// </summary>
    /// <param name="data">The bytecode data.</param>
    /// <returns>The loaded bytecode module.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when data format is invalid.</exception>
    public static BytecodeModule LoadFromBytes(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length < 12)
        {
            throw new InvalidOperationException(
                $"Invalid bytecode: file too small ({data.Length} bytes, minimum 12)");
        }

        int offset = 0;

        // Read and validate magic number (0x00-0x03)
        uint magic = ReadUInt32(data, ref offset);
        if (magic != BytecodeFormat.MagicNumber)
        {
            throw new InvalidOperationException(
                $"Invalid bytecode: wrong magic number (expected 0x{BytecodeFormat.MagicNumber:X8}, got 0x{magic:X8})");
        }

        // Read version (0x04-0x05) - 2 bytes
        ushort version = ReadUInt16(data, ref offset);
        if (version != BytecodeFormat.Version)
        {
            throw new InvalidOperationException(
                $"Unsupported bytecode version: {version} (expected {BytecodeFormat.Version})");
        }

        // Read entry point index (0x06-0x07)
        ushort entryPointIndex = ReadUInt16(data, ref offset);

        // Read global variable count (0x08-0x09)
        ushort globalVariableCount = ReadUInt16(data, ref offset);

        // Read function count (0x0A-0x0B)
        ushort functionCount = ReadUInt16(data, ref offset);

        // Read functions
        Dictionary<ushort, FunctionInfo> functions = new();

        for (int i = 0; i < functionCount; i++)
        {
            EnsureBytes(data, offset, 6);

            // Per spec: arity (1 byte), locals_count (1 byte), code_size (4 bytes)
            byte arity = data[offset++];
            byte localsCount = data[offset++];
            uint bytecodeSize = ReadUInt32(data, ref offset);

            EnsureBytes(data, offset, (int)bytecodeSize);

            byte[] bytecode = new byte[bytecodeSize];
            Array.Copy(data, offset, bytecode, 0, (int)bytecodeSize);
            offset += (int)bytecodeSize;

            // Function index is implicit (position in the file)
            ushort funcIndex = (ushort)i;
            FunctionInfo functionInfo = new(funcIndex, arity, localsCount, bytecode);
            functions[funcIndex] = functionInfo;
        }

        // Validate entry point exists
        if (!functions.ContainsKey(entryPointIndex))
        {
            throw new InvalidOperationException(
                $"Invalid bytecode: entry point function {entryPointIndex} not found");
        }

        return new BytecodeModule(version, globalVariableCount, entryPointIndex, functions);
    }

    private static uint ReadUInt32(byte[] data, ref int offset)
    {
        EnsureBytes(data, offset, 4);
        uint value = BitConverter.ToUInt32(data, offset);
        offset += 4;
        return value;
    }

    private static ushort ReadUInt16(byte[] data, ref int offset)
    {
        EnsureBytes(data, offset, 2);
        ushort value = BitConverter.ToUInt16(data, offset);
        offset += 2;
        return value;
    }

    private static void EnsureBytes(byte[] data, int offset, int count)
    {
        if (offset + count > data.Length)
        {
            throw new InvalidOperationException(
                $"Invalid bytecode: unexpected end of file at offset {offset}, needed {count} bytes");
        }
    }
}

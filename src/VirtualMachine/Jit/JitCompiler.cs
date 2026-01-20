using Tutel.VirtualMachine.Core;
using Tutel.VirtualMachine.Memory;
using ExecutionContext = Tutel.VirtualMachine.Instructions.ExecutionContext;

namespace Tutel.VirtualMachine.Jit;

/// <summary>
/// JIT compiler: parses bytecode into a small IR and builds a managed entry point.
/// </summary>
internal sealed class JitCompiler
{
    public bool Debug { get; } = false;

    /// <summary>
    /// Optional resolver (for future inlining and CALL support).
    /// You can pass a resolver from JitRuntime when you’re ready.
    /// </summary>
    internal interface IFunctionResolver
    {
        FunctionInfo GetFunction(ushort index);
    }

    private readonly IFunctionResolver? _resolver;

    public JitCompiler(IFunctionResolver? resolver = null)
    {
        _resolver = resolver;
    }

    internal sealed class ParseContext
    {
        public byte[] Code { get; }

        public int Pc { get; set; }

        private readonly Dictionary<int, int> _bytePcToInstr = new();

        public int CurrentInstrStartPc { get; set; }

        public ParseContext(byte[] code)
        {
            Code = code;
        }

        public void MarkInstructionStart(int bytePc, int instrIndex)
        {
            _bytePcToInstr[bytePc] = instrIndex;
        }

        public int MapBytePcToInstrIndex(int bytePc)
        {
            if (!_bytePcToInstr.TryGetValue(bytePc, out int instr))
                throw new InvalidOperationException($"JMP target not parsed: bytePc={bytePc}");
            return instr;
        }

        public bool TryMapBytePcToInstrIndex(int bytePc, out int instrIndex)
        {
            return _bytePcToInstr.TryGetValue(bytePc, out instrIndex);
        }
    }

    internal delegate long JitEntryPoint(ExecutionContext context);

    internal enum JitOp
    {
        // Stack / const
        Nop,
        PushConst,
        Pop,
        Dup,

        // Arithmetic
        Add,
        Sub,
        Mul,
        Div,
        Mod,
        Neg,

        // Comparisons
        CmpEq,
        CmpNe,
        CmpLt,
        CmpLe,
        CmpGt,
        CmpGe,

        // Control flow
        Jmp,
        Jz,
        Jnz,
        Ret,
        Halt,

        // Memory
        LoadLocal,
        StoreLocal,
        LoadGlobal,
        StoreGlobal,

        Call,

        // I/O
        PrintInt,
        ReadInt,

        // Arrays
        ArrayNew,
        ArrayLoad,
        ArrayStore,
        ArrayLen,
    }

    internal sealed class JitInstruction
    {
        public JitOp Op { get; }

        public long Operand { get; }

        public int TargetInstr { get; set; } = -1;

        public int? TargetBytePc { get; }

        public JitInstruction(JitOp op, long operand = 0, int? targetBytePc = null)
        {
            Op = op;
            Operand = operand;
            TargetBytePc = targetBytePc;
        }

        public override string ToString()
        {
            return Op switch
            {
                JitOp.PushConst => $"PushInt({Operand})",
                JitOp.LoadLocal => $"LoadLocal({Operand})",
                JitOp.StoreLocal => $"StoreLocal({Operand})",
                JitOp.LoadGlobal => $"LoadGlobal({Operand})",
                JitOp.StoreGlobal => $"StoreGlobal({Operand})",
                JitOp.Jmp => $"Jmp({TargetInstr})",
                JitOp.Jz => $"Jz({TargetInstr})",
                JitOp.Jnz => $"Jnz({TargetInstr})",
                JitOp.Nop => Op.ToString(),
                JitOp.Pop => Op.ToString(),
                JitOp.Dup => Op.ToString(),
                JitOp.Add => Op.ToString(),
                JitOp.Sub => Op.ToString(),
                JitOp.Mul => Op.ToString(),
                JitOp.Div => Op.ToString(),
                JitOp.Mod => Op.ToString(),
                JitOp.Neg => Op.ToString(),
                JitOp.CmpEq => Op.ToString(),
                JitOp.CmpNe => Op.ToString(),
                JitOp.CmpLt => Op.ToString(),
                JitOp.CmpLe => Op.ToString(),
                JitOp.CmpGt => Op.ToString(),
                JitOp.CmpGe => Op.ToString(),
                JitOp.Ret => Op.ToString(),
                JitOp.Call => $"Call({Operand})",
                JitOp.Halt => Op.ToString(),
                JitOp.PrintInt => Op.ToString(),
                JitOp.ReadInt => Op.ToString(),
                JitOp.ArrayNew => Op.ToString(),
                JitOp.ArrayLoad => Op.ToString(),
                JitOp.ArrayStore => Op.ToString(),
                JitOp.ArrayLen => Op.ToString(),
                _ => Op.ToString(),
            };
        }
    }

    public bool TryCompile(FunctionInfo functionInfo, out JitEntryPoint? entry)
    {
        if (Debug)
        {
            Console.WriteLine($"[JIT] Compiling function {functionInfo.Index}");
        }

        if (!TryParseBytecode(functionInfo, out List<JitInstruction> instructions))
        {
            entry = null;
            return false;
        }

        if (_resolver != null)
        {
            InlineLeafCalls(instructions);
        }

        bool hasBackwardJump = HasBackwardJump(instructions);
        entry = BuildEntryPoint(instructions, hasBackwardJump);

        if (Debug)
        {
            Console.WriteLine(
                $"[JIT] Compiled function {functionInfo.Index} as " +
                string.Join(", ", instructions));
        }

        return true;
    }

    private static bool HasBackwardJump(List<JitInstruction> instructions)
    {
        for (int i = 0; i < instructions.Count; i++)
        {
            JitOp op = instructions[i].Op;
            if (op is JitOp.Jmp or JitOp.Jz or JitOp.Jnz)
            {
                if (instructions[i].TargetInstr >= 0 && instructions[i].TargetInstr < i)
                    return true;
            }
        }

        return false;
    }

    private static bool TryParseBytecode(FunctionInfo functionInfo, out List<JitInstruction> instructions)
    {
        byte[] code = functionInfo.Bytecode;
        instructions = new List<JitInstruction>();

        var ctx = new ParseContext(code);
        Dictionary<Opcode, Action> opcodeHandlers = CreateOpcodeParsers(ctx, instructions);

        while (ctx.Pc < code.Length)
        {
            int instrStartPc = ctx.Pc;
            ctx.CurrentInstrStartPc = instrStartPc;

            ctx.MarkInstructionStart(instrStartPc, instructions.Count);

            var opcode = (Opcode)ctx.Code[ctx.Pc++];

            if (!opcodeHandlers.TryGetValue(opcode, out Action? handler))
                return false;

            handler();

            ctx.Pc = instrStartPc + OpcodeInfo.GetInstructionSize(opcode);

            if (ctx.Pc < 0 || ctx.Pc > code.Length)
            {
                throw new InvalidOperationException($"JIT parser PC out of range after {opcode} at {instrStartPc}");
            }
        }

        for (int i = 0; i < instructions.Count; i++)
        {
            JitInstruction inst = instructions[i];

            if (inst.TargetBytePc is null)
                continue;

            if (!ctx.TryMapBytePcToInstrIndex(inst.TargetBytePc.Value, out int targetInstr))
            {
                throw new InvalidOperationException(
                    $"JMP target not parsed: bytePc={inst.TargetBytePc.Value}");
            }

            inst.TargetInstr = targetInstr;
        }

        return true;
    }

    private static Dictionary<Opcode, Action> CreateOpcodeParsers(
        ParseContext ctx,
        List<JitInstruction> instructions)
    {
        return new Dictionary<Opcode, Action>()
        {
            [Opcode.Nop] = () => instructions.Add(new JitInstruction(JitOp.Nop, 0)),

            [Opcode.PushInt] = () =>
            {
                long value = BitConverter.ToInt64(ctx.Code, ctx.Pc);
                instructions.Add(new JitInstruction(JitOp.PushConst, value));
            },
            [Opcode.Ret] = () => instructions.Add(new JitInstruction(JitOp.Ret)),
            [Opcode.Pop] = () => instructions.Add(new JitInstruction(JitOp.Pop, 0)),
            [Opcode.Dup] = () => instructions.Add(new JitInstruction(JitOp.Dup, 0)),

            [Opcode.Add] = () => instructions.Add(new JitInstruction(JitOp.Add, 0)),
            [Opcode.Sub] = () => instructions.Add(new JitInstruction(JitOp.Sub, 0)),
            [Opcode.Mul] = () => instructions.Add(new JitInstruction(JitOp.Mul, 0)),
            [Opcode.Div] = () => instructions.Add(new JitInstruction(JitOp.Div, 0)),
            [Opcode.Mod] = () => instructions.Add(new JitInstruction(JitOp.Mod, 0)),
            [Opcode.Neg] = () => instructions.Add(new JitInstruction(JitOp.Neg, 0)),

            [Opcode.CmpEq] = () => instructions.Add(new JitInstruction(JitOp.CmpEq, 0)),
            [Opcode.CmpNe] = () => instructions.Add(new JitInstruction(JitOp.CmpNe, 0)),
            [Opcode.CmpLt] = () => instructions.Add(new JitInstruction(JitOp.CmpLt, 0)),
            [Opcode.CmpLe] = () => instructions.Add(new JitInstruction(JitOp.CmpLe, 0)),
            [Opcode.CmpGt] = () => instructions.Add(new JitInstruction(JitOp.CmpGt, 0)),
            [Opcode.CmpGe] = () => instructions.Add(new JitInstruction(JitOp.CmpGe, 0)),
            [Opcode.LoadLocal] = () =>
            {
                byte index = ctx.Code[ctx.Pc];
                instructions.Add(new JitInstruction(JitOp.LoadLocal, index));
            },
            [Opcode.StoreLocal] = () =>
            {
                byte index = ctx.Code[ctx.Pc];
                instructions.Add(new JitInstruction(JitOp.StoreLocal, index));
            },
            [Opcode.LoadGlobal] = () =>
            {
                ushort index = BitConverter.ToUInt16(ctx.Code, ctx.Pc);
                instructions.Add(new JitInstruction(JitOp.LoadGlobal, index));
            },
            [Opcode.StoreGlobal] = () =>
            {
                ushort index = BitConverter.ToUInt16(ctx.Code, ctx.Pc);
                instructions.Add(new JitInstruction(JitOp.StoreGlobal, index));
            },
            [Opcode.Jmp] = () =>
            {
                int offset = BitConverter.ToInt32(ctx.Code, ctx.Pc);

                int instrStartPc = ctx.CurrentInstrStartPc;
                int nextInstrPc = instrStartPc + OpcodeInfo.GetInstructionSize(Opcode.Jmp);
                int targetBytePc = nextInstrPc + offset;

                instructions.Add(
                    new JitInstruction(
                        JitOp.Jmp,
                        targetBytePc: targetBytePc));
            },
            [Opcode.Jz] = () =>
            {
                int offset = BitConverter.ToInt32(ctx.Code, ctx.Pc);

                int instrStartPc = ctx.CurrentInstrStartPc;
                int nextInstrPc = instrStartPc + OpcodeInfo.GetInstructionSize(Opcode.Jz);
                int targetBytePc = nextInstrPc + offset;

                instructions.Add(
                    new JitInstruction(
                        JitOp.Jz,
                        targetBytePc: targetBytePc));
            },
            [Opcode.Jnz] = () =>
            {
                int offset = BitConverter.ToInt32(ctx.Code, ctx.Pc);

                int instrStartPc = ctx.CurrentInstrStartPc;
                int nextInstrPc = instrStartPc + OpcodeInfo.GetInstructionSize(Opcode.Jnz);
                int targetBytePc = nextInstrPc + offset;

                instructions.Add(
                    new JitInstruction(
                        JitOp.Jnz,
                        targetBytePc: targetBytePc));
            },
            [Opcode.Halt] = () =>
            {
                instructions.Add(new JitInstruction(JitOp.Halt));
            },
            [Opcode.Call] = () =>
            {
                ushort funcIndex = BitConverter.ToUInt16(ctx.Code, ctx.Pc);

                int instrSize = OpcodeInfo.GetInstructionSize(Opcode.Call);
                int returnBytePc = ctx.CurrentInstrStartPc + instrSize;

                instructions.Add(new JitInstruction(JitOp.Call, operand: funcIndex, targetBytePc: returnBytePc));
            },
            [Opcode.PrintInt] = () =>
                instructions.Add(new JitInstruction(JitOp.PrintInt)),

            [Opcode.ReadInt] = () =>
                instructions.Add(new JitInstruction(JitOp.ReadInt)),
            [Opcode.ArrayNew] = () =>
            {
                instructions.Add(new JitInstruction(JitOp.ArrayNew));
            },

            [Opcode.ArrayLoad] = () =>
            {
                instructions.Add(new JitInstruction(JitOp.ArrayLoad));
            },

            [Opcode.ArrayStore] = () =>
            {
                instructions.Add(new JitInstruction(JitOp.ArrayStore));
            },

            [Opcode.ArrayLen] = () =>
            {
                instructions.Add(new JitInstruction(JitOp.ArrayLen));
            },
        };
    }

    private void InlineLeafCalls(List<JitInstruction> instructions)
    {
        if (_resolver == null)
            return;

        for (int i = 0; i < instructions.Count; i++)
        {
            JitInstruction inst = instructions[i];
            if (inst.Op != JitOp.Call) continue;

            ushort calleeIndex = (ushort)inst.Operand;
            FunctionInfo callee = _resolver.GetFunction(calleeIndex);

            if (!TryGetInlineBody(callee, out List<JitInstruction> body))
                continue;

            instructions.RemoveAt(i);
            instructions.InsertRange(i, body);

            i += body.Count - 1;
        }
    }

    private bool TryGetInlineBody(FunctionInfo callee, out List<JitInstruction> body)
    {
        body = new List<JitInstruction>();

        if (callee.LocalVariableCount != 0)
            return false;

        if (!TryParseBytecode(callee, out List<JitInstruction> calleeIr))
            return false;

        if (calleeIr.Count == 0 || calleeIr[^1].Op != JitOp.Ret)
            return false;

        foreach (JitInstruction ins in calleeIr)
        {
            if (ins.Op is JitOp.Jmp or JitOp.Jz or JitOp.Jnz) return false;
            if (ins.Op is JitOp.Call) return false;
            if (ins.Op is JitOp.LoadLocal or JitOp.StoreLocal) return false;
        }

        const int maxInline = 16;
        if (calleeIr.Count > maxInline)
            return false;

        calleeIr.RemoveAt(calleeIr.Count - 1);

        body = calleeIr;
        return true;
    }

    private struct FastStack
    {
        private readonly OperandStack _stack;

        private int _cached;
        private long _t0; // top
        private long _t1; // second

        public FastStack(OperandStack stack)
        {
            _stack = stack;
            _cached = 0;
            _t0 = 0;
            _t1 = 0;
        }

        public void Flush()
        {
            if (_cached == 2)
            {
                _stack.Push(_t1);
                _stack.Push(_t0);
            }
            else if (_cached == 1)
            {
                _stack.Push(_t0);
            }

            _cached = 0;
        }

        public void Push(long v)
        {
            if (_cached == 0)
            {
                _t0 = v;
                _cached = 1;
                return;
            }

            if (_cached == 1)
            {
                _t1 = _t0;
                _t0 = v;
                _cached = 2;
                return;
            }

            _stack.Push(_t1);
            _t1 = _t0;
            _t0 = v;
        }

        public long Pop()
        {
            if (_cached == 2)
            {
                long v = _t0;
                _t0 = _t1;
                _cached = 1;
                return v;
            }

            if (_cached == 1)
            {
                long v = _t0;
                _cached = 0;
                return v;
            }

            return _stack.Pop();
        }

        public void Dup()
        {
            long v = Pop();
            Push(v);
            Push(v);
        }
    }

    private static readonly Dictionary<JitOp, Func<long, long, long>> BinaryOps =
        new()
        {
            [JitOp.Add] = (a, b) => a + b,
            [JitOp.Sub] = (a, b) => a - b,
            [JitOp.Mul] = (a, b) => a * b,
            [JitOp.Div] = (a, b) => a / b,
            [JitOp.Mod] = (a, b) => a % b,

            [JitOp.CmpEq] = (a, b) => a == b ? 1 : 0,
            [JitOp.CmpNe] = (a, b) => a != b ? 1 : 0,
            [JitOp.CmpLt] = (a, b) => a < b ? 1 : 0,
            [JitOp.CmpLe] = (a, b) => a <= b ? 1 : 0,
            [JitOp.CmpGt] = (a, b) => a > b ? 1 : 0,
            [JitOp.CmpGe] = (a, b) => a >= b ? 1 : 0,
        };

    private static void ExecuteInstruction(ref FastStack fs, ExecutionContext ctx, JitInstruction inst)
    {
        if (TryExecuteSimple(ref fs, inst))
            return;

        if (TryExecuteMemory(ref fs, ctx, inst))
            return;

        if (TryExecuteArray(ref fs, ctx, inst))
            return;

        if (TryExecuteIo(ref fs, ctx, inst))
            return;

        if (TryExecuteBinary(ref fs, inst))
            return;

        throw new InvalidOperationException($"Unsupported JIT op: {inst.Op}");
    }

    private static bool TryExecuteSimple(ref FastStack fs, JitInstruction inst)
    {
        switch (inst.Op)
        {
            case JitOp.Nop:
                return true;

            case JitOp.PushConst:
                fs.Push(inst.Operand);
                return true;

            case JitOp.Pop:
                fs.Pop();
                return true;

            case JitOp.Dup:
                fs.Dup();
                return true;

            case JitOp.Neg:
                fs.Push(-fs.Pop());
                return true;

            default:
                return false;
        }
    }

    private static bool TryExecuteMemory(ref FastStack fs, ExecutionContext ctx, JitInstruction inst)
    {
        switch (inst.Op)
        {
            case JitOp.LoadLocal:
            {
                StackFrame frame = ctx.Memory.CallStack.CurrentFrame;
                fs.Push(frame.GetLocal((byte)inst.Operand));
                return true;
            }

            case JitOp.StoreLocal:
            {
                StackFrame frame = ctx.Memory.CallStack.CurrentFrame;
                frame.SetLocal((byte)inst.Operand, fs.Pop());
                return true;
            }

            case JitOp.LoadGlobal:
            {
                fs.Flush();
                fs.Push(ctx.Memory.GetGlobal((ushort)inst.Operand));
                return true;
            }

            case JitOp.StoreGlobal:
            {
                fs.Flush();
                ctx.Memory.SetGlobal((ushort)inst.Operand, fs.Pop());
                return true;
            }

            default:
                return false;
        }
    }

    private static bool TryExecuteArray(ref FastStack fs, ExecutionContext ctx, JitInstruction inst)
    {
        MemoryManager memory = ctx.Memory;

        switch (inst.Op)
        {
            case JitOp.ArrayNew:
            {
                long size = fs.Pop();
                fs.Flush();

                if (size < 0)
                    throw new InvalidOperationException("ArrayNew: negative size");

                long taggedHandle = memory.AllocateArray((int)size);
                fs.Push(taggedHandle);
                return true;
            }

            case JitOp.ArrayLoad:
            {
                long index = fs.Pop();
                long taggedHandle = fs.Pop();
                fs.Flush();

                if (!Value.IsArray(taggedHandle))
                    throw new InvalidOperationException("ArrayLoad: value is not an array handle");

                int handle = Value.GetHandle(taggedHandle);
                long value = memory.GC.GetElement(handle, (int)index);
                fs.Push(value);
                return true;
            }

            case JitOp.ArrayStore:
            {
                long value = fs.Pop();
                long index = fs.Pop();
                long taggedHandle = fs.Pop();
                fs.Flush();

                if (!Value.IsArray(taggedHandle))
                    throw new InvalidOperationException("ArrayStore: value is not an array handle");

                int handle = Value.GetHandle(taggedHandle);
                memory.GC.SetElement(handle, (int)index, value);
                return true;
            }

            case JitOp.ArrayLen:
            {
                long taggedHandle = fs.Pop();
                fs.Flush();

                if (!Value.IsArray(taggedHandle))
                    throw new InvalidOperationException("ArrayLen: value is not an array handle");

                int handle = Value.GetHandle(taggedHandle);
                int len = memory.GC.GetArrayLength(handle);
                fs.Push(len);
                return true;
            }

            default:
                return false;
        }
    }

    private static bool TryExecuteIo(ref FastStack fs, ExecutionContext ctx, JitInstruction inst)
    {
        switch (inst.Op)
        {
            case JitOp.PrintInt:
            {
                fs.Flush();
                long value = fs.Pop();
                Console.WriteLine(value);
                return true;
            }

            case JitOp.ReadInt:
            {
                fs.Flush();
                string? line = Console.ReadLine();
                if (line == null)
                    throw new InvalidOperationException("ReadInt: input stream closed");

                long value = long.Parse(line);
                fs.Push(value);
                return true;
            }

            default:
                return false;
        }
    }

    private static bool TryExecuteBinary(ref FastStack fs, JitInstruction inst)
    {
        if (!BinaryOps.TryGetValue(inst.Op, out Func<long, long, long>? op))
            return false;

        long b = fs.Pop();
        long a = fs.Pop();
        fs.Push(op(a, b));
        return true;
    }

    private static JitEntryPoint BuildEntryPoint(List<JitInstruction> code, bool hasBackwardJump)
    {
        return ctx =>
        {
            int pc = 0;
            var fs = new FastStack(ctx.Memory.OperandStack);
#if DEBUG
            int steps = 0;
            int maxSteps = hasBackwardJump ? 50_000_000 : 10_000_000;
#endif
            while (true)
            {
                ctx.ProgramCounter = pc;
#if DEBUG
                if (++steps > maxSteps)
                    throw new InvalidOperationException("JIT infinite loop detected");
#endif

                JitInstruction instr = code[pc];

                switch (instr.Op)
                {
                    case JitOp.Jmp:
                        pc = instr.TargetInstr;
                        continue;

                    case JitOp.Jz:
                    {
                        long cond = fs.Pop();
                        if (cond == 0)
                        {
                            pc = instr.TargetInstr;
                            continue;
                        }

                        pc++;
                        break;
                    }

                    case JitOp.Jnz:
                    {
                        long cond = fs.Pop();
                        if (cond != 0)
                        {
                            pc = instr.TargetInstr;
                            continue;
                        }

                        pc++;
                        break;
                    }

                    case JitOp.Ret:
                    {
                        long returnValue = fs.Pop();
                        fs.Flush();

                        CallStack callStack = ctx.Memory.CallStack;
                        OperandStack stack = ctx.Memory.OperandStack;

                        StackFrame finishedFrame = callStack.PopFrame();

                        if (callStack.IsEmpty || finishedFrame.ReturnAddress == -1)
                        {
                            ctx.Result = returnValue;
                            ctx.Halted = true;
                            stack.Push(returnValue);
                            return returnValue;
                        }

                        StackFrame callerFrame = callStack.CurrentFrame;

                        ctx.SwitchToFunction(callerFrame.FunctionIndex);
                        ctx.ProgramCounter = finishedFrame.ReturnAddress;

                        stack.Push(returnValue);

                        ctx.Result = returnValue;
                        return returnValue;
                    }

                    case JitOp.Halt:
                    {
                        long value = fs.Pop();
                        fs.Flush();

                        ctx.Result = value;
                        ctx.Halted = true;

                        ctx.Memory.OperandStack.Push(value);
                        return value;
                    }

                    case JitOp.Call:
                    {
                        fs.Flush();

                        ushort calleeIndex = (ushort)instr.Operand;
                        FunctionInfo callee = ctx.GetFunction(calleeIndex);

                        callee.CallCount++;
                        ctx.Jit.EnsureCompiled(callee, ctx);

                        int returnBytePc = instr.TargetBytePc
                                           ?? throw new InvalidOperationException("CALL has no return byte PC");

                        OperandStack opStack = ctx.Memory.OperandStack;
                        int arity = callee.Arity;
                        long[] args = arity == 0 ? Array.Empty<long>() : new long[arity];

                        for (int i = arity - 1; i >= 0; i--)
                            args[i] = opStack.Pop();

                        ctx.Memory.CallStack.PushFrame(
                            calleeIndex,
                            returnAddress: returnBytePc,
                            localVariableCount: callee.LocalVariableCount);

                        StackFrame calleeFrame = ctx.Memory.CallStack.CurrentFrame;
                        for (int i = 0; i < arity; i++)
                            calleeFrame.SetLocal((byte)i, args[i]);

                        ctx.SwitchToFunction(calleeIndex);
                        ctx.ProgramCounter = 0;

                        ctx.Jit.TryExecute(callee, ctx);

                        long ret = ctx.Result;
                        fs.Push(ret);

                        return ret;
                    }

                    default:
                        ExecuteInstruction(ref fs, ctx, instr);
                        pc++;
                        break;
                }
            }
        };
    }
}
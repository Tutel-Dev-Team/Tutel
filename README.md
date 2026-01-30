# Tutel

A programming language featuring a bytecode VM, automatic memory management, and a JIT compiler. 


## Tutel Virtual Machine Implementation

### Summary

Complete bytecode Virtual Machine for the Tutel language. Interprets `.tbc` files and executes 28 opcodes covering stack operations, arithmetic, comparisons, control flow, variables, and arrays.

### Features

- **Bytecode Loading**: Parse `.tbc` files with magic number, version, function metadata
- **Stack-based Execution**: Operand stack (65536 max) and call stack (1024 frames max)
- **28 Opcodes**: `PUSH_INT`, arithmetic, comparisons, jumps, calls, variables, arrays
- **Memory Management**: Heap for arrays (prepared for GC integration)
- **CLI**: Run bytecode via command line

### Files Added (19 source files)

#### Core (`src/VirtualMachine/Core/`)
| File | Purpose |
|------|---------|
| `Opcode.cs` | 28 opcodes enum |
| `BytecodeFormat.cs` | Magic number, version constants |
| `VmLimits.cs` | Stack/frame limits |
| `OpcodeInfo.cs` | Instruction sizes |
| `FunctionInfo.cs` | Function metadata |
| `BytecodeModule.cs` | Loaded module |
| `BytecodeLoader.cs` | Parse .tbc files |
| `ExecutionEngine.cs` | Fetch-decode-execute loop |
| `TutelVm.cs` | Public API |

#### Memory (`src/VirtualMachine/Memory/`)
| File | Purpose |
|------|---------|
| `Value.cs` | Long wrapper for values |
| `OperandStack.cs` | LIFO stack |
| `StackFrame.cs` | Call frame |
| `CallStack.cs` | Frame stack |
| `Heap.cs` | Array storage |
| `MemoryManager.cs` | Unified memory access |

#### Instructions (`src/VirtualMachine/Instructions/`)
| File | Purpose |
|------|---------|
| `ExecutionContext.cs` | Execution state |
| `DecodedInstruction.cs` | Decoded opcode |
| `InstructionDecoder.cs` | Bytecode decoder |

#### Handlers (`src/VirtualMachine/Instructions/Handlers/`)
| File | Opcodes |
|------|---------|
| `StackInstructions.cs` | PUSH_INT, POP, DUP |
| `ArithmeticOps.cs` | ADD, SUB, MUL, DIV, MOD, NEG |
| `ComparisonOps.cs` | CMP_EQ/NE/LT/LE/GT/GE |
| `ControlFlow.cs` | JMP, JZ, JNZ, CALL, RET |
| `VariableOps.cs` | LOAD/STORE LOCAL/GLOBAL |
| `ArrayOps.cs` | ARRAY_NEW/LOAD/STORE/LEN |
| `MiscOps.cs` | NOP, HALT |

#### CLI (`src/VirtualMachine/CLI/`)
| File | Purpose |
|------|---------|
| `VmLauncher.cs` | CLI entry point |

### Usage

```csharp
// One-liner
long result = TutelVm.RunFile("program.tbc");

// Step by step
var vm = new TutelVm();
vm.Load("program.tbc");
long result = vm.Run();
```

```bash
dotnet run --project src/CLI -- program.tbc
```

### GC Integration Points

| Component | What it provides |
|-----------|------------------|
| `Heap.cs` | Array storage — needs `FreeArray()` or tracing |
| `MemoryManager.cs` | Roots: stack, frames, globals |
| `Value.cs` | Raw long — may need type tagging |

## VM Memory Structure & GC Integration

### Memory Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     MemoryManager                            │
├─────────────────┬─────────────────┬─────────────┬───────────┤
│  OperandStack   │    CallStack    │    Heap     │  Globals  │
│  (long[])       │  (StackFrame[]) │  (long[][]) │  (long[]) │
├─────────────────┼─────────────────┼─────────────┼───────────┤
│  Max: 65536     │  Max: 1024      │  Unbounded  │  Max:65536│
│  values         │  frames         │  arrays     │  vars     │
└─────────────────┴─────────────────┴─────────────┴───────────┘
                          │                 ▲
                          │                 │
                    Contains handles ───────┘
                    (int indices to Heap)
```

### Data Structures

#### OperandStack
```csharp
// Location: src/VirtualMachine/Memory/OperandStack.cs
Stack<long> _stack;  // Values OR array handles
```

#### CallStack  
```csharp
// Location: src/VirtualMachine/Memory/CallStack.cs
Stack<StackFrame> _frames;

// StackFrame contains:
long[] _localVariables;  // Values OR array handles
int ReturnAddress;
ushort FunctionIndex;
```

#### Heap
```csharp
// Location: src/VirtualMachine/Memory/Heap.cs
List<long[]> _arrays;  // Dynamic arrays
// Handle = index in this list
```

#### Globals
```csharp
// Location: src/VirtualMachine/Memory/MemoryManager.cs
long[] _globalVariables;  // Values OR array handles
```

---

### GC Roots

All live objects are reachable from these roots:

| Root | Location | Access |
|------|----------|--------|
| Operand Stack | `MemoryManager.OperandStack` | Iterate all values |
| Local Variables | `CallStack.CurrentFrame.GetLocal(i)` | For each frame, each local |
| Global Variables | `MemoryManager.GetGlobal(i)` | Iterate all globals |

---

### Memory Layout Diagram

```
┌────────────────────── VM Execution ──────────────────────┐
│                                                          │
│   OperandStack              CallStack                    │
│   ┌─────────┐              ┌─────────────┐              │
│   │ 42      │              │ Frame 2     │              │
│   │ [H:3]───┼──────────┐   │ locals[0]=7 │              │
│   │ 100     │          │   │ locals[1]=[H:5]──────┐     │
│   │ [H:1]───┼────────┐ │   ├─────────────┤       │     │
│   └─────────┘        │ │   │ Frame 1     │       │     │
│                      │ │   │ locals[0]=0 │       │     │
│                      │ │   └─────────────┘       │     │
│                      │ │                         │     │
│   Globals            │ │         Heap            │     │
│   ┌─────────┐        │ │   ┌─────────────┐       │     │
│   │ [H:0]───┼──────┐ │ │   │ 0: [1,2,3]  │◄──────┼─────┘
│   │ 999     │      │ │ │   │ 1: [4,5]    │◄──┐   │
│   └─────────┘      │ │ │   │ 2: null     │   │   │
│                    │ │ │   │ 3: [6,7,8,9]│◄──┼───┘
│                    │ │ └──►│ 4: null     │   │
│                    │ │     │ 5: [0,0]    │◄──┘
│                    └─┼────►│ ...         │
│                      │     └─────────────┘
│                      │           ▲
│                      └───────────┘
└──────────────────────────────────────────────────────────┘

[H:N] = Array Handle pointing to Heap index N
```


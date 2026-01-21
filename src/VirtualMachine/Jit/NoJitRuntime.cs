using Tutel.VirtualMachine.Core;
using ExecutionContext = Tutel.VirtualMachine.Instructions.ExecutionContext;

namespace Tutel.VirtualMachine.Jit;

public sealed class NoJitRuntime : IJitRuntime
{
    public int HotThreshold => int.MaxValue;

    public void EnsureCompiled(FunctionInfo functionInfo, ExecutionContext context) { }

    public bool TryExecute(FunctionInfo functionInfo, ExecutionContext context)
    {
        return false;
    }
}
using Tutel.VirtualMachine.Core;
using ExecutionContext = Tutel.VirtualMachine.Instructions.ExecutionContext;

namespace Tutel.VirtualMachine.Jit;

public interface IJitRuntime
{
    int HotThreshold { get; }

    /// <summary>
    /// Ensure function is JIT-compiled if hot.
    /// </summary>
    void EnsureCompiled(FunctionInfo functionInfo, ExecutionContext context);

    /// <summary>
    /// Try to execute compiled function.
    /// Returns true if executed natively.
    /// </summary>
    bool TryExecute(FunctionInfo functionInfo, ExecutionContext context);
}
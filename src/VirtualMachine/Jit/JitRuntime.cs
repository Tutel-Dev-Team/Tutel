using Tutel.VirtualMachine.Core;
using ExecutionContext = Tutel.VirtualMachine.Instructions.ExecutionContext;

namespace Tutel.VirtualMachine.Jit;

public sealed class JitRuntime : IJitRuntime, JitCompiler.IFunctionResolver
{
    public int HotThreshold { get; } = 10;

    private readonly JitCompiler _compiler;

    private BytecodeModule _module;

    public void SetModule(BytecodeModule module)
    {
        _module = module;
    }

    public JitRuntime(BytecodeModule module)
    {
        _module = module;
        _compiler = new JitCompiler(this);
    }

    public FunctionInfo GetFunction(ushort index)
    {
        return _module.GetFunction(index);
    }

    public void EnsureCompiled(FunctionInfo functionInfo, ExecutionContext context)
    {
#if DEBUG
        Console.WriteLine($"[JIT] EnsureCompiled fn={functionInfo.Index}, nativePtr={functionInfo.NativePtr}");
#endif

        if (functionInfo.NativePtr != IntPtr.Zero)
            return;

        if (functionInfo.JitFailed)
        {
#if DEBUG
            Console.WriteLine($"[JIT] JIT disabled for function {functionInfo.Index}");
#endif
            return;
        }

        if (_compiler.TryCompile(functionInfo, out JitCompiler.JitEntryPoint? entry))
        {
            functionInfo.NativeDelegate = entry;
            functionInfo.NativePtr = 1;
            functionInfo.JitCompileCount++;
        }
        else
        {
            functionInfo.JitFailed = true;
        }
    }

    public bool TryExecute(FunctionInfo functionInfo, ExecutionContext context)
    {
        if (functionInfo.NativeDelegate is not JitCompiler.JitEntryPoint entry)
            return false;
#if DEBUG
        Console.WriteLine($"[JIT] Executing function {functionInfo.Index}");
#endif
        var sw = System.Diagnostics.Stopwatch.StartNew();
        entry(context);
        sw.Stop();

        functionInfo.JitExecutionCount++;
        functionInfo.JitTotalTicks += sw.ElapsedTicks;
        return true;
    }
}
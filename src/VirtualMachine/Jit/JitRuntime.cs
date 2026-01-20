using Tutel.VirtualMachine.Core;
using ExecutionContext = Tutel.VirtualMachine.Instructions.ExecutionContext;

namespace Tutel.VirtualMachine.Jit;

public sealed class JitRuntime : IJitRuntime, JitCompiler.IFunctionResolver
{
    public int HotThreshold { get; } = 10;

    private readonly JitCompiler _compiler;

    private readonly BytecodeModule _module;

    public JitRuntime(BytecodeModule module)
    {
        _compiler = new JitCompiler(this);
        _module = module;
    }

    public FunctionInfo GetFunction(ushort index)
    {
        return _module.GetFunction(index);
    }

    public void EnsureCompiled(FunctionInfo functionInfo, ExecutionContext context)
    {
        if (_compiler.Debug)
        {
            Console.WriteLine($"[JIT] EnsureCompiled fn={functionInfo.Index}, nativePtr={functionInfo.NativePtr}");
        }

        if (functionInfo.NativePtr != IntPtr.Zero)
            return;

        if (functionInfo.JitFailed)
        {
            if (_compiler.Debug)
            {
                Console.WriteLine($"[JIT] JIT disabled for function {functionInfo.Index}");
            }

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
        {
            return false;
        }

        if (_compiler.Debug)
        {
            Console.WriteLine($"[JIT] Executing function {functionInfo.Index}");
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        entry(context);
        sw.Stop();

        functionInfo.JitExecutionCount++;
        functionInfo.JitTotalTicks += sw.ElapsedTicks;
        return true;
    }
}
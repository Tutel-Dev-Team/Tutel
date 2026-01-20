using System.Collections.ObjectModel;

namespace Tutel.Core.Compiler.Bytecode.Models;

public class FunctionCode
{
    public FunctionCode(Collection<byte> code)
    {
        Code = new Collection<byte>(code.ToArray());
    }

    public byte Arity { get; set; }

    public byte LocalsCount { get; set; }

    public Collection<byte> Code { get;  }
}
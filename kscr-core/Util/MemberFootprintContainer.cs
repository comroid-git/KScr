using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KScr.Core.Bytecode;
using KScr.Core.Model;

namespace KScr.Core.Util;

public sealed class MemberFootprintContainer : IEnumerable<IClassMember>
{
    private readonly Dictionary<string, IClassMember> _members = new();

    public IClassMember this[IClassInstance instance, params ITypeInfo[] args] 
        => _members[string.Join(",", args.Select(x => x.CanonicalName))];

    public IEnumerator<IClassMember> GetEnumerator() => _members.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

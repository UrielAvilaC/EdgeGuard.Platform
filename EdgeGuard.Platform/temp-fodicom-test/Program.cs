using System;
using System.Linq;
using System.Reflection;
using FellowOakDicom.Network;
var t = typeof(IDicomCFindProvider);
foreach (var m in t.GetMethods())
{
    Console.WriteLine(m.ReturnType + " " + m.Name);
    foreach(var p in m.GetParameters())
        Console.WriteLine("  param: " + p.ParameterType + " " + p.Name);
}

using AspNetCoreDebugBackdoor.Lib.Models;

namespace AspNetCoreDebugBackdoor.Lib.Interfaces;

public interface IReflectionService
{
    IEnumerable<AssemblyModel> GetAssemblies();
    IEnumerable<TypeModel> GetTypes(string assemblyName);
    IEnumerable<MemberModel> GetMembers(string assemblyName, string typeName);
    object? GetMemberValue(string assemblyName, string typeName, string memberName);
    void SetMemberValue(string assemblyName, string typeName, string memberName, string value, string? path = null);
    IEnumerable<MemberModel> GetCollectionItems(string assemblyName, string typeName, string memberName);
    IEnumerable<MemberModel> InspectObject(string assemblyName, string typeName, string memberName, string? path);
    object? InvokeMethod(string assemblyName, string typeName, string memberName, string? path = null, string[]? parameters = null);
}

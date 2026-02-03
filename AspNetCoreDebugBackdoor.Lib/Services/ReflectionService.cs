using System.Reflection;
using System.Linq;
using AspNetCoreDebugBackdoor.Lib.Interfaces;
using AspNetCoreDebugBackdoor.Lib.Models;

namespace AspNetCoreDebugBackdoor.Lib.Services;

public class ReflectionService : IReflectionService
{
    public IEnumerable<AssemblyModel> GetAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic) // Filter dynamic assemblies if needed, or keep them
            .Select(a => new AssemblyModel(a))
            .OrderBy(a => a.Name);
    }

    public IEnumerable<TypeModel> GetTypes(string assemblyName)
    {
        var assembly = GetAssemblyByName(assemblyName);
        if (assembly == null) return Enumerable.Empty<TypeModel>();

        try
        {
            return assembly.GetTypes()
                .Where(t => !IsCompilerGenerated(t))
                .Select(t => new TypeModel(t))
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name);
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Return successfully loaded types
            return ex.Types.Where(t => t != null).Select(t => new TypeModel(t!));
        }
    }

    public IEnumerable<MemberModel> GetMembers(string assemblyName, string typeName)
    {
        var type = GetTypeByName(assemblyName, typeName);
        if (type == null) return Enumerable.Empty<MemberModel>();

        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        return type.GetMembers(flags)
            .Where(m => !IsCompilerGenerated(m))
            .Select(m => ToMemberModel(m, null))
            .OrderBy(m => m.MemberType).ThenBy(m => m.Name);
    }

    public object? GetMemberValue(string assemblyName, string typeName, string memberName)
    {
        var type = GetTypeByName(assemblyName, typeName);
        var member = type?.GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).FirstOrDefault();
        if (member == null) return null;

        try
        {
            object? val = member switch
            {
                FieldInfo f when f.IsStatic => f.GetValue(null),
                PropertyInfo p when p.GetGetMethod(true)?.IsStatic == true => p.GetValue(null),
                _ => null
            };
            return SafeGetValue(val, out _);
        }
        catch { return null; }
    }

    public void SetMemberValue(string assemblyName, string typeName, string memberName, string value, string? path = null)
    {
        var (targetInstance, finalMember) = ResolveTarget(assemblyName, typeName, memberName, path);
        if (finalMember == null) throw new Exception("Member not found");

        Type targetType = finalMember switch
        {
            FieldInfo f => f.FieldType,
            PropertyInfo p => p.PropertyType,
            _ => throw new Exception("Not a field or property")
        };

        var parsedValue = ParseValue(value, targetType);
        if (finalMember is FieldInfo field) field.SetValue(targetInstance, parsedValue);
        else if (finalMember is PropertyInfo prop) prop.SetValue(targetInstance, parsedValue);
    }

    public object? InvokeMethod(string assemblyName, string typeName, string memberName, string? path = null, string[]? parameters = null)
    {
        var (targetInstance, finalMember) = ResolveTarget(assemblyName, typeName, memberName, path);
        if (finalMember is not MethodInfo method) throw new Exception("Member is not a method");

        var methodParams = method.GetParameters();
        var parsedParams = new object?[methodParams.Length];

        if (parameters != null && parameters.Length > 0)
        {
            for (int i = 0; i < Math.Min(parameters.Length, methodParams.Length); i++)
            {
                parsedParams[i] = ParseValue(parameters[i], methodParams[i].ParameterType);
            }
        }

        // Fill remaining optional parameters with Type.Missing or defaults
        for (int i = 0; i < methodParams.Length; i++)
        {
            if (i >= (parameters?.Length ?? 0))
            {
                if (methodParams[i].HasDefaultValue) parsedParams[i] = methodParams[i].DefaultValue;
                else if (methodParams[i].ParameterType.IsValueType) parsedParams[i] = Activator.CreateInstance(methodParams[i].ParameterType);
                else parsedParams[i] = null;
            }
        }

        var result = method.Invoke(targetInstance, parsedParams);
        return SafeGetValue(result, out _);
    }

    private bool IsCompilerGenerated(MemberInfo m) 
    {
        if (m.Name.StartsWith("<")) return true;
        if (m is MethodBase mb && mb.IsSpecialName) return true;
        if (m is Type t && t.Name.Contains("<")) return true;
        return m.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true).Any();
    }

    private (object? Instance, MemberInfo? Member) ResolveTarget(string assemblyName, string typeName, string memberName, string? path)
    {
        var type = GetTypeByName(assemblyName, typeName);
        var member = type?.GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).FirstOrDefault();
        if (type == null || member == null) return (null, null);

        if (string.IsNullOrEmpty(path))
        {
            if (IsStatic(member)) return (null, member);
            throw new Exception("Member is not static and no path provided");
        }

        object? root = GetStaticValue(member);
        if (root == null) throw new Exception("Root member is null or not static");

        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        object? current = root;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            current = ResolvePathPart(current, parts[i]);
            if (current == null) throw new Exception($"Path part '{parts[i]}' resolved to null");
        }

        var lastPart = parts.Last();
        if (IsIndexer(lastPart))
        {
            // For indexers in Set/Invoke, we might need special handling. 
            // Currently SetMemberValue handles it separately. 
            // Let's keep it simple for now and return the collection as instance and indexer as member if possible, 
            // but Reflection doesn't have a "MemberInfo" for a specific index.
            throw new Exception("Indexers not supported in this resolution path yet");
        }

        var finalMember = current!.GetType().GetMember(lastPart, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).FirstOrDefault();
        return (current, finalMember);
    }

    public IEnumerable<MemberModel> GetCollectionItems(string assemblyName, string typeName, string memberName)
    {
        var type = GetTypeByName(assemblyName, typeName);
        var member = type?.GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).FirstOrDefault();
        
        if (member != null && GetStaticValue(member) is System.Collections.IEnumerable enumerable)
        {
            return enumerable.Cast<object>().Select((item, index) => new MemberModel
            {
                Name = $"[{index}]",
                MemberType = "Item",
                ReturnType = item?.GetType().Name ?? "Object",
                Value = SafeGetValue(item, out var vType),
                ValueType = vType,
                IsEditable = false
            }).Take(1000).ToList();
        }
        return Enumerable.Empty<MemberModel>();
    }

    public IEnumerable<MemberModel> InspectObject(string assemblyName, string typeName, string memberName, string? path)
    {
        var type = GetTypeByName(assemblyName, typeName);
        var member = type?.GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).FirstOrDefault();
        if (member == null) return Enumerable.Empty<MemberModel>();

        object? current = GetStaticValue(member);
        if (current == null) return Enumerable.Empty<MemberModel>();

        if (!string.IsNullOrEmpty(path))
        {
            var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                current = ResolvePathPart(current, part);
                if (current == null) break;
            }
        }

        if (current == null) return Enumerable.Empty<MemberModel>();

        return current.GetType().GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => !IsCompilerGenerated(m) && m is not MethodInfo && m is not ConstructorInfo)
            .Select(m => ToMemberModel(m, current))
            .OrderBy(m => m.Name);
    }

    // --- Private Helpers ---

    private MemberModel ToMemberModel(MemberInfo member, object? instance)
    {
        var model = new MemberModel
        {
            Name = member.Name,
            IsStatic = IsStatic(member),
            IsPublic = IsPublic(member)
        };

        try
        {
            switch (member)
            {
                case FieldInfo f:
                    model.MemberType = "Field";
                    model.ReturnType = f.FieldType.Name;
                    model.IsEditable = IsEditableType(f.FieldType) && (f.IsStatic || instance != null) && !f.IsInitOnly;
                    if (f.IsStatic || instance != null)
                    {
                        model.Value = SafeGetValue(f.GetValue(instance), out var vType);
                        model.ValueType = vType;
                    }
                    break;
                case PropertyInfo p:
                    model.MemberType = "Property";
                    model.ReturnType = p.PropertyType.Name;
                    bool isStatic = p.GetGetMethod(true)?.IsStatic ?? false;
                    model.IsEditable = IsEditableType(p.PropertyType) && (isStatic || instance != null) && p.CanWrite;
                    if ((isStatic || instance != null) && p.GetIndexParameters().Length == 0)
                    {
                        model.Value = SafeGetValue(p.GetValue(instance), out var vType);
                        model.ValueType = vType;
                    }
                    break;
                case MethodInfo m:
                    model.MemberType = "Method";
                    model.ReturnType = m.ReturnType.Name;
                    model.Parameters = m.GetParameters().Select(p => new ParameterModel { Name = p.Name ?? "", Type = p.ParameterType.Name }).ToList();
                    break;
                case ConstructorInfo c:
                    model.MemberType = "Constructor";
                    model.ReturnType = c.DeclaringType?.Name ?? "";
                    model.Parameters = c.GetParameters().Select(p => new ParameterModel { Name = p.Name ?? "", Type = p.ParameterType.Name }).ToList();
                    break;
                default:
                    model.MemberType = member.MemberType.ToString();
                    break;
            }
        }
        catch (Exception ex) { model.Error = ex.Message; }
        return model;
    }

    private object? GetStaticValue(MemberInfo member) => member switch
    {
        FieldInfo f when f.IsStatic => f.GetValue(null),
        PropertyInfo p when p.GetGetMethod(true)?.IsStatic == true => p.GetValue(null),
        _ => null
    };

    private object? ResolvePathPart(object? current, string part)
    {
        if (current == null) return null;
        if (IsIndexer(part))
        {
            int index = ParseIndexer(part);
            if (current is Array arr && index < arr.Length) return arr.GetValue(index);
            if (current is System.Collections.IList list && index < list.Count) return list[index];
            if (current is System.Collections.IEnumerable en) return en.Cast<object>().Skip(index).FirstOrDefault();
            return null;
        }

        var m = current.GetType().GetMember(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
        return m switch
        {
            FieldInfo f => f.GetValue(current),
            PropertyInfo p => p.GetValue(current),
            _ => null
        };
    }

    // Handled in expanded IsCompilerGenerated above
    // private bool IsCompilerGenerated(MemberInfo m) => m.Name.StartsWith("<") || m is MethodBase mb && mb.IsSpecialName;
    
    private bool IsIndexer(string part) => part.StartsWith("[") && part.EndsWith("]");
    private int ParseIndexer(string part) => int.TryParse(part.Substring(1, part.Length - 2), out int i) ? i : -1;

    private Type GetElementType(object collection)
    {
        var t = collection.GetType();
        if (t.IsArray) return t.GetElementType()!;
        return t.IsGenericType ? t.GetGenericArguments()[0] : typeof(object);
    }

    private void SetCollectionItem(object collection, int index, object? value)
    {
        if (collection is Array arr) arr.SetValue(value, index);
        else if (collection is System.Collections.IList list) list[index] = value;
        else throw new Exception("Collection not indexable");
    }

    private Assembly? GetAssemblyByName(string name) => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name || a.FullName == name);
    private Type? GetTypeByName(string assemblyName, string typeName) => GetAssemblyByName(assemblyName)?.GetTypes().FirstOrDefault(t => t.FullName == typeName || t.Name == typeName);

    private bool IsStatic(MemberInfo m) => m switch { FieldInfo f => f.IsStatic, PropertyInfo p => p.GetGetMethod(true)?.IsStatic ?? false, MethodBase mb => mb.IsStatic, _ => false };
    private bool IsPublic(MemberInfo m) => m switch { FieldInfo f => f.IsPublic, PropertyInfo p => p.GetGetMethod(true)?.IsPublic ?? false, MethodBase mb => mb.IsPublic, Type t => t.IsPublic, _ => false };

    private object? ParseValue(string value, Type targetType)
    {
        if (value == "null") return null;
        if (targetType == typeof(string)) return value;
        if (targetType == typeof(int)) return int.Parse(value);
        if (targetType == typeof(long)) return long.Parse(value);
        if (targetType == typeof(double)) return double.Parse(value);
        if (targetType == typeof(float)) return float.Parse(value);
        if (targetType == typeof(decimal)) return decimal.Parse(value);
        if (targetType == typeof(bool)) return bool.Parse(value);
        if (targetType.IsEnum) return Enum.Parse(targetType, value);
        if (targetType == typeof(DateTime)) return DateTime.Parse(value);
        if (targetType == typeof(Guid)) return Guid.Parse(value);
        throw new Exception($"Type {targetType.Name} not supported");
    }

    private bool IsEditableType(Type t) => t.IsPrimitive || t == typeof(string) || t.IsEnum || t == typeof(decimal) || t == typeof(DateTime) || t == typeof(Guid);

    private object? SafeGetValue(object? value, out string valueType)
    {
        if (value == null) { valueType = "null"; return null; }
        var type = value.GetType();
        if (type == typeof(string)) { valueType = "string"; return value; }
        if (type.IsPrimitive || type.IsEnum || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(Guid)) { valueType = "primitive"; return value; }
        
        valueType = (type.IsArray || value is System.Collections.IEnumerable) ? "collection" : "complex";
        if (valueType == "collection")
        {
            int? count = (value is System.Collections.ICollection c) ? c.Count : (value is Array a) ? a.Length : null;
            var name = type.Name;
            if (name.Contains("`")) name = name.Substring(0, name.IndexOf('`'));
            return count.HasValue ? $"{name} (Count={count})" : name;
        }
        return value.ToString();
    }
}

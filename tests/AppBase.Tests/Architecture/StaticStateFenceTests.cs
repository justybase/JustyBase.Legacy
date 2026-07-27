using System.Collections;
using System.Reflection;
using AppBase.Data;
using AppBase.Services;

namespace AppBase.Tests.Architecture;

/// <summary>
/// Freeze-list of process-wide mutable statics. New public static mutable fields in
/// App.Data.Netezza / AppBase.Services must be justified and added to the allowlist
/// (prefer injected catalogs/registries instead — see ADR-007).
/// </summary>
public sealed class StaticStateFenceTests
{
    private static readonly HashSet<string> AllowedNetezzaStaticMutables = new(StringComparer.Ordinal)
    {
    };

    private static readonly HashSet<string> AllowedServicesStaticMutables = new(StringComparer.Ordinal)
    {
        // Compiled regex / immutable pattern tables — not schema/session state.
        "AppBase.Services.ImportExportTasks.rxImportXlsxTxt",
        "AppBase.Services.Utilities.FileSearchEngine.DefaultExtensionPatterns",
    };

    [Fact]
    public void App_Data_Netezza_public_static_mutables_are_on_the_allowlist()
    {
        AssertNoUnexpectedStaticMutables(
            typeof(NetezzaHelpers).Assembly,
            AllowedNetezzaStaticMutables);
    }

    [Fact]
    public void AppBase_Services_public_static_mutables_are_on_the_allowlist()
    {
        AssertNoUnexpectedStaticMutables(
            typeof(ImportExportTasks).Assembly,
            AllowedServicesStaticMutables);
    }

    private static void AssertNoUnexpectedStaticMutables(Assembly assembly, HashSet<string> allowlist)
    {
        var offenders = new List<string>();

        foreach (Type type in assembly.GetExportedTypes())
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            {
                if (field.IsLiteral)
                    continue;

                if (!IsMutableField(field))
                    continue;

                string key = $"{type.FullName}.{field.Name}";
                if (!allowlist.Contains(key))
                    offenders.Add(key);
            }
        }

        Assert.True(
            offenders.Count == 0,
            "New public static mutable fields are not allowed without an ADR/allowlist update. Offenders: "
            + string.Join(", ", offenders));
    }

    private static bool IsMutableField(FieldInfo field)
    {
        if (!field.IsInitOnly)
            return true;

        // readonly Dictionary/List/etc. are still process-wide mutable state.
        Type type = field.FieldType;
        if (type == typeof(string) || type.IsPrimitive || type.IsEnum)
            return false;

        if (typeof(IDictionary).IsAssignableFrom(type)
            || typeof(IList).IsAssignableFrom(type)
            || typeof(ICollection).IsAssignableFrom(type))
            return true;

        if (type.IsArray)
            return true;

        if (type.IsGenericType)
        {
            Type open = type.GetGenericTypeDefinition();
            if (open == typeof(Dictionary<,>)
                || open == typeof(List<>)
                || open == typeof(HashSet<>)
                || open == typeof(Queue<>)
                || open == typeof(Stack<>))
                return true;
        }

        return false;
    }
}

using System.Reflection;
using CommunityToolkit.Mvvm.Messaging;
using JustData.Application.Login;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace JustData.ViewModels.Tests;

public sealed class ArchitectureTests
{
    private static readonly string[] ForbiddenReferences =
    [
        "System.Windows.Forms",
        "System.Drawing",
        "JustData",
        "AppBase.Common",
        "AppBase.Services",
        "App.Data.DB2",
        "App.Data.MsSqlDb",
        "App.Data.Netezza",
        "App.Data.Oracle",
        "App.Data.Postgres"
    ];

    [Theory]
    [MemberData(nameof(CleanAssemblies))]
    public void Clean_layers_do_not_reference_Windows_or_legacy_projects(Assembly assembly)
    {
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name);

        Assert.DoesNotContain(references, reference => ForbiddenReferences.Contains(reference, StringComparer.Ordinal));
    }

    public static TheoryData<Assembly> CleanAssemblies => new()
    {
        typeof(JustData.Application.IUiDispatcher).Assembly,
        typeof(JustData.ViewModels.ViewModelBase).Assembly
    };

    [Fact]
    public void ShellViewModel_resolves_without_static_state()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IApplicationSession>(Substitute.For<IApplicationSession>());
        services.AddSingleton(Substitute.For<IMessenger>());
        services.AddTransient<ShellViewModel>();
        using var provider = services.BuildServiceProvider();

        var shell = provider.GetRequiredService<ShellViewModel>();

        Assert.NotNull(shell);
        Assert.Null(shell.CurrentLogin);
    }

    [Fact]
    public void Clean_assemblies_have_no_public_mutable_collections()
    {
        var cleanAssemblies = new[]
        {
            typeof(JustData.Application.IUiDispatcher).Assembly,
            typeof(JustData.ViewModels.ViewModelBase).Assembly
        };

        foreach (Assembly assembly in cleanAssemblies)
        {
            foreach (Type type in assembly.GetExportedTypes())
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                    .Where(f => IsMutableCollection(f.FieldType));

                Assert.Empty(fields);
            }
        }
    }

    [Fact]
    public void Clean_assemblies_have_no_public_mutable_properties()
    {
        var cleanAssemblies = new[]
        {
            typeof(JustData.Application.IUiDispatcher).Assembly,
            typeof(JustData.ViewModels.ViewModelBase).Assembly
        };

        foreach (Assembly assembly in cleanAssemblies)
        {
            foreach (Type type in assembly.GetExportedTypes())
            {
                // Settings/snapshot DTOs are serialization models with public
                // setters by design; they are not domain state containers.
                if (type.Namespace?.Contains("Settings") == true
                    || type.Name.EndsWith("Snapshot")
                    || type.Name.EndsWith("Settings"))
                    continue;

                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite && IsMutableCollection(p.PropertyType));

                foreach (var property in properties)
                {
                    Assert.Fail(
                        $"Type '{type.FullName}' exposes public settable collection property '{property.Name}' of type '{property.PropertyType.Name}'. Use read-only collections with private mutation.");
                }
            }
        }
    }

    [Fact]
    public void Clean_assemblies_do_not_expose_a_service_locator_surface()
    {
        var cleanAssemblies = new[]
        {
            typeof(JustData.Application.IUiDispatcher).Assembly,
            typeof(JustData.ViewModels.ViewModelBase).Assembly
        };

        foreach (Assembly assembly in cleanAssemblies)
        {
            foreach (Type type in assembly.GetExportedTypes())
            {
                foreach (ConstructorInfo constructor in type.GetConstructors())
                {
                    Assert.DoesNotContain(
                        constructor.GetParameters(),
                        parameter => parameter.ParameterType == typeof(IServiceProvider));
                }

                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    Assert.NotEqual(typeof(IServiceProvider), method.ReturnType);
                    Assert.DoesNotContain(
                        method.GetParameters(),
                        parameter => parameter.ParameterType == typeof(IServiceProvider));
                }

                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                    Assert.NotEqual(typeof(IServiceProvider), property.PropertyType);
            }
        }
    }

    private static bool IsMutableCollection(Type type)
    {
        if (!type.IsGenericType)
            return false;

        Type openType = type.GetGenericTypeDefinition();
        return openType == typeof(System.Collections.Generic.List<>)
            || openType == typeof(System.Collections.Generic.Dictionary<,>)
            || openType == typeof(System.Collections.Generic.HashSet<>)
            || openType == typeof(System.Collections.ObjectModel.Collection<>);
    }
}

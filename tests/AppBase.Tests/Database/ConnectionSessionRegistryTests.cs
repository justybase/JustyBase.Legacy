using AppBase.Data.Core.Core;
using System.Reflection;

namespace AppBase.Tests.Database;

public sealed class ConnectionSessionRegistryTests
{
    [Fact]
    public void SetAndRemoveAreCaseInsensitiveAndExposeSnapshots()
    {
        ConnectionSessionRegistry registry = new();
        IGeneralDb first = CreateDatabaseProxy();
        IGeneralDb replacement = CreateDatabaseProxy();

        registry.Set("Reporting", first);
        registry["reporting"] = replacement;

        Assert.Single(registry);
        Assert.True(registry.ContainsKey("REPORTING"));
        Assert.True(registry.TryGetValue("REPORTING", out IGeneralDb? stored));
        Assert.Same(replacement, stored);
        Assert.Equal("Reporting", Assert.Single(registry.Keys));
        Assert.Same(replacement, Assert.Single(registry.Values));

        Assert.True(registry.Remove("REPORTING"));
        Assert.False(registry.ContainsKey("Reporting"));
        Assert.Empty(registry);
    }

    [Fact]
    public void ParallelWritesDoNotLoseSessions()
    {
        ConnectionSessionRegistry registry = new();

        Parallel.For(0, 100, index =>
        {
            registry.Set($"connection-{index}", CreateDatabaseProxy());
        });

        Assert.Equal(100, registry.Count);
        Assert.Equal(100, registry.ToArray().Length);
    }

    [Fact]
    public void ConcurrentReadsAndWritesExposeConsistentSnapshots()
    {
        ConnectionSessionRegistry registry = new();

        Parallel.For(0, 200, index =>
        {
            string name = $"connection-{index % 25}";
            registry.Set(name, CreateDatabaseProxy());
            _ = registry.ContainsKey(name);
            _ = registry.TryGetValue(name, out _);
            _ = registry.Keys.Count();
            _ = registry.Values.Count();
        });

        Assert.Equal(25, registry.Count);
        Assert.All(registry, pair => Assert.NotNull(pair.Value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void SetRejectsInvalidConnectionNames(string? connectionName)
    {
        ConnectionSessionRegistry registry = new();

        Assert.ThrowsAny<ArgumentException>(() => registry.Set(connectionName!, CreateDatabaseProxy()));
    }

    [Fact]
    public void ClearRemovesAllSessions()
    {
        ConnectionSessionRegistry registry = new();
        registry["one"] = CreateDatabaseProxy();
        registry["two"] = CreateDatabaseProxy();

        registry.Clear();

        Assert.Empty(registry);
        Assert.False(registry.TryGetValue("one", out _));
    }

    private static IGeneralDb CreateDatabaseProxy()
    {
        return DispatchProxy.Create<IGeneralDb, GeneralDbDispatchProxy>();
    }

    private class GeneralDbDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null || targetMethod.ReturnType == typeof(void))
            {
                return null;
            }

            return targetMethod.ReturnType.IsValueType
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }
}

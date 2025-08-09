using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using ReportOnFailure.Enums;
using ReportOnFailure.Interfaces.Reporters;
using ReportOnFailure.Interfaces.Resolvers;
using ReportOnFailure.Registries;
using ReportOnFailure.Reporters;
using Xunit;

namespace ReportOnFailure.Tests.Registry;

public class ResolverRegistryTests
{
    #region Resolver Instance Registration Tests

    [Fact]
    public void RegisterResolver_WithValidResolverInstance_RegistersSuccessfully()
    {
        
        var registry = new ResolverRegistry();
        var mockResolver = new Mock<TestResolverClass>();

        
        registry.RegisterResolver<ITestReporter, TestResolverClass>(mockResolver.Object);

        
        Assert.True(registry.CanResolve<ITestReporter>());
        Assert.True(registry.CanResolve(typeof(ITestReporter)));
    }

    [Fact]
    public void RegisterResolver_WithNullResolverInstance_ThrowsArgumentNullException()
    {
        
        var registry = new ResolverRegistry();

        
        Assert.Throws<ArgumentNullException>(() => 
            registry.RegisterResolver<ITestReporter, TestResolverClass>(null!));
    }

    [Fact]
    public void RegisterResolver_WithSameType_OverwritesPreviousResolver()
    {
        
        var registry = new ResolverRegistry();
        var firstResolver = new Mock<TestResolverClass>();
        var secondResolver = new Mock<TestResolverClass>();

        firstResolver.Setup(r => r.ResolveSync(It.IsAny<ITestReporter>()))
            .Returns("First resolver result");
        secondResolver.Setup(r => r.ResolveSync(It.IsAny<ITestReporter>()))
            .Returns("Second resolver result");

        var reporter = new Mock<ITestReporter>().Object;

        
        registry.RegisterResolver<ITestReporter, TestResolverClass>(firstResolver.Object);
        registry.RegisterResolver<ITestReporter, TestResolverClass>(secondResolver.Object);

        var result = registry.ResolveSync(reporter);

        
        Assert.Equal("Second resolver result", result);
    }

    #endregion

    #region Delegate Registration Tests

    [Fact]
    public void RegisterResolver_WithValidDelegates_RegistersSuccessfully()
    {
        
        var registry = new ResolverRegistry();
        var asyncResolver = new Func<ITestReporter, CancellationToken, Task<string>>((reporter, ct) => 
            Task.FromResult("Async delegate result"));
        var syncResolver = new Func<ITestReporter, string>(reporter => "Sync delegate result");

        
        registry.RegisterResolver(asyncResolver, syncResolver);

        
        Assert.True(registry.CanResolve<ITestReporter>());
        Assert.True(registry.CanResolve(typeof(ITestReporter)));
    }

    [Fact]
    public void RegisterResolver_WithNullAsyncDelegate_ThrowsArgumentNullException()
    {
        
        var registry = new ResolverRegistry();
        var syncResolver = new Func<ITestReporter, string>(reporter => "Sync result");

        
        Assert.Throws<ArgumentNullException>(() => 
            registry.RegisterResolver<ITestReporter>(null!, syncResolver));
    }

    [Fact]
    public void RegisterResolver_WithNullSyncDelegate_ThrowsArgumentNullException()
    {
        
        var registry = new ResolverRegistry();
        var asyncResolver = new Func<ITestReporter, CancellationToken, Task<string>>((reporter, ct) => 
            Task.FromResult("Async result"));

        
        Assert.Throws<ArgumentNullException>(() => 
            registry.RegisterResolver<ITestReporter>(asyncResolver, null!));
    }

    [Fact]
    public void RegisterResolver_WithDelegates_OverwritesPreviousDelegates()
    {
        
        var registry = new ResolverRegistry();
        var reporter = new Mock<ITestReporter>().Object;

        var firstAsync = new Func<ITestReporter, CancellationToken, Task<string>>((r, ct) => 
            Task.FromResult("First async"));
        var firstSync = new Func<ITestReporter, string>(r => "First sync");

        var secondAsync = new Func<ITestReporter, CancellationToken, Task<string>>((r, ct) => 
            Task.FromResult("Second async"));
        var secondSync = new Func<ITestReporter, string>(r => "Second sync");

        
        registry.RegisterResolver(firstAsync, firstSync);
        registry.RegisterResolver(secondAsync, secondSync);

        var result = registry.ResolveSync(reporter);

        
        Assert.Equal("Second sync", result);
    }

    #endregion

    #region Async Resolution Tests

    [Fact]
    public async Task ResolveAsync_WithRegisteredResolverInstance_CallsResolverCorrectly()
    {
        
        var registry = new ResolverRegistry();
        var mockResolver = new Mock<TestResolverClass>();
        var reporter = new Mock<ITestReporter>().Object;

        mockResolver.Setup(r => r.ResolveAsync(It.IsAny<ITestReporter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Async resolver result");

        registry.RegisterResolver<ITestReporter, TestResolverClass>(mockResolver.Object);

        
        var result = await registry.ResolveAsync(reporter);

        
        Assert.Equal("Async resolver result", result);
        mockResolver.Verify(r => r.ResolveAsync(reporter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WithRegisteredDelegate_CallsDelegateCorrectly()
    {
        
        var registry = new ResolverRegistry();
        var reporter = new Mock<ITestReporter>().Object;
        var delegateCalled = false;

        var asyncResolver = new Func<ITestReporter, CancellationToken, Task<string>>((r, ct) =>
        {
            delegateCalled = true;
            return Task.FromResult("Delegate async result");
        });
        var syncResolver = new Func<ITestReporter, string>(r => "Sync result");

        registry.RegisterResolver(asyncResolver, syncResolver);

        
        var result = await registry.ResolveAsync(reporter);

        
        Assert.Equal("Delegate async result", result);
        Assert.True(delegateCalled);
    }

    [Fact]
    public async Task ResolveAsync_WithNullReporter_ThrowsArgumentNullException()
    {
        
        var registry = new ResolverRegistry();

        
        await Assert.ThrowsAsync<ArgumentNullException>(() => registry.ResolveAsync<ITestReporter>(null!));
    }

    [Fact]
    public async Task ResolveAsync_WithUnregisteredReporter_ThrowsNotSupportedException()
    {
        
        var registry = new ResolverRegistry();
        var reporter = new Mock<IUnregisteredReporter>().Object;

        
        var exception = await Assert.ThrowsAsync<NotSupportedException>(() => 
            registry.ResolveAsync(reporter));
        Assert.Contains("No resolver registered for reporter type", exception.Message);
    }

    [Fact]
    public async Task ResolveAsync_WithCancellationToken_PassesToResolver()
    {
        
        var registry = new ResolverRegistry();
        var reporter = new Mock<ITestReporter>().Object;
        var mockResolver = new Mock<TestResolverClass>();
        var cts = new CancellationTokenSource();

        mockResolver.Setup(r => r.ResolveAsync(It.IsAny<ITestReporter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Result");

        registry.RegisterResolver<ITestReporter, TestResolverClass>(mockResolver.Object);

        
        await registry.ResolveAsync(reporter, cts.Token);

        
        mockResolver.Verify(r => r.ResolveAsync(reporter, cts.Token), Times.Once);
    }

    #endregion

    #region Sync Resolution Tests

    [Fact]
    public void ResolveSync_WithRegisteredResolverInstance_CallsResolverCorrectly()
    {
        
        var registry = new ResolverRegistry();
        var mockResolver = new Mock<TestResolverClass>();
        var reporter = new Mock<ITestReporter>().Object;

        mockResolver.Setup(r => r.ResolveSync(It.IsAny<ITestReporter>()))
            .Returns("Sync resolver result");

        registry.RegisterResolver<ITestReporter, TestResolverClass>(mockResolver.Object);

        
        var result = registry.ResolveSync(reporter);

        
        Assert.Equal("Sync resolver result", result);
        mockResolver.Verify(r => r.ResolveSync(reporter), Times.Once);
    }

    [Fact]
    public void ResolveSync_WithRegisteredDelegate_CallsDelegateCorrectly()
    {
        
        var registry = new ResolverRegistry();
        var reporter = new Mock<ITestReporter>().Object;
        var delegateCalled = false;

        var asyncResolver = new Func<ITestReporter, CancellationToken, Task<string>>((r, ct) => 
            Task.FromResult("Async result"));
        var syncResolver = new Func<ITestReporter, string>(r =>
        {
            delegateCalled = true;
            return "Delegate sync result";
        });

        registry.RegisterResolver(asyncResolver, syncResolver);

        
        var result = registry.ResolveSync(reporter);

        
        Assert.Equal("Delegate sync result", result);
        Assert.True(delegateCalled);
    }

    [Fact]
    public void ResolveSync_WithNullReporter_ThrowsArgumentNullException()
    {
        
        var registry = new ResolverRegistry();

        
        Assert.Throws<ArgumentNullException>(() => registry.ResolveSync<ITestReporter>(null!));
    }

    [Fact]
    public void ResolveSync_WithUnregisteredReporter_ThrowsNotSupportedException()
    {
        
        var registry = new ResolverRegistry();
        var reporter = new Mock<IUnregisteredReporter>().Object;

        
        var exception = Assert.Throws<NotSupportedException>(() => registry.ResolveSync(reporter));
        Assert.Contains("No resolver registered for reporter type", exception.Message);
    }

    #endregion

    #region Type Hierarchy Resolution Tests

    [Fact]
    public async Task ResolveAsync_WithConcreteTypeRegistration_ResolvesForConcreteType()
    {
        
        var registry = new ResolverRegistry();
        var mockResolver = new Mock<ConcreteTestResolverClass>();
        var reporter = new ConcreteTestReporter();

        mockResolver.Setup(r => r.ResolveAsync(It.IsAny<ConcreteTestReporter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Concrete type result");

        registry.RegisterResolver<ConcreteTestReporter, ConcreteTestResolverClass>(mockResolver.Object);

        
        var result = await registry.ResolveAsync(reporter);

        
        Assert.Equal("Concrete type result", result);
    }

    [Fact]
    public async Task ResolveAsync_WithInterfaceRegistration_ResolvesForImplementingType()
    {
        
        var registry = new ResolverRegistry();
        var mockResolver = new Mock<TestResolverClass>();
        var reporter = new ConcreteTestReporter(); 

        mockResolver.Setup(r => r.ResolveAsync(It.IsAny<ITestReporter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Interface resolution result");

        registry.RegisterResolver<ITestReporter, TestResolverClass>(mockResolver.Object);

        
        var result = await registry.ResolveAsync<ITestReporter>(reporter);

        
        Assert.Equal("Interface resolution result", result);
    }

    [Fact]
    public void ResolveSync_WithInheritanceHierarchy_ResolvesToCorrectType()
    {
        
        var registry = new ResolverRegistry();
        var reporter = new ConcreteTestReporter(); 
        
        var interfaceResolver = new Func<ITestReporter, string>(r => "Interface resolver");
        var concreteResolver = new Func<ConcreteTestReporter, string>(r => "Concrete resolver");

        
        registry.RegisterResolver<ITestReporter>(
            async (r, ct) => interfaceResolver(r), 
            interfaceResolver);
        registry.RegisterResolver<ConcreteTestReporter>(
            async (r, ct) => concreteResolver(r), 
            concreteResolver);

        
        var result = registry.ResolveSync(reporter);

        
        Assert.Equal("Concrete resolver", result);
    }

    #endregion

    #region CanResolve Tests

    [Fact]
    public void CanResolve_WithRegisteredType_ReturnsTrue()
    {
        
        var registry = new ResolverRegistry();
        var mockResolver = new Mock<TestResolverClass>();

        registry.RegisterResolver<ITestReporter, TestResolverClass>(mockResolver.Object);

        
        Assert.True(registry.CanResolve<ITestReporter>());
        Assert.True(registry.CanResolve(typeof(ITestReporter)));
    }

    [Fact]
    public void CanResolve_WithUnregisteredType_ReturnsFalse()
    {
        
        var registry = new ResolverRegistry();

        
        Assert.False(registry.CanResolve<IUnregisteredReporter>());
        Assert.False(registry.CanResolve(typeof(IUnregisteredReporter)));
    }

    [Fact]
    public void CanResolve_WithRegisteredDelegate_ReturnsTrue()
    {
        
        var registry = new ResolverRegistry();
        
        registry.RegisterResolver<ITestReporter>(
            async (r, ct) => "async result",
            r => "sync result");

        
        Assert.True(registry.CanResolve<ITestReporter>());
        Assert.True(registry.CanResolve(typeof(ITestReporter)));
    }

    [Fact]
    public void CanResolve_WithInterfaceImplementation_ReturnsTrue()
    {
        
        var registry = new ResolverRegistry();
        var mockResolver = new Mock<TestResolverClass>();

        registry.RegisterResolver<ITestReporter, TestResolverClass>(mockResolver.Object);

        
        Assert.True(registry.CanResolve(typeof(ConcreteTestReporter)));
    }

    #endregion

    #region Delegate vs Instance Priority Tests

    [Fact]
    public async Task ResolveAsync_WithBothDelegateAndInstance_PrioritizesDelegates()
    {
        
        var registry = new ResolverRegistry();
        var reporter = new Mock<ITestReporter>().Object;

        
        var mockResolver = new Mock<TestResolverClass>();
        mockResolver.Setup(r => r.ResolveAsync(It.IsAny<ITestReporter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Instance result");
        registry.RegisterResolver<ITestReporter, TestResolverClass>(mockResolver.Object);

        
        registry.RegisterResolver<ITestReporter>(
            async (r, ct) => "Delegate result",
            r => "Delegate sync result");

        
        var result = await registry.ResolveAsync(reporter);

        
        Assert.Equal("Delegate result", result);
        mockResolver.Verify(r => r.ResolveAsync(It.IsAny<ITestReporter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ResolveSync_WithBothDelegateAndInstance_PrioritizesDelegates()
    {
        
        var registry = new ResolverRegistry();
        var reporter = new Mock<ITestReporter>().Object;

        
        var mockResolver = new Mock<TestResolverClass>();
        mockResolver.Setup(r => r.ResolveSync(It.IsAny<ITestReporter>()))
            .Returns("Instance result");
        registry.RegisterResolver<ITestReporter, TestResolverClass>(mockResolver.Object);

        
        registry.RegisterResolver<ITestReporter>(
            async (r, ct) => "Delegate async result",
            r => "Delegate result");

        
        var result = registry.ResolveSync(reporter);

        
        Assert.Equal("Delegate result", result);
        mockResolver.Verify(r => r.ResolveSync(It.IsAny<ITestReporter>()), Times.Never);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task ResolverRegistry_ConcurrentAccess_ThreadSafe()
    {
        
        var registry = new ResolverRegistry();
        var reporter = new Mock<ITestReporter>().Object;
        const int taskCount = 10;

        registry.RegisterResolver<ITestReporter>(
            async (r, ct) => "Concurrent result",
            r => "Concurrent sync result");

        
        var tasks = new Task[taskCount];
        for (int i = 0; i < taskCount; i++)
        {
            var taskIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                
                if (taskIndex % 2 == 0)
                {
                    await registry.ResolveAsync(reporter);
                }
                else
                {
                    registry.ResolveSync(reporter);
                }
            });
        }

        
        await Task.WhenAll(tasks);
        Assert.True(registry.CanResolve<ITestReporter>());
    }

    [Fact]
    public void ResolverRegistry_ConcurrentRegistration_LastWriterWins()
    {
        
        var registry = new ResolverRegistry();
        var reporter = new Mock<ITestReporter>().Object;
        const int registrationCount = 10;

        var tasks = new Task[registrationCount];
        var results = new string[registrationCount];

        
        for (int i = 0; i < registrationCount; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                registry.RegisterResolver<ITestReporter>(
                    async (r, ct) => $"Result-{index}",
                    r => $"SyncResult-{index}");
            });
        }

        Task.WaitAll(tasks);

        
        var finalResult = registry.ResolveSync(reporter);
        Assert.StartsWith("SyncResult-", finalResult);
    }

    #endregion

    #region Test Helper Classes

    public interface ITestReporter : IReporter
    {
        string TestProperty { get; set; }
    }

    public interface IUnregisteredReporter : IReporter
    {
        string UnregisteredProperty { get; set; }
    }

    public class ConcreteTestReporter : BaseReporter<ConcreteTestReporter>, ITestReporter
    {
        public string TestProperty { get; set; } = string.Empty;
    }

    public class TestResolverClass : IReportResolverAsync<ITestReporter>, IReportResolverSync<ITestReporter>
    {
        public virtual async Task<string> ResolveAsync(ITestReporter reporter, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult("Test resolver async result");
        }

        public virtual string ResolveSync(ITestReporter reporter)
        {
            return "Test resolver sync result";
        }
    }

    public class ConcreteTestResolverClass : IReportResolverAsync<ConcreteTestReporter>, IReportResolverSync<ConcreteTestReporter>
    {
        public virtual async Task<string> ResolveAsync(ConcreteTestReporter reporter, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult("Concrete test resolver async result");
        }

        public virtual string ResolveSync(ConcreteTestReporter reporter)
        {
            return "Concrete test resolver sync result";
        }
    }

    #endregion
}

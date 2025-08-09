using ReportOnFailure.Interfaces.Registry;
using ReportOnFailure.Interfaces.Reporters;
using ReportOnFailure.Interfaces.Resolvers;
using System.Collections.Concurrent;

namespace ReportOnFailure.Registries;

public class ResolverRegistry : IResolverRegistry
{
    private readonly ConcurrentDictionary<Type, object> _resolvers = new();
    private readonly ConcurrentDictionary<Type, (Func<object, CancellationToken, Task<string>> AsyncResolver, Func<object, string> SyncResolver)> _delegateResolvers = new();

    public void RegisterResolver<TReporter, TResolver>(TResolver resolver)
        where TReporter : class, IReporter
        where TResolver : class, IReportResolverAsync<TReporter>, IReportResolverSync<TReporter>
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _resolvers.AddOrUpdate(typeof(TReporter), resolver, (_, _) => resolver);
    }

    public void RegisterResolver<TReporter>(Func<TReporter, CancellationToken, Task<string>> asyncResolver, Func<TReporter, string> syncResolver)
        where TReporter : class, IReporter
    {
        ArgumentNullException.ThrowIfNull(asyncResolver);
        ArgumentNullException.ThrowIfNull(syncResolver);

        _delegateResolvers.AddOrUpdate(
            typeof(TReporter),
            ((reporter, ct) => asyncResolver((TReporter)reporter, ct), reporter => syncResolver((TReporter)reporter)),
            (_, _) => ((reporter, ct) => asyncResolver((TReporter)reporter, ct), reporter => syncResolver((TReporter)reporter))
        );
    }

    private async Task<(bool Success, string Result)> TryResolveAsync(Type typeToCheck, object reporter, CancellationToken cancellationToken)
    {

        if (_delegateResolvers.TryGetValue(typeToCheck, out var delegateResolver))
        {
            var result = await delegateResolver.AsyncResolver(reporter, cancellationToken);
            return (true, result);
        }


        if (_resolvers.TryGetValue(typeToCheck, out var resolver))
        {

            var resolverType = resolver.GetType();
            var asyncInterface = resolverType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReportResolverAsync<>));

            if (asyncInterface != null)
            {
                var resolveMethod = asyncInterface.GetMethod("ResolveAsync");
                if (resolveMethod != null)
                {
                    var task = (Task<string>)resolveMethod.Invoke(resolver, new object[] { reporter, cancellationToken })!;
                    var result = await task;
                    return (true, result);
                }
            }
        }

        return (false, string.Empty);
    }

    public async Task<string> ResolveAsync<TReporter>(TReporter reporter, CancellationToken cancellationToken = default)
        where TReporter : class, IReporter
    {
        ArgumentNullException.ThrowIfNull(reporter);

        var reporterType = typeof(TReporter);
        var actualType = reporter.GetType();


        var (success, result) = await TryResolveAsync(reporterType, reporter, cancellationToken);
        if (success)
        {
            return result;
        }


        if (actualType != reporterType)
        {
            (success, result) = await TryResolveAsync(actualType, reporter, cancellationToken);
            if (success)
            {
                return result;
            }
        }


        foreach (var interfaceType in actualType.GetInterfaces().Where(i => typeof(IReporter).IsAssignableFrom(i)))
        {
            (success, result) = await TryResolveAsync(interfaceType, reporter, cancellationToken);
            if (success)
            {
                return result;
            }
        }

        throw new NotSupportedException($"No resolver registered for reporter type {actualType.Name}");
    }

    public string ResolveSync<TReporter>(TReporter reporter)
        where TReporter : class, IReporter
    {
        ArgumentNullException.ThrowIfNull(reporter);

        var reporterType = typeof(TReporter);
        var actualType = reporter.GetType();


        if (TryResolveSync(reporterType, reporter, out var result))
        {
            return result;
        }


        if (actualType != reporterType)
        {
            if (TryResolveSync(actualType, reporter, out result))
            {
                return result;
            }
        }


        foreach (var interfaceType in actualType.GetInterfaces().Where(i => typeof(IReporter).IsAssignableFrom(i)))
        {
            if (TryResolveSync(interfaceType, reporter, out result))
            {
                return result;
            }
        }

        throw new NotSupportedException($"No resolver registered for reporter type {actualType.Name}");
    }

    public bool CanResolve<TReporter>() where TReporter : class, IReporter
    {
        return CanResolve(typeof(TReporter));
    }

    public bool CanResolve(Type reporterType)
    {
        if (_resolvers.ContainsKey(reporterType) || _delegateResolvers.ContainsKey(reporterType))
        {
            return true;
        }


        return reporterType.GetInterfaces()
            .Where(i => typeof(IReporter).IsAssignableFrom(i))
            .Any(interfaceType => _resolvers.ContainsKey(interfaceType) || _delegateResolvers.ContainsKey(interfaceType));
    }

    private bool TryResolveSync(Type typeToCheck, object reporter, out string result)
    {
        result = string.Empty;


        if (_delegateResolvers.TryGetValue(typeToCheck, out var delegateResolver))
        {
            result = delegateResolver.SyncResolver(reporter);
            return true;
        }


        if (_resolvers.TryGetValue(typeToCheck, out var resolver))
        {

            var resolverType = resolver.GetType();
            var syncInterface = resolverType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReportResolverSync<>));

            if (syncInterface != null)
            {
                var resolveMethod = syncInterface.GetMethod("ResolveSync");
                if (resolveMethod != null)
                {
                    result = (string)resolveMethod.Invoke(resolver, new object[] { reporter })!;
                    return true;
                }
            }
        }

        return false;
    }
}
using Maviray.Blazor.Components.Pro.Core.Access;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

public sealed class RecordingFetchObserver : IGridFetchObserver
{
    public List<GridRowsLoaded<int>> Loaded { get; } = [];
    public List<GridAccessDenied> Denied { get; } = [];
    public List<CancellationToken> Tokens { get; } = [];
    public Exception? ThrowOnLoaded { get; init; }

    public ValueTask OnRowsLoadedAsync<TDataItem, TKey>(GridRowsLoaded<TKey> loaded, CancellationToken cancellationToken)
        where TDataItem : class
        where TKey : IEquatable<TKey>
    {
        if (ThrowOnLoaded != null)
        {
            throw ThrowOnLoaded;
        }

        Loaded.Add((GridRowsLoaded<int>)(object)loaded);
        Tokens.Add(cancellationToken);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnAccessDeniedAsync<TDataItem>(GridAccessDenied denied, CancellationToken cancellationToken)
        where TDataItem : class
    {
        Denied.Add(denied);
        Tokens.Add(cancellationToken);
        return ValueTask.CompletedTask;
    }
}

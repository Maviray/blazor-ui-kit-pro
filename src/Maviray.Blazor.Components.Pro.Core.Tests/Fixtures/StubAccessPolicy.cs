using Maviray.Blazor.Components.Pro.Core.Access;
using System.Linq.Expressions;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;

/// <summary>Answers every evaluation with the supplied <see cref="TestOrder"/> decision and records the requests.</summary>
public sealed class StubAccessPolicy(Func<GridAccessDecision<TestOrder>?> decide) : IGridAccessPolicy
{
    public List<GridAccessRequest> Requests { get; } = [];
    public List<CancellationToken> Tokens { get; } = [];

    public static StubAccessPolicy Allowing(Expression<Func<TestOrder, bool>>? rowFilter = null) => new(() => GridAccessDecision<TestOrder>.Allow(rowFilter));
    public static StubAccessPolicy Denying(string? message = null) => new(() => GridAccessDecision<TestOrder>.Deny(message));
    public static StubAccessPolicy NotApplicable() => new(GridAccessDecision<TestOrder>.NotApplicable);

    public ValueTask<GridAccessDecision<TDataItem>> EvaluateAsync<TDataItem>(GridAccessRequest request, CancellationToken cancellationToken)
        where TDataItem : class
    {
        Requests.Add(request);
        Tokens.Add(cancellationToken);
        return ValueTask.FromResult((GridAccessDecision<TDataItem>)(object)decide()!);
    }
}

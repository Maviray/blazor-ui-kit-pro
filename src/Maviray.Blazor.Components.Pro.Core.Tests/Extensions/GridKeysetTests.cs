using Maviray.Blazor.Components.Core.Enums;
using Maviray.Blazor.Components.Pro.Core.Extensions;
using Maviray.Blazor.Components.Pro.Core.Models.Grid;
using Maviray.Blazor.Components.Pro.Core.Tests.Fixtures;
using Xunit;

namespace Maviray.Blazor.Components.Pro.Core.Tests.Extensions;

public class GridKeysetTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // Ordered by Name then Id: (A,2) (A,5) (B,1) (B,3) (B,6) (C,4). Equal names make the Id tie-break matter.
    private static readonly TestOrder[] Orders =
    [
        new() { Id = 1, Name = "B" }, new() { Id = 2, Name = "A" }, new() { Id = 3, Name = "B" },
        new() { Id = 4, Name = "C" }, new() { Id = 5, Name = "A" }, new() { Id = 6, Name = "B" }
    ];

    private static GridKeyset<TestOrder> ByNameThenId(SortOrder order = SortOrder.Ascending) =>
        new([new GridOrderKey("Name", order), new GridOrderKey("Id", order)]);

    private static Task<GridKeysetPage<TestOrder>> Load(GridKeyset<TestOrder> keyset, MaviGridCursor? cursor, int skip = 0, int take = 2) =>
        keyset.LoadPageAsync(Orders.AsQueryable(), cursor, skip, take, (q, _) => Task.FromResult(q.ToList()), Ct);

    private static int[] Ids(GridKeysetPage<TestOrder> page) => page.Items.Select(o => o.Id).ToArray();

    [Fact]
    public async Task WithoutACursor_SkipsAndTakesInTheKeysetOrder()
    {
        var page = await Load(ByNameThenId(), cursor: null, skip: 2);

        Assert.Equal([1, 3], Ids(page));
    }

    [Fact]
    public async Task Next_LoadsTheRowsAfterTheLastRow_BreakingTiesByTheLaterKeys()
    {
        var keyset = ByNameThenId();
        var first = await Load(keyset, cursor: null);

        var second = await Load(keyset, new MaviGridCursor(first.LastRowCursor!, MaviGridCursorDirection.Next), skip: 999);
        var third = await Load(keyset, new MaviGridCursor(second.LastRowCursor!, MaviGridCursorDirection.Next), skip: 999);

        Assert.Equal([2, 5], Ids(first));
        Assert.Equal([1, 3], Ids(second));
        Assert.Equal([6, 4], Ids(third));
    }

    [Fact]
    public async Task Previous_LoadsTheRowsBeforeTheFirstRow_InTheKeysetOrder()
    {
        var keyset = ByNameThenId();
        var third = await Load(keyset, cursor: null, skip: 4);

        var second = await Load(keyset, new MaviGridCursor(third.FirstRowCursor!, MaviGridCursorDirection.Previous), skip: 999);
        var first = await Load(keyset, new MaviGridCursor(second.FirstRowCursor!, MaviGridCursorDirection.Previous), skip: 999);

        Assert.Equal([6, 4], Ids(third));
        Assert.Equal([1, 3], Ids(second));
        Assert.Equal([2, 5], Ids(first));
    }

    [Fact]
    public async Task Descending_SeeksTheOtherWay()
    {
        // (C,4) (B,6) (B,3) (B,1) (A,5) (A,2)
        var keyset = ByNameThenId(SortOrder.Descending);
        var first = await Load(keyset, cursor: null);

        var second = await Load(keyset, new MaviGridCursor(first.LastRowCursor!, MaviGridCursorDirection.Next));

        Assert.Equal([4, 6], Ids(first));
        Assert.Equal([3, 1], Ids(second));
    }

    [Fact]
    public async Task LastPage_HasFewerRows_AndPastTheEndIsEmpty()
    {
        var keyset = ByNameThenId();
        var page = await Load(keyset, cursor: null, skip: 4, take: 4);

        var after = await Load(keyset, new MaviGridCursor(page.LastRowCursor!, MaviGridCursorDirection.Next));

        Assert.Equal([6, 4], Ids(page));
        Assert.Empty(after.Items);
        Assert.Null(after.FirstRowCursor);
        Assert.Null(after.LastRowCursor);
    }

    [Fact]
    public async Task CursorOfAnotherOrder_IsIgnored_AndTheRowsAreSkipped()
    {
        var byName = await Load(ByNameThenId(), cursor: null);
        var byId = new GridKeyset<TestOrder>([new GridOrderKey("Id", SortOrder.Ascending)]);

        var page = await Load(byId, new MaviGridCursor(byName.LastRowCursor!, MaviGridCursorDirection.Next), skip: 2);

        Assert.Equal([3, 4], Ids(page));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("""{"o":"Name+,Id+","v":["A"]}""")]
    [InlineData("""{"o":"Name+,Id+","v":["A","not a number"]}""")]
    public async Task MalformedCursor_IsIgnored_AndTheRowsAreSkipped(string cursor)
    {
        var page = await Load(ByNameThenId(), new MaviGridCursor(cursor, MaviGridCursorDirection.Next), skip: 2);

        Assert.Equal([1, 3], Ids(page));
    }

    [Theory]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(long), true)]
    [InlineData(typeof(decimal), true)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(DateTime), true)]
    [InlineData(typeof(DateOnly), true)]
    [InlineData(typeof(bool), true)]
    [InlineData(typeof(Guid), true)]
    [InlineData(typeof(TestOrderStatus), true)]
    [InlineData(typeof(int?), false)]
    [InlineData(typeof(bool?), false)]
    [InlineData(typeof(TestVendor), false)]
    public void CanSeek_AcceptsTypesAStoreComparesTheWayItOrders(Type type, bool expected)
    {
        Assert.Equal(expected, GridKeyset<TestOrder>.CanSeek(type));
    }

    [Fact]
    public void KeyThatCannotBeSought_IsRejected()
    {
        // A nullable value without a null default: NULLs don't compare.
        Assert.False(GridKeyset<TestOrder>.TryCreate([new GridOrderKey("Approved", SortOrder.Ascending), new GridOrderKey("Id", SortOrder.Ascending)], out _));
        Assert.Throws<ArgumentException>(() => new GridKeyset<TestOrder>([new GridOrderKey("Vendor", SortOrder.Ascending)]));
    }

    // Walks every page by cursor (skip 999 proves each later page is sought, not skipped to), forward or backward.
    private static async Task<List<int>> Walk<T>(GridKeyset<T> keyset, IQueryable<T> source, Func<T, int> id, bool backward = false) where T : class
    {
        const int take = 2;
        var count = source.Count();
        var page = await keyset.LoadPageAsync(source, null, backward ? (count - 1) / take * take : 0, take, (q, _) => Task.FromResult(q.ToList()), Ct);
        var pages = new List<List<int>> { page.Items.Select(id).ToList() };

        while (pages.Sum(p => p.Count) < count)
        {
            var cursor = backward
                ? new MaviGridCursor(page.FirstRowCursor!, MaviGridCursorDirection.Previous)
                : new MaviGridCursor(page.LastRowCursor!, MaviGridCursorDirection.Next);
            page = await keyset.LoadPageAsync(source, cursor, 999, take, (q, _) => Task.FromResult(q.ToList()), Ct);
            Assert.NotEmpty(page.Items);
            pages.Add(page.Items.Select(id).ToList());
        }

        if (backward)
        {
            pages.Reverse();
        }

        return [.. pages.SelectMany(p => p)];
    }

    private static readonly TestOrder[] Mixed =
    [
        new() { Id = 1, Status = TestOrderStatus.Closed, Approved = true }, new() { Id = 2, Status = TestOrderStatus.Open, Approved = null },
        new() { Id = 3, Status = TestOrderStatus.Closed, Approved = false }, new() { Id = 4, Status = TestOrderStatus.Open, Approved = true },
        new() { Id = 5, Status = TestOrderStatus.Open, Approved = false }
    ];

    [Theory]
    [InlineData(SortOrder.Ascending, false)]
    [InlineData(SortOrder.Descending, false)]
    [InlineData(SortOrder.Ascending, true)]
    [InlineData(SortOrder.Descending, true)]
    public async Task EnumSort_IsSought(SortOrder order, bool backward)
    {
        var keyset = new GridKeyset<TestOrder>([new GridOrderKey("Status", order), new GridOrderKey("Id", order)]);
        var expected = order == SortOrder.Ascending
            ? Mixed.OrderBy(o => o.Status).ThenBy(o => o.Id)
            : Mixed.OrderByDescending(o => o.Status).ThenByDescending(o => o.Id);

        Assert.Equal(expected.Select(o => o.Id), await Walk(keyset, Mixed.AsQueryable(), o => o.Id, backward));
    }

    [Theory]
    [InlineData(SortOrder.Ascending, false)]
    [InlineData(SortOrder.Descending, false)]
    [InlineData(SortOrder.Ascending, true)]
    [InlineData(SortOrder.Descending, true)]
    public async Task BoolSort_WithANullDefault_IsSought(SortOrder order, bool backward)
    {
        var keyset = new GridKeyset<TestOrder>([new GridOrderKey("Approved", order, NullDefault: false), new GridOrderKey("Id", order)]);
        var expected = order == SortOrder.Ascending
            ? Mixed.OrderBy(o => o.Approved ?? false).ThenBy(o => o.Id)
            : Mixed.OrderByDescending(o => o.Approved ?? false).ThenByDescending(o => o.Id);

        Assert.Equal(expected.Select(o => o.Id), await Walk(keyset, Mixed.AsQueryable(), o => o.Id, backward));
    }

    public sealed class GuidItem
    {
        public Guid Id { get; set; }
        public int Number { get; set; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GuidKey_IsSought(bool backward)
    {
        var items = Enumerable.Range(1, 7).Select(n => new GuidItem { Id = Guid.NewGuid(), Number = n }).ToArray();
        var keyset = new GridKeyset<GuidItem>([new GridOrderKey("Id", SortOrder.Ascending)]);

        Assert.Equal(items.OrderBy(i => i.Id).Select(i => i.Number), await Walk(keyset, items.AsQueryable(), i => i.Number, backward));
    }

    [Fact]
    public void NoKeys_AreRejected()
    {
        Assert.Throws<ArgumentException>(() => new GridKeyset<TestOrder>([]));
    }
}

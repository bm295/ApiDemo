using System.Text.Json.Nodes;
using Cqrs.RetailerIsolation;
using FunctionalProgramming.Infrastructure.Persistence;
using FunctionalProgramming.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ApiDemo.Tests;

public sealed class MessageServiceTests
{
    [Fact]
    public void CountWindowsWithSum_CountsOverlappingContiguousWindows()
    {
        var count = MessageWindowAnalytics.CountWindowsWithSum([1m, 2m, 1m, 2m, 3m], 2, 3m);

        Assert.Equal(3, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(4)]
    public void CountWindowsWithSum_ReturnsZeroForAnInvalidWindowLength(int windowLength)
    {
        var count = MessageWindowAnalytics.CountWindowsWithSum([1m, 2m, 3m], windowLength, 3m);

        Assert.Equal(0, count);
    }

    [Fact]
    public void AddAfterDelete_UsesANewId()
    {
        using var store = TestStore.Create();
        var first = store.Service.Add("first", new JsonObject());
        Assert.True(store.Service.Delete(first.Id));

        var added = store.Service.Add("new message", new JsonObject());

        Assert.True(added.Id > first.Id);
    }

    [Fact]
    public void ReadResults_DoNotExposeStoredMutableState()
    {
        using var store = TestStore.Create();
        var added = store.Service.Add("message", new JsonObject());
        var message = store.Service.GetById(added.Id)!;
        message.Attributes["changed"] = true;

        Assert.False(store.Service.GetById(added.Id)!.Attributes.ContainsKey("changed"));
    }

    [Fact]
    public void Add_ClonesCallerAttributes()
    {
        using var store = TestStore.Create();
        var attributes = new JsonObject { ["source"] = "test" };

        var added = store.Service.Add("message", attributes);
        attributes["source"] = "changed";

        Assert.Equal("test", added.Attributes["source"]!.GetValue<string>());
        Assert.Equal("test", store.Service.GetById(added.Id)!.Attributes["source"]!.GetValue<string>());
    }

    [Fact]
    public void Update_MergesAttributesAndPreservesTextWhenBlank()
    {
        using var store = TestStore.Create();
        var original = store.Service.Add("Hello from REST", new JsonObject { ["audience"] = "general" });
        var updated = store.Service.Update(original.Id, " ", new JsonObject { ["region"] = "apac" });

        Assert.NotNull(updated);
        Assert.Equal("Hello from REST", updated!.Text);
        Assert.Equal("general", updated.Attributes["audience"]!.GetValue<string>());
        Assert.Equal("apac", updated.Attributes["region"]!.GetValue<string>());
    }

    [Fact]
    public void Messages_are_isolated_by_retailer_through_the_CQRS_interceptor_and_query_filter()
    {
        using var retailerA = TestStore.Create();
        using var retailerB = TestStore.Create(retailerA.Connection);
        retailerA.Service.Add("retailer A", new JsonObject());
        retailerB.Service.Add("retailer B", new JsonObject());

        Assert.Single(retailerA.Service.GetAll());
        Assert.Single(retailerB.Service.GetAll());
        Assert.Equal("retailer A", retailerA.Service.GetAll()[0].Text);
        Assert.Equal("retailer B", retailerB.Service.GetAll()[0].Text);
    }

    private sealed class TestStore : IDisposable
    {
        private readonly ApiDemoDbContext _dbContext;
        private readonly bool _ownsConnection;

        private TestStore(ApiDemoDbContext dbContext, SqliteConnection connection, bool ownsConnection)
        {
            _dbContext = dbContext;
            Connection = connection;
            _ownsConnection = ownsConnection;
            Service = new EfMessageService(dbContext);
        }

        public SqliteConnection Connection { get; }
        public EfMessageService Service { get; }

        public static TestStore Create(SqliteConnection? connection = null)
        {
            var ownsConnection = connection is null;
            connection ??= new SqliteConnection("Data Source=:memory:");
            if (connection.State != System.Data.ConnectionState.Open) connection.Open();
            var retailerContext = new TestRetailerContext(Guid.NewGuid());
            var options = new DbContextOptionsBuilder<ApiDemoDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(new RetailerSaveChangesInterceptor(retailerContext))
                .Options;
            var dbContext = new ApiDemoDbContext(options, retailerContext);
            dbContext.Database.EnsureCreated();
            return new TestStore(dbContext, connection, ownsConnection);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            if (_ownsConnection) Connection.Dispose();
        }
    }

    private sealed class TestRetailerContext(Guid retailerId) : IRetailerContext
    {
        public Guid RetailerId { get; } = retailerId;
    }
}

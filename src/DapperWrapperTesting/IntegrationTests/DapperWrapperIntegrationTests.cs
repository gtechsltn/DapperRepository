using Dapper;

using DapperWrapperTesting.Application.Interfaces;
using DapperWrapperTesting.Domain.Entities;
using DapperWrapperTesting.Infrastructure;
using DapperWrapperTesting.IntegrationTests.Shared.Fixtures;

using Xunit;

namespace DapperWrapperTesting.IntegrationTests;

public class DapperWrapperIntegrationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;
    private readonly IDapperWrapper _dapper;

    public DapperWrapperIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _dapper = new DapperWrapper(); // real wrapper
    }

    [Fact]
    public async Task Execute_ShouldInsertUser()
    {
        using var conn = _fixture.GetOpenConnection();
        var sql = "INSERT INTO Users (Name, Email) VALUES (@Name, @Email)";
        var rows = await _dapper.ExecuteAsync(conn, sql, new { Name = "Alice", Email = "alice@test.com" });

        Assert.Equal(1, rows);
    }

    [Fact]
    public async Task Query_ShouldReturnUsers()
    {
        using var conn = _fixture.GetOpenConnection();
        await _dapper.ExecuteAsync(conn, "INSERT INTO Users (Name, Email) VALUES ('Bob', 'bob@test.com')");

        var users = await _dapper.QueryAsync<User>(conn, "SELECT * FROM Users");
        Assert.Contains(users, u => u.Email == "bob@test.com");
    }

    [Fact]
    public async Task QueryFirstOrDefault_ShouldReturnSingleUser()
    {
        using var conn = _fixture.GetOpenConnection();
        await _dapper.ExecuteAsync(conn, "INSERT INTO Users (Name, Email) VALUES ('Charlie', 'charlie@test.com')");

        var user = await _dapper.QueryFirstOrDefaultAsync<User>(
            conn, "SELECT * FROM Users WHERE Email = @Email", new { Email = "charlie@test.com" });

        Assert.NotNull(user);
        Assert.Equal("Charlie", user.Name);
    }

    [Fact]
    public async Task QueryMultiple_ShouldReturnUserAndOrders()
    {
        using var conn = _fixture.GetOpenConnection();

        // Arrange: insert user
        var userId = await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO Users (Name, Email) 
                  OUTPUT INSERTED.UserId
                  VALUES ('Dave', 'dave@test.com');");

        // Arrange: insert orders
        await conn.ExecuteAsync(
            @"INSERT INTO Orders (UserId, ProductName, Quantity, Amount) 
                  VALUES (@UserId, 'Laptop', 1, 999.99),
                         (@UserId, 'Mouse', 2, 25.50);",
            new { UserId = userId });

        // Act: query multiple result sets
        var (users, orderList) = await _dapper.QueryMultipleAsync<User, Order>(
            conn,
            @"SELECT * FROM Users WHERE UserId = @Id;
                  SELECT * FROM Orders WHERE UserId = @Id;",
            new { Id = userId });

        var user = users.First();
        var orders = orderList.ToList();

        // Assert
        Assert.NotNull(user);
        Assert.Equal(userId, user.UserId);
        Assert.Equal("Dave", user.Name);
        Assert.Equal("dave@test.com", user.Email);

        Assert.Equal(2, orders.Count);
        Assert.All(orders, o => Assert.Equal(userId, o.UserId));
        Assert.Contains(orders, o => o.ProductName == "Laptop" && o.Quantity == 1 && o.Amount == 999.99m);
        Assert.Contains(orders, o => o.ProductName == "Mouse" && o.Quantity == 2 && o.Amount == 25.50m);
    }

    [Fact]
    public async Task MultiMapping_ShouldReturnUserWithOrders()
    {
        using var conn = _fixture.GetOpenConnection();

        var userId = await conn.ExecuteScalarAsync<int>(
            "INSERT INTO Users (Name, Email) OUTPUT INSERTED.UserId VALUES ('Eva','eva@test.com')");

        await conn.ExecuteAsync("INSERT INTO Orders (UserId, ProductName, Quantity, Amount) VALUES (@UserId, 'Phone', 2, 499.99)",
            new { UserId = userId });

        var sql = @"SELECT u.UserId, u.Name, u.Email, o.OrderId, o.UserId, o.ProductName, o.Quantity, o.Amount
                        FROM Users u
                        INNER JOIN Orders o ON u.UserId = o.UserId
                        WHERE u.UserId = @UserId";

        var userWithOrders = (await _dapper.QueryAsync<User, Order, User>(
            conn, sql, (u, o) =>
            {
                u.Orders ??= new List<Order>();
                u.Orders.Add(o);
                return u;
            },
            new { UserId = userId },
            splitOn: "UserId")).First();

        Assert.Equal("Eva", userWithOrders.Name);
        Assert.Single(userWithOrders.Orders);
    }
}
using System.Data;

using Dapper;

using DapperWrapperTesting.Application.Interfaces;
using DapperWrapperTesting.Domain.Entities;

using Moq;

using Xunit;

namespace DapperWrapperTesting.UnitTests;

public class DapperWrapperUnitTests
{
    private readonly Mock<IDapperWrapper> _mock;
    private readonly IDbConnection _fakeConn;

    public DapperWrapperUnitTests()
    {
        _mock = new Mock<IDapperWrapper>();
        _fakeConn = new Mock<IDbConnection>().Object;
    }

    [Fact]
    public async Task QueryAsync_ShouldReturnUsers()
    {
        _mock.Setup(m => m.QueryAsync<User>(_fakeConn, "SELECT * FROM Users", null, null))
             .ReturnsAsync(new List<User> { new User { UserId = 1, Name = "Alice" } });

        var result = await _mock.Object.QueryAsync<User>(_fakeConn, "SELECT * FROM Users");

        var list = result.AsList();
        Assert.Single(list);
        Assert.Equal("Alice", list[0].Name);
    }

    [Fact]
    public async Task QueryFirstAsync_ShouldReturnOneUser()
    {
        _mock.Setup(m => m.QueryFirstAsync<User>(_fakeConn, "SELECT TOP 1 * FROM Users", null, null))
             .ReturnsAsync(new User { UserId = 1, Name = "Bob" });

        var user = await _mock.Object.QueryFirstAsync<User>(_fakeConn, "SELECT TOP 1 * FROM Users");

        Assert.Equal("Bob", user.Name);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_ShouldReturnNullIfNoUser()
    {
        _mock.Setup(m => m.QueryFirstOrDefaultAsync<User>(_fakeConn, "SELECT TOP 1 * FROM Users WHERE Id=-1", null, null))
             .ReturnsAsync((User?)null);

        var user = await _mock.Object.QueryFirstOrDefaultAsync<User>(_fakeConn, "SELECT TOP 1 * FROM Users WHERE Id=-1");

        Assert.Null(user);
    }

    [Fact]
    public async Task QuerySingleAsync_ShouldReturnOnlyUser()
    {
        _mock.Setup(m => m.QuerySingleAsync<User>(_fakeConn, "SELECT * FROM Users WHERE Id=1", null, null))
             .ReturnsAsync(new User { UserId = 1, Name = "Charlie" });

        var user = await _mock.Object.QuerySingleAsync<User>(_fakeConn, "SELECT * FROM Users WHERE Id=1");

        Assert.Equal("Charlie", user.Name);
    }

    [Fact]
    public async Task QuerySingleOrDefaultAsync_ShouldReturnNullIfNone()
    {
        _mock.Setup(m => m.QuerySingleOrDefaultAsync<User>(_fakeConn, "SELECT * FROM Users WHERE Id=-1", null, null))
             .ReturnsAsync((User?)null);

        var user = await _mock.Object.QuerySingleOrDefaultAsync<User>(_fakeConn, "SELECT * FROM Users WHERE Id=-1");

        Assert.Null(user);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnRowsAffected()
    {
        _mock.Setup(m => m.ExecuteAsync(_fakeConn, "DELETE FROM Users WHERE Id=1", null, null))
             .ReturnsAsync(1);

        var rows = await _mock.Object.ExecuteAsync(_fakeConn, "DELETE FROM Users WHERE Id=1");

        Assert.Equal(1, rows);
    }

    [Fact]
    public async Task ExecuteScalarAsync_ShouldReturnCount()
    {
        _mock.Setup(m => m.ExecuteScalarAsync<int>(_fakeConn, "SELECT COUNT(*) FROM Users", null, null))
             .ReturnsAsync(42);

        var count = await _mock.Object.ExecuteScalarAsync<int>(_fakeConn, "SELECT COUNT(*) FROM Users");

        Assert.Equal(42, count);
    }

    [Fact]
    public async Task QueryMultipleAsync_ShouldReturnTwoSets()
    {
        var expectedUsers = new List<User> { new User { UserId = 1, Name = "Alice" } };
        var expectedOrders = new List<Order> { new Order { Id = 10, Amount = 100 } };

        _mock.Setup(m => m.QueryMultipleAsync<User, Order>(_fakeConn, "SELECT * FROM Users; SELECT * FROM Orders;", null, null))
             .ReturnsAsync((expectedUsers, expectedOrders));

        var (users, orders) = await _mock.Object.QueryMultipleAsync<User, Order>(
            _fakeConn, "SELECT * FROM Users; SELECT * FROM Orders;");

        Assert.Single(users);
        Assert.Single(orders);
        Assert.Equal("Alice", users.First().Name);
        Assert.Equal(100, orders.First().Amount);
    }

    [Fact]
    public async Task QueryAsync_MultiMapping_TwoTypes()
    {
        var joinedUsers = new List<User>
        {
            new User
            {
                UserId = 1,
                Name = "Alice",
                Address = new Address { Id = 5, City = "Paris" }
            }
        };

        _mock.Setup(m => m.QueryAsync<User, Address, User>(
                _fakeConn,
                "SELECT * FROM Users u JOIN Addresses a ON u.Id = a.UserId",
                It.IsAny<Func<User, Address, User>>(),
                null, null, "Id"))
             .ReturnsAsync(joinedUsers);

        var result = await _mock.Object.QueryAsync<User, Address, User>(
            _fakeConn,
            "SELECT * FROM Users u JOIN Addresses a ON u.Id = a.UserId",
            (u, a) => { u.Address = a; return u; },
            null, null, "Id");

        var list = result.AsList();
        Assert.Single(list);
        Assert.Equal("Alice", list[0].Name);
        Assert.Equal("Paris", list[0].Address?.City);
    }

    [Fact]
    public async Task QueryAsync_MultiMapping_ThreeTypes()
    {
        var joined = new List<User>
        {
            new User
            {
                UserId = 1,
                Name = "Alice",
                Address = new Address { Id = 5, City = "Paris" },
                Orders = new List<Order> { new Order { Id = 9, Amount = 123.45m } }
            }
        };

        _mock.Setup(m => m.QueryAsync<User, Address, Order, User>(
                _fakeConn,
                "SELECT * FROM Users u JOIN Addresses a ON u.Id = a.UserId JOIN Orders o ON u.Id = o.UserId",
                It.IsAny<Func<User, Address, Order, User>>(),
                null, null, "Id"))
             .ReturnsAsync(joined);

        var result = await _mock.Object.QueryAsync<User, Address, Order, User>(
            _fakeConn,
            "SELECT * FROM Users u JOIN Addresses a ON u.Id = a.UserId JOIN Orders o ON u.Id = o.UserId",
            (u, a, o) =>
            {
                u.Address = a;
                u.Orders.Add(o);
                return u;
            },
            null, null, "Id");

        var list = result.AsList();
        Assert.Single(list);
        var u1 = list[0];

        Assert.Equal("Alice", u1.Name);
        Assert.Equal("Paris", u1.Address?.City);
        Assert.Single(u1.Orders);
        Assert.Equal(123.45m, u1.Orders[0].Amount);
    }
}

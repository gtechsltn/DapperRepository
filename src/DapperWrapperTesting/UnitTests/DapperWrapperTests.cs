using System.Data;

using Dapper;

using DapperWrapperTesting.Application.Interfaces;
using DapperWrapperTesting.Domain.Entities;

using Moq;

using Xunit;

namespace DapperWrapperTesting.UnitTests;

public class DapperWrapperTests
{
    [Fact]
    public async Task QueryAsync_ShouldReturnUsers()
    {
        var mock = new Mock<IDapperWrapper>();
        mock.Setup(m => m.QueryAsync<User>(It.IsAny<IDbConnection>(), "SELECT * FROM Users", null, null))
            .ReturnsAsync(new List<User> { new User { UserId = 1, Name = "Alice" } });

        var users = await mock.Object.QueryAsync<User>(new Mock<IDbConnection>().Object, "SELECT * FROM Users");

        Assert.Single(users);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnRowsAffected()
    {
        var mock = new Mock<IDapperWrapper>();
        mock.Setup(m => m.ExecuteAsync(It.IsAny<IDbConnection>(), "DELETE FROM Users WHERE Id=1", null, null))
            .ReturnsAsync(1);

        var rows = await mock.Object.ExecuteAsync(new Mock<IDbConnection>().Object, "DELETE FROM Users WHERE Id=1");

        Assert.Equal(1, rows);
    }

    [Fact]
    public async Task ExecuteScalarAsync_ShouldReturnCount()
    {
        var mock = new Mock<IDapperWrapper>();
        mock.Setup(m => m.ExecuteScalarAsync<int>(It.IsAny<IDbConnection>(), "SELECT COUNT(*) FROM Users", null, null))
            .ReturnsAsync(42);

        var count = await mock.Object.ExecuteScalarAsync<int>(new Mock<IDbConnection>().Object, "SELECT COUNT(*) FROM Users");

        Assert.Equal(42, count);
    }

    [Fact]
    public async Task QueryMultipleAsync_ShouldReturnTwoSets()
    {
        var mock = new Mock<IDapperWrapper>();
        mock.Setup(m => m.QueryMultipleAsync<User, Order>(
                It.IsAny<IDbConnection>(),
                "SELECT * FROM Users; SELECT * FROM Orders;",
                null,
                null))
            .ReturnsAsync((
                new List<User> { new User { UserId = 1, Name = "Alice" } },
                new List<Order> { new Order { Id = 10, Amount = 99.5m } }
            ));

        var (users, orders) = await mock.Object.QueryMultipleAsync<User, Order>(
            new Mock<IDbConnection>().Object,
            "SELECT * FROM Users; SELECT * FROM Orders;");

        Assert.Single(users);
        Assert.Single(orders);
    }

    [Fact]
    public async Task MultiMapping_TwoTypes_ShouldReturnJoinedData()
    {
        var mock = new Mock<IDapperWrapper>();
        mock.Setup(m => m.QueryAsync<User, Address, User>(
                It.IsAny<IDbConnection>(),
                "SELECT u.*, a.* FROM Users u JOIN Address a ON u.Id=a.UserId",
                It.IsAny<Func<User, Address, User>>(),
                null,
                null,
                "Id"))
            .ReturnsAsync(new List<User>
            {
                new User { UserId = 1, Name = "Alice", Address = new Address { City = "Paris" } }
            });

        var result = await mock.Object.QueryAsync<User, Address, User>(
            new Mock<IDbConnection>().Object,
            "SELECT u.*, a.* FROM Users u JOIN Address a ON u.Id=a.UserId",
            (u, a) => { u.Address = a; return u; });

        Assert.Single(result);
        Assert.Equal("Paris", result.AsList()[0].Address?.City ?? string.Empty);
    }

    [Fact]
    public async Task MultiMapping_ThreeTypes_ShouldReturnJoinedData()
    {
        var mock = new Mock<IDapperWrapper>();
        mock.Setup(m => m.QueryAsync<User, Address, Order, User>(
                It.IsAny<IDbConnection>(),
                "SELECT u.*, a.*, o.* FROM Users u JOIN Address a ON u.Id=a.UserId JOIN Orders o ON u.Id=o.UserId",
                It.IsAny<Func<User, Address, Order, User>>(),
                null,
                null,
                "Id"))
            .ReturnsAsync(new List<User>
            {
                new User
                {
                    UserId = 1,
                    Name = "Bob",
                    Address = new Address { City = "London" },
                    Orders = new List<Order> { new Order { Id = 99, Amount = 123.45m } }
                }
            });

        var result = await mock.Object.QueryAsync<User, Address, Order, User>(
            new Mock<IDbConnection>().Object,
            "SELECT u.*, a.*, o.* FROM Users u JOIN Address a ON u.Id=a.UserId JOIN Orders o ON u.Id=o.UserId",
            (u, a, o) => { u.Address = a; u.Orders = new List<Order> { o }; return u; });

        Assert.Single(result);
        Assert.Equal("London", result.AsList()[0].Address?.City ?? string.Empty);
        Assert.Single(result.AsList()[0].Orders!);
    }
}
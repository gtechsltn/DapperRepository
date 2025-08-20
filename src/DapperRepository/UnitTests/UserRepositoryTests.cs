using System.Data;

using DapperRepository.Application.Constants;
using DapperRepository.Application.Dtos;
using DapperRepository.Application.Interfaces;
using DapperRepository.Domain.Entities;
using DapperRepository.Infrastructure.Repositories;

using FluentAssertions;

using Moq;

using Xunit;

namespace DapperRepository.UnitTests;

public class UserRepositoryTests
{
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock;
    private readonly Mock<IDbConnection> _dbConnectionMock;
    private readonly Mock<IDapperWrapper> _dapperWrapperMock;
    private readonly Mock<IMultiResultReader> _multiResultReaderMock;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _connectionFactoryMock = new Mock<IDbConnectionFactory>();
        _dbConnectionMock = new Mock<IDbConnection>();
        _dapperWrapperMock = new Mock<IDapperWrapper>();
        _multiResultReaderMock = new Mock<IMultiResultReader>();
        _connectionFactoryMock.Setup(f => f.CreateConnection()).Returns(_dbConnectionMock.Object);
        _repository = new UserRepository(_connectionFactoryMock.Object, _dapperWrapperMock.Object);
    }

    [Fact]
    public void Search_ReturnsExpectedResults()
    {
        // Arrange

        var fakeUsers = new List<User> { new User { Id = 1, Name = "Alice" } };
        var fakeTotal = 1;

        _multiResultReaderMock
            .Setup(r => r.ReadAsync<User>())
            .ReturnsAsync(fakeUsers);

        _multiResultReaderMock
            .Setup(m => m.ReadSingleAsync<int>())
            .ReturnsAsync(fakeTotal);

        _dapperWrapperMock
            .Setup(d => d.QueryMultiple(_dbConnectionMock.Object, It.IsAny<string>(), It.IsAny<object>(),
                null, null, null))
            .Returns(_multiResultReaderMock.Object);

        // Act
        var result = _repository.Search("Alice", null, false, 1, 10);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("Alice", result.Items.First().Name);
    }

    [Fact]
    public void Search_Should_Return_Empty_When_No_Users()
    {
        // Arrange
        var fakeUsers = new List<User>();
        var fakeTotal = 0;

        var emptyResult = new PagedResult<User>
        {
            Items = fakeUsers,
            TotalCount = fakeTotal,
            Page = 1,
            PageSize = 20
        };

        _multiResultReaderMock
             .Setup(r => r.ReadAsync<User>())
             .ReturnsAsync(fakeUsers);

        _multiResultReaderMock
            .Setup(m => m.ReadSingleAsync<int>())
            .ReturnsAsync(fakeTotal);

        _dapperWrapperMock
            .Setup(d => d.QueryMultiple(_dbConnectionMock.Object, It.IsAny<string>(), It.IsAny<object>(),
                null, null, null))
            .Returns(_multiResultReaderMock.Object);

        // Act
        var result = _repository.Search(searchTerm: "abc");

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public void Search_Should_Apply_MaxPageSize()
    {
        // Arrange
        var fakeUsers = new List<User>();
        var fakeTotal = 0;

        var emptyResult = new PagedResult<User>
        {
            Items = fakeUsers,
            TotalCount = fakeTotal,
            Page = 1,
            PageSize = 20
        };

        _multiResultReaderMock
             .Setup(r => r.ReadAsync<User>())
             .ReturnsAsync(fakeUsers);

        _multiResultReaderMock
            .Setup(m => m.ReadSingleAsync<int>())
            .ReturnsAsync(fakeTotal);

        _dapperWrapperMock
            .Setup(d => d.QueryMultiple(_dbConnectionMock.Object, It.IsAny<string>(), It.IsAny<object>(),
                null, null, null))
            .Returns(_multiResultReaderMock.Object);

        // Act
        var result = _repository.Search(pageSize: PaginationDefaults.MaxPageSize + 1); // over limit

        // Assert
        result.PageSize.Should().BeLessOrEqualTo(PaginationDefaults.MaxPageSize); // your max page size cap
    }
}
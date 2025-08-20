using DapperRepository.Application.Constants;
using DapperRepository.Infrastructure.Repositories;
using DapperRepository.IntegrationTests.Shared.Fixtures;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace DapperRepository.IntegrationTests;

public class UserRepositoryIntegrationTests : IClassFixture<TestDatabaseFixture>
{
    public static IConfiguration Configuration
    {
        get
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
{ "ConnectionStrings:MasterConnection", "Server=(local);Database=master;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;" },
{ "ConnectionStrings:DefaultConnection", "Server=(local);Database=MyDatabase;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;" },
{ "Scripts:Folder", "Scripts" }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }
    }

    private readonly UserRepository _repository;

    public UserRepositoryIntegrationTests(TestDatabaseFixture fixture)
    {
        _repository = new UserRepository(new SqlConnectionFactory(Configuration), new DapperWrapper());
    }

    [Fact]
    public void Search_Should_Return_Seeded_Users()
    {
        // Act
        var result = _repository.Search(searchTerm: "Alice");

        // Assert
        result.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        result.Items.Should().Contain(u => u.Name.Contains("Alice"));
    }

    [Fact]
    public void Search_Should_Paginate_Results()
    {
        // Act
        var resultPage1 = _repository.Search(page: 1, pageSize: 2);
        var resultPage2 = _repository.Search(page: 2, pageSize: 2);

        // Assert
        resultPage1.Items.Should().NotBeEmpty();
        resultPage2.Items.Should().NotBeEmpty();
        resultPage1.Items.Should().NotIntersectWith(resultPage2.Items);
    }

    [Fact]
    public void Search_Should_Apply_MaxPageSize()
    {
        // Act
        var result = _repository.Search(page: 1, pageSize: PaginationDefaults.MaxPageSize + 1); // Exceeding max page size

        // Assert
        result.PageSize.Should().Be(PaginationDefaults.MaxPageSize); // Assuming max page size is set to 1_000_000 in the repository
        result.Items.Should().HaveCountLessOrEqualTo(PaginationDefaults.MaxPageSize);
    }

    [Fact]
    public void GetAll_Should_Return_All_Users()
    {
        // Act
        var users = _repository.GetAll();

        // Assert
        users.Should().NotBeEmpty();
    }

    [Fact]
    public void GetById_Should_Return_User_When_Exists()
    {
        // Arrange
        var userId = 1; // Assuming this user exists in the seeded data

        // Act
        var user = _repository.GetById(userId);

        // Assert
        user.Should().NotBeNull();
        user?.Id.Should().Be(userId);
    }

    [Fact]
    public void GetById_Should_Return_Null_When_Not_Exists()
    {
        // Act
        var user = _repository.GetById(999_999_999); // Non-existent ID

        // Assert
        user.Should().BeNull();
    }
}
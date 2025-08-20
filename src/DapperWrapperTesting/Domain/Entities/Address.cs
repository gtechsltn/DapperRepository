
namespace DapperWrapperTesting.Domain.Entities;

public class Address
{
    public int Id { get; set; }
    public string City { get; set; } = string.Empty;
    public string? Street { get; set; }
    public string? ZipCode { get; set; }
}

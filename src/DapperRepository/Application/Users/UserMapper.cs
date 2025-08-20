using DapperRepository.Application.Dtos;
using DapperRepository.Domain.Entities;

namespace DapperRepository.Application.Users;

public static class UserMapper
{
    public static UserDto ToDto(User user) => new UserDto
    {
        Id = user.Id,
        Email = user.Email,
        Name = user.Name,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    public static User ToEntity(UserDto dto) => new User
    {
        Id = dto.Id,
        Email = dto.Email,
        Name = dto.Name,
        CreatedAt = dto.CreatedAt,
        UpdatedAt = dto.UpdatedAt
    };
}

namespace MediatorPoC.Models;

public record User(Guid Id, string Name, string Email);

public record UserDto(Guid Id, string Name, string Email);

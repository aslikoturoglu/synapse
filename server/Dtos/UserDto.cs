namespace Server.Dtos;

public class UserDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required string Surname { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required string JobTitle { get; set; }
    public required string Role { get; set; }
}

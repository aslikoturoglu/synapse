namespace Server.Dtos;

public class GroupDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
}

public class CreateGroupRequest
{
    public required string Name { get; set; }
}

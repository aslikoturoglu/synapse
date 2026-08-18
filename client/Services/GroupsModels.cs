namespace Client.Services;

public class GroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class CreateGroupRequest
{
    public required string Name { get; set; }
}

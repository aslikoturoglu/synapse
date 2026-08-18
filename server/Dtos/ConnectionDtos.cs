namespace Server.Dtos;

public class ConnectionDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required string Surname { get; set; }
    public string JobTitle { get; set; } = "";
}

public class FollowResponse
{
    public bool Following { get; set; }
}

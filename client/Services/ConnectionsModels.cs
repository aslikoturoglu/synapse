namespace Client.Services;

public class ConnectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Surname { get; set; } = "";
    public string JobTitle { get; set; } = "";

    public string FullName => $"{Name} {Surname}";
}

public class FollowResponse
{
    public bool Following { get; set; }
}

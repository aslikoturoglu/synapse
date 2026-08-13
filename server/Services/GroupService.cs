using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Dtos;
using Server.Models;

namespace Server.Services;

public enum GroupOpResult { Success, Conflict }

public class GroupService(AppDbContext db)
{
    public async Task<List<GroupDto>> GetMineAsync(int userId) =>
        await db.Groups.Where(g => g.OwnerId == userId)
            .OrderBy(g => g.Name)
            .Select(g => new GroupDto { Id = g.Id, Name = g.Name })
            .ToListAsync();

    public async Task<(GroupOpResult Result, GroupDto? Group)> CreateAsync(int userId, string name)
    {
        var trimmed = name.Trim();
        if (await db.Groups.AnyAsync(g => g.OwnerId == userId && g.Name == trimmed))
            return (GroupOpResult.Conflict, null);

        var group = new Group { Name = trimmed, OwnerId = userId };
        db.Groups.Add(group);
        await db.SaveChangesAsync();

        return (GroupOpResult.Success, new GroupDto { Id = group.Id, Name = group.Name });
    }
}

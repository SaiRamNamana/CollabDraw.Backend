using Microsoft.EntityFrameworkCore;
using CollabDraw.Backend.Models;

namespace CollabDraw.Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<RoomUpdate> RoomUpdates => Set<RoomUpdate>();
}
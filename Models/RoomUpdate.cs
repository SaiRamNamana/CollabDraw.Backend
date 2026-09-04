namespace CollabDraw.Backend.Models;

public class RoomUpdate
{
    public int Id { get; set; }
    public string RoomId { get; set; } = default!;
    public byte[] UpdateData { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
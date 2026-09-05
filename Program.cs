using CollabDraw.Backend.Hubs;
using Microsoft.EntityFrameworkCore;
using CollabDraw.Backend.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins("http://localhost:4200", "http://localhost:53001", "https://collab-draw-frontend-lake.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
    
var app = builder.Build();

app.UseCors("AngularDev");
app.MapHub<DrawingHub>("/hubs/drawing");

app.Run();
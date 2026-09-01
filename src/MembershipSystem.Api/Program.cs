using MembershipSystem.Adapters;
using MembershipSystem.Api;
using MembershipSystem.UseCases;
using MembershipSystem.UseCases.Ports;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDirectory);
var dbPath = Path.Combine(dataDirectory, "app.db");
var photosDirectory = Path.Combine(dataDirectory, "photos");

builder.Services.AddDbContext<MembershipDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<IMemberRepository, EfMemberRepository>();
builder.Services.AddScoped<ISportRepository, EfSportRepository>();
builder.Services.AddScoped<IBranchRepository, EfBranchRepository>();
builder.Services.AddSingleton<IPhotoStore>(new LocalDiskPhotoStore(photosDirectory));

builder.Services.AddScoped<MemberUseCases>();
builder.Services.AddScoped<SportUseCases>();
builder.Services.AddScoped<BranchUseCases>();

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer<PhotoUploadOperationTransformer>();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MembershipDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "MembershipSystem.Api v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program;

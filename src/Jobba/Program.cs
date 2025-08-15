using Jobba.Data;
using Jobba.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// EF Core + SQLite
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Jobba")));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Basic health
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Applications endpoints
var group = app.MapGroup("/applications");

// GET all
group.MapGet("/", async ( AppDbContext db, ApplicationStatus? status, string? sortBy, string? company, int page = 1, int pageSize = 10) =>
{
    var q = db.Applications.AsQueryable();

    if (status.HasValue)
        q = q.Where(a => a.Status == status);

    if (!string.IsNullOrWhiteSpace(company))
        q = q.Where(a => a.Company.Contains(company));

    q = sortBy switch
    {
        "company" => q.OrderBy(a => a.Company),
        "role" => q.OrderBy(a => a.Role),
        "date" => q.OrderByDescending(a => a.AppliedAt),
        _ => q.OrderByDescending(a => a.Id)
    };

    var total = await q.CountAsync();
    var results = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    return Results.Ok(new { total, page, pageSize, results });
});


// GET one
group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
    await db.Applications.FindAsync(id) is { } a ? Results.Ok(a) : Results.NotFound());

// POST create
group.MapPost("/", async (JobApplication input, AppDbContext db) =>
{
    if (input.Company.Length < 1)
        return Results.BadRequest("Company is a required field.");

    if (input.Role.Length < 1)
        return Results.BadRequest("Role is a required field.");

    input.Id = 0;
    db.Applications.Add(input);
    await db.SaveChangesAsync();

    Log.Information($"Created application {input.Id} for {input.Company}");

    return Results.Created($"/applications/{input.Id}", input);
});

// PUT update
group.MapPut("/{id:int}", async (int id, JobApplication update, AppDbContext db) =>
{
    var existing = await db.Applications.FindAsync(id);
    if (existing is null) return Results.NotFound();

    existing.Company = update.Company;
    existing.Role = update.Role;
    existing.Source = update.Source;
    existing.Status = update.Status;
    existing.Location = update.Location;
    existing.SalaryRange = update.SalaryRange;
    existing.AppliedAt = update.AppliedAt;
    existing.NextAction = update.NextAction;
    existing.Notes = update.Notes;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

// DELETE
group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
{
    var existing = await db.Applications.FindAsync(id);
    if (existing is null) return Results.NotFound();
    db.Applications.Remove(existing);
    await db.SaveChangesAsync();
    return Results.NoContent();
});


// ACTIVITIES
var activityGroup = app.MapGroup("/applications/{applicationId:int}/activities");

// GET activities for an application
activityGroup.MapGet("/", async (int applicationId, AppDbContext db) =>
{
    var appEntity = await db.Applications.FindAsync(applicationId);
    if (appEntity is null) return Results.NotFound();

    var activities = await db.Activities
        .Where(a => a.JobApplicationId == applicationId)
        .OrderByDescending(a => a.Timestamp)
        .ToListAsync();

    return Results.Ok(activities);
});

// POST activity
activityGroup.MapPost("/", async (int applicationId, Activity input, AppDbContext db) =>
{
    var appEntity = await db.Applications.FindAsync(applicationId);
    if (appEntity is null) return Results.NotFound();

    input.JobApplicationId = applicationId;
    db.Activities.Add(input);
    await db.SaveChangesAsync();

    return Results.Created($"/applications/{applicationId}/activities/{input.Id}", input);
});


// Dev seed
await EnsureSeedAsync(app);

app.Run();

static async Task EnsureSeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (!await db.Applications.AnyAsync())
    {
        db.Applications.AddRange(
            new JobApplication { Company = "PartnerStack", Role = "T1 Support Agent", Status = ApplicationStatus.Applied, Source = "Posting", Location = "Remote", NextAction = "Follow-up Tue" },
            new JobApplication { Company = "Foundant", Role = "Junior Dev", Status = ApplicationStatus.Screening, Source = "Referral", Location = "Remote", NextAction = "Prep for call" },
            new JobApplication { Company = "Local MSP", Role = "IT Support", Status = ApplicationStatus.Draft, Source = "Indeed", Location = "Medicine Hat", Notes = "Tailor resume" }
        );
        await db.SaveChangesAsync();
    }
}

using Jobba.Data;
using Jobba.Models;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;
using Jobba.Contracts;
using Jobba.Validation;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// Basic rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("fixed", o =>
    {
        o.PermitLimit = 100;          // 100 requests
        o.Window = TimeSpan.FromMinutes(1); // per minute
        o.QueueLimit = 0;
        o.AutoReplenishment = true;
    });
});

// Exception handler
builder.Services.AddProblemDetails(); // built-in

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

app.UseRateLimiter();

// Setup custom exception response
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        // Let the framework produce a ProblemDetails payload
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem(
            title: "Unexpected error",
            statusCode: StatusCodes.Status500InternalServerError)
            .ExecuteAsync(ctx);
    });
});

app.UseSerilogRequestLogging(); // logs HTTP request info

// Basic health
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).RequireRateLimiting("fixed");

// Applications endpoints
var group = app.MapGroup("/applications").RequireRateLimiting("fixed");

// GET all
group.MapGet("/", async (
    AppDbContext db,
    ApplicationStatus? status,
    string? sortBy,
    string? company,
    int page = 1,
    int pageSize = 10
) =>
{
    var q = db.Applications.AsNoTracking().AsQueryable();

    if (status.HasValue) q = q.Where(a => a.Status == status);
    if (!string.IsNullOrWhiteSpace(company)) q = q.Where(a => a.Company.Contains(company));

    q = sortBy switch
    {
        "company" => q.OrderBy(a => a.Company),
        "role" => q.OrderBy(a => a.Role),
        "date" => q.OrderByDescending(a => a.AppliedAt),
        _ => q.OrderByDescending(a => a.Id)
    };

    var total = await q.CountAsync();

    var results = await q
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(a => new Jobba.Contracts.JobApplicationListItemDto
        {
            Id = a.Id,
            Company = a.Company,
            Role = a.Role,
            Status = a.Status,
            AppliedAt = a.AppliedAt
        })
        .ToListAsync();

    return Results.Ok(new { total, page, pageSize, results });
}).RequireRateLimiting("fixed");


// GET one
group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
    await db.Applications.FindAsync(id) is { } a ? Results.Ok(a) : Results.NotFound()).RequireRateLimiting("fixed");

// POST create
group.MapPost("/", async (JobApplicationCreateDto input, AppDbContext db) =>
{
    var (ok, message) = input.Validate();
    if (!ok) return Results.Problem(title: "Validation failed", detail: message, statusCode: 400);

    var entity = input.ToEntity();
    db.Applications.Add(entity);
    await db.SaveChangesAsync();

    return Results.Created($"/applications/{entity.Id}", new { entity.Id });
}).RequireRateLimiting("fixed");

// PUT update
group.MapPut("/{id:int}", async (int id, JobApplicationUpdateDto input, AppDbContext db) =>
{
    var (ok, message) = input.Validate();
    if (!ok) return Results.Problem(title: "Validation failed", detail: message, statusCode: 400);

    var existing = await db.Applications.FindAsync(id);
    if (existing is null) return Results.NotFound();

    existing.Apply(input);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireRateLimiting("fixed");

// DELETE
group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
{
    var existing = await db.Applications.FindAsync(id);
    if (existing is null) return Results.NotFound();
    db.Applications.Remove(existing);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireRateLimiting("fixed");


// ACTIVITIES
var activityGroup = app.MapGroup("/applications/{applicationId:int}/activities").RequireRateLimiting("fixed");

// GET activities for an application
activityGroup.MapGet("/", async (int applicationId, AppDbContext db) =>
{
    var exists = await db.Applications.AsNoTracking().AnyAsync(a => a.Id == applicationId);
    if (!exists) return Results.NotFound();

    var activities = await db.Activities
        .AsNoTracking()
        .Where(a => a.JobApplicationId == applicationId)
        .OrderByDescending(a => a.Timestamp)
        .Select(a => new ActivityDto
        {
            Id = a.Id,
            JobApplicationId = a.JobApplicationId,
            Timestamp = a.Timestamp,
            Type = a.Type,
            Details = a.Details
        })
        .ToListAsync();

    return Results.Ok(activities);
}).RequireRateLimiting("fixed");

// POST activity
activityGroup.MapPost("/", async (int applicationId, Jobba.Models.Activity input, AppDbContext db) =>
{
    var exists = await db.Applications.AsNoTracking().AnyAsync(a => a.Id == applicationId);
    if (!exists) return Results.NotFound();

    input.JobApplicationId = applicationId;
    db.Activities.Add(input);
    await db.SaveChangesAsync();

    var dto = new ActivityDto
    {
        Id = input.Id,
        JobApplicationId = input.JobApplicationId,
        Timestamp = input.Timestamp,
        Type = input.Type,
        Details = input.Details
    };

    return Results.Created($"/applications/{applicationId}/activities/{input.Id}", dto);
}).RequireRateLimiting("fixed");


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

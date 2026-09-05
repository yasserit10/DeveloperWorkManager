using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DeveloperWorkManager.Components;
using DeveloperWorkManager.Components.Account;
using DeveloperWorkManager.Components.Shared;
using DeveloperWorkManager.Data;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("/var/dwm-keys"))
        .SetApplicationName("DeveloperWorkManager");
}

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<DatePickerPopoverState>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;

        options.Password.RequiredLength = 1;
        options.Password.RequiredUniqueChars = 1;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseStaticFiles();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.MapGet("/api/notifications/unread", [Authorize] async (ClaimsPrincipal user, IDbContextFactory<ApplicationDbContext> dbFactory) =>
{
    var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(currentUserId)) return Results.Forbid();

    await using var db = await dbFactory.CreateDbContextAsync();
    var notifications = await db.Notifications.AsNoTracking()
        .Where(x => x.UserId == currentUserId && x.ReadAt == null)
        .OrderByDescending(x => x.CreatedAt)
        .Take(20)
        .Select(x => new { x.Id, x.Title, x.Message, x.TargetUrl, x.CreatedAt })
        .ToListAsync();
    return Results.Ok(notifications);
});

app.MapGet("/reports/export", [Authorize(Roles = "UnitManager,ReportViewer")] async (int? projectId, string? search, DateOnly? from, DateOnly? to, IDbContextFactory<ApplicationDbContext> dbFactory) =>
{
    var startDate = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-29));
    var endDate = to ?? DateOnly.FromDateTime(DateTime.Today);
    if (endDate < startDate) return Results.BadRequest("تاريخ النهاية يجب أن يكون بعد تاريخ البداية.");

    var start = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    var end = endDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    await using var db = await dbFactory.CreateDbContextAsync();
    var updatesQuery = db.WorkUpdates.AsNoTracking()
        .Include(x => x.WorkItem)
        .Include(x => x.CreatedBy)
        .Where(x => x.CreatedAt >= start && x.CreatedAt < end);
    if (projectId.GetValueOrDefault() > 0) updatesQuery = updatesQuery.Where(x => x.WorkItem.ProjectId == projectId);
    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        updatesQuery = updatesQuery.Where(x => x.WorkItem.Title.Contains(term) || x.Summary.Contains(term) || x.CreatedBy.FullName.Contains(term));
    }
    var updates = await updatesQuery.OrderByDescending(x => x.CreatedAt).ToListAsync();

    var statesQuery = db.WorkStateEntries.AsNoTracking()
        .Include(x => x.WorkItem)
        .Include(x => x.CreatedBy)
        .Where(x => x.CreatedAt >= start && x.CreatedAt < end);
    if (projectId.GetValueOrDefault() > 0) statesQuery = statesQuery.Where(x => x.ProjectId == projectId);
    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        statesQuery = statesQuery.Where(x => x.WorkItem.Title.Contains(term) || x.CreatedBy.FullName.Contains(term));
    }
    var states = await statesQuery.OrderByDescending(x => x.CreatedAt).ToListAsync();

    var activities = new List<ReportCsvActivity>();
    activities.AddRange(updates.Select(update => new ReportCsvActivity(
        update.CreatedAt,
        string.IsNullOrWhiteSpace(update.CreatedBy.FullName) ? update.CreatedBy.UserName ?? string.Empty : update.CreatedBy.FullName,
        update.WorkItem.Title,
        update.Summary,
        update.HoursSpent,
        update.BlockerReason,
        "تحديث")));
    activities.AddRange(states.Select(state => new ReportCsvActivity(
        state.CreatedAt,
        string.IsNullOrWhiteSpace(state.CreatedBy.FullName) ? state.CreatedBy.UserName ?? string.Empty : state.CreatedBy.FullName,
        state.WorkItem.Title,
        WorkStateText(state.Status),
        null,
        null,
        "تغيير حالة")));

    var csv = new System.Text.StringBuilder("\uFEFFالتاريخ,عضو الفريق,المهمة,نوع النشاط,الملخص,الساعات,العائق\r\n");
    foreach (var activity in activities.OrderByDescending(x => x.CreatedAt))
    {
        csv.Append(CsvCell(activity.CreatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"))).Append(',')
            .Append(CsvCell(activity.MemberName)).Append(',')
            .Append(CsvCell(activity.WorkItemTitle)).Append(',')
            .Append(CsvCell(activity.Type)).Append(',')
            .Append(CsvCell(activity.Summary)).Append(',')
            .Append(activity.HoursSpent?.ToString("0.##") ?? string.Empty).Append(',')
            .Append(CsvCell(activity.BlockerReason)).AppendLine();
    }

    return Results.File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"team-report-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.csv");
});

app.MapGet("/reports/project-pdf", [Authorize(Roles = "UnitManager,ReportViewer")] async (int projectId, DateOnly? from, DateOnly? to, IDbContextFactory<ApplicationDbContext> dbFactory) =>
{
    var startDate = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-29));
    var endDate = to ?? DateOnly.FromDateTime(DateTime.Today);
    if (projectId <= 0 || endDate < startDate) return Results.BadRequest();
    var start = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    var end = endDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    await using var db = await dbFactory.CreateDbContextAsync();
    var project = await db.Projects.AsNoTracking().Include(x => x.AssignedDeveloper).Include(x => x.Developers).ThenInclude(x => x.Developer).SingleOrDefaultAsync(x => x.Id == projectId);
    if (project is null) return Results.NotFound();
    var workStates = await db.WorkStateEntries.AsNoTracking()
        .Include(x => x.CreatedBy)
        .Include(x => x.WorkItem)
        .Where(x => x.ProjectId == projectId && x.CreatedAt >= start && x.CreatedAt < end)
        .OrderBy(x => x.CreatedAt)
        .ToListAsync();
    var tasks = await db.WorkItems.AsNoTracking()
        .Include(x => x.AssignedTo)
        .Where(x => x.ProjectId == projectId)
        .OrderBy(x => x.CreatedAt)
        .ToListAsync();
    var workUpdates = await db.WorkUpdates.AsNoTracking()
        .Include(x => x.WorkItem)
        .Include(x => x.CreatedBy)
        .Where(x => x.WorkItem.ProjectId == projectId && x.CreatedAt >= start && x.CreatedAt < end)
        .OrderBy(x => x.CreatedAt)
        .ToListAsync();
    var projectDeveloperIds = project.Developers.Select(x => x.DeveloperId).ToHashSet();
    if (projectDeveloperIds.Count == 0 && !string.IsNullOrWhiteSpace(project.AssignedDeveloperId)) projectDeveloperIds.Add(project.AssignedDeveloperId);
    var achievements = await db.Achievements.AsNoTracking()
        .Include(x => x.CreatedBy)
        .Where(x => projectDeveloperIds.Contains(x.CreatedById) && x.AchievementDate >= startDate && x.AchievementDate <= endDate)
        .OrderBy(x => x.AchievementDate)
        .ToListAsync();
    var pdf = new ProjectReportPdf(project, workStates, tasks, workUpdates, achievements, startDate, endDate).GeneratePdf();
    return Results.File(pdf, "application/pdf", $"project-report-{project.Id}-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.pdf");
});

app.MapGet("/memo-requests/{id:int}/image", [Authorize] async (int id, ClaimsPrincipal user, IConfiguration configuration, IDbContextFactory<ApplicationDbContext> dbFactory) =>
{
    var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(currentUserId)) return Results.Forbid();

    await using var db = await dbFactory.CreateDbContextAsync();
    var memo = await db.MemoRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
    if (memo is null) return Results.NotFound();
    if (!user.IsInRole("UnitManager") && memo.AssignedToId != currentUserId) return Results.Forbid();

    var storagePath = configuration["Storage:MemoImagesPath"] ?? Path.Combine(app.Environment.ContentRootPath, "App_Data", "memo-images");
    var filePath = Path.Combine(storagePath, memo.ImageFileName);
    if (!File.Exists(filePath)) return Results.NotFound();
    return Results.File(filePath, "image/webp", enableRangeProcessing: true);
});

// Roles are created on first start. Assign "UnitManager" only to the unit supervisor.
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    foreach (var roleName in new[] { "UnitManager", "Programmer", "ReportViewer" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var adminEmail = builder.Configuration["BootstrapAdmin:Email"];
    var adminPassword = builder.Configuration["BootstrapAdmin:Password"];
    var adminFullName = builder.Configuration["BootstrapAdmin:FullName"];

    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var administrator = await userManager.FindByEmailAsync(adminEmail);
        if (administrator is null)
        {
            administrator = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = string.IsNullOrWhiteSpace(adminFullName) ? "مسؤول الوحدة" : adminFullName
            };

            var result = await userManager.CreateAsync(administrator, adminPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Could not create the bootstrap administrator: {string.Join("; ", result.Errors.Select(x => x.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(administrator, "UnitManager"))
        {
            var result = await userManager.AddToRoleAsync(administrator, "UnitManager");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Could not assign the UnitManager role: {string.Join("; ", result.Errors.Select(x => x.Description))}");
            }
        }
    }

    var superAdminUserName = builder.Configuration["BootstrapSuperAdmin:UserName"];
    var superAdminPassword = builder.Configuration["BootstrapSuperAdmin:Password"];
    var superAdminFullName = builder.Configuration["BootstrapSuperAdmin:FullName"];

    if (!string.IsNullOrWhiteSpace(superAdminUserName) && !string.IsNullOrWhiteSpace(superAdminPassword))
    {
        var superAdministrator = await userManager.FindByNameAsync(superAdminUserName);
        if (superAdministrator is null)
        {
            superAdministrator = new ApplicationUser
            {
                UserName = superAdminUserName,
                Email = $"{superAdminUserName}@system.local",
                EmailConfirmed = true,
                FullName = string.IsNullOrWhiteSpace(superAdminFullName) ? "مدير النظام" : superAdminFullName
            };

            var result = await userManager.CreateAsync(superAdministrator, superAdminPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Could not create the bootstrap super administrator: {string.Join("; ", result.Errors.Select(x => x.Description))}");
            }
        }

        foreach (var roleName in new[] { "UnitManager", "Programmer", "ReportViewer" })
        {
            if (!await userManager.IsInRoleAsync(superAdministrator, roleName))
            {
                var result = await userManager.AddToRoleAsync(superAdministrator, roleName);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Could not assign {roleName} to the bootstrap super administrator: {string.Join("; ", result.Errors.Select(x => x.Description))}");
                }
            }
        }

        app.Logger.LogInformation("Bootstrap super administrator {UserName} ensured.", superAdminUserName);
    }
}

app.Run();

static string CsvCell(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";

static string WorkStateText(WorkActivityStatus status) => status switch
{
    WorkActivityStatus.WorkingNow => "تم تغيير حالة العمل إلى: أعمل الآن.",
    WorkActivityStatus.Paused => "تم تغيير حالة العمل إلى: متوقف.",
    WorkActivityStatus.Finished => "تم تغيير حالة العمل إلى: منتهي.",
    WorkActivityStatus.NotStarted => "تم تغيير حالة العمل إلى: لم يبدأ.",
    _ => "تم تغيير حالة العمل."
};

sealed record ReportCsvActivity(DateTime CreatedAt, string MemberName, string WorkItemTitle, string Summary, decimal? HoursSpent, string? BlockerReason, string Type);

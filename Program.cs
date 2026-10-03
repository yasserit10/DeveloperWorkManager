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
using ClosedXML.Excel;
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

app.MapGet("/reports/export", [Authorize(Roles = "UnitManager,ReportViewer")] async (int? projectId, string? search, DateOnly? from, DateOnly? to, string? scope, string? memberId, IDbContextFactory<ApplicationDbContext> dbFactory) =>
{
    var startDate = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-29));
    var endDate = to ?? DateOnly.FromDateTime(DateTime.Today);
    if (endDate < startDate) return Results.BadRequest("تاريخ النهاية يجب أن يكون بعد تاريخ البداية.");

    var start = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    var end = endDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    await using var db = await dbFactory.CreateDbContextAsync();
    var reportScope = scope?.ToLowerInvariant() switch
    {
        "member" => "member",
        "completed" => "completed",
        _ => "team"
    };
    var selectedProjectName = projectId.GetValueOrDefault() > 0
        ? await db.Projects.AsNoTracking().Where(x => x.Id == projectId).Select(x => x.Name).SingleOrDefaultAsync() ?? "المشروع المحدد"
        : "كل المشاريع";
    var searchTerm = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
    using var workbook = new XLWorkbook();

    if (reportScope == "member")
    {
        if (string.IsNullOrWhiteSpace(memberId)) return Results.BadRequest("اختر المبرمج قبل تصدير التقرير.");

        var memberName = await db.Users.AsNoTracking()
            .Where(x => x.Id == memberId)
            .Select(x => string.IsNullOrWhiteSpace(x.FullName) ? x.UserName ?? "المبرمج" : x.FullName)
            .SingleOrDefaultAsync();
        if (memberName is null) return Results.NotFound("المبرمج غير موجود.");

        var achievementsQuery = db.Achievements.AsNoTracking()
            .Include(x => x.SourceWorkItem)
                .ThenInclude(x => x!.Project)
            .Where(x => x.CreatedById == memberId && x.AchievementDate >= startDate && x.AchievementDate <= endDate);
        if (projectId.GetValueOrDefault() > 0) achievementsQuery = achievementsQuery.Where(x => x.SourceWorkItem != null && x.SourceWorkItem.ProjectId == projectId);
        if (searchTerm is not null) achievementsQuery = achievementsQuery.Where(x => x.Title.Contains(searchTerm) || x.Details.Contains(searchTerm));
        var achievements = await achievementsQuery.OrderByDescending(x => x.AchievementDate).ThenByDescending(x => x.CreatedAt).ToListAsync();

        var sheet = CreateExcelReportSheet(workbook, "منجزات مبرمج", memberName, selectedProjectName, startDate, endDate, ["المنجز", "التفاصيل", "المشروع", "تاريخ المنجز"]);
        var row = 5;
        foreach (var achievement in achievements)
        {
            sheet.Cell(row, 1).Value = achievement.Title;
            sheet.Cell(row, 2).Value = achievement.Details;
            sheet.Cell(row, 3).Value = achievement.SourceWorkItem?.Project.Name ?? "منجز يدوي";
            sheet.Cell(row, 4).Value = achievement.AchievementDate.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 4).Style.DateFormat.Format = "yyyy/mm/dd";
            row++;
        }
        sheet.Column(2).Width = 52;
        return ExcelFile(workbook, $"member-achievements-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.xlsx");
    }

    if (reportScope == "completed")
    {
        var tasksQuery = db.WorkItems.AsNoTracking()
            .Include(x => x.Project)
            .Include(x => x.AssignedTo)
            .Where(x => x.CompletedAt >= start && x.CompletedAt < end);
        if (projectId.GetValueOrDefault() > 0) tasksQuery = tasksQuery.Where(x => x.ProjectId == projectId);
        if (searchTerm is not null) tasksQuery = tasksQuery.Where(x => x.Title.Contains(searchTerm) || (x.Description ?? string.Empty).Contains(searchTerm) || x.AssignedTo.FullName.Contains(searchTerm));
        var tasks = await tasksQuery.OrderByDescending(x => x.CompletedAt).ToListAsync();

        var sheet = CreateExcelReportSheet(workbook, "المهام المكتملة", null, selectedProjectName, startDate, endDate, ["المهمة", "المشروع", "المسؤول", "تاريخ الإنجاز"]);
        var row = 5;
        foreach (var task in tasks)
        {
            sheet.Cell(row, 1).Value = task.Title;
            sheet.Cell(row, 2).Value = task.Project.Name;
            sheet.Cell(row, 3).Value = string.IsNullOrWhiteSpace(task.AssignedTo.FullName) ? task.AssignedTo.UserName ?? string.Empty : task.AssignedTo.FullName;
            sheet.Cell(row, 4).Value = task.CompletedAt!.Value.ToLocalTime();
            sheet.Cell(row, 4).Style.DateFormat.Format = "yyyy/mm/dd hh:mm";
            row++;
        }
        return ExcelFile(workbook, $"completed-tasks-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.xlsx");
    }

    var updatesQuery = db.WorkUpdates.AsNoTracking()
        .Include(x => x.WorkItem)
        .Include(x => x.CreatedBy)
        .Where(x => x.CreatedAt >= start && x.CreatedAt < end);
    if (projectId.GetValueOrDefault() > 0) updatesQuery = updatesQuery.Where(x => x.WorkItem.ProjectId == projectId);
    if (searchTerm is not null) updatesQuery = updatesQuery.Where(x => x.WorkItem.Title.Contains(searchTerm) || x.Summary.Contains(searchTerm) || x.CreatedBy.FullName.Contains(searchTerm));
    var updates = await updatesQuery.ToListAsync();

    var statesQuery = db.WorkStateEntries.AsNoTracking()
        .Include(x => x.WorkItem)
        .Include(x => x.CreatedBy)
        .Where(x => x.CreatedAt >= start && x.CreatedAt < end);
    if (projectId.GetValueOrDefault() > 0) statesQuery = statesQuery.Where(x => x.ProjectId == projectId);
    if (searchTerm is not null) statesQuery = statesQuery.Where(x => x.WorkItem.Title.Contains(searchTerm) || x.CreatedBy.FullName.Contains(searchTerm));
    var states = await statesQuery.ToListAsync();
    var activities = updates.Select(x => new ExcelTeamActivity(x.CreatedById, DisplayName(x.CreatedBy), x.WorkItemId, x.HoursSpent ?? 0, x.CreatedAt))
        .Concat(states.Select(x => new ExcelTeamActivity(x.CreatedById, DisplayName(x.CreatedBy), x.WorkItemId, 0, x.CreatedAt)));
    var teamRows = activities.GroupBy(x => new { x.MemberId, x.MemberName })
        .Select(x => new ExcelTeamSummary(x.Key.MemberName, x.Count(), x.Select(activity => activity.WorkItemId).Distinct().Count(), x.Sum(activity => activity.Hours), x.Max(activity => activity.CreatedAt)))
        .OrderByDescending(x => x.Hours)
        .ThenByDescending(x => x.ActivityCount)
        .ToList();

    var teamSheet = CreateExcelReportSheet(workbook, "إنجاز أعضاء الفريق", null, selectedProjectName, startDate, endDate, ["المبرمج", "الأنشطة", "المهام", "الساعات", "آخر نشاط"]);
    var teamRow = 5;
    foreach (var teamMember in teamRows)
    {
        teamSheet.Cell(teamRow, 1).Value = teamMember.MemberName;
        teamSheet.Cell(teamRow, 2).Value = teamMember.ActivityCount;
        teamSheet.Cell(teamRow, 3).Value = teamMember.WorkItemCount;
        teamSheet.Cell(teamRow, 4).Value = teamMember.Hours;
        teamSheet.Cell(teamRow, 5).Value = teamMember.LastActivity.ToLocalTime();
        teamSheet.Cell(teamRow, 5).Style.DateFormat.Format = "yyyy/mm/dd hh:mm";
        teamRow++;
    }
    return ExcelFile(workbook, $"team-achievements-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.xlsx");
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

static string DisplayName(ApplicationUser user) =>
    string.IsNullOrWhiteSpace(user.FullName) ? user.UserName ?? string.Empty : user.FullName;

static IXLWorksheet CreateExcelReportSheet(
    XLWorkbook workbook,
    string title,
    string? memberName,
    string projectName,
    DateOnly startDate,
    DateOnly endDate,
    string[] headers)
{
    var sheet = workbook.Worksheets.Add("التقرير");
    sheet.RightToLeft = true;

    var titleRange = sheet.Range(1, 1, 1, headers.Length);
    titleRange.Merge();
    titleRange.Value = title;
    titleRange.Style.Font.Bold = true;
    titleRange.Style.Font.FontSize = 16;
    titleRange.Style.Font.FontColor = XLColor.White;
    titleRange.Style.Fill.BackgroundColor = XLColor.FromHtml("2563EB");
    titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    sheet.Row(1).Height = 28;

    var detailsRange = sheet.Range(2, 1, 2, headers.Length);
    detailsRange.Merge();
    detailsRange.Value = $"الفترة: {startDate:yyyy/MM/dd} — {endDate:yyyy/MM/dd} | المشروع: {projectName}" + (string.IsNullOrWhiteSpace(memberName) ? string.Empty : $" | المبرمج: {memberName}");
    detailsRange.Style.Font.Bold = true;
    detailsRange.Style.Font.FontColor = XLColor.FromHtml("334155");
    detailsRange.Style.Fill.BackgroundColor = XLColor.FromHtml("EFF6FF");
    detailsRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    detailsRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    sheet.Row(2).Height = 23;

    for (var column = 0; column < headers.Length; column++)
    {
        var cell = sheet.Cell(4, column + 1);
        cell.Value = headers[column];
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontColor = XLColor.White;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("0F172A");
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    sheet.Row(4).Height = 23;
    sheet.SheetView.FreezeRows(4);
    sheet.Columns().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    sheet.Columns().Style.Alignment.WrapText = true;
    for (var column = 1; column <= headers.Length; column++) sheet.Column(column).Width = 22;
    return sheet;
}

static IResult ExcelFile(XLWorkbook workbook, string fileName)
{
    using (workbook)
    using (var stream = new MemoryStream())
    {
        workbook.SaveAs(stream);
        return Results.File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}

sealed record ExcelTeamActivity(string MemberId, string MemberName, int WorkItemId, decimal Hours, DateTime CreatedAt);
sealed record ExcelTeamSummary(string MemberName, int ActivityCount, int WorkItemCount, decimal Hours, DateTime LastActivity);

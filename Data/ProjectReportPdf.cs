using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DeveloperWorkManager.Data;

public sealed class ProjectReportPdf(
    WorkProject project,
    IReadOnlyList<WorkStateEntry> workStates,
    IReadOnlyList<WorkItem> tasks,
    IReadOnlyList<WorkUpdate> workUpdates,
    IReadOnlyList<Achievement> achievements,
    DateOnly from,
    DateOnly to) : IDocument
{
    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"تقرير مشروع {project.Name}",
        Author = "مدير العمل"
    };

    public void Compose(IDocumentContainer container)
    {
        var completedTasks = tasks.Count(x => x.Status == WorkStatus.Completed);
        var totalHours = workUpdates.Sum(x => x.HoursSpent ?? 0);
        var developerNames = project.Developers.Count == 0
            ? DisplayName(project.AssignedDeveloper)
            : string.Join("، ", project.Developers.OrderBy(x => x.Developer.FullName).Select(x => DisplayName(x.Developer)));

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.35f, Unit.Centimetre);
            page.DefaultTextStyle(style => style.FontFamily("Noto Naskh Arabic").FontSize(10.5f));
            page.Header().ContentFromRightToLeft().Column(header =>
            {
                header.Spacing(4);
                header.Item().Text("تقرير إنجاز المشروع").FontSize(23).Bold().FontColor(Colors.Blue.Darken3);
                header.Item().Text(project.Name).FontSize(17).Bold().FontColor(Colors.Grey.Darken4);
                header.Item().Text($"الفترة: {from:yyyy/MM/dd} — {to:yyyy/MM/dd}").FontColor(Colors.Grey.Darken1);
                if (!string.IsNullOrWhiteSpace(project.ProjectUrl)) header.Item().Text($"رابط المشروع: {project.ProjectUrl}").FontColor(Colors.Blue.Darken2);
                if (!string.IsNullOrWhiteSpace(project.LatestReleaseUrl)) header.Item().Text($"رابط آخر نسخة: {project.LatestReleaseUrl}").FontColor(Colors.Blue.Darken2);
                header.Item().Text($"فريق العمل: {developerNames}").FontColor(Colors.Grey.Darken1);
                if (!string.IsNullOrWhiteSpace(project.Description)) header.Item().Text(project.Description).FontColor(Colors.Grey.Darken1);
                header.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Blue.Lighten4);
            });

            page.Content().PaddingTop(12).ContentFromRightToLeft().Column(column =>
            {
                column.Spacing(12);
                column.Item().Row(row =>
                {
                    Metric(row.RelativeItem(), "حالات العمل", workStates.Count.ToString(), Colors.Blue.Medium);
                    Metric(row.RelativeItem(), "المهام المكتملة", $"{completedTasks}/{tasks.Count}", Colors.Green.Medium);
                    Metric(row.RelativeItem(), "الساعات المسجلة", totalHours.ToString("0.##"), Colors.Purple.Medium);
                    Metric(row.RelativeItem(), "منجزات الفريق", achievements.Count.ToString(), Colors.Orange.Medium);
                });

                SectionTitle(column.Item(), "سجل حالات العمل خلال الفترة");
                if (workStates.Count == 0)
                {
                    EmptyMessage(column.Item(), "لا توجد حالات عمل مسجلة ضمن هذه الفترة.");
                }
                else
                {
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(2);
                        });
                        table.Header(header =>
                        {
                            HeaderCell(header.Cell()).Text("المهمة");
                            HeaderCell(header.Cell()).Text("المبرمج");
                            HeaderCell(header.Cell()).Text("الحالة");
                            HeaderCell(header.Cell()).Text("وقت التسجيل");
                        });
                        foreach (var state in workStates)
                        {
                            BodyCell(table.Cell()).Text(state.WorkItem.Title);
                            BodyCell(table.Cell()).Text(DisplayName(state.CreatedBy));
                            BodyCell(table.Cell()).Text(WorkStateLabel(state.Status));
                            BodyCell(table.Cell()).Text(state.CreatedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm"));
                        }
                    });
                }

                SectionTitle(column.Item(), "المهام وحالة الإنجاز");
                if (tasks.Count == 0)
                {
                    EmptyMessage(column.Item(), "لا توجد مهام مرتبطة بهذا المشروع.");
                }
                else
                {
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1);
                        });
                        table.Header(header =>
                        {
                            HeaderCell(header.Cell()).Text("المهمة");
                            HeaderCell(header.Cell()).Text("المسؤول");
                            HeaderCell(header.Cell()).Text("الحالة");
                            HeaderCell(header.Cell()).Text("الإنجاز");
                        });
                        foreach (var task in tasks)
                        {
                            BodyCell(table.Cell()).Text(task.Title);
                            BodyCell(table.Cell()).Text(DisplayName(task.AssignedTo));
                            BodyCell(table.Cell()).Text(StatusLabel(task.Status));
                            BodyCell(table.Cell()).Text($"{task.CompletionPercent}%");
                        }
                    });
                }

                SectionTitle(column.Item(), "متابعة العمل والساعات المسجلة");
                if (workUpdates.Count == 0)
                {
                    EmptyMessage(column.Item(), "لا توجد تحديثات متابعة للمهام ضمن هذه الفترة.");
                }
                else
                {
                    foreach (var update in workUpdates)
                    {
                        column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(7).Column(item =>
                        {
                            item.Item().Text($"{update.WorkItem.Title} — {DisplayName(update.CreatedBy)}").Bold();
                            item.Item().Text(update.Summary).FontColor(Colors.Grey.Darken2);
                            item.Item().Text($"{update.CreatedAt.ToLocalTime():yyyy/MM/dd HH:mm}{(update.HoursSpent is null ? string.Empty : $" | {update.HoursSpent:0.##} ساعة")}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                    }
                }

                SectionTitle(column.Item(), "منجزات فريق المشروع");
                if (achievements.Count == 0)
                {
                    EmptyMessage(column.Item(), "لا توجد منجزات مسجلة لفريق المشروع خلال هذه الفترة.");
                }
                else
                {
                    foreach (var achievement in achievements)
                    {
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(item =>
                        {
                            item.Item().Text(achievement.Title).Bold();
                            item.Item().Text($"{DisplayName(achievement.CreatedBy)} — {achievement.AchievementDate:yyyy/MM/dd}").FontSize(9.5f).FontColor(Colors.Grey.Darken1);
                            item.Item().Text(achievement.Details).LineHeight(1.5f);
                        });
                    }
                }
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("مدير العمل — تقرير مولد بتاريخ ");
                text.Span(DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
                text.Span(" | صفحة ");
                text.CurrentPageNumber();
                text.Span(" من ");
                text.TotalPages();
            });
        });
    }

    private static void Metric(IContainer container, string label, string value, string color) => container
        .PaddingHorizontal(3).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Column(item =>
        {
            item.Spacing(2);
            item.Item().Text(label).FontSize(8.5f).FontColor(Colors.Grey.Darken1);
            item.Item().Text(value).FontSize(16).Bold().FontColor(color);
        });

    private static void SectionTitle(IContainer container, string title) => container
        .PaddingTop(3).BorderBottom(1).BorderColor(Colors.Blue.Lighten3).PaddingBottom(4).Text(title).FontSize(14).Bold().FontColor(Colors.Blue.Darken3);

    private static void EmptyMessage(IContainer container, string message) => container
        .Background(Colors.Grey.Lighten4).Padding(8).Text(message).FontColor(Colors.Grey.Darken1);

    private static IContainer HeaderCell(IContainer container) => container
        .Background(Colors.Blue.Lighten5).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignCenter();

    private static IContainer BodyCell(IContainer container) => container
        .Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).AlignCenter();

    private static string DisplayName(ApplicationUser? user) => string.IsNullOrWhiteSpace(user?.FullName) ? user?.UserName ?? "مستخدم" : user.FullName;

    private static string StatusLabel(WorkStatus status) => status switch
    {
        WorkStatus.New => "جديدة",
        WorkStatus.InProgress => "قيد التنفيذ",
        WorkStatus.WaitingForReview => "بانتظار المراجعة",
        WorkStatus.Completed => "مكتملة",
        WorkStatus.Blocked => "متوقفة",
        _ => "غير محددة"
    };

    private static string WorkStateLabel(WorkActivityStatus status) => status switch
    {
        WorkActivityStatus.NotStarted => "لم أبدأ بعد",
        WorkActivityStatus.WorkingNow => "أعمل الآن",
        WorkActivityStatus.Paused => "متوقف",
        WorkActivityStatus.Finished => "منتهي",
        _ => "غير محددة"
    };
}

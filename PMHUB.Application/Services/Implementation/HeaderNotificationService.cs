using PMHUB.Application.DTOs;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using PMHUB.Domain.Enums;
using PMHUB.Infrastructure.Repositories;

namespace PMHUB.Application.Services.Implementation
{
    public class HeaderNotificationService : IHeaderNotificationService
    {
        private const string Critical = "critical";
        private const string Warning = "warning";
        private const string Info = "info";

        private readonly IProjectRepository _projectRepository;
        private readonly IHourBookingReminderService _hourBookingReminderService;
        private readonly IRepository<HeaderNotificationState> _notificationStateRepository;

        public HeaderNotificationService(
            IProjectRepository projectRepository,
            IHourBookingReminderService hourBookingReminderService,
            IRepository<HeaderNotificationState> notificationStateRepository)
        {
            _projectRepository = projectRepository;
            _hourBookingReminderService = hourBookingReminderService;
            _notificationStateRepository = notificationStateRepository;
        }

        public async Task<HeaderNotificationSummaryDto> GetHeaderNotificationsAsync(
            int dueSoonDays = 14,
            int recentUpdatedDays = 7,
            int lowProgressThreshold = 70,
            int maxItems = 50,
            bool includeAdminNotifications = true,
            Guid? userId = null,
            CancellationToken cancellationToken = default)
        {
            dueSoonDays = Math.Max(dueSoonDays, 1);
            recentUpdatedDays = Math.Max(recentUpdatedDays, 1);
            lowProgressThreshold = Math.Max(0, Math.Min(lowProgressThreshold, 100));
            maxItems = Math.Max(maxItems, 1);

            var notifications = await BuildCurrentNotificationsAsync(
                dueSoonDays,
                recentUpdatedDays,
                lowProgressThreshold,
                includeAdminNotifications,
                cancellationToken);

            var visibleNotifications = userId.HasValue
                ? await ApplyUserStateAsync(userId.Value, notifications, cancellationToken)
                : notifications;

            var orderedItems = visibleNotifications
                .OrderBy(n => SeverityRank(n.Severity))
                .ThenBy(n => GroupRank(n.GroupKey))
                .ThenBy(n => n.OccurredAt ?? n.CreatedAt)
                .Take(maxItems)
                .ToList();

            return BuildSummary(orderedItems);
        }

        public async Task<HeaderNotificationStateChangeDto> MarkAllAsReadAsync(
            Guid userId,
            int dueSoonDays = 14,
            int recentUpdatedDays = 7,
            int lowProgressThreshold = 70,
            bool includeAdminNotifications = true,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var notifications = await BuildCurrentNotificationsAsync(
                Math.Max(dueSoonDays, 1),
                Math.Max(recentUpdatedDays, 1),
                Math.Max(0, Math.Min(lowProgressThreshold, 100)),
                includeAdminNotifications,
                cancellationToken);

            var affectedCount = await UpsertStatesAsync(
                userId,
                notifications.Select(n => n.Id),
                now,
                markRead: true,
                dismiss: false,
                cancellationToken);

            return new HeaderNotificationStateChangeDto
            {
                AffectedCount = affectedCount,
                UpdatedAt = now
            };
        }

        public async Task<HeaderNotificationStateChangeDto> ClearAllAsync(
            Guid userId,
            int dueSoonDays = 14,
            int recentUpdatedDays = 7,
            int lowProgressThreshold = 70,
            bool includeAdminNotifications = true,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var notifications = await BuildCurrentNotificationsAsync(
                Math.Max(dueSoonDays, 1),
                Math.Max(recentUpdatedDays, 1),
                Math.Max(0, Math.Min(lowProgressThreshold, 100)),
                includeAdminNotifications,
                cancellationToken);

            var affectedCount = await UpsertStatesAsync(
                userId,
                notifications.Select(n => n.Id),
                now,
                markRead: true,
                dismiss: true,
                cancellationToken);

            return new HeaderNotificationStateChangeDto
            {
                AffectedCount = affectedCount,
                UpdatedAt = now
            };
        }

        public async Task<HeaderNotificationStateChangeDto> ResetAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var states = (await _notificationStateRepository.FindAsync(state => state.UserId == userId)).ToList();

            foreach (var state in states)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _notificationStateRepository.Remove(state);
            }

            if (states.Count > 0)
            {
                await _notificationStateRepository.SaveChangesAsync();
            }

            return new HeaderNotificationStateChangeDto
            {
                AffectedCount = states.Count,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private async Task<List<HeaderNotificationDto>> BuildCurrentNotificationsAsync(
            int dueSoonDays,
            int recentUpdatedDays,
            int lowProgressThreshold,
            bool includeAdminNotifications,
            CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            var dueLimit = today.AddDays(dueSoonDays);
            var recentlyUpdatedSince = DateTime.UtcNow.AddDays(-recentUpdatedDays);
            var notifications = new List<HeaderNotificationDto>();

            cancellationToken.ThrowIfCancellationRequested();

            var projects = (await _projectRepository.FindForHeaderNotificationsAsync(p =>
                    p.Status != ProjectStatus.Done &&
                    ((p.EstimatedDueDate.HasValue && p.EstimatedDueDate.Value.Date <= dueLimit) ||
                     (p.UpdatedAt.HasValue && p.UpdatedAt.Value >= recentlyUpdatedSince)),
                    cancellationToken))
                .ToList();

            AddProjectDueNotifications(notifications, projects, today, lowProgressThreshold);
            AddRecentProjectUpdateNotifications(notifications, projects, recentlyUpdatedSince);
            AddRoadblockNotifications(notifications, projects, today, dueLimit);

            if (includeAdminNotifications)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await AddBookingNotificationsAsync(notifications, cancellationToken);
            }

            return notifications;
        }

        private async Task<List<HeaderNotificationDto>> ApplyUserStateAsync(
            Guid userId,
            IEnumerable<HeaderNotificationDto> notifications,
            CancellationToken cancellationToken)
        {
            var items = notifications.ToList();
            if (items.Count == 0)
            {
                return items;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var notificationIds = items.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
            var states = (await _notificationStateRepository.FindAsync(state => state.UserId == userId))
                .Where(state => notificationIds.Contains(state.NotificationId))
                .ToDictionary(state => state.NotificationId, StringComparer.Ordinal);

            return items
                .Where(item =>
                {
                    if (!states.TryGetValue(item.Id, out var state))
                    {
                        return true;
                    }

                    item.IsRead = state.IsRead;
                    return !state.IsDismissed;
                })
                .ToList();
        }

        private async Task<int> UpsertStatesAsync(
            Guid userId,
            IEnumerable<string> notificationIds,
            DateTime now,
            bool markRead,
            bool dismiss,
            CancellationToken cancellationToken)
        {
            var ids = notificationIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (ids.Count == 0)
            {
                return 0;
            }

            var existingStates = (await _notificationStateRepository.FindAsync(state => state.UserId == userId))
                .Where(state => ids.Contains(state.NotificationId, StringComparer.Ordinal))
                .ToDictionary(state => state.NotificationId, StringComparer.Ordinal);

            foreach (var notificationId in ids)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!existingStates.TryGetValue(notificationId, out var state))
                {
                    state = new HeaderNotificationState
                    {
                        UserId = userId,
                        NotificationId = notificationId,
                        CreatedAtUtc = now
                    };
                }

                if (markRead)
                {
                    state.IsRead = true;
                    state.ReadAtUtc ??= now;
                }

                if (dismiss)
                {
                    state.IsDismissed = true;
                    state.DismissedAtUtc ??= now;
                }

                state.UpdatedAtUtc = now;

                if (existingStates.ContainsKey(notificationId))
                {
                    _notificationStateRepository.Update(state);
                }
                else
                {
                    await _notificationStateRepository.AddAsync(state);
                }
            }

            await _notificationStateRepository.SaveChangesAsync();
            return ids.Count;
        }

        private static void AddProjectDueNotifications(
            ICollection<HeaderNotificationDto> notifications,
            IEnumerable<Project> projects,
            DateTime today,
            int lowProgressThreshold)
        {
            foreach (var project in projects.Where(p => p.EstimatedDueDate.HasValue))
            {
                var dueDate = project.EstimatedDueDate!.Value.Date;
                var daysUntilDue = (dueDate - today).Days;

                if (daysUntilDue < 0)
                {
                    notifications.Add(CreateProjectNotification(
                        project,
                        "ProjectOverdue",
                        Critical,
                        "overdue",
                        "Overdue",
                        "Project overdue",
                        $"{project.Name} is overdue by {Math.Abs(daysUntilDue)} day(s).",
                        dueDate,
                        new Dictionary<string, object?>
                        {
                            ["daysOverdue"] = Math.Abs(daysUntilDue),
                            ["estimatedDueDate"] = dueDate,
                            ["progressPercentage"] = project.ProgressPercentage
                        }));

                    continue;
                }

                notifications.Add(CreateProjectNotification(
                    project,
                    "ProjectDueSoon",
                    daysUntilDue <= 3 ? Critical : Warning,
                    ResolveDueGroupKey(dueDate, today),
                    ResolveDueGroupLabel(dueDate, today),
                    daysUntilDue == 0 ? "Project due today" : "Project due soon",
                    daysUntilDue == 0
                        ? $"{project.Name} is due today and is still {project.Status}."
                        : $"{project.Name} is due in {daysUntilDue} day(s) and is still {project.Status}.",
                    dueDate,
                    new Dictionary<string, object?>
                    {
                        ["daysUntilDue"] = daysUntilDue,
                        ["estimatedDueDate"] = dueDate,
                        ["progressPercentage"] = project.ProgressPercentage
                    }));

                if (project.ProgressPercentage < lowProgressThreshold)
                {
                    notifications.Add(CreateProjectNotification(
                        project,
                        "ProjectLowProgressNearDue",
                        Warning,
                        ResolveDueGroupKey(dueDate, today),
                        ResolveDueGroupLabel(dueDate, today),
                        "Low progress near due date",
                        $"{project.Name} is due in {daysUntilDue} day(s) with {project.ProgressPercentage}% progress.",
                        dueDate,
                        new Dictionary<string, object?>
                        {
                            ["daysUntilDue"] = daysUntilDue,
                            ["estimatedDueDate"] = dueDate,
                            ["progressPercentage"] = project.ProgressPercentage,
                            ["lowProgressThreshold"] = lowProgressThreshold
                        }));
                }
            }
        }

        private static void AddRecentProjectUpdateNotifications(
            ICollection<HeaderNotificationDto> notifications,
            IEnumerable<Project> projects,
            DateTime recentlyUpdatedSince)
        {
            foreach (var project in projects
                         .Where(p => p.UpdatedAt.HasValue && p.UpdatedAt.Value >= recentlyUpdatedSince)
                         .OrderByDescending(p => p.UpdatedAt)
                         .Take(10))
            {
                notifications.Add(CreateProjectNotification(
                    project,
                    "ProjectRecentlyUpdated",
                    Info,
                    "recent-updates",
                    "Recently updated",
                    "Project updated",
                    $"{project.Name} was updated recently.",
                    project.UpdatedAt,
                    new Dictionary<string, object?>
                    {
                        ["updatedAt"] = project.UpdatedAt,
                        ["status"] = project.Status.ToString(),
                        ["phase"] = project.Phase.ToString(),
                        ["progressPercentage"] = project.ProgressPercentage
                    }));
            }
        }

        private static void AddRoadblockNotifications(
            ICollection<HeaderNotificationDto> notifications,
            IEnumerable<Project> projects,
            DateTime today,
            DateTime dueLimit)
        {
            foreach (var project in projects)
            {
                foreach (var roadblock in (project.RoadblockEntries ?? Array.Empty<ProjectRoadblock>())
                             .Where(r => r.Status != RoadblockStatus.Resolved && r.DueAt.Date <= dueLimit))
                {
                    var daysUntilDue = (roadblock.DueAt.Date - today).Days;
                    var isOverdue = daysUntilDue < 0;

                    notifications.Add(new HeaderNotificationDto
                    {
                        Id = $"Roadblock:{roadblock.Id}",
                        Type = isOverdue ? "RoadblockOverdue" : "RoadblockDueSoon",
                        Severity = isOverdue ? Critical : Warning,
                        GroupKey = isOverdue ? "overdue" : ResolveDueGroupKey(roadblock.DueAt.Date, today),
                        GroupLabel = isOverdue ? "Overdue" : ResolveDueGroupLabel(roadblock.DueAt.Date, today),
                        Title = isOverdue ? "Roadblock overdue" : "Roadblock due soon",
                        Message = isOverdue
                            ? $"{roadblock.Title} on {project.Name} is overdue by {Math.Abs(daysUntilDue)} day(s)."
                            : $"{roadblock.Title} on {project.Name} is due in {daysUntilDue} day(s).",
                        CreatedAt = roadblock.CreatedAt,
                        OccurredAt = roadblock.DueAt,
                        TargetType = "Roadblock",
                        TargetId = roadblock.Id,
                        ProjectId = project.Id,
                        ActionUrl = $"/projects/{project.Id}?tab=roadblocks",
                        Metadata = new Dictionary<string, object?>
                        {
                            ["projectName"] = project.Name,
                            ["roadblockTitle"] = roadblock.Title,
                            ["roadblockStatus"] = roadblock.Status.ToString(),
                            ["dueAt"] = roadblock.DueAt,
                            ["daysUntilDue"] = daysUntilDue
                        }
                    });
                }
            }
        }

        private async Task AddBookingNotificationsAsync(
            ICollection<HeaderNotificationDto> notifications,
            CancellationToken cancellationToken)
        {
            var inactiveBookingNotifications = await _hourBookingReminderService.GetUsersWithoutRecentBookingsAsync(cancellationToken);
            foreach (var booking in inactiveBookingNotifications)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var targetType = string.IsNullOrWhiteSpace(booking.TargetType) ? "User" : booking.TargetType;
                var actionUrl = string.Equals(targetType, "Intern", StringComparison.OrdinalIgnoreCase)
                    ? $"/interns/{booking.UserId}"
                    : $"/users/{booking.UserId}?tab=hours";

                notifications.Add(new HeaderNotificationDto
                {
                    Id = $"NoBooking:{targetType}:{booking.UserId}",
                    Type = "NoRecentBooking",
                    Severity = booking.DaysWithoutBooking >= 45 ? Critical : Warning,
                    GroupKey = "booking-alerts",
                    GroupLabel = "Booking alerts",
                    Title = "No recent booking",
                    Message = booking.Message,
                    CreatedAt = DateTime.UtcNow,
                    OccurredAt = booking.LastBookingDate,
                    TargetType = targetType,
                    TargetId = booking.UserId,
                    ActionUrl = actionUrl,
                    Metadata = new Dictionary<string, object?>
                    {
                        ["firstName"] = booking.FirstName,
                        ["lastName"] = booking.LastName,
                        ["email"] = booking.Email,
                        ["lastBookingDate"] = booking.LastBookingDate,
                        ["daysWithoutBooking"] = booking.DaysWithoutBooking,
                         
                    }
                });
            }

            var monthlyTargetNotifications = await _hourBookingReminderService.GetUsersBelowMonthlyTargetAsync(cancellationToken: cancellationToken);
            foreach (var target in monthlyTargetNotifications)
            {
                cancellationToken.ThrowIfCancellationRequested();

                notifications.Add(new HeaderNotificationDto
                {
                    Id = $"MonthlyTarget:{target.Year}:{target.Month}:{target.UserId}",
                    Type = "MonthlyTargetBelowExpected",
                    Severity = target.CompletionRate < 50 ? Critical : Warning,
                    GroupKey = "booking-alerts",
                    GroupLabel = "Booking alerts",
                    Title = "Monthly target below expected",
                    Message = target.Message,
                    CreatedAt = DateTime.UtcNow,
                    TargetType = "User",
                    TargetId = target.UserId,
                    ActionUrl = $"/users/{target.UserId}?tab=hours&year={target.Year}&month={target.Month}",
                    Metadata = new Dictionary<string, object?>
                    {
                        ["firstName"] = target.FirstName,
                        ["lastName"] = target.LastName,
                        ["email"] = target.Email,
                        ["year"] = target.Year,
                        ["month"] = target.Month,
                        ["bookedHours"] = target.BookedHours,
                        ["targetHours"] = target.TargetHours,
                        ["missingHours"] = target.MissingHours,
                        ["completionRate"] = target.CompletionRate
                    }
                });
            }
        }

        private static HeaderNotificationDto CreateProjectNotification(
            Project project,
            string type,
            string severity,
            string groupKey,
            string groupLabel,
            string title,
            string message,
            DateTime? occurredAt,
            IDictionary<string, object?> metadata)
        {
            metadata["projectName"] = project.Name;
            metadata["status"] = project.Status.ToString();
            metadata["phase"] = project.Phase.ToString();
            metadata["projectManagerId"] = project.ProjectManagerId;
            metadata["projectManagerName"] = project.ProjectManager is null
                ? null
                : $"{project.ProjectManager.FirstName} {project.ProjectManager.LastName}".Trim();

            return new HeaderNotificationDto
            {
                Id = $"{type}:{project.Id}",
                Type = type,
                Severity = severity,
                GroupKey = groupKey,
                GroupLabel = groupLabel,
                Title = title,
                Message = message,
                CreatedAt = project.CreatedAt,
                OccurredAt = occurredAt,
                TargetType = "Project",
                TargetId = project.Id,
                ProjectId = project.Id,
                ActionUrl = $"/projects/{project.Id}",
                Metadata = metadata
            };
        }

        private static HeaderNotificationSummaryDto BuildSummary(ICollection<HeaderNotificationDto> items)
        {
            return new HeaderNotificationSummaryDto
            {
                TotalCount = items.Count,
                CriticalCount = items.Count(i => i.Severity == Critical),
                WarningCount = items.Count(i => i.Severity == Warning),
                InfoCount = items.Count(i => i.Severity == Info),
                GeneratedAt = DateTime.UtcNow,
                Groups = items
                    .GroupBy(i => new { i.GroupKey, i.GroupLabel })
                    .OrderBy(g => GroupRank(g.Key.GroupKey))
                    .Select(g => new HeaderNotificationGroupDto
                    {
                        Key = g.Key.GroupKey,
                        Label = g.Key.GroupLabel,
                        Count = g.Count(),
                        CriticalCount = g.Count(i => i.Severity == Critical),
                        WarningCount = g.Count(i => i.Severity == Warning),
                        InfoCount = g.Count(i => i.Severity == Info)
                    })
                    .ToList(),
                Items = items
            };
        }

        private static string ResolveDueGroupKey(DateTime dueDate, DateTime today)
        {
            if (dueDate < today)
            {
                return "overdue";
            }

            var currentWeekStart = GetWeekStart(today);
            var nextWeekStart = currentWeekStart.AddDays(7);
            var followingWeekStart = nextWeekStart.AddDays(7);

            if (dueDate < nextWeekStart)
            {
                return "due-this-week";
            }

            return dueDate < followingWeekStart ? "due-next-week" : "later";
        }

        private static string ResolveDueGroupLabel(DateTime dueDate, DateTime today)
        {
            return ResolveDueGroupKey(dueDate, today) switch
            {
                "overdue" => "Overdue",
                "due-this-week" => "Due this week",
                "due-next-week" => "Due next week",
                _ => "Later"
            };
        }

        private static DateTime GetWeekStart(DateTime date)
        {
            var daysSinceMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return date.Date.AddDays(-daysSinceMonday);
        }

        private static int SeverityRank(string severity) =>
            severity switch
            {
                Critical => 0,
                Warning => 1,
                _ => 2
            };

        private static int GroupRank(string groupKey) =>
            groupKey switch
            {
                "overdue" => 0,
                "due-this-week" => 1,
                "due-next-week" => 2,
                "booking-alerts" => 3,
                "recent-updates" => 4,
                "later" => 5,
                _ => 9
            };
    }
}

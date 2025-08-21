using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;

public interface IAppLogger
{
    Task LogActivityAsync(string? userId, string action, string description);
}

public class AppLogger : IAppLogger
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AppLogger> _logger;

    public AppLogger(ApplicationDbContext context, ILogger<AppLogger> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogActivityAsync(string? userId, string action, string description)
    {
        try
        {
            _context.tbl_AuditLogs.Add(new tbl_AuditLogs
            {
                UserId = userId,
                Action = action,
                Description = description,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit log saved: {Action} - {Description}", action, description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save audit log for action {Action}", action);
        }
    }
}

using MailCrafter.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace MailCrafter.MVC.Jobs;

[DisallowConcurrentExecution]
public class ProcessDueEmailsJob : IJob
{
    private readonly IEmailScheduleService _emailScheduleService;
    private readonly ILogger<ProcessDueEmailsJob> _logger;

    public ProcessDueEmailsJob(
        IEmailScheduleService emailScheduleService,
        ILogger<ProcessDueEmailsJob> logger)
    {
        _emailScheduleService = emailScheduleService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting scheduled job: ProcessDueEmailsJob");
        try
        {
            await _emailScheduleService.ProcessDueEmailsAsync();
            _logger.LogInformation("Successfully processed due email schedules");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing due email schedules");
        }
    }
} 
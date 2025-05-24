using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace MailCrafter.MVC.Jobs;

public static class QuartzJobConfiguration
{
    public static IServiceCollection ConfigureQuartzJobs(this IServiceCollection services)
    {
        // Configure Quartz
        services.AddQuartz(q =>
        {
            // Register ProcessDueEmailsJob
            q.AddJob<ProcessDueEmailsJob>(opts => opts.WithIdentity("ProcessDueEmailsJob"));

            // Create trigger for ProcessDueEmailsJob - run every minute
            q.AddTrigger(opts => opts
                .ForJob("ProcessDueEmailsJob")
                .WithIdentity("ProcessDueEmailsJob-Trigger")
                .WithCronSchedule("0 * * * * ?")); // Run every minute

            // Use a scoped job factory to ensure services like IEmailScheduleService are properly resolved
            q.UseMicrosoftDependencyInjectionJobFactory();
        });

        // Add the Quartz.NET hosted service
        services.AddQuartzHostedService(q => 
        {
            q.WaitForJobsToComplete = true;
        });

        return services;
    }
} 
using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Domain.Models;

namespace AdviseCare.API.BackgroundServices
{
    // Every 5 minutes, marks sent external referrals whose follow-up date has passed without an outcome as overdue
    public class ExternalReferralOverdueService : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ExternalReferralOverdueService> _logger;

        public ExternalReferralOverdueService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ExternalReferralOverdueService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        // Run the check until the API stops; a failed run is logged and retried at the next interval
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MarkOverdueReferralsAsync();
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to mark overdue external referrals.");
                }

                try
                {
                    await Task.Delay(CheckInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        // Helper: mark every due referral as overdue in one save
        private async Task MarkOverdueReferralsAsync()
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IGenericRepository<Externalreferral>>();
            var now = DateTime.UtcNow;
            var dueReferrals = await repository.SearchAsync(r =>
                !r.IsOverdue && r.Status == "sent" && r.FollowUpDueAt != null && r.FollowUpDueAt <= now);
            if (dueReferrals.Count == 0)
                return;

            foreach (var referral in dueReferrals)
                referral.IsOverdue = true;
            await repository.SaveAsync();
            _logger.LogInformation("Marked {Count} external referral(s) overdue.", dueReferrals.Count);
        }
    }
}

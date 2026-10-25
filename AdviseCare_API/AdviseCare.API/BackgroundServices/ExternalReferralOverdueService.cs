using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Domain.Models;

namespace AdviseCare.API.BackgroundServices
{
    public class ExternalReferralOverdueService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ExternalReferralOverdueService> logger) : BackgroundService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await MarkOverdueReferralsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to update overdue external referrals.");
                }

                try
                {
                    await Task.Delay(CheckInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task MarkOverdueReferralsAsync(CancellationToken cancellationToken)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IGenericRepository<Externalreferral>>();
            var now = DateTime.UtcNow;
            var dueReferrals = await repository.SearchAsync(referral =>
                !referral.IsOverdue &&
                referral.Status == "sent" &&
                referral.FollowUpDueAt.HasValue &&
                referral.FollowUpDueAt.Value <= now);

            if (dueReferrals.Count == 0)
                return;

            foreach (var referral in dueReferrals)
                referral.IsOverdue = true;

            cancellationToken.ThrowIfCancellationRequested();
            await repository.SaveAsync();
            logger.LogInformation("Marked {Count} external referrals overdue.", dueReferrals.Count);
        }
    }
}

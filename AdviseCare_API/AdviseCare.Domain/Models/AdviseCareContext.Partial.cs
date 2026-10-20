using Microsoft.EntityFrameworkCore;

namespace AdviseCare.Domain.Models;

// Hand-written additions to the scaffolded AdviseCareContext. Kept in its own file so re-scaffolding
// with EF Core Power Tools does not overwrite them.
public partial class AdviseCareContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // isEnabled has DEFAULT 1 in the database. Without this, EF skips "false" on insert (it is the CLR default)
        // and the database would store "true", so a new preference row could never start turned off.
        modelBuilder.Entity<Notificationpreference>()
            .Property(e => e.IsEnabled)
            .ValueGeneratedNever();
    }
}

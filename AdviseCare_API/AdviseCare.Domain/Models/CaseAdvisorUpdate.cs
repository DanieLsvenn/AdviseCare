#nullable disable
namespace AdviseCare.Domain.Models;

public partial class CaseAdvisorUpdate
{
    public Guid CaseAdvisorUpdateId { get; set; }

    public Guid AuthorId { get; set; }

    public Guid CaseId { get; set; }

    public string MessageText { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Author { get; set; }

    public virtual Case Case { get; set; }
}

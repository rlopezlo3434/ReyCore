using ReyplastCoreAPI.Shared.Helpers;

namespace ReyplastCoreAPI.Domain.Common;

public abstract class AuditableEntity
{
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateHelper.PeruNow();
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
}

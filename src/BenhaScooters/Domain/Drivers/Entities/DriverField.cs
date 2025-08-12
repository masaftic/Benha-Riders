using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using Vogen;

namespace BenhaScooters.Domain.Drivers.Entities;

[ValueObject<int>]
public partial struct DriverFieldId;


public enum FieldStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}


public class DriverField
{
    public DriverFieldId Id { get; private set; }
    public DriverId DriverId { get; private set; }
    public string Step { get; private set; }
    public string FieldName { get; private set; }
    public FieldStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    public Driver Driver { get; private set; } = null!;


    public DriverField(DriverId driverId, string step, string fieldName)
    {
        DriverId = driverId;
        Step = step;
        FieldName = fieldName;
        Status = FieldStatus.Pending;
    }

    public ErrorOr<Success> Approve()
    {
        if (Status != FieldStatus.Pending)
            return DriverErrors.FieldAlreadyReviewed;

        Status = FieldStatus.Approved;
        ReviewedAt = DateTime.UtcNow;

        return Result.Success;
    }

    public ErrorOr<Success> Reject(string reason)
    {
        if (Status != FieldStatus.Pending)
            return DriverErrors.FieldAlreadyReviewed;

        RejectionReason = reason;
        Status = FieldStatus.Rejected;
        ReviewedAt = DateTime.UtcNow;

        return Result.Success;
    }
}

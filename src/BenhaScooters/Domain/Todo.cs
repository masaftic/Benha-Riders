using Vogen;

namespace BenhaScooters.Domain;

[ValueObject<Guid>]
public partial struct TodoId;

[ValueObject<string>]
public partial struct TodoTitle;

[ValueObject<int>]
public partial struct TodoPriority
{
    private static Validation Validate(int input) => input switch
    {
        < 1 => Validation.Invalid("Priority must be at least 1"),
        > 5 => Validation.Invalid("Priority must be at most 5"),
        _ => Validation.Ok
    };
}

public class Todo
{
    public TodoId Id { get; set; }
    public TodoTitle Title { get; set; } = TodoTitle.From("Untitled");
    public TodoPriority Priority { get; set; } = TodoPriority.From(1);
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

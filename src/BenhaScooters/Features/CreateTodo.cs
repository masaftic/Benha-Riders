using BenhaScooters.Data;
using BenhaScooters.Domain;
using FastEndpoints;
using FluentValidation;

namespace BenhaScooters.Features;

public record CreateTodoRequest(string Title, int Priority);

public record TodoResponse(TodoId Id, TodoTitle Title, TodoPriority Priority, bool IsCompleted, DateTime CreatedAt);

public class CreateTodoRequestValidator : Validator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title must be at most 100 characters long.");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 5).WithMessage("Priority must be between 1 and 5.");
    }
}

public class CreateTodo(AppDbContext db) : Endpoint<CreateTodoRequest, TodoResponse>
{
    public override void Configure()
    {
        Post("/todos");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateTodoRequest req, CancellationToken ct)
    {
        var todo = new Todo
        {
            Id = TodoId.From(Guid.NewGuid()),
            Title = TodoTitle.From(req.Title),
            Priority = TodoPriority.From(req.Priority),
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        db.Todos.Add(todo);
        await db.SaveChangesAsync(ct);

        await SendOkAsync(new TodoResponse(todo.Id, todo.Title, todo.Priority, todo.IsCompleted, todo.CreatedAt), ct);
    }
}

using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using BenhaScooters.Shared.Localization;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record SetPreferredLanguageCommand(UserId UserId, string Language) : IRequest<ErrorOr<Success>>;

public class SetPreferredLanguageCommandValidator : AbstractValidator<SetPreferredLanguageCommand>
{
    public SetPreferredLanguageCommandValidator()
    {
        RuleFor(x => x.Language)
            .Must(AppLanguages.IsSupported)
            .WithMessage("Preferred language must be either 'en' or 'ar'.");
    }
}

public class SetPreferredLanguageCommandHandler : IRequestHandler<SetPreferredLanguageCommand, ErrorOr<Success>>
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public SetPreferredLanguageCommandHandler(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<ErrorOr<Success>> Handle(SetPreferredLanguageCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return AppErrors.User.NotFound();
        }

        user.SetPreferredLanguage(request.Language);
        await _db.SaveChangesAsync(cancellationToken);


        _cache.Remove($"PreferredLanguage-{user.Id}");

        return Result.Success;
    }
}

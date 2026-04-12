using FluentValidation;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Search.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.Search.Commands;

public sealed record AddBookmarkCommand(Guid UserId, Guid EnvironmentId, ResourceType ResourceType, string ResourceId, string DisplayName);

public sealed class AddBookmarkCommandValidator : AbstractValidator<AddBookmarkCommand>
{
    public AddBookmarkCommandValidator()
    {
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
    }
}

public sealed class AddBookmarkCommandHandler(IBookmarkRepository repository) : IUseCase<AddBookmarkCommand, Guid>
{
    public async Task ExecuteAsync(AddBookmarkCommand request, IOutputPort<Guid> outputPort, CancellationToken cancellationToken)
    {
        var bookmark = Bookmark.Create(request.UserId, request.EnvironmentId, request.ResourceType, request.ResourceId, request.DisplayName);
        await repository.AddAsync(bookmark, cancellationToken);
        outputPort.Success(bookmark.Id);
    }
}

public sealed record RemoveBookmarkCommand(Guid UserId, Guid BookmarkId);

public sealed class RemoveBookmarkCommandHandler(IBookmarkRepository repository) : IUseCase<RemoveBookmarkCommand, Unit>
{
    public async Task ExecuteAsync(RemoveBookmarkCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        var bookmark = await repository.GetByIdAsync(request.BookmarkId, cancellationToken);
        if (bookmark is null || bookmark.UserId != request.UserId)
        {
            outputPort.Success(Unit.Value);
            return;
        }

        await repository.RemoveAsync(request.BookmarkId, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

public sealed record SetPreferenceCommand(Guid UserId, string Key, string Value);

public sealed class SetPreferenceCommandValidator : AbstractValidator<SetPreferenceCommand>
{
    public SetPreferenceCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100);
    }
}

public sealed class SetPreferenceCommandHandler(IUserPreferenceRepository repository) : IUseCase<SetPreferenceCommand, Unit>
{
    public async Task ExecuteAsync(SetPreferenceCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        var existing = await repository.GetAsync(request.UserId, request.Key, cancellationToken);
        if (existing is not null)
        {
            existing.UpdateValue(request.Value);
            await repository.UpsertAsync(existing, cancellationToken);
        }
        else
        {
            var pref = UserPreference.Create(request.UserId, request.Key, request.Value);
            await repository.UpsertAsync(pref, cancellationToken);
        }
        outputPort.Success(Unit.Value);
    }
}

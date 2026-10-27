using FluentValidation;
using StudShare.Api.DTos;

namespace StudShare.Api.Validators;

public class CreateFolderRequestValidator : AbstractValidator<CreateFolderRequest>
{
    public CreateFolderRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.ParentId)
            .Must(id => id is null || Guid.TryParse(id, out _))
            .WithMessage("ParentId must be a valid GUID.")
            .When(x => x.ParentId is not null);
    }
}

public class RenameFolderRequestValidator : AbstractValidator<RenameFolderRequest>
{
    public RenameFolderRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);
    }
}

public class RenameFileRequestValidator : AbstractValidator<RenameFileRequest>
{
    public RenameFileRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);
    }
}

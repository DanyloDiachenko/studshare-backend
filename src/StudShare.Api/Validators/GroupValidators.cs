using FluentValidation;
using StudShare.Api.DTos;

namespace StudShare.Api.Validators;

public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Faculty)
            .NotEmpty()
            .MaximumLength(200);
    }
}

public class UpdateGroupRequestValidator : AbstractValidator<UpdateGroupRequest>
{
    public UpdateGroupRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Faculty)
            .NotEmpty()
            .MaximumLength(200);
    }
}

public class DeleteGroupRequestValidator : AbstractValidator<DeleteGroupRequest>
{
    public DeleteGroupRequestValidator()
    {
        RuleFor(x => x.ConfirmName)
            .NotEmpty();
    }
}

using FluentValidation;
using StudShare.Api.DTos;

namespace StudShare.Api.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.MiddleName)
            .MaximumLength(100)
            .When(x => x.MiddleName is not null);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100);

        RuleFor(x => x.RepeatPassword)
            .NotEmpty()
            .Equal(x => x.NewPassword)
            .WithMessage("Passwords do not match.");
    }
}

public class DeleteProfileRequestValidator : AbstractValidator<DeleteProfileRequest>
{
    public DeleteProfileRequestValidator()
    {
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class UpdateMemberRequestValidator : AbstractValidator<UpdateMemberRequest>
{
    public UpdateMemberRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.MiddleName)
            .MaximumLength(100)
            .When(x => x.MiddleName is not null);
    }
}

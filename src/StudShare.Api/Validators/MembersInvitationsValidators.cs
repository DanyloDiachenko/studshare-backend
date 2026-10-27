using FluentValidation;
using StudShare.Api.DTos;

namespace StudShare.Api.Validators;

public class InviteUserRequestValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}

public class SetUploadAccessRequestValidator : AbstractValidator<SetUploadAccessRequest>
{
    public SetUploadAccessRequestValidator()
    {
        RuleFor(x => x.UploadAccess)
            .NotNull();
    }
}

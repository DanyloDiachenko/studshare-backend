using StudShare.Api.DTos;
using StudShare.Api.Validators;
using Xunit;

namespace StudShare.UnitTests.Unit;

public class AuthValidatorsTests
{
    private readonly SignUpRequestValidator _signUpValidator = new();
    private readonly SignInRequestValidator _signInValidator = new();
    private readonly ChangePasswordRequestValidator _changePasswordValidator = new();

    [Fact]
    public void SignUpRequest_ValidData_ShouldPassValidation()
    {
        var req = new SignUpRequest("student@kpi.ua", "StrongPassword123!", "Іван", "Петренко", "Олегович");
        var result = _signUpValidator.Validate(req);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    [InlineData("   ")]
    public void SignUpRequest_InvalidEmail_ShouldFail(string email)
    {
        var req = new SignUpRequest(email, "StrongPassword123!", "Іван", "Петренко", null);
        var result = _signUpValidator.Validate(req);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SignUpRequest.Email));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    [InlineData("")]
    public void SignUpRequest_ShortOrEmptyPassword_ShouldFail(string password)
    {
        var req = new SignUpRequest("test@kpi.ua", password, "Іван", "Петренко", null);
        var result = _signUpValidator.Validate(req);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SignUpRequest.Password));
    }

    [Fact]
    public void SignUpRequest_EmptyFirstNameOrLastName_ShouldFail()
    {
        var req = new SignUpRequest("test@kpi.ua", "StrongPassword123!", "", "", null);
        var result = _signUpValidator.Validate(req);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SignUpRequest.FirstName));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SignUpRequest.LastName));
    }

    [Fact]
    public void SignInRequest_ValidData_ShouldPass()
    {
        var req = new SignInRequest("student@kpi.ua", "Secret123!");
        var result = _signInValidator.Validate(req);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void SignInRequest_EmptyFields_ShouldFail()
    {
        var req = new SignInRequest("", "");
        var result = _signInValidator.Validate(req);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ChangePasswordRequest_MismatchedPasswords_ShouldFail()
    {
        var req = new ChangePasswordRequest("OldPassword123!", "NewPassword123!", "DifferentPassword123!");
        var result = _changePasswordValidator.Validate(req);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Passwords do not match.");
    }

    [Fact]
    public void ChangePasswordRequest_ValidData_ShouldPass()
    {
        var req = new ChangePasswordRequest("OldPassword123!", "NewPassword123!", "NewPassword123!");
        var result = _changePasswordValidator.Validate(req);
        Assert.True(result.IsValid);
    }
}

using Api.Constants;
using Api.Extensions;
using Application.Dto;
using Application.Features.Auth.Commands.ChangePassword;
using Application.Features.Auth.Commands.Login;
using Application.Features.Auth.Commands.Logout;
using Application.Features.Auth.Commands.LogoutAll;
using Application.Features.Auth.Commands.Refresh;
using Application.Features.Auth.Commands.Register;
using Application.Features.Auth.Commands.ResendVerification;
using Application.Features.Auth.Commands.VerifyEmail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting(ApiConstants.RateLimits.Anonymous)]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.FirstName, request.LastName, request.Email, request.Password);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return StatusCode(StatusCodes.Status201Created, result.value);
    }

    [AllowAnonymous]
    [EnableRateLimiting(ApiConstants.RateLimits.Anonymous)]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return Ok(result.value);
    }

    [AllowAnonymous]
    [EnableRateLimiting(ApiConstants.RateLimits.Anonymous)]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return Ok(result.value);
    }

    [AllowAnonymous]
    [EnableRateLimiting(ApiConstants.RateLimits.Anonymous)]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var command = new LogoutCommand(request.RefreshToken);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new LogoutAllCommand(userId.Value);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }

    [AllowAnonymous]
    [EnableRateLimiting(ApiConstants.RateLimits.Anonymous)]
    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] VerifyEmailRequestDto request, CancellationToken cancellationToken)
    {
        var command = new VerifyEmailCommand(request.UserId, request.Token);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return Ok(new { message = "Your email has been verified. You can now log in." });
    }

    [AllowAnonymous]
    [EnableRateLimiting(ApiConstants.RateLimits.Anonymous)]
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(ResendVerificationRequestDto request, CancellationToken cancellationToken)
    {
        var command = new ResendVerificationCommand(request.Email);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return Ok(new { message = "If the account exists and is not verified yet, a new verification email has been sent." });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
            return Unauthorized();

        var command = new ChangePasswordCommand(userId.Value, request.CurrentPassword, request.NewPassword);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToProblemResult();

        return NoContent();
    }
}
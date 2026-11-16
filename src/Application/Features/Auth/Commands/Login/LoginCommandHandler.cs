using Application.Abstractions.Auth;
using Application.Constants;
using Application.Dto;
using Domain.Aggregates.LoginAudit;
using Domain.Aggregates.LoginAudit.Enums;
using Domain.Aggregates.RefreshToken;
using Domain.Aggregates.User;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Commands.Login;

public class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    ISecureTokenService secureTokenService,
    IRefreshTokenRepository refreshTokenRepository,
    ILoginAuditRepository loginAuditRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<LoginCommand, ResultT<TokenDto>>
{
    public async Task<ResultT<TokenDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await userRepository.GetByEmailAsync(email, cancellationToken);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash.Value))
        {
            await RecordFailedLoginAsync(user?.Id, email, LoginOutcome.InvalidCredentials, request, nowUtc, cancellationToken);

            return AuthCommandErrors.LoginErrors.InvalidCredentials;
        }

        if (!user.IsEmailVerified)
        {
            await RecordFailedLoginAsync(user.Id, email, LoginOutcome.EmailNotVerified, request, nowUtc, cancellationToken);

            return AuthCommandErrors.LoginErrors.EmailNotVerified;
        }

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user);

        var rawRefreshToken = secureTokenService.GenerateToken();
        var refreshTokenExpiresAtUtc = nowUtc.Add(AuthConstants.RefreshTokenLifetime);

        var refreshTokenResult = RefreshToken.Create(
            secureTokenService.Hash(rawRefreshToken),
            user.Id,
            refreshTokenExpiresAtUtc,
            nowUtc);

        if (refreshTokenResult.IsFailure)
            return refreshTokenResult.Error;

        await refreshTokenRepository.AddAsync(refreshTokenResult.value, cancellationToken);

        // Saved together with the refresh token, so a login is never recorded without it.
        await loginAuditRepository.AddAsync(
            LoginAudit.Create(user.Id, email, LoginOutcome.Succeeded, request.IpAddress, request.UserAgent, nowUtc),
            cancellationToken);

        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);

        if (saveResult.IsFailure)
            return saveResult.Error;

        return new TokenDto(
            accessToken.Token,
            rawRefreshToken,
            accessToken.ExpiresAtUtc,
            refreshTokenExpiresAtUtc);
    }

    /// <summary>
    /// A failed attempt has nothing else to save, so it is stored right away.
    /// If storing the audit row fails (UnitOfWork already logs it), the caller
    /// still gets the normal "invalid credentials" answer.
    /// </summary>
    private async Task RecordFailedLoginAsync(
        Guid? userId,
        string email,
        LoginOutcome outcome,
        LoginCommand request,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await loginAuditRepository.AddAsync(
            LoginAudit.Create(userId, email, outcome, request.IpAddress, request.UserAgent, nowUtc),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
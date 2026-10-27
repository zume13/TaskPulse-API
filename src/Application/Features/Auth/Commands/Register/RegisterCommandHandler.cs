using Application.Abstractions.Auth;
using Application.Dto;
using Domain.Aggregates.User;
using Domain.Aggregates.User.Enums;
using Domain.Shared.Base;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ISecureTokenService secureTokenService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<RegisterCommand, ResultT<UserDto>>
{
    public async Task<ResultT<UserDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
            return AuthCommandErrors.RegisterErrors.EmailAlreadyExists;

        var passwordHash = passwordHasher.Hash(request.Password);
        var verificationToken = secureTokenService.GenerateToken();
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var userResult = User.Create(
            email,
            passwordHash,
            request.FirstName,
            request.LastName,
            Role.Employee,
            verificationToken,
            nowUtc);

        if (userResult.IsFailure)
            return userResult.Error;

        var user = userResult.value;

        await userRepository.AddAsync(user, cancellationToken);

        var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);

        if (saveResult.IsFailure)
            return saveResult.Error;

        return new UserDto(
            user.Id,
            user.Email.Value,
            user.FirstName.Value,
            user.LastName.Value,
            user.Role,
            user.IsEmailVerified);
    }
}
using Application.Dto;
using Application.Features.Auth;
using Domain.Aggregates.User;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Users.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetCurrentUserQuery, ResultT<UserDto>>
{
    public async Task<ResultT<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return AuthErrors.UserNotFound;

        return new UserDto(
            user.Id,
            user.Email.Value,
            user.FirstName.Value,
            user.LastName.Value,
            user.Role,
            user.IsEmailVerified);
    }
}
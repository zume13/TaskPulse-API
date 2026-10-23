using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Users.Commands.UpdateProfile;

public record UpdateProfileCommand(Guid UserId, string FirstName, string LastName) : IRequest<ResultT<UserDto>>;
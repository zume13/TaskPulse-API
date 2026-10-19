using Application.Dto;
using MediatR;
using Domain.Shared.ResultPattern;

namespace Application.Features.Auth.Commands.Register;

public record RegisterCommand(string FirstName, string LastName, string Email, string Password)
    : IRequest<ResultT<UserDto>>;
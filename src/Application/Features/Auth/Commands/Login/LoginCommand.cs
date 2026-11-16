using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<ResultT<TokenDto>>;
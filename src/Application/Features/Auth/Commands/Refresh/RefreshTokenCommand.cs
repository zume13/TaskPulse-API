using Application.Dto;
using MediatR;
using Shared.ResultPattern;

namespace Application.Features.Auth.Commands.Refresh;

public record RefreshTokenCommand(string RefreshToken) : IRequest<ResultT<TokenDto>>;
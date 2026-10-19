using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Commands.Refresh;

public record RefreshTokenCommand(string RefreshToken) : IRequest<ResultT<TokenDto>>;
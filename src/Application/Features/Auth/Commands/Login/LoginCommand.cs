using Application.Dto;
using MediatR;
using Shared.ResultPattern;

namespace Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<ResultT<TokenDto>>;
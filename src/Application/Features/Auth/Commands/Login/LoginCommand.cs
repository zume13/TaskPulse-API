using Application.Dto;
using Domain.Shared.ResultPattern;
using MediatR;

namespace Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<ResultT<TokenDto>>;
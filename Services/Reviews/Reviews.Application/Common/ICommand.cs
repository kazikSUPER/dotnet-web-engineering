using MediatR;

namespace Bakery.Reviews.Application.Common;

public interface ICommand : IRequest<Unit>;

public interface ICommand<out TResponse> : IRequest<TResponse>;

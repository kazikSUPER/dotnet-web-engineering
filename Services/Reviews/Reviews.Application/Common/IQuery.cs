using MediatR;

namespace Bakery.Reviews.Application.Common;

public interface IQuery<out TResponse> : IRequest<TResponse>;

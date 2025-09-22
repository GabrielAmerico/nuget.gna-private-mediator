using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GNA.Private.Mediator.Interfaces
{
    public class PrivateMediator : IPrivateMediator
    {
        private readonly IServiceProvider _provider;

        public PrivateMediator(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
            var handler = _provider.GetService(handlerType);

            if(handler is null)
                throw new InvalidOperationException($"Handler for {request.GetType().Name} not found.");

            return await (Task<TResponse>)handlerType
                .GetMethod("Handle")
                .Invoke(handler, new object[] { request, cancellationToken });
        }

        public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notification.GetType());
            var handlers = _provider.GetServices(handlerType);

            if (handlers is null)
                throw new InvalidOperationException($"Handler for {notification.GetType().Name} not found.");

            foreach (var handler in handlers)
            {
                await (Task)handlerType
                    .GetMethod("Handle")
                    .Invoke(handler, new object[] { notification, cancellationToken });
            }
        }

    }
}

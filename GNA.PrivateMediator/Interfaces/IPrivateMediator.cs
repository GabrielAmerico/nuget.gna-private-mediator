using System.Threading;
using System.Threading.Tasks;

namespace GNA.Private.Mediator.Interfaces
{
    public interface IPrivateMediator
    { 
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification; 
    }
}

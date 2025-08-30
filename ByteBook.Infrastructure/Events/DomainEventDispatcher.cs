using ByteBook.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ByteBook.Infrastructure.Events;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Dispatching domain event: {EventType}", domainEvent.GetType().Name);

        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = _serviceProvider.GetServices(handlerType);

        var tasks = handlers.Select(handler => 
        {
            var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));
            return (Task)method!.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
        });

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogDebug("Successfully dispatched domain event: {EventType}", domainEvent.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching domain event: {EventType}", domainEvent.GetType().Name);
            throw;
        }
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var events = domainEvents.ToList();
        _logger.LogDebug("Dispatching {Count} domain events", events.Count);

        var tasks = events.Select(domainEvent => DispatchAsync(domainEvent, cancellationToken));
        
        try
        {
            await Task.WhenAll(tasks);
            _logger.LogDebug("Successfully dispatched all {Count} domain events", events.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching domain events");
            throw;
        }
    }
}
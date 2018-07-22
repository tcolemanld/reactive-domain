using System;

namespace ReactiveDomain.Messaging.Bus
{
    /// <inheritdoc cref="IQueuedHandler"/>
    /// <inheritdoc cref="IBus"/>
    public interface IDispatcher : IQueuedHandler, IBus
    {
       new string Name { get; }
    }
}

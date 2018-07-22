//using EventStore.Core.Services.Monitoring.Stats;

using System;

namespace ReactiveDomain.Messaging.Bus
{
    public interface IQueuedHandler: IHandle<Message>, IPublisher,IDisposable
    {
        string Name { get; }
        void Start();
        void Stop();
        void RequestStop();
        bool Idle { get; }
        
        //QueueStats GetStatistics();
    }
}
using System;

namespace ReactiveDomain.Messaging.Bus {

    /// <inheritdoc cref="IDispatcher"/>
    public class Dispatcher : IDispatcher {
        private readonly QueuedHandler _queue;
        private readonly InMemoryBus _bus;
        private bool _disposed;
        public Dispatcher(
                    string name,
                    bool watchSlowMsg = false,
                    TimeSpan? slowMsgThreshold = null) {
            if (slowMsgThreshold == null) {
                slowMsgThreshold = TimeSpan.FromMilliseconds(100);
            }
            _bus = new InMemoryBus(name, watchSlowMsg, slowMsgThreshold);
            _queue = new QueuedHandler(_bus, name, watchSlowMsg, slowMsgThreshold);
            _queue.Start();
        }

        public void Publish(Message message) => _queue.Publish(message);

        public IDisposable Subscribe<T>(IHandle<T> handler) where T : Message
            => _bus.Subscribe(handler);

        public void Unsubscribe<T>(IHandle<T> handler) where T : Message {
            _bus.Unsubscribe(handler);
        }

        public bool HasSubscriberFor<T>(bool includeDerived = false) where T : Message
            => _bus.HasSubscriberFor<T>(includeDerived);

        public string Name => _bus.Name;
        public bool Idle => _queue.Idle;
        public void Start() => _queue.Start();
        public void Stop() => _queue.Stop();
        public void RequestStop() => _queue.RequestStop();

        public void Handle(Message message) => _queue.Handle(message);
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing) {
            if (_disposed)
                return;
            _disposed = true;
            if (disposing) {
                _queue?.RequestStop();
                _bus?.Dispose();
            }
        }
    }
}

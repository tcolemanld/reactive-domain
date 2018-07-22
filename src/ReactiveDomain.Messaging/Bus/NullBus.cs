using System;

namespace ReactiveDomain.Messaging.Bus {
    /// <summary>
    /// A bus that is always null
    /// </summary>
    public class NullBus : IBus, IHandle<Message> {
        public NullBus(string name = "NullBus") { Name = name; }
        public string Name { get; }
        public void Publish(Message message) {/*null bus, just drop it*/}
        public void Handle(Message message) => Publish(message);
        public IDisposable Subscribe<T>(IHandle<T> handler) where T : Message {
            throw new InvalidOperationException("Cannot subscribe to a null bus");
        }
        public void Unsubscribe<T>(IHandle<T> handler) where T : Message {/*null bus, just drop it*/}
        public bool HasSubscriberFor<T>(bool includeDerived = false) where T : Message { return false; }

    }
}

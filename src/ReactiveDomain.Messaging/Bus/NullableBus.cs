using System;

namespace ReactiveDomain.Messaging.Bus
{
    /// <summary>
    /// A bus you can turn off.
    /// 
    /// Subscriptions and unsubscribe are always redirected to the target.
    /// 
    /// When RedirectToNull == true.
    /// Drops all messages published.
    /// 
    /// </summary>
    public class NullableBus : IBus, IHandle<Message>
    {
        private readonly IBus _target;
        public string Name { get; }
        

        public NullableBus(IBus target, bool directToNull = true, string name = null)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            Name = name ?? _target.Name;
            RedirectToNull = directToNull;
        }

        public bool RedirectToNull { get; set; }

        public void Publish(Message message)
        {
            if (RedirectToNull) return;
            _target.Publish(message);
        }
        public void Handle(Message message)  => Publish(message);

        public IDisposable Subscribe<T>(IHandle<T> handler) where T : Message
        {

            return _target.Subscribe(handler);
        }

        public void Unsubscribe<T>(IHandle<T> handler) where T : Message
        {

            _target.Unsubscribe(handler);
        }

        public bool HasSubscriberFor<T>(bool includeDerived = false) where T : Message
        {
            return _target.HasSubscriberFor<T>(includeDerived);
        }
    }
}

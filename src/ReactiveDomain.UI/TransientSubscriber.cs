using System;
using System.Collections.Generic;
using ReactiveDomain.Foundation.Commands;
using ReactiveDomain.Messaging;
using ReactiveDomain.Messaging.Bus;
using ReactiveDomain.Util;
using ReactiveUI;

// ReSharper disable once CheckNamespace
namespace ReactiveDomain.Foundation {
    public abstract class TransientSubscriber : ReactiveObject, IDisposable {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly IBus _bus;

        protected TransientSubscriber(IDispatcher dispatcher) : this((IBus)dispatcher) { }

        protected TransientSubscriber(IBus bus) {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        protected void Subscribe<T>(IHandle<T> handler) where T : Message {
            _subscriptions.Add(_bus.Subscribe(handler));
        }

        protected void Subscribe<T>(IHandleCommand<T> handler) where T : Command {
            var cmdHandler = new CommandHandler<T>(_bus, handler);
            var subscription = _bus.Subscribe(cmdHandler); 
            var disposer = new Disposer(
                () => {
                    subscription?.Dispose();
                    cmdHandler.Dispose();
                    return Unit.Default;
            });
            _subscriptions.Add(disposer);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool _disposed;
        protected virtual void Dispose(bool disposing) {
            if (_disposed)
                return;
            if (disposing) {
                _subscriptions?.ForEach(s => s.Dispose());
            }
            _disposed = true;
        }
    }
}


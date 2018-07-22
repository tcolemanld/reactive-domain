using System;
using ReactiveDomain.Logging;
using ReactiveDomain.Messaging.Bus;
using ReactiveDomain.Util;

namespace ReactiveDomain.Foundation.Commands {
    public sealed class CommandHandler<T> :
        IDisposable,
        IHandle<T> where T : Command {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly ILogger Log = LogManager.GetLogger("ReactiveDomain");
        private readonly IPublisher _bus;
        private readonly Func<T, CommandResponse> _handler;
        private readonly Guid _id;
        private readonly string _name;
        public CommandHandler(IPublisher returnBus, IHandleCommand<T> handler, string name = null) :
            this(returnBus, handler == null ? (Func<T, CommandResponse>)null : handler.Handle, name) {
        }
        public CommandHandler(IPublisher returnBus, Func<T, bool> handleFunc, string name = null) :
            this(returnBus, cmd => handleFunc(cmd) ? cmd.Succeed() : cmd.Failed(), name) {
            Ensure.NotNull(handleFunc, "handler");
        }
        public CommandHandler(IPublisher returnBus, Func<T, CommandResponse> handleFunc, string name = null) {
            Ensure.NotNull(returnBus, "returnBus");
            Ensure.NotNull(handleFunc, "handler");
            _bus = returnBus;
            _handler = handleFunc;
            _id = Guid.NewGuid();
            _name = string.IsNullOrWhiteSpace(name) ? $"{nameof(T)} handler" : name;
            _bus.Publish(new CommandHandlerRegistered(_name, _id, typeof(T).FullName));
        }

        public void Handle(T command) {
            if (_disposed) { return; }
            _bus.Publish(new CommandTracker.AckCommand(command.MsgId, command.GetType().FullName, _id));
            try {
                if (command.IsCanceled) {
                    _bus.Publish(command.Canceled());
                    return;
                }
                if (Log.LogLevel >= LogLevel.Debug)
                    Log.Debug("{0} command handled by {1}", command.GetType().Name, _handler.GetType().Name);
                _bus.Publish(_handler(command));
            }
            catch (Exception ex) {
                _bus.Publish(command.Failed(ex));
            }
        }

        private bool _disposed;
        public void Dispose() {
            _disposed = true;
            _bus?.Publish(new CommandHandlerUnregistered(_name, _id, typeof(T).FullName));
        }
    }
}
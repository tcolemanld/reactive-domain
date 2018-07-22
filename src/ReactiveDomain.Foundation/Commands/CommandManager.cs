using System;
using System.Collections.Concurrent;
using ReactiveDomain.Messaging.Bus;

namespace ReactiveDomain.Foundation.Commands {
    public sealed class CommandManager :
        ICommandSender,
        IHandle<CommandResponse>,
        IHandle<CommandTracker.AckCommand>,
        IHandle<CommandTracker.AckTimeout>,
        IHandle<CommandTracker.CompletionTimeout>,
        IHandle<CommandTracker.CommandComplete>,
        IDisposable {
        private readonly ConcurrentDictionary<Guid, CommandTracker> _commands;
        private readonly IBus _bus;
        private readonly TimeSource _timeSource;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly LaterService _laterService;
        private readonly InMemoryBus _timeoutBus;

        public CommandManager(IBus bus, TimeSource timeSource = null) {
            _bus = bus;
            _timeSource = timeSource ?? TimeSource.System;
            _bus.Subscribe<CommandTracker.AckCommand>(this);
            _bus.Subscribe<CommandResponse>(this);
            _bus.Subscribe<CommandTracker.CommandComplete>(this);

            _timeoutBus = new InMemoryBus(nameof(_timeoutBus), false);
            _laterService = new LaterService(_timeoutBus, TimeSource.System);
            // ReSharper disable once RedundantTypeArgumentsOfMethod
            _timeoutBus.Subscribe<DelaySendEnvelope>(_laterService);
            _timeoutBus.Subscribe<CommandTracker.AckTimeout>(this);
            _timeoutBus.Subscribe<CommandTracker.CompletionTimeout>(this);

            _laterService.Start();

            _commands = new ConcurrentDictionary<Guid, CommandTracker>();
        }
        public bool TrySend(Command command, out CommandResponse response, TimeSpan? responseTimeout = null,
                            TimeSpan? ackTimeout = null) {
            try {
                response = Send(command, true, responseTimeout, ackTimeout);
            }
            catch (Exception ex) {
                response = command.Failed(ex);
            }
            return response is Success;
        }
        public void Send(Command command, string exceptionMsg = null, TimeSpan? responseTimeout = null, TimeSpan? ackTimeout = null) {
            var response = Send(command, true, responseTimeout, ackTimeout);
            switch (response) {
                case Success _:
                    return;
                case Fail fail when fail.Exception != null:
                    throw new CommandException(exceptionMsg ?? fail.Exception.Message, fail.Exception, command.MsgId, command.GetType().FullName, Guid.Empty);
                default:
                    throw new CommandException(exceptionMsg ?? $"{command.GetType().Name}: Failed", command.MsgId, command.GetType().FullName, Guid.Empty);
            }
        }
        public void SendAsync(Command command, TimeSpan? responseTimeout = null, TimeSpan? ackTimeout = null) {
            Send(command, false, responseTimeout, ackTimeout);
        }
        private CommandResponse Send(
                    Command command,
                    bool blocking,
                    TimeSpan? responseTimeout = null,
                    TimeSpan? ackTimeout = null) {

            if (command.IsCanceled) { return command.Canceled(); }

            if (!_commands.TryGetValue(command.MsgId, out var tracker)) {
                tracker = new CommandTracker(
                                        command,
                                        _bus,
                                        _timeoutBus,
                                        responseTimeout ?? TimeSpan.FromMilliseconds(500),
                                        ackTimeout ?? TimeSpan.FromMilliseconds(100),
                                        _timeSource);
                _commands.AddOrUpdate(command.MsgId, id => tracker, (id, tr) => throw new InvalidOperationException("Already tracking Command"));
            }
            else { throw new InvalidOperationException("Already tracking Command"); }

            return tracker.Send(blocking);
        }


        public void Handle(CommandResponse message) {
            if (_commands.TryGetValue(message.MsgId, out var tracker)) {
                tracker.Handle(message);
            }
        }
        public void Handle(CommandTracker.AckCommand message) {
            if (_commands.TryGetValue(message.MsgId, out var tracker)) {
                tracker.Handle(message);
            }
        }
        public void Handle(CommandTracker.AckTimeout message) {
            if (_commands.TryGetValue(message.MsgId, out var tracker)) {
                tracker.Handle(message);
            }
        }
        public void Handle(CommandTracker.CompletionTimeout message) {
            if (_commands.TryGetValue(message.MsgId, out var tracker)) {
                tracker.Handle(message);
            }
        }

        public void Handle(CommandTracker.CommandComplete message) {
            _commands.TryRemove(message.MsgId, out var tracker);
            tracker.Dispose();
        }

        public void Dispose() {
            _laterService?.Dispose();
            _timeoutBus?.Dispose();
        }
    }
}

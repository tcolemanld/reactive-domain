using System;
using System.Collections.Concurrent;
using ReactiveDomain.Messaging.Bus;

namespace ReactiveDomain.Foundation.Commands {
    public sealed class CommandManager :
        ICommandSender,
        IHandle<CommandResponse>,
        IHandle<CommandTracker.CommandReceived>,
        IHandle<CommandTracker.CommandReceiptTimeoutExpired>,
        IHandle<CommandTracker.CommandCompletionTimeoutExpired>,
        IHandle<CommandTracker.CommandProcessingCompleted>,
        IDisposable {
        private readonly ConcurrentDictionary<Guid, CommandTracker> _commands;
        private readonly IBus _bus;
        private readonly ITimeSource _timeSource;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly LaterService _laterService;
        private readonly InMemoryBus _timeoutBus;

        public CommandManager(IBus bus, ITimeSource timeSource = null) {
            _bus = bus;
            _timeSource = timeSource ?? TimeSource.System;
            _bus.Subscribe<CommandTracker.CommandReceived>(this);
            _bus.Subscribe<CommandResponse>(this);
            _bus.Subscribe<CommandTracker.CommandProcessingCompleted>(this);

            _timeoutBus = new InMemoryBus(nameof(_timeoutBus), false);
            _laterService = new LaterService(_timeoutBus, TimeSource.System);
            // ReSharper disable once RedundantTypeArgumentsOfMethod
            _timeoutBus.Subscribe<DelaySendEnvelope>(_laterService);
            _timeoutBus.Subscribe<CommandTracker.CommandReceiptTimeoutExpired>(this);
            _timeoutBus.Subscribe<CommandTracker.CommandCompletionTimeoutExpired>(this);

            _laterService.Start();

            _commands = new ConcurrentDictionary<Guid, CommandTracker>();
        }
        public bool TrySend(Command command, 
                            out CommandResponse response,
                            TimeSpan? ackTimeout = null, 
                            TimeSpan? completionTimeout = null) {
            try {
                response = Send(command, true, ackTimeout, completionTimeout);
            }
            catch (Exception ex) {
                response = command.Failed(ex);
            }
            return response is Success;
        }
        public void Send(Command command, string exceptionMsg = null, TimeSpan? ackTimeout = null, TimeSpan? completionTimeout = null) {
            var response = Send(command, true, ackTimeout, completionTimeout);
            switch (response) {
                case Success _:
                    return;
                case Fail fail when fail.Exception != null:
                    throw new CommandException(exceptionMsg ?? fail.Exception.Message, fail.Exception, command.MsgId, command.GetType().FullName, Guid.Empty);
                default:
                    throw new CommandException(exceptionMsg ?? $"{command.GetType().Name}: Failed", command.MsgId, command.GetType().FullName, Guid.Empty);
            }
        }
        public void SendAsync(Command command, TimeSpan? ackTimeout = null, TimeSpan? completionTimeout = null) {
            Send(command, false, ackTimeout, completionTimeout);
        }
        private CommandResponse Send(
                    Command command,
                    bool blocking,
                    TimeSpan? ackTimeout = null,
                    TimeSpan? completionTimeout = null) {

            if (command.IsCanceled) { return command.Canceled(); }

            if (!_commands.TryGetValue(command.MsgId, out var tracker)) {
                tracker = new CommandTracker(
                                        command,
                                        _bus,
                                        _timeoutBus,
                                        ackTimeout ?? TimeSpan.FromMilliseconds(100),
                                        completionTimeout ?? TimeSpan.FromMilliseconds(500),
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
        public void Handle(CommandTracker.CommandReceived message) {
            if (_commands.TryGetValue(message.MsgId, out var tracker)) {
                tracker.Handle(message);
            }
        }
        public void Handle(CommandTracker.CommandReceiptTimeoutExpired message) {
            if (_commands.TryGetValue(message.MsgId, out var tracker)) {
                tracker.Handle(message);
            }
        }
        public void Handle(CommandTracker.CommandCompletionTimeoutExpired message) {
            if (_commands.TryGetValue(message.MsgId, out var tracker)) {
                tracker.Handle(message);
            }
        }

        public void Handle(CommandTracker.CommandProcessingCompleted message) {
            _commands.TryRemove(message.MsgId, out var tracker);
            tracker.Dispose();
        }

        public void Dispose() {
            _laterService?.Dispose();
            _timeoutBus?.Dispose();
        }
    }
}

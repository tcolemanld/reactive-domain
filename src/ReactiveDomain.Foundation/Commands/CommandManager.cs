using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using ReactiveDomain.Logging;
using ReactiveDomain.Messaging;
using ReactiveDomain.Messaging.Bus;

namespace ReactiveDomain.Foundation.Commands {
    public class CommandManager :
        QueuedSubscriber,
        IHandle<CommandResponse>,
        IHandle<CommandTracker.AckCommand>,
        IHandle<CommandTracker.AckTimeout>,
        IHandle<CommandTracker.CompletionTimeout> {
        private static readonly ILogger Log = LogManager.GetLogger("ReactiveDomain");
        private static readonly TimeSpan DefaultAckTimeout = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan DefaultResponseTimeout = TimeSpan.FromMilliseconds(500);
        private readonly IBus _outBus;
        private readonly IBus _timeoutBus;
        private readonly ConcurrentDictionary<Guid, IntegratedCommandTracker> _pendingCommands;
        private bool _disposed;

        public CommandManager(IBus bus, IBus timeoutBus) : base(bus) {
            _outBus = bus;
            _timeoutBus = timeoutBus;
            _pendingCommands = new ConcurrentDictionary<Guid, IntegratedCommandTracker>();
            Subscribe<CommandResponse>(this);
            Subscribe<CommandTracker.AckCommand>(this);
        }
        public TaskCompletionSource<CommandResponse> RegisterCommandAsync(
                                                                Command command,
                                                                TimeSpan? ackTimeout = null,
                                                                TimeSpan? responseTimeout = null) {
            if (_disposed) {
                throw new ObjectDisposedException(nameof(CommandManager));
            }

            if (Log.LogLevel >= LogLevel.Debug)
                Log.Debug("Registering command tracker for" + command.GetType().Name);
            if (_pendingCommands.ContainsKey(command.MsgId))
                throw new CommandException($"Command tracker already registered for this Command {command.GetType().Name} Id {command.MsgId}.", command.MsgId, command.GetType().FullName, Guid.Empty);

            var tcs = new TaskCompletionSource<CommandResponse>();
            var tracker = new IntegratedCommandTracker(
                                    command,
                                    tcs,
                                    () => {
                                        if (_pendingCommands.TryRemove(command.MsgId, out var tr))
                                            tr.Dispose();
                                    },
                                    () => {
                                        _outBus.Publish(new Canceled(command.MsgId, command.GetType().FullName, Guid.Empty, command.CorrelationId, new SourceId(command)));
                                        if (_pendingCommands.TryRemove(command.MsgId, out var tr))
                                            tr.Dispose();
                                    },
                                    ackTimeout ?? DefaultAckTimeout,
                                    responseTimeout ?? DefaultResponseTimeout,
                                    _timeoutBus);
            if (_pendingCommands.TryAdd(command.MsgId, tracker)) {
                return tcs;
            }
            //Add failed, cleanup & throw
            tracker.Dispose();
            tcs.SetResult(new Canceled(command.MsgId, command.GetType().FullName, Guid.Empty, command.CorrelationId, new SourceId(command)));
            tcs.SetCanceled();
            throw new CommandException($"Failed to register command tracker for this Command {command.GetType().Name} Id {command.MsgId}.", command.MsgId, command.GetType().FullName, Guid.Empty);

        }

        public void Handle(CommandResponse message) {
            _pendingCommands.TryGetValue(message.CommandId, out var tracker);
            tracker?.Handle(message);
        }

        public void Handle(CommandTracker.AckCommand message) {
            _pendingCommands.TryGetValue(message.CommandId, out var tracker);
            tracker?.Handle(message);
        }
        public void Handle(CommandTracker.AckTimeout message) {
            _pendingCommands.TryGetValue(message.CommandId, out var tracker);
            tracker?.Handle(message);
        }
        public void Handle(CommandTracker.CompletionTimeout message) {
            _pendingCommands.TryGetValue(message.CommandId, out var tracker);
            tracker?.Handle(message);
        }


        protected override void Dispose(bool disposing) {
            //n.b. we want to shutdown the queue in the base class before iterating through the trackers
            base.Dispose(disposing);

            if (_disposed)
                return;
            _disposed = true;
            if (!disposing) return;

            var trackers = _pendingCommands.Values.ToArray();
            for (var i = 0; i < trackers.Length; i++) {
                trackers[i]?.Dispose();
            }
        }
    }
}

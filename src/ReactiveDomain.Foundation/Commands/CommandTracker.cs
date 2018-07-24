using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ReactiveDomain.Messaging;
using ReactiveDomain.Messaging.Bus;
using ReactiveDomain.Util;

namespace ReactiveDomain.Foundation.Commands {
    public sealed class CommandTracker : EventDrivenStateMachine,
                                        IHandle<CommandResponse>,
                                        IHandle<CommandTracker.CommandReceived>,
                                        IHandle<CommandTracker.CommandReceiptTimeoutExpired>,
                                        IHandle<CommandTracker.CommandCompletionTimeoutExpired>,
                                        IDisposable {
        private readonly Command _command;
        private readonly IBus _publishBus;
        private readonly IBus _timeoutBus;
        private readonly TimeSpan _completionTimeout;
        private readonly TimeSpan _ackTimeout;
        private readonly ITimeSource _timeSource;
        private Guid? _handlerId;
        private TaskCompletionSource<CommandResponse> _tcs;
        private States _state;
        private enum States {
            Started,
            Sent,
            Acked,
            Completed,
            Disposed
        }
        private CommandTracker() {
            Register<Started>(_ => _state = States.Started);
            Register<Sent>(_ => _state = States.Sent);
            Register<Acked>(_ => _state = States.Acked);
            Register<Canceled>(_ => _state = States.Completed);
            Register<Completed>(_ => _state = States.Completed);
            Register<Failed>(_ => _state = States.Completed);
            Register<Disposed>(_ => _state = States.Disposed);
        }
        public CommandTracker(
                Command command,
                IBus publishBus,
                IBus timeoutBus,
                TimeSpan ackTimeout,
                TimeSpan completionTimeout,
                ITimeSource timeSource) : this() {
            Ensure.NotNull(command, nameof(command));
            Ensure.NotNull(publishBus, nameof(publishBus));
            Ensure.NotNull(timeoutBus, nameof(timeoutBus));
            _command = command;
            _publishBus = publishBus;
            _timeoutBus = timeoutBus;
            _completionTimeout = completionTimeout;
            _ackTimeout = ackTimeout;
            _timeSource = timeSource;
            Raise(new Started());
        }


        public CommandResponse Send(bool blocking = true) {
            switch (_state) {
                case States.Disposed:
                case States.Completed:
                case States.Acked:
                case States.Sent:
                    return FailCommand(new InvalidOperationException("Received Send sent message"));
                case States.Started:
                    if (!TryPublish(out var response)) { return FailCommand(response); }
                    Raise(new Sent());
                    if (!blocking) { return null; }
                    return Block();
                default:
                    return FailCommand(new InvalidOperationException($"Unknown State {_state}"));
            }
        }

        public void Handle(CommandReceived message) {
            switch (_state) {
                case States.Disposed:
                case States.Completed:
                case States.Acked:
                    return;
                case States.Started:
                    FailCommand(new InvalidOperationException("Received Ack on unsent message"));
                    break;
                case States.Sent:
                    _handlerId = message.HandlerId;
                    Raise(new Acked());
                    break;
                default:
                    FailCommand(new InvalidOperationException($"Unknown State {_state}"));
                    break;
            }
        }

        public void Handle(CommandResponse message) {
            switch (_state) {
                case States.Disposed:
                case States.Completed:
                    return;
                case States.Started:
                    FailCommand(new InvalidOperationException("Received response on unsent message"));
                    break;
                case States.Acked:
                case States.Sent:
                    _tcs.TrySetResult(message);
                    Raise(new Completed());
                    NotifyComplete();
                    break;
                default:
                    FailCommand(new InvalidOperationException($"Unknown State {_state}"));
                    break;
            }
        }

        public void Handle(CommandReceiptTimeoutExpired message) {
            switch (_state) {
                case States.Disposed:
                case States.Completed:
                case States.Acked:
                    return;
                case States.Started:
                    FailCommand(new InvalidOperationException("Received Ack Timeout on unsent message"));
                    break;
                case States.Sent:
                    CancelAck();
                    NotifyComplete();
                    Raise(new Canceled());
                    break;
                default:
                    FailCommand(new InvalidOperationException($"Unknown State {_state}"));
                    break;
            }
        }

        public void Handle(CommandCompletionTimeoutExpired message) {
            switch (_state) {
                case States.Disposed:
                case States.Completed:
                    return;
                case States.Started:
                    FailCommand(new InvalidOperationException("Received Timeout on unsent message"));
                    break;
                case States.Acked:
                case States.Sent:
                    CancelCompletion();
                    NotifyComplete();
                    Raise(new Canceled());
                    break;
                default:
                    FailCommand(new InvalidOperationException($"Unknown State {_state}"));
                    break;
            }
        }
        public void Dispose() {
            switch (_state) {
                case States.Disposed:
                    return;
                case States.Completed:
                    _tcs?.Task?.Dispose();
                    break;
                case States.Started:
                case States.Acked:
                case States.Sent:
                    Cancel(new ObjectDisposedException(nameof(CommandTracker)));
                    break;
            }
            Raise(new Disposed());
        }
        private CommandResponse FailCommand(Exception ex) {
            return FailCommand(_command.Failed(ex));
        }
        private CommandResponse FailCommand(CommandResponse response) {
            _tcs?.TrySetResult(response);
            Raise(new Failed());
            NotifyComplete();
            return response;
        }
        private CommandResponse Block() {
            _tcs = new TaskCompletionSource<CommandResponse>();
            try {
                //blocking caller until result is set 
                return _tcs.Task.Result;
            }
            catch (AggregateException aggEx) {
                Raise(new Failed());
                NotifyComplete();
                return _command.Failed(aggEx.InnerException);
            }
        }
        private bool TryPublish(out CommandResponse response) {
            response = null;
            try {
                //n.b. if this does not throw result will be set asynchronously 
                _publishBus.Publish(_command);
            }
            catch (Exception ex) {
                response = _command.Failed(ex);
                return false;
            }
            _timeoutBus.Publish(new DelaySendEnvelope(_timeSource, _ackTimeout, new CommandReceiptTimeoutExpired(_command.MsgId)));
            _timeoutBus.Publish(new DelaySendEnvelope(_timeSource, _completionTimeout, new CommandCompletionTimeoutExpired(_command.MsgId)));
            return true;
        }
        private void NotifyComplete() {
            _publishBus.Publish(new CommandProcessingCompleted(_command.MsgId));
        }
        private void Cancel(Exception ex = null) {
            _publishBus.Publish(new CommandCancellationRequested(_command.MsgId, _command.GetType().FullName, _handlerId));
            _tcs?.TrySetResult(_command.Canceled(_handlerId, ex));
        }

        private void CancelAck() {
            _publishBus.Publish(new CommandCancellationRequested(_command.MsgId, _command.GetType().FullName, _handlerId));
            _tcs?.TrySetResult(_command.CanceledAckTimeout(_handlerId, new TimeoutException()));
        }

        private void CancelCompletion() {
            _publishBus.Publish(new CommandCancellationRequested(_command.MsgId, _command.GetType().FullName, _handlerId));
            _tcs?.TrySetResult(_command.CanceledCompletionTimeout(_handlerId, new TimeoutException()));
        }



        #region Messages
        /// <summary>
        /// Indicates receipt of a command message at the command handler.
        /// Does not indicate success or failure of command processing.
        /// </summary>
        /// <inheritdoc cref="Message"/>
        public class CommandReceived : Message {
            /// <summary>
            /// MsgId of the Command being acked
            /// </summary>
            public readonly Guid CommandId;
            /// <summary>
            /// Full Type Name of the Command being acked
            /// </summary>
            public readonly string CommandFullName;
            /// <summary>
            /// Id of the Command Handler sending the ack
            /// </summary>
            public readonly Guid HandlerId;

            public CommandReceived(Command cmd, Guid handlerId):this(cmd.MsgId,cmd.GetType().FullName, handlerId) {}
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="commandId">MsgId of the Command being acked</param>
            /// <param name="commandFullName">Full Type Name of the Command being acked</param>
            /// <param name="handlerId">Id of the Command Handler sending the ack</param>
            [JsonConstructor]
            public CommandReceived(Guid commandId, string commandFullName, Guid handlerId) {
                CommandId = commandId;
                CommandFullName = commandFullName;
                HandlerId = handlerId;
            }
        }
        public class CommandReceiptTimeoutExpired : Message {
            public readonly Guid CommandId;
            public CommandReceiptTimeoutExpired(
                Guid commandId) {
                CommandId = commandId;
            }
        }

        public class CommandCompletionTimeoutExpired : Message {
            public readonly Guid CommandId;
            public CommandCompletionTimeoutExpired(
                Guid commandId) {
                CommandId = commandId;
            }
        }
        public class CommandProcessingCompleted : Message {
            public readonly Guid CommandId;
            public CommandProcessingCompleted(
                Guid commandId) {
                CommandId = commandId;
            }
        }
        public class CommandCancellationRequested : Message {
            public readonly Guid CommandId;
            public readonly string CommandFullName;
            public readonly Guid? HandlerId;
            public CommandCancellationRequested(
                Guid commandId,
                string commandFullName,
                Guid? handlerId) {
                CommandId = commandId;
                CommandFullName = commandFullName;
                HandlerId = handlerId;
            }
        }
        class Started : Message { }
        class Sent : Message { }
        class Acked : Message { }
        class Completed : Message { }
        class Canceled : Message { }
        class Failed : Message { }
        class Disposed : Message { }
        #endregion


    }
}


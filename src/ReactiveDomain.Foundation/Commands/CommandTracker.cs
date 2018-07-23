using System;
using System.Threading.Tasks;
using ReactiveDomain.Messaging;
using ReactiveDomain.Messaging.Bus;
using ReactiveDomain.Util;

namespace ReactiveDomain.Foundation.Commands {
    public sealed class CommandTracker : EventDrivenStateMachine,
                                        IHandle<CommandResponse>,
                                        IHandle<CommandTracker.AckCommand>,
                                        IHandle<CommandTracker.AckTimeout>,
                                        IHandle<CommandTracker.CompletionTimeout>,
                                        IDisposable {
        private readonly Command _command;
        private readonly IBus _publishBus;
        private readonly IBus _timeoutBus;
        private readonly TimeSpan _responseTimeout;
        private readonly TimeSpan _ackTimeout;
        private readonly TimeSource _timeSource;
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
                TimeSpan responseTimeout,
                TimeSpan ackTimeout,
                TimeSource timeSource) : this() {
            Ensure.NotNull(command, nameof(command));
            Ensure.NotNull(publishBus, nameof(publishBus));
            Ensure.NotNull(timeoutBus, nameof(timeoutBus));
            _command = command;
            _publishBus = publishBus;
            _timeoutBus = timeoutBus;
            _responseTimeout = responseTimeout;
            _ackTimeout = ackTimeout;
            _timeSource = timeSource;
            Raise(new Started());
        }


        public CommandResponse Send(bool blocking) {
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

        public void Handle(AckCommand message) {
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

        public void Handle(AckTimeout message) {
            switch (_state) {
                case States.Disposed:
                case States.Completed:
                case States.Acked:
                    return;
                case States.Started:
                    FailCommand(new InvalidOperationException("Received Ack Timeout on unsent message"));
                    break;
                case States.Sent:
                    Cancel();
                    NotifyComplete();
                    Raise(new Canceled());
                    break;
                default:
                    FailCommand(new InvalidOperationException($"Unknown State {_state}"));
                    break;
            }
        }

        public void Handle(CompletionTimeout message) {
            switch (_state) {
                case States.Disposed:
                case States.Completed:
                    return;
                case States.Started:
                    FailCommand(new InvalidOperationException("Received Timeout on unsent message"));
                    break;
                case States.Acked:
                case States.Sent:
                    Cancel();
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
                    //??? Hmm what should we do here?
                    _tcs?.Task?.Dispose();
                    break;
                case States.Started:
                case States.Acked:
                case States.Sent:
                    Cancel();
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
            _timeoutBus.Publish(new DelaySendEnvelope(_timeSource, _ackTimeout, new AckTimeout(_command.MsgId)));
            _timeoutBus.Publish(new DelaySendEnvelope(_timeSource, _responseTimeout, new CompletionTimeout(_command.MsgId)));
            return true;
        }
        private void NotifyComplete() {
            _publishBus.Publish(new CommandComplete(_command.MsgId));
        }
        private void Cancel() {
            _publishBus.Publish(new CommandCancelRequest(_command.MsgId, _command.GetType().FullName, _handlerId));
            _tcs?.TrySetResult(_command.Canceled());
        }

        

        #region Messages
        /// <summary>
        /// Indicates receipt of a command message at the command handler.
        /// Does not indicate success or failure of command processing.
        /// </summary>
        /// <inheritdoc cref="Message"/>
        public class AckCommand : Message {
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

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="commandId">MsgId of the Command being acked</param>
            /// <param name="commandFullName">Full Type Name of the Command being acked</param>
            /// <param name="handlerId">Id of the Command Handler sending the ack</param>
            public AckCommand(Guid commandId, string commandFullName, Guid handlerId) {
                CommandId = commandId;
                CommandFullName = commandFullName;
                HandlerId = handlerId;
            }
        }
        public class AckTimeout : Message {
            public readonly Guid CommandId;
            public AckTimeout(
                Guid commandId) {
                CommandId = commandId;
            }
        }

        public class CompletionTimeout : Message {
            public readonly Guid CommandId;
            public CompletionTimeout(
                Guid commandId) {
                CommandId = commandId;
            }
        }
        public class CommandComplete : Message {
            public readonly Guid CommandId;
            public CommandComplete(
                Guid commandId) {
                CommandId = commandId;
            }
        }
        public class CommandCancelRequest : Message {
            public readonly Guid CommandId;
            public readonly string CommandFullName;
            public readonly Guid? HandlerId;
            public CommandCancelRequest(
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


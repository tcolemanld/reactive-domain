using System;
using System.Threading.Tasks;
using ReactiveDomain.Messaging;
using ReactiveDomain.Messaging.Bus;
using ReactiveDomain.Util;

namespace ReactiveDomain.Foundation.Commands {
    public sealed class CommandTracker :
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

        public CommandTracker(
                Command command,
                IBus publishBus,
                IBus timeoutBus,
                TimeSpan responseTimeout,
                TimeSpan ackTimeout,
                TimeSource timeSource) {
            Ensure.NotNull(command, nameof(command));
            Ensure.NotNull(publishBus, nameof(publishBus));
            Ensure.NotNull(timeoutBus, nameof(timeoutBus));
            _command = command;
            _publishBus = publishBus;
            _timeoutBus = timeoutBus;
            _responseTimeout = responseTimeout;
            _ackTimeout = ackTimeout;
            _timeSource = timeSource;
            _state = States.Started;
        }

        //todo turn this into a process manager
        public CommandResponse Send(bool blocking) {
            if (_state != States.Started) { throw new InvalidOperationException(); }
            try {
                //n.b. if this does not throw result will be set asynchronously 
                _publishBus.Publish(_command);
            }
            catch (Exception ex) {
                _state = States.Completed;
                _timeoutBus.Publish(new CommandComplete(_command.MsgId));
                return _command.Failed(ex);
            }
            _timeoutBus.Publish(new DelaySendEnvelope(_timeSource, _ackTimeout, new AckTimeout(_command.MsgId)));
            _timeoutBus.Publish(new DelaySendEnvelope(_timeSource, _responseTimeout, new CompletionTimeout(_command.MsgId)));
            if (!blocking) { return null; }
            _tcs = new TaskCompletionSource<CommandResponse>();
            try {
                //blocking caller until result is set 
                var rslt = _tcs.Task.Result;
                _state = States.Completed;
                _timeoutBus.Publish(new CommandComplete(_command.MsgId));
                return rslt;
            }
            catch (AggregateException aggEx) {
                _state = States.Completed;
                _timeoutBus.Publish(new CommandComplete(_command.MsgId));
                return _command.Failed(aggEx.InnerException);
            }

        }

        public void Handle(AckCommand message) {
            if (_state == States.Sent) { _state = States.Acked; }

            _handlerId = message.HandlerId;
        }

        public void Handle(CommandResponse message) {
            _tcs?.SetResult(message);
            _state = States.Completed;
            _timeoutBus.Publish(new CommandComplete(_command.MsgId));
        }

        public void Handle(AckTimeout message) {
            if (_state != States.Sent) { return; }

            _publishBus.Publish(new CommandCancelRequest(_command.MsgId, _command.GetType().FullName, _handlerId));
            _tcs?.SetResult(_command.Canceled());
            _state = States.Completed;
            _timeoutBus.Publish(new CommandComplete(_command.MsgId));
        }

        public void Handle(CompletionTimeout message) {
            if (_state == States.Completed) { return; }

            _publishBus.Publish(new CommandCancelRequest(_command.MsgId, _command.GetType().FullName, _handlerId));
            _tcs?.SetResult(_command.Canceled());
            _state = States.Completed;
            _timeoutBus.Publish(new CommandComplete(_command.MsgId));
        }


        public void Dispose() {
            if (_state == States.Disposed) { return; }
           
            if (_state != States.Completed) {
                _publishBus.Publish(new CommandCancelRequest(_command.MsgId, _command.GetType().FullName, _handlerId));
                _tcs?.SetResult(_command.Canceled());
            }
            //??? Hmm what should we do here?
            _tcs?.Task?.Dispose();
            _state = States.Disposed;
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
        #endregion


    }
}


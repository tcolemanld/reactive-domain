using System;
using ReactiveDomain.Messaging;
using ReactiveDomain.Messaging.Bus;

namespace ReactiveDomain.Foundation.Commands {
    public sealed class CommandTracker :
                                        IHandle<CommandResponse>,
                                        IHandle<CommandTracker.AckCommand>,
                                        IHandle<CommandTracker.AckTimeout>,
                                        IHandle<CommandTracker.CompletionTimeout> {
        private readonly Command _command;
        private readonly IBus _publishBus;
        private readonly IBus _timeoutBus;
        private readonly TimeSpan _responseTimeout;
        private readonly TimeSpan _ackTimeout;

        public CommandTracker(
                Command command,
                IBus publishBus,
                IBus timeoutBus,
                TimeSpan responseTimeout,
                TimeSpan ackTimeout) {
            _command = command;
            _publishBus = publishBus;
            _timeoutBus = timeoutBus;
            _responseTimeout = responseTimeout;
            _ackTimeout = ackTimeout;
        }
        public CommandResponse Send() {
            return _command.Succeed();
        }

        public CommandResponse SendAsync() {
            return null;
        }
        public void Handle(AckCommand message) {
            throw new NotImplementedException();
        }

        public void Handle(CommandResponse message) {
            throw new NotImplementedException();
        }

        public void Handle(AckTimeout message) {
            throw new NotImplementedException();
        }

        public void Handle(CompletionTimeout message) {
            throw new NotImplementedException();
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

        #endregion
        }
}

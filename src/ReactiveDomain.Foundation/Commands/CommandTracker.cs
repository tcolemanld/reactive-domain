using System;
using ReactiveDomain.Messaging;

namespace ReactiveDomain.Foundation.Commands {
    public sealed class CommandTracker : ICommandSender
    {
        private Command _trackedCommand; 
        public CommandTracker() { }

        #region ICommandPublisher
        public void Send(Command command, string exceptionMsg = null, TimeSpan? responseTimeout = null, TimeSpan? ackTimeout = null) {
            if(_trackedCommand != null) {
                throw new InvalidOperationException("Already tracking a command");
            }
            _trackedCommand = command;

        }

        public bool TrySend(Command command, out CommandResponse response, TimeSpan? responseTimeout = null,
                            TimeSpan? ackTimeout = null) {
            if(_trackedCommand != null) {
                throw new InvalidOperationException("Already tracking a command");
            }
            _trackedCommand = command;
            response = null;
            return false;
        }

        public void SendAsync(Command command, TimeSpan? responseTimeout = null, TimeSpan? ackTimeout = null) {
            if(_trackedCommand != null) {
                throw new InvalidOperationException("Already tracking a command");
            }
            _trackedCommand = command;
        }
        #endregion

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

        #endregion

        
    }
}

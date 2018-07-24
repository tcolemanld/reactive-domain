using System;
using Newtonsoft.Json;
using ReactiveDomain.Messaging;

namespace ReactiveDomain.Foundation.Commands {
    public abstract class CommandResponse : CorrelatedMessage {
        /// <summary>
        /// MsgId of the Command
        /// </summary>
        public readonly Guid CommandId;
        /// <summary>
        /// Full Type Name of the Command 
        /// </summary>
        public readonly string CommandFullName;
        /// <summary>
        /// Id of the Command Handler sending the response
        /// </summary>
        public readonly Guid HandlerId;

        protected CommandResponse(Command cmd, Guid handlerId) : this(cmd.MsgId, cmd.GetType().FullName, handlerId, cmd.CorrelationId, new SourceId(cmd)) {}
        [JsonConstructor]
        protected CommandResponse(Guid commandId, string commandFullName, Guid handlerId, CorrelationId correlationId, SourceId sourceId) : base(correlationId, sourceId) {
            CommandId = commandId;
            CommandFullName = commandFullName;
            HandlerId = handlerId;
        }
    }

    public class Success : CommandResponse {
        public Success(Command cmd, Guid handlerId):base(cmd, handlerId) {}
        [JsonConstructor]
        public Success(Guid commandId, string commandFullName, Guid handlerId, CorrelationId correlationId, SourceId sourceId) :
            base(commandId, commandFullName, handlerId, correlationId, sourceId) { }
    }

    public class Fail : CommandResponse {
        public Exception Exception { get; }
        public Fail(Command cmd, Guid handlerId, Exception ex):base(cmd, handlerId) {
            Exception = ex;
        }
        [JsonConstructor]
        public Fail(Guid commandId, string commandFullName, Guid handlerId, Exception exception, CorrelationId correlationId, SourceId sourceId) :
            base(commandId, commandFullName, handlerId, correlationId, sourceId) {
            Exception = exception;
        }
    }

    public class Canceled : Fail {
        public Canceled(Command cmd, Guid handlerId, Exception ex):base(cmd, handlerId, ex) { }
        [JsonConstructor]
        public Canceled(Guid commandId, string commandFullName, Guid handlerId, CorrelationId correlationId, SourceId sourceId) :
            base(commandId, commandFullName, handlerId, new CommandCanceledException(commandId, commandFullName, handlerId), correlationId, sourceId) { }
    }
    public class CanceledAckTimeout : Fail {
        public CanceledAckTimeout(Command cmd, Guid handlerId, Exception ex):base(cmd, handlerId, ex) { }
        [JsonConstructor]
        public CanceledAckTimeout(Guid commandId, string commandFullName, Guid handlerId, CorrelationId correlationId, SourceId sourceId) :
            base(commandId, commandFullName, handlerId, new CommandCanceledException(commandId, commandFullName, handlerId), correlationId, sourceId) { }
    }
    public class CanceledCompletionTimeout : Fail {
        public CanceledCompletionTimeout(Command cmd, Guid handlerId, Exception ex):base(cmd, handlerId, ex) { }
        [JsonConstructor]
        public CanceledCompletionTimeout(Guid commandId, string commandFullName, Guid handlerId, CorrelationId correlationId, SourceId sourceId) :
            base(commandId, commandFullName, handlerId, new CommandCanceledException(commandId, commandFullName, handlerId), correlationId, sourceId) { }
    }
}

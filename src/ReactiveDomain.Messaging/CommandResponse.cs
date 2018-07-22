using System;
using System.Threading;
using Newtonsoft.Json;
using ReactiveDomain.Messaging.Bus;

namespace ReactiveDomain.Messaging
{
    public abstract class CommandResponse : CorrelatedMessage
    {
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

        
        protected CommandResponse(Guid commandId, string commandFullName, Guid handlerId, CorrelationId correlationId, SourceId sourceId) : base(correlationId, sourceId) {
            CommandId = commandId;
            CommandFullName = commandFullName;
            HandlerId = handlerId;
        }
    }

    public class Success : CommandResponse
    {
        public Success(Guid commandId, string commandFullName, Guid handlerId, CorrelationId correlationId, SourceId sourceId) :
            base( commandId,  commandFullName,  handlerId,  correlationId,  sourceId) {}
    }

    public class Fail : CommandResponse
    {
        public Exception Exception { get; }
        public Fail(Guid commandId, string commandFullName, Guid handlerId, Exception exception, CorrelationId correlationId, SourceId sourceId) :
            base( commandId,  commandFullName,  handlerId,  correlationId,  sourceId) 
        {
            Exception = exception;
        }
    }

    public class Canceled : Fail
    {
        public Canceled(Guid commandId, string commandFullName, Guid handlerId, CorrelationId correlationId, SourceId sourceId) :
            base( commandId,  commandFullName,  handlerId,  new CommandCanceledException(commandId, commandFullName,handlerId),correlationId,  sourceId ) { }
    }
}

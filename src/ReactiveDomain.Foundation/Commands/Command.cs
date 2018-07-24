﻿using System;
using System.Threading;
using Newtonsoft.Json;
using ReactiveDomain.Messaging;

namespace ReactiveDomain.Foundation.Commands {
    /// <summary>
    /// A correlated command that is optionally cancellable using a CancellationToken. 
    /// </summary>
    /// <inheritdoc cref="Message"/>
    public class Command : CorrelatedMessage {

        public bool TimeoutTcpWait = true;

        /// <summary>
        /// The CancellationToken for this command
        /// </summary>
        [JsonIgnore]
        public readonly CancellationToken? CancellationToken;

        /// <summary>
        /// Has this command been canceled?
        /// </summary>
        public bool IsCanceled => CancellationToken?.IsCancellationRequested ?? false;

        /// <summary>
        /// Does this command allow cancellation?
        /// </summary>
        public bool IsCancelable => CancellationToken != null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">The source command, the cancellation token will be propagated.</param>
        public Command(Command source) : this(source, source.CancellationToken) { }
        public Command(CorrelatedMessage source, CancellationToken? token = null) : this(source.CorrelationId, new SourceId(source), token) { }

        [JsonConstructor]
        public Command(CorrelationId correlationId, SourceId sourceId, CancellationToken? token = null) : base(correlationId, sourceId) {
            CancellationToken = token;
        }

        /// <summary>
        /// Create a CommandResponse indicating that this command has succeeded.
        /// </summary>
        public CommandResponse Succeed() {
            return new Success(MsgId, GetType().FullName, Guid.Empty, CorrelationId, new SourceId(this));
        }

        /// <summary>
        /// Create a CommandResponse indicating that this command has failed.
        /// </summary>
        public CommandResponse Failed(Exception ex = null) {
            return new Fail(MsgId, GetType().FullName, Guid.Empty, ex, CorrelationId, new SourceId(this));
        }

        /// <summary>
        /// Create a CommandResponse indicating that this command has been canceled.
        /// </summary>
        public CommandResponse Canceled(Guid? handlerGuid = null, Exception ex = null) {
            return new Canceled(this, handlerGuid ?? Guid.Empty, ex);
        }
        /// <summary>
        /// Create a CommandResponse indicating that this command has been canceled.
        /// </summary>
        public CommandResponse CanceledAckTimeout(Guid? handlerGuid = null, Exception ex = null) {
            return new CanceledAckTimeout(this, handlerGuid ?? Guid.Empty, ex);
        }
        /// <summary>
        /// Create a CommandResponse indicating that this command has been canceled.
        /// </summary>
        public CommandResponse CanceledCompletionTimeout(Guid? handlerGuid = null, Exception ex = null) {
            return new CanceledCompletionTimeout(this, handlerGuid ?? Guid.Empty, ex);
        }
    }
}

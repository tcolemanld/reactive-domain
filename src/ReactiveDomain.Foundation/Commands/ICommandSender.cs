using System;

namespace ReactiveDomain.Foundation.Commands {
    public interface ICommandSender {
        /// <summary>
        /// Send blocks the calling thread until a command response or timeout is received
        /// </summary>
        /// <param name="command">The command to send</param>
        /// <param name="exceptionMsg">The text of Exception wrapping the thrown exception, 
        ///                 useful for displaying error information in UI applications</param>
        /// <param name="completionTimeout">How long to wait for completion before throwing a timeout exception and sending a cancel</param>
        /// <param name="ackTimeout">How long to wait for processing to start before throwing a timeout exception and sending a cancel</param>
        void Send(Command command, string exceptionMsg = null, TimeSpan? completionTimeout = null, TimeSpan? ackTimeout = null);

        /// <summary>
        /// TrySend will block the calling thread and returns the command response via the out parameter.
        /// Will not throw, check the command response exception property on failed responses for the exception
        /// </summary>
        /// <param name="command">the command to send</param>
        /// <param name="response">the command response, of type success or fail</param>
        /// <param name="completionTimeout">How long to wait for completion before throwing a timeout exception and sending a cancel</param>
        /// <param name="ackTimeout">How long to wait for processing to start before throwing a timeout exception and sending a cancel</param>
        /// <returns>true if command response is of type Success, False if CommandResponse is of type Fail</returns>
        bool TrySend(Command command, out CommandResponse response, TimeSpan? completionTimeout = null, TimeSpan? ackTimeout = null);

        /// <summary>
        /// SendAsync publishes the command and returns. 
        /// 
        /// Useful for very long running commands, but also consider using a pair of matched messages instead. 
        /// 
        /// If handling the response is required, the caller must subscribe directly to the Command Response messages 
        /// and correlate on the message id, this can be expensive.
        /// 
        /// Using an explicitly typed set of CommandResponses may allow for the caller to process fewer CommandResponses 
        /// by subscribing explicitly.
        /// 
        ///  </summary>
        /// <param name="command">the command to send</param>
        /// <param name="completionTimeout">How long to wait for completion before throwing a timeout exception and sending a cancel</param>
        /// <param name="ackTimeout">How long to wait for processing to start before throwing a timeout exception and sending a cancel</param> 
        void SendAsync(Command command, TimeSpan? completionTimeout = null, TimeSpan? ackTimeout = null);
    }
}

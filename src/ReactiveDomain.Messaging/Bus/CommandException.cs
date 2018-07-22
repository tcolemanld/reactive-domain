using System;
using System.Runtime.Serialization;

namespace ReactiveDomain.Messaging.Bus {
	
    [Serializable]
    public class CommandException : Exception
    {
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

        public CommandException(Guid commandId, string commandFullName, Guid handlerId) : base($"{commandFullName}: Failed")
        {
            CommandId = commandId;
            CommandFullName = commandFullName;
            HandlerId = handlerId;
        }

        public CommandException(string message, Guid commandId, string commandFullName, Guid handlerId) : base($"{commandFullName}: {message}")
        {
            CommandId = commandId;
            CommandFullName = commandFullName;
            HandlerId = handlerId;
        }

        public CommandException(string message, Exception inner, Guid commandId, string commandFullName, Guid handlerId) : base($"{commandFullName}: {message}", inner)
        {
            CommandId = commandId;
            CommandFullName = commandFullName;
            HandlerId = handlerId;
        }

        protected CommandException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

	[Serializable]
    public class CommandCanceledException : CommandException
    {

        public CommandCanceledException(Guid commandId, string commandFullName, Guid handlerId) : base(" canceled", commandId,  commandFullName,  handlerId)
        {
        }

        public CommandCanceledException(string message, Guid commandId, string commandFullName, Guid handlerId) : base(message, commandId,  commandFullName,  handlerId)
        {
        }

        public CommandCanceledException(string message, Exception inner, Guid commandId, string commandFullName, Guid handlerId) : base(message, inner, commandId,  commandFullName,  handlerId)
        {
        }

        protected CommandCanceledException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
    [Serializable]
    public class CommandTimedOutException : CommandException
    {

        public CommandTimedOutException(Guid commandId, string commandFullName, Guid handlerId) : base(" timed out", commandId,  commandFullName,  handlerId)
        {
        }

        public CommandTimedOutException(string message, Guid commandId, string commandFullName, Guid handlerId) : base(message, commandId,  commandFullName,  handlerId)
        {
        }

        public CommandTimedOutException(string message, Exception inner, Guid commandId, string commandFullName, Guid handlerId) : base(message, inner, commandId,  commandFullName,  handlerId)
        {
        }

        protected CommandTimedOutException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
    /// <summary>
    /// More than one handler acked this message.
    /// Most of this means the command is subscribed on
    /// multiple connected buses
    /// </summary>
    [Serializable]
    public class CommandOversubscribedException : CommandException
    {

        public CommandOversubscribedException(Guid commandId, string commandFullName, Guid handlerId) : base(" oversubscribed", commandId,  commandFullName,  handlerId)
        {
        }

        public CommandOversubscribedException(string message, Guid commandId, string commandFullName, Guid handlerId) : base(message, commandId,  commandFullName,  handlerId)
        {
        }

        public CommandOversubscribedException(string message, Exception inner, Guid commandId, string commandFullName, Guid handlerId) : base(message, inner, commandId,  commandFullName,  handlerId)
        {
        }

        protected CommandOversubscribedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
	[Serializable]
	public class CommandNotHandledException : CommandException
	{

		public CommandNotHandledException(Guid commandId, string commandFullName, Guid handlerId) : base(" not handled", commandId,  commandFullName,  handlerId)
		{
		}

		public CommandNotHandledException(string message, Guid commandId, string commandFullName, Guid handlerId) : base(message, commandId,  commandFullName,  handlerId)
		{
		}

		public CommandNotHandledException(string message, Exception inner, Guid commandId, string commandFullName, Guid handlerId) : base(message, inner,  commandId,  commandFullName,  handlerId)
		{
		}

		protected CommandNotHandledException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}
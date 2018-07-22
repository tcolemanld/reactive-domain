using System;
using ReactiveDomain.Messaging;

namespace ReactiveDomain.Foundation.Commands {
    public class CommandHandlerRegistered : Message {
        public readonly string Name;
        public readonly Guid HandlerId;
        public readonly string CommandName;

        public CommandHandlerRegistered(
            string name,
            Guid handlerId,
            string commandName) {
            Name = name;
            HandlerId = handlerId;
            CommandName = commandName;
        }
    }
    public class CommandHandlerUnregistered : Message {
        public readonly string Name;
        public readonly Guid HandlerId;
        public readonly string CommandName;

        public CommandHandlerUnregistered(
            string name,
            Guid handlerId,
            string commandName) {
            Name = name;
            HandlerId = handlerId;
            CommandName = commandName;
        }
    }
}

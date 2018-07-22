using System;
using System.Threading;
using Newtonsoft.Json;
using ReactiveDomain.Foundation.Commands;
using ReactiveDomain.Messaging;


// ReSharper disable once CheckNamespace
namespace ReactiveDomain.Testing {
    public class TestCommands {
        public class TimeoutTestCommand : Command {
            public TimeoutTestCommand(CorrelatedMessage source) : base(source) { }
        }
        public class Fail : Command {
            public Fail(CorrelatedMessage source) : base(source) { }
        }
        public class Throw : Command {
            public Throw(CorrelatedMessage source) : base(source) { }
        }
        public class WrapException : Command {
            public WrapException(CorrelatedMessage source) : base(source) { }
        }
        public class ChainedCaller : Command {
            public ChainedCaller(CorrelatedMessage source) : base(source) { }
        }
        public class AckedCommand : Command {
            public AckedCommand(CorrelatedMessage source) : base(source) { }
        }
        public class Command1 : Command {
            public Command1(CorrelatedMessage source) : base(source) { }
            public Command1() : base(NewRoot()) { }
        }
        public class Command2 : Command {
            public Command2(CorrelatedMessage source) : base(source) { }
            public Command2() : base(NewRoot()) { }
        }
        public class Command3 : Command {
            public Command3(CorrelatedMessage source) : base(source) { }
            public Command3() : base(NewRoot()) { }
        }
        public class Command4 : Command {
            public Command4() : base(NewRoot()) { }
        }
        public class CancelableCommand : Command {
            public CancelableCommand(CancellationToken token) : base(NewRoot(), token) { }
        }
        public class RemoteHandled : Command {
            public RemoteHandled(CorrelatedMessage source) : base(source) { }
        }
        //n.b. don't register a handler for this
        public class Unhandled : Command {
            public Unhandled(CorrelatedMessage source) : base(source) { }
        }
        public class LongRunning : Command {
            public LongRunning(CorrelatedMessage source) : base(source) { }
        }
        public class TypedResponse : Command {
            public readonly bool FailCommand;
            public TypedResponse(
                bool failCommand,
                CorrelatedMessage source) : base(source) {
                FailCommand = failCommand;
            }
            [JsonConstructor]
            public TypedResponse(
                bool failCommand,
                CorrelationId correlationId,
                SourceId sourceId) : base(correlationId, sourceId) {
                FailCommand = failCommand;
            }
            public TestResponse Succeed(int data) {
                return new TestResponse(
                            this,
                            data);
            }
            public FailedResponse Failed(Exception ex, int data) {
                return new FailedResponse(
                            this,
                            ex,
                            data);
            }
        }
        public class DisjunctCommand : Command {
            public DisjunctCommand(CorrelatedMessage source) : base(source) { }
        }
        public class UnsubscribedCommand : Command {
            public UnsubscribedCommand(CorrelatedMessage source) : base(source) { }
        }

        public class TestResponse : Success {
            public int Data { get; }

            public TestResponse(
                TypedResponse sourceCommand,
                int data) :
                    base(sourceCommand.MsgId, sourceCommand.GetType().FullName, Guid.Empty, sourceCommand.CorrelationId, new SourceId(sourceCommand)) {
                Data = data;
            }
            [JsonConstructor]
            public TestResponse(int data, Guid commandId, string commandFullName, Guid handlerId, CorrelationId correlationId, SourceId sourceId):
                base(commandId, commandFullName, handlerId,correlationId, sourceId) {
                Data = data;
            }
        }
        public class FailedResponse : Foundation.Commands.Fail {
            public int Data { get; }
            public FailedResponse(
               TypedResponse sourceCommand,
               Exception exception,
               int data) :
                    base(sourceCommand.MsgId, sourceCommand.GetType().FullName, Guid.Empty, exception, sourceCommand.CorrelationId, new SourceId(sourceCommand)) {
                Data = data;
            }
            [JsonConstructor]
            public FailedResponse(int data, Guid commandId, string commandFullName, Guid handlerId,Exception exception,CorrelationId correlationId, SourceId sourceId):
                base(commandId, commandFullName, handlerId, exception, correlationId, sourceId) {
                Data = data;
            }
        }
    }
}

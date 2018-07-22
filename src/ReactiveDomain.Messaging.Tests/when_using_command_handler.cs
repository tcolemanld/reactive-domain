using System;
using System.Threading;
using ReactiveDomain.Messaging.Bus;
using ReactiveDomain.Testing;
using Xunit;

namespace ReactiveDomain.Messaging.Tests {

    // ReSharper disable once InconsistentNaming
    public class when_using_command_handler :
        IHandleCommand<TestCommands.Command1>,
        IHandleCommand<TestCommands.Command2>,
        IHandleCommand<TestCommands.Command3>,
        IHandleCommand<TestCommands.Command4>,
        IHandleCommand<TestCommands.WrapException>,
        IHandle<AckCommand>,
        IHandle<Fail>,
        IHandle<Success> {
        private readonly IBus _bus;
        private long _cmd1Count;
        private long _cmd2Count;
        private long _cmd3Count;
        private long _cmd4Count;
        private long _ackCount;
        private long _failCount;
        private long _wrapCount;
        private long _successCount;
        private Guid _ackId;
        private Guid _responseId;
        private Exception _wrappedException;
        public when_using_command_handler() {
            _bus = new InMemoryBus(nameof(when_using_command_handler), false);
            _bus.Subscribe<AckCommand>(this);
            _bus.Subscribe<Fail>(this);
            _bus.Subscribe<Success>(this);

            _bus.Subscribe(new CommandHandler<TestCommands.Command1>(_bus, this));
            _bus.Subscribe(new CommandHandler<TestCommands.Command2>(_bus, this));
            _bus.Subscribe(new CommandHandler<TestCommands.Command3>(_bus, this));
            _bus.Subscribe(new CommandHandler<TestCommands.WrapException>(_bus, this));

        }
        [Fact]
        public void must_have_return_bus_and_target() {
            Assert.Throws<ArgumentNullException>(
                ()=>new CommandHandler<TestCommands.Command1>(null, this));
            Assert.Throws<ArgumentNullException>(
                ()=>new CommandHandler<TestCommands.Command1>(_bus, null));
        }
        [Fact]
        public void can_be_called_directly() {
            var cmdHandler = new CommandHandler<TestCommands.Command4>(_bus,this);
            var cmd = new TestCommands.Command4(CorrelatedMessage.NewRoot());
            //direct call (no bus subscription inbound)
            cmdHandler.Handle(cmd);
            Assert.Equal(1, _cmd4Count);
            Assert.Equal(1, _successCount);
            Assert.Equal(cmd.MsgId, _responseId);
        }
        [Fact]
        public void command_messages_are_acked() {
            var cmd = new TestCommands.Command1(CorrelatedMessage.NewRoot());
            _bus.Publish(cmd);
            Assert.Equal(1, _ackCount);
            Assert.Equal(cmd.MsgId, _ackId);
        }
        [Fact]
        public void command_messages_can_return_success() {
            var cmd = new TestCommands.Command1(CorrelatedMessage.NewRoot());
            _bus.Publish(cmd);
            Assert.Equal(1, _successCount);
            Assert.Equal(cmd.MsgId, _responseId);
        }
        [Fact]
        public void command_messages_can_return_fail() {
            var cmd = new TestCommands.Command2(CorrelatedMessage.NewRoot());
            _bus.Publish(cmd);
            Assert.Equal(1, _failCount);
            Assert.Equal(cmd.MsgId, _responseId);
            Assert.Null(_wrappedException);
        }
        [Fact]
        public void command_messages_can_return_fail_with_exception() {
            var cmd = new TestCommands.Command3(CorrelatedMessage.NewRoot());
            _bus.Publish(cmd);
            Assert.Equal(1, _cmd3Count);
            Assert.Equal(cmd.MsgId, _responseId);
            Assert.IsType<CommandTestException>(_wrappedException);
        }
        [Fact]
        public void command_exceptions_are_wrapped() {
            var cmd = new TestCommands.WrapException(CorrelatedMessage.NewRoot());
            _bus.Publish(cmd);
            Assert.Equal(1, _wrapCount);
            Assert.Equal(cmd.MsgId, _responseId);
            Assert.IsType<CommandTestException>(_wrappedException);
        }
        public CommandResponse Handle(TestCommands.Command1 command) {
            Interlocked.Increment(ref _cmd1Count);
            return command.Succeed();
        }

        public CommandResponse Handle(TestCommands.Command2 command) {
            Interlocked.Increment(ref _cmd2Count);
            return command.Fail();
        }
        public CommandResponse Handle(TestCommands.Command3 command) {
            Interlocked.Increment(ref _cmd3Count);
            return command.Fail(new CommandTestException());
        }
        public CommandResponse Handle(TestCommands.Command4 command) {
            Interlocked.Increment(ref _cmd4Count);
            return command.Succeed();
        }
        public CommandResponse Handle(TestCommands.WrapException command) {
            Interlocked.Increment(ref _wrapCount);
            throw new CommandTestException();
        }

        void IHandle<AckCommand>.Handle(AckCommand message) {
            Interlocked.Increment(ref _ackCount);
            _ackId = message.CommandId;
        }

        void IHandle<Fail>.Handle(Fail message) {
            Interlocked.Increment(ref _failCount);
            _wrappedException = message.Exception;
            _responseId = message.CommandId;
        }

        void IHandle<Success>.Handle(Success message) {
            Interlocked.Increment(ref _successCount);
            _responseId = message.CommandId;
        }


        public class CommandTestException : Exception { }
    }
}

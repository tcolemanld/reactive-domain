using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using ReactiveDomain.Messaging.Bus;
using ReactiveDomain.Testing;
using Xunit;

namespace ReactiveDomain.Messaging.Tests {

    // ReSharper disable once InconsistentNaming
    public sealed class when_using_command_handler :
        IHandleCommand<TestCommands.Command1>,
        IHandleCommand<TestCommands.Command2>,
        IHandleCommand<TestCommands.Command3>,
        IHandleCommand<TestCommands.Command4>,
        IHandleCommand<TestCommands.WrapException>,
        IHandleCommand<TestCommands.CancelableCommand>,
        IHandle<CommandTracker.AckCommand>,
        IHandle<Fail>,
        IHandle<Success>,
        IHandle<Canceled>,
        IDisposable {
        private readonly InMemoryBus _bus;
        private long _cmd1Count;
        private long _cmd2Count;
        private long _cmd3Count;
        private long _cmd4Count;
        private long _ackCount;
        private long _failCount;
        private long _wrapCount;
        private long _cancelCmdCount;
        private long _cancelCount;
        private long _successCount;
        private Guid _ackId;
        private Guid _responseId;
        private Guid _cancelId;
        private Exception _wrappedException;
        private readonly ManualResetEventSlim _releaseCmd = new ManualResetEventSlim();
        public when_using_command_handler() {
            _bus = new InMemoryBus(nameof(when_using_command_handler), false);
            _bus.Subscribe<CommandTracker.AckCommand>(this);
            _bus.Subscribe<Fail>(this);
            _bus.Subscribe<Success>(this);
            _bus.Subscribe<Canceled>(this);

            _bus.Subscribe(new CommandHandler<TestCommands.Command1>(_bus, this));
            _bus.Subscribe(new CommandHandler<TestCommands.Command2>(_bus, this));
            _bus.Subscribe(new CommandHandler<TestCommands.Command3>(_bus, this));
            _bus.Subscribe(new CommandHandler<TestCommands.CancelableCommand>(_bus, this));
            _bus.Subscribe(new CommandHandler<TestCommands.WrapException>(_bus, this));

        }
        [Fact]
        public void must_have_return_bus_and_target() {
            Assert.Throws<ArgumentNullException>(
                () => new CommandHandler<TestCommands.Command1>(null, this));
            //null case for  IHandleCommand<TestCommands.Command1>
            Assert.Throws<ArgumentNullException>(
                () => new CommandHandler<TestCommands.Command1>(_bus, (when_using_command_handler)null));
            Assert.Throws<ArgumentNullException>(
                () => new CommandHandler<TestCommands.Command1>(_bus, (Func<TestCommands.Command1, CommandResponse>)null));
            Assert.Throws<ArgumentNullException>(
                () => new CommandHandler<TestCommands.Command1>(_bus, (Func<TestCommands.Command1, bool>)null));
        }
        [Fact]
        public void can_be_called_directly() {
            var cmdHandler = new CommandHandler<TestCommands.Command4>(_bus, this);
            var cmd = new TestCommands.Command4();
            //direct call (no bus subscription inbound)
            cmdHandler.Handle(cmd);
            Assert.Equal(1, _cmd4Count);
            Assert.Equal(1, _successCount);
            Assert.Equal(cmd.MsgId, _responseId);
        }
        [Fact]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        public void will_notify_on_create_and_dispose() {
            var gotCmd = 0L;
            var registered = 0L;
            var unregistered = 0L;
            string regCommandName = null;
            string unRegCommandName = null;
            var regId = Guid.Empty;
            var unRegId = Guid.Empty;
            var bus = new InMemoryBus("temp");
            bus.Subscribe(new AdHocHandler<CommandHandlerRegistered>(
                                msg => {
                                    Interlocked.Increment(ref registered);
                                    regCommandName = msg.CommandName;
                                    regId = msg.HandlerId;
                                }));
            bus.Subscribe(new AdHocHandler<CommandHandlerUnregistered>(
                                msg => {
                                    Interlocked.Increment(ref unregistered);
                                    unRegCommandName = msg.CommandName;
                                    unRegId = msg.HandlerId;
                                }));

            var handler = new CommandHandler<TestCommands.Command1>(bus, cmd => Interlocked.Increment(ref gotCmd) == 0);
            handler.Handle(new TestCommands.Command1());
            Assert.Equal(1, Interlocked.Read(ref gotCmd));
            Assert.Equal(1, Interlocked.Read(ref registered));
            Assert.Equal(typeof(TestCommands.Command1).FullName,regCommandName);
            Assert.Equal(0, Interlocked.Read(ref unregistered));
            handler.Dispose();
            Assert.Equal(1, Interlocked.Read(ref unregistered));
            Assert.Equal(typeof(TestCommands.Command1).FullName,unRegCommandName);
            Assert.Equal(regId,unRegId);
        }
        [Fact]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        public void will_not_process_after_dispose() {
            var gotCmd = 0L;
            var bus = new InMemoryBus("temp");
            var handler = new CommandHandler<TestCommands.Command1>(bus, cmd => Interlocked.Increment(ref gotCmd) == 0);
            handler.Handle(new TestCommands.Command1());
            Assert.Equal(1, Interlocked.Read(ref gotCmd));
            handler.Dispose();
            handler.Handle(new TestCommands.Command1());
            Assert.Equal(1, Interlocked.Read(ref gotCmd));

        }
        [Fact]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        public void can_use_ad_hoc_func_cmd_response() {
            var gotIt = 0L;
            var cmdHandler = new CommandHandler<TestCommands.Command1>(
                                    _bus,
                                     c => {
                                         Interlocked.Increment(ref gotIt);
                                         return c.Succeed();
                                     });

            var cmd = new TestCommands.Command1();

            cmdHandler.Handle(cmd);
            Assert.Equal(1, Interlocked.Read(ref gotIt));
            Assert.Equal(1, _successCount);
            Assert.Equal(cmd.MsgId, _responseId);

        }
        [Fact]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        public void can_use_ad_hoc_func_cmd_bool_success() {
            var gotIt = 0L;
            var cmdHandler = new CommandHandler<TestCommands.Command1>(
                _bus,
                c => {
                    Interlocked.Increment(ref gotIt);
                    return true;
                });

            var cmd = new TestCommands.Command1();

            cmdHandler.Handle(cmd);
            Assert.Equal(1, Interlocked.Read(ref gotIt));
            Assert.Equal(1, _successCount);
            Assert.Equal(cmd.MsgId, _responseId);

        }
        [Fact]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        public void can_use_ad_hoc_func_cmd_bool_fail() {
            var gotIt = 0L;
            var cmdHandler = new CommandHandler<TestCommands.Command1>(
                _bus,
                c => {
                    Interlocked.Increment(ref gotIt);
                    return false;
                });

            var cmd = new TestCommands.Command1();
            cmdHandler.Handle(cmd);
            Assert.Equal(1, Interlocked.Read(ref gotIt));
            Assert.Equal(1, _failCount);
            Assert.Equal(cmd.MsgId, _responseId);

        }
        [Fact]
        public void command_messages_are_acked() {
            var cmd = new TestCommands.Command1();
            _bus.Publish(cmd);
            Assert.Equal(1, _ackCount);
            Assert.Equal(cmd.MsgId, _ackId);
        }
        [Fact]
        public void command_messages_can_return_success() {
            var cmd = new TestCommands.Command1();
            _bus.Publish(cmd);
            Assert.Equal(1, _successCount);
            Assert.Equal(cmd.MsgId, _responseId);
        }
        [Fact]
        public void command_messages_can_return_fail() {
            var cmd = new TestCommands.Command2();
            _bus.Publish(cmd);
            Assert.Equal(1, _failCount);
            Assert.Equal(cmd.MsgId, _responseId);
            Assert.Null(_wrappedException);
        }
        [Fact]
        public void command_messages_can_return_fail_with_exception() {
            var cmd = new TestCommands.Command3();
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
        [Fact]
        public void uncanceled_commands_succeed() {
            using (var ts = new CancellationTokenSource()) {
                var cmd = new TestCommands.CancelableCommand(ts.Token);
                using (var _ = Task.Run(() => _bus.Publish(cmd))) {
                    AssertEx.IsOrBecomesTrue(() => _cancelCmdCount == 1);
                    _releaseCmd.Set();
                    AssertEx.IsOrBecomesTrue(() => _successCount == 1);
                    Assert.Equal(0, _cancelCount);
                }
            }
        }
        [Fact]
        public void commands_can_be_canceled() {
            using (var ts = new CancellationTokenSource()) {
                var cmd = new TestCommands.CancelableCommand(ts.Token);
                using (var _ = Task.Run(() => _bus.Publish(cmd))) {
                    AssertEx.IsOrBecomesTrue(() => _cancelCmdCount == 1);
                    ts.Cancel();
                    _releaseCmd.Set();
                    AssertEx.IsOrBecomesTrue(() => _cancelCount == 1);
                    Assert.Equal(0, _successCount);
                    Assert.Equal(_cancelId, cmd.MsgId);
                }
            }
        }

        [Fact]
        public void can_pre_cancel_commands() {
            using (var ts = new CancellationTokenSource()) {
                var cmd = new TestCommands.CancelableCommand(ts.Token);
                ts.Cancel();
                _bus.Publish(cmd);
                Assert.Equal(0, _cancelCmdCount);
                Assert.Equal(1, _cancelCount);
                Assert.Equal(0, _successCount);
                Assert.Equal(_cancelId, cmd.MsgId);
            }
        }

        public CommandResponse Handle(TestCommands.Command1 command) {
            Interlocked.Increment(ref _cmd1Count);
            return command.Succeed();
        }

        public CommandResponse Handle(TestCommands.Command2 command) {
            Interlocked.Increment(ref _cmd2Count);
            return command.Failed();
        }
        public CommandResponse Handle(TestCommands.Command3 command) {
            Interlocked.Increment(ref _cmd3Count);
            return command.Failed(new CommandTestException());
        }
        public CommandResponse Handle(TestCommands.Command4 command) {
            Interlocked.Increment(ref _cmd4Count);
            return command.Succeed();
        }
        public CommandResponse Handle(TestCommands.WrapException command) {
            Interlocked.Increment(ref _wrapCount);
            throw new CommandTestException();
        }

        void IHandle<CommandTracker.AckCommand>.Handle(CommandTracker.AckCommand message) {
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
        void IHandle<Canceled>.Handle(Canceled message) {
            Interlocked.Increment(ref _cancelCount);
            _cancelId = message.CommandId;
        }
        public CommandResponse Handle(TestCommands.CancelableCommand command) {
            Interlocked.Increment(ref _cancelCmdCount);
            _releaseCmd.Wait();
            return command.IsCanceled ? command.Canceled() : command.Succeed();
        }
        public class CommandTestException : Exception { }


        public void Dispose() {
            _releaseCmd?.Dispose();
            _bus?.Dispose();
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using ReactiveDomain.Foundation.Commands;
using ReactiveDomain.Messaging;
using ReactiveDomain.Messaging.Bus;
using ReactiveDomain.Testing;
using Xunit;
using static System.Threading.Interlocked;
using CanceledAckTimeout = ReactiveDomain.Foundation.Commands.CanceledAckTimeout;

namespace ReactiveDomain.Foundation.Tests.Commands {
    // ReSharper disable once InconsistentNaming
    public class when_using_command_tracker {
        private readonly TestTimeSource _timeSource = new TestTimeSource();
        private readonly TimeSpan _ackTimeout = TimeSpan.FromMilliseconds(500);
        private readonly TimeSpan _completionTimeout = TimeSpan.FromMilliseconds(1000);

        [Fact]
        public void can_send_command() {
            var gotCmd = 0L;
            var gotTComplete = 0L;
            var bus = new InMemoryBus("temp");
            bus.Subscribe(new AdHocHandler<TestCommands.Command1>(_ => Increment(ref gotCmd)));
            bus.Subscribe(new AdHocHandler<CommandTracker.CommandProcessingCompleted>(_ => Increment(ref gotTComplete)));
            var cmd = new TestCommands.Command1();
            var t = new CommandTracker(
                cmd,
                bus,
                bus,
                _ackTimeout,
                _completionTimeout,
                _timeSource);
            CommandResponse response = null;
            Task.Run(() => response = t.Send());
            AssertEx.IsOrBecomesTrue(() => Read(ref gotCmd) == 1);
            t.Handle(cmd.Succeed());
            AssertEx.IsOrBecomesTrue(() => Read(ref gotTComplete) == 1);
            AssertEx.IsOrBecomesTrue(() => response is Success);
        }
        [Fact]
        public void command_can_timeout_on_ack() {
            var gotCmd = 0L;
            var gotTComplete = 0L;
            var gotCancel = 0L;
            var bus = new InMemoryBus("temp");
            bus.Subscribe(new AdHocHandler<TestCommands.Command1>(_ => Increment(ref gotCmd)));
            bus.Subscribe(new AdHocHandler<CommandTracker.CommandProcessingCompleted>(_ => Increment(ref gotTComplete)));
            bus.Subscribe(new AdHocHandler<CommandTracker.CommandCancellationRequested>(_ => Increment(ref gotCancel)));
            var cmd = new TestCommands.Command1();
            var t = new CommandTracker(
                cmd,
                bus,
                bus,
                _ackTimeout,
                _completionTimeout,
                _timeSource);
            CommandResponse response = null;
            Task.Run(() => response = t.Send());
            AssertEx.IsOrBecomesTrue(() => Read(ref gotCmd) == 1);
            t.Handle(new CommandTracker.CommandReceiptTimeoutExpired(cmd.MsgId));

            AssertEx.IsOrBecomesTrue(() => Read(ref gotTComplete) == 1);
            AssertEx.IsOrBecomesTrue(() => Read(ref gotCancel) == 1);
            AssertEx.IsOrBecomesTrue(() => (response as CanceledAckTimeout)?.Exception is TimeoutException);
        }


        [Fact]
        public void command_will_timeout_on_completion() {
            var gotCmd = 0L;
            var gotTComplete = 0L;
            var gotCancel = 0L;
            var bus = new InMemoryBus("temp");
            bus.Subscribe(new AdHocHandler<TestCommands.Command1>(_ => Increment(ref gotCmd)));
            bus.Subscribe(new AdHocHandler<CommandTracker.CommandProcessingCompleted>(_ => Increment(ref gotTComplete)));
            bus.Subscribe(new AdHocHandler<CommandTracker.CommandCancellationRequested>(_ => Increment(ref gotCancel)));
            var cmd = new TestCommands.Command1();
            var t = new CommandTracker(
               cmd,
                bus,
                bus,
                _ackTimeout,
                _completionTimeout,
                _timeSource);
            CommandResponse response = null;
            Task.Run(() => response = t.Send());
            AssertEx.IsOrBecomesTrue(() => Read(ref gotCmd) == 1);
            t.Handle(new CommandTracker.CommandReceived(cmd, Guid.Empty));
            t.Handle(new CommandTracker.CommandCompletionTimeoutExpired(cmd.MsgId));

            AssertEx.IsOrBecomesTrue(() => Read(ref gotTComplete) == 1);
            AssertEx.IsOrBecomesTrue(() => Read(ref gotCancel) == 1);
            AssertEx.IsOrBecomesTrue(() => (response as CanceledCompletionTimeout)?.Exception is TimeoutException);
        }
    }
}

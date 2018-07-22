using System;
using ReactiveDomain.Foundation.Commands;
using ReactiveDomain.Testing;
using Xunit;

namespace ReactiveDomain.Foundation.Tests.Commands
{
    // ReSharper disable once InconsistentNaming
    public class when_tracking_commands
    {
        [Fact]
        public void can_only_track_one_command_once() {
            var tracker = new CommandTracker();
            var cmd1 = new TestCommands.Command1();
            var cmd2 = new TestCommands.Command1();
            tracker.Send(cmd1);
            Assert.Throws<InvalidOperationException>(() => tracker.Send(cmd1));
            Assert.Throws<InvalidOperationException>(() => tracker.Send(cmd2));
            Assert.Throws<InvalidOperationException>(() => tracker.SendAsync(cmd1));
            Assert.Throws<InvalidOperationException>(() => tracker.SendAsync(cmd2));
            Assert.Throws<InvalidOperationException>(() => tracker.TrySend(cmd1, out _));
            Assert.Throws<InvalidOperationException>(() => tracker.TrySend(cmd2, out _));
            tracker = new CommandTracker();
            tracker.TrySend(cmd1, out _);
            Assert.Throws<InvalidOperationException>(() => tracker.Send(cmd1));
            Assert.Throws<InvalidOperationException>(() => tracker.Send(cmd2));
            Assert.Throws<InvalidOperationException>(() => tracker.SendAsync(cmd1));
            Assert.Throws<InvalidOperationException>(() => tracker.SendAsync(cmd2));
            Assert.Throws<InvalidOperationException>(() => tracker.TrySend(cmd1, out _));
            Assert.Throws<InvalidOperationException>(() => tracker.TrySend(cmd2, out _));
            tracker = new CommandTracker();
            tracker.SendAsync(cmd1);
            Assert.Throws<InvalidOperationException>(() => tracker.Send(cmd1));
            Assert.Throws<InvalidOperationException>(() => tracker.Send(cmd2));
            Assert.Throws<InvalidOperationException>(() => tracker.SendAsync(cmd1));
            Assert.Throws<InvalidOperationException>(() => tracker.SendAsync(cmd2));
            Assert.Throws<InvalidOperationException>(() => tracker.TrySend(cmd1, out _));
            Assert.Throws<InvalidOperationException>(() => tracker.TrySend(cmd2, out _));
        }
    }
}

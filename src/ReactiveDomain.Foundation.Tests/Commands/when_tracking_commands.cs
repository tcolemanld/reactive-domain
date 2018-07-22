using System;
using ReactiveDomain.Foundation.Commands;
using ReactiveDomain.Messaging.Bus;
using ReactiveDomain.Testing;
using Xunit;

namespace ReactiveDomain.Foundation.Tests.Commands
{
    // ReSharper disable once InconsistentNaming
    public class when_tracking_commands
    {
        [Fact]
        public void can_only_track_one_command_once() {
            var bus = new InMemoryBus("test");
            var manager = new CommandManager(bus);
            var cmd1 = new TestCommands.Command1();
            var cmd2 = new TestCommands.Command1();
            manager.Send(cmd1);
            manager.Send(cmd2);
            Assert.Throws<InvalidOperationException>(() => manager.Send(cmd1));
            Assert.Throws<InvalidOperationException>(() => manager.Send(cmd2));
            Assert.Throws<InvalidOperationException>(() => manager.SendAsync(cmd1));
            Assert.Throws<InvalidOperationException>(() => manager.SendAsync(cmd2));
            Assert.False(manager.TrySend(cmd1, out _));
            Assert.False(manager.TrySend(cmd2, out _));
            manager = new CommandManager(bus);
            manager.TrySend(cmd1, out _);
            manager.TrySend(cmd2, out _);
            Assert.Throws<InvalidOperationException>(() => manager.Send(cmd1));
            Assert.Throws<InvalidOperationException>(() => manager.Send(cmd2));
            Assert.Throws<InvalidOperationException>(() => manager.SendAsync(cmd1));
            Assert.Throws<InvalidOperationException>(() => manager.SendAsync(cmd2));
            Assert.False(manager.TrySend(cmd1, out _));
            Assert.False(manager.TrySend(cmd2, out _));
            manager = new CommandManager(bus);
            manager.SendAsync(cmd1);
            manager.SendAsync(cmd2);
            Assert.Throws<InvalidOperationException>(() => manager.Send(cmd1));
            Assert.Throws<InvalidOperationException>(() => manager.Send(cmd2));
            Assert.Throws<InvalidOperationException>(() => manager.SendAsync(cmd1));
            Assert.Throws<InvalidOperationException>(() => manager.SendAsync(cmd2));
            Assert.False(manager.TrySend(cmd1, out _));
            Assert.False(manager.TrySend(cmd2, out _));
        }
    }
}

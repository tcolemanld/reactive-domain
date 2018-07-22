using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveDomain.Messaging.Bus;
using ReactiveDomain.Util;

namespace ReactiveDomain.Foundation.Commands {
    public abstract class CommandSubscriber : QueuedSubscriber {
        private List<IDisposable> _cmdHandlers;
        protected CommandSubscriber(IBus bus) : base(bus) {
            _cmdHandlers = new List<IDisposable>();
        }
        protected void Subscribe<T>(IHandleCommand<T> handler) where T : Command {
            var cmdHandler = new CommandHandler<T>(ExternalBus, handler);
            _cmdHandlers.Add(cmdHandler);

            Subscribe(cmdHandler);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _cmdHandlers?.ForEach(h => h?.Dispose());
            }
        }
    }
}

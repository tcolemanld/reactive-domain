using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveDomain.Testing;
using ReactiveDomain.Testing.EventStore;
using Xunit;

namespace ReactiveDomain.Foundation.Tests.SynchronizedStreamListenerTests
{
    public class when_using_read_model_base : 
        ReadModelBase
    {
        
        private static IListener GetListener() {
            return new SynchronizableStreamListener(
                        nameof(when_using_read_model_base),
                        conn,
                        namer,
                        serializer,
                        true);
        }
        private static IStreamStoreConnection conn = new MockStreamStoreConnection(nameof(when_using_read_model_base));
        private static IEventSerializer serializer = new JsonMessageSerializer();
        private static IStreamNameBuilder namer = new PrefixedCamelCaseStreamNameBuilder(nameof(when_using_read_model_base));

        public when_using_read_model_base()
                    :base(nameof(when_using_read_model_base),GetListener) {
            
        }        
    }
}

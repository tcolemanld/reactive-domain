using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveDomain.Testing;
using Xunit;

namespace ReactiveDomain.Foundation.Tests.SynchronizedStreamListenerTests
{
    public class when_using_read_model_base :         ReadModelBase,        IClassFixture<StreamStoreConnectionFixture>{        
    }
}

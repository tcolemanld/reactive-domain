using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveDomain.Util;

namespace ReactiveDomain.Foundation.EventStore
{
    public class ReadModelState
    {
        public readonly string ModelName;
        public readonly List<Tuple<string, long>> Checkpoints;
        public readonly object State;

        public ReadModelState(
            string modelName,
            List<Tuple<string,long>> checkpoint,
            object state) {
            Ensure.NotNullOrEmpty(modelName,nameof(modelName));
            ModelName = modelName;
            Checkpoints = checkpoint;
            State = state;
        }
    }
}

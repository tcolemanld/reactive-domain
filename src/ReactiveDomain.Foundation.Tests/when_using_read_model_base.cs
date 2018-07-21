using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveDomain.Messaging;
using ReactiveDomain.Messaging.Bus;
using ReactiveDomain.Testing;
using ReactiveDomain.Testing.EventStore;
using Xunit;

namespace ReactiveDomain.Foundation.Tests {
    public class when_using_read_model_base :
                    ReadModelBase,
                    IHandle<when_using_read_model_base.ReadModelTestEvent>,
                    IClassFixture<StreamStoreConnectionFixture>{

        private static IListener GetListener() {
            return new SynchronizableStreamListener(
                        nameof(when_using_read_model_base),
                        Conn,
                        Namer,
                        Serializer,
                        true);
        }

        private static IStreamStoreConnection Conn;
        private static readonly IEventSerializer Serializer =
            new JsonMessageSerializer();
        private static readonly IStreamNameBuilder Namer =
            new PrefixedCamelCaseStreamNameBuilder(nameof(when_using_read_model_base));

        private readonly string _stream1;
        private readonly string _stream2;


        public when_using_read_model_base(StreamStoreConnectionFixture fixture)
                    : base(nameof(when_using_read_model_base), GetListener) {
            Conn = fixture.Connection;
            Conn.Connect();

            EventStream.Subscribe<ReadModelTestEvent>(this);

            _stream1 = Namer.GenerateForAggregate(typeof(TestAggregate), Guid.NewGuid());
            _stream2 = Namer.GenerateForAggregate(typeof(TestAggregate), Guid.NewGuid());

            AppendEvents(10, Conn, _stream1,2);
            AppendEvents(10, Conn, _stream2,3);
        }

        private void AppendEvents(
                        int numEventsToBeSent, 
                        IStreamStoreConnection conn, 
                        string streamName,
                        int value) {
            for (int evtNumber = 0; evtNumber < numEventsToBeSent; evtNumber++) {
                var evt = new ReadModelTestEvent(evtNumber, value);
                conn.AppendToStream(streamName, ExpectedVersion.Any, null, Serializer.Serialize(evt));
            }
        }

        [Fact]
        public void can_read_one_stream() {
            Start(_stream1);
            AssertEx.IsOrBecomesTrue(() => Count == 10, 1000, msg: $"Expected 10 got {Count}");
            AssertEx.IsOrBecomesTrue(() => Sum == 20);
        }
        [Fact]
        public void can_read_two_streams() {
            Start(_stream1);
            Start(_stream2);
            AssertEx.IsOrBecomesTrue(() => Count == 20, 1000, msg: $"Expected 20 got {Count}");
            AssertEx.IsOrBecomesTrue(() => Sum == 50);
        }
        [Fact]
        public void can_wait_for_one_stream_to_go_live() {
            Start(_stream1, null, true);
            Assert.Equal(10, Count);
            Assert.Equal(20, Sum);
        }
        [Fact]
        public void can_wait_for_two_streams_to_go_live() {
            Start(_stream1, null, true);
            AssertEx.IsOrBecomesTrue(() => Count == 10, 10, msg: $"Expected 10 got {Count}");
            AssertEx.IsOrBecomesTrue(() => Sum == 20,10);

            Start(_stream2, null, true);
            AssertEx.IsOrBecomesTrue(() => Count == 20, 10, msg: $"Expected 20 got {Count}");
            AssertEx.IsOrBecomesTrue(() => Sum == 50,10);
        }
        [Fact]
        public void can_listen_to_one_stream() {
            Start(_stream1);
            AssertEx.IsOrBecomesTrue(() => Count == 10, 1000, msg: $"Expected 10 got {Count}");
            AssertEx.IsOrBecomesTrue(() => Sum == 20);
            //add more messages
            AppendEvents(10, Conn, _stream1,5);
            AssertEx.IsOrBecomesTrue(() => Count == 20, 1000, msg: $"Expected 20 got {Count}");
            AssertEx.IsOrBecomesTrue(() => Sum == 70);

        }
        [Fact]
        public void can_listen_to_two_streams() {
            Start(_stream1);
            Start(_stream2);
            AssertEx.IsOrBecomesTrue(() => Count == 20, 1000, msg: $"Expected 20 got {Count}");
            AssertEx.IsOrBecomesTrue(() => Sum == 50);
            //add more messages
            AppendEvents(10, Conn, _stream1,5);
            AppendEvents(10, Conn, _stream2,7);
            AssertEx.IsOrBecomesTrue(() => Count == 40, 1000, msg: $"Expected 20 got {Count}");
            AssertEx.IsOrBecomesTrue(() => Sum == 170);

        }
        [Fact]
        public void can_listen_to_the_same_stream_twice() {
            Start(_stream1);
            Start(_stream1);
            AssertEx.IsOrBecomesTrue(() => Count == 20, 1000, msg: $"Expected 20 got {Count}");
            AssertEx.IsOrBecomesTrue(() => Sum == 40);
            AppendEvents(10, Conn, _stream1,5);
            AssertEx.IsOrBecomesTrue(() => Count == 40, 1000, msg: $"Expected 20 got {Count}");
            AssertEx.IsOrBecomesTrue(() => Sum == 140);
        }

        public long Sum { get; private set; }
        public long Count { get; private set; }
        public void Handle(ReadModelTestEvent @event) {
            Sum += @event.Value;
            Count++;
        }
        public class ReadModelTestEvent : Message {
            public readonly int Number;
            public readonly int Value;

            public ReadModelTestEvent(
                int number,
                int value
                ) {
                Number = number;
                Value = value;
            }
        }
    }
}

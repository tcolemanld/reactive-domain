﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ReactiveDomain.Messaging.Bus;
using ReactiveDomain.Testing;
using Xunit;

namespace ReactiveDomain.Messaging.Tests {
    // ReSharper disable once InconsistentNaming
    public sealed class when_publishing_a_message : TestWrapper,
        IHandle<TestEvent>,
        IHandle<ParentTestEvent>,
        IHandle<ChildTestEvent>,
        IHandle<GrandChildTestEvent> {
        private int _testEventCount;
        private int _parentTestEventCount;
        private int _childTestEventCount;
        private int _grandChildTestEventCount;
        private IBus _bus;

        // Called before every test (see TestWrapper)
        protected override void Reset() {
            _bus = new InMemoryBus("testBus");
            _testEventCount = 0;
            _parentTestEventCount = 0;
            _childTestEventCount = 0;
            _grandChildTestEventCount = 0;
        }

        [Fact]
        public void TestPublishSimpleMessage() {
            _bus.Subscribe<TestEvent>(this);
            _bus.Publish(new TestEvent(CorrelatedMessage.NewRoot()));
            Assert.IsOrBecomesTrue(() => _testEventCount == 1, msg: $"Expected 1 got {_testEventCount}");
            Assert.IsOrBecomesTrue(() => _parentTestEventCount == 0, msg: $"Expected 0 got {_parentTestEventCount}");
            Assert.IsOrBecomesTrue(() => _childTestEventCount == 0, msg: $"Expected 0 got {_childTestEventCount}");
            Assert.IsOrBecomesTrue(() => _grandChildTestEventCount == 0, msg: $"Expected 0 got {_grandChildTestEventCount}");
        }

        [Fact]
        public void TestParentTestMessage() {
            // When subscribing to ParentTest Event the appropriate handler will be invoked when
            //  child (descendant) message types are published.
            _bus.Subscribe<ParentTestEvent>(this);
            var parentTestEvent = new ParentTestEvent(CorrelatedMessage.NewRoot());
            _bus.Publish(parentTestEvent);
            Assert.IsOrBecomesTrue(() => _testEventCount == 0, msg: $"Expected 0 got {_testEventCount}");
            Assert.IsOrBecomesTrue(() => _parentTestEventCount == 1, msg: $"Expected 1 got {_parentTestEventCount}");
            Assert.IsOrBecomesTrue(() => _childTestEventCount == 0, msg: $"Expected 0 got {_childTestEventCount}");
            Assert.IsOrBecomesTrue(() => _grandChildTestEventCount == 0, msg: $"Expected 0 got {_grandChildTestEventCount}");

            _bus.Subscribe<ChildTestEvent>(this);
            var childTestEvent = new ChildTestEvent(parentTestEvent);
            _bus.Publish(childTestEvent);
            Assert.IsOrBecomesTrue(() => _testEventCount == 0, msg: $"Expected 0 got {_testEventCount}");
            Assert.IsOrBecomesTrue(() => _parentTestEventCount == 2, msg: $"Expected 2 got {_parentTestEventCount}");
            Assert.IsOrBecomesTrue(() => _childTestEventCount == 1, msg: $"Expected 1 got {_childTestEventCount}");
            Assert.IsOrBecomesTrue(() => _grandChildTestEventCount == 0, msg: $"Expected 0 got {_grandChildTestEventCount}");

            _bus.Subscribe<GrandChildTestEvent>(this);
            var grandChildTestEvent = new GrandChildTestEvent(childTestEvent);
            _bus.Publish(grandChildTestEvent);
            Assert.IsOrBecomesTrue(() => _testEventCount == 0, msg: $"Expected 0 got {_testEventCount}");
            Assert.IsOrBecomesTrue(() => _parentTestEventCount == 3, msg: $"Expected 3 got {_parentTestEventCount}");
            Assert.IsOrBecomesTrue(() => _childTestEventCount == 2, msg: $"Expected 2 got {_childTestEventCount}");
            Assert.IsOrBecomesTrue(() => _grandChildTestEventCount == 1, msg: $"Expected 1 got {_grandChildTestEventCount}");
        }
        [Fact]
        public void TestUnsubscribeTestMessage() {
            _bus.Subscribe<ParentTestEvent>(this);
            _bus.Publish(new ParentTestEvent(CorrelatedMessage.NewRoot()));
            Assert.IsOrBecomesTrue(() => _testEventCount == 0, msg: $"Expected 0 got {_testEventCount}");
            Assert.IsOrBecomesTrue(() => _parentTestEventCount == 1, msg: $"Expected 1 got {_parentTestEventCount}");
            Assert.IsOrBecomesTrue(() => _childTestEventCount == 0, msg: $"Expected 0 got {_childTestEventCount}");
            Assert.IsOrBecomesTrue(() => _grandChildTestEventCount == 0, msg: $"Expected 0 got {_grandChildTestEventCount}");
            _bus.Publish(new GrandChildTestEvent(CorrelatedMessage.NewRoot()));
            Assert.IsOrBecomesTrue(() => _testEventCount == 0, msg: $"Expected 0 got {_testEventCount}");
            Assert.IsOrBecomesTrue(() => _parentTestEventCount == 2, msg: $"Expected 1 got {_parentTestEventCount}");
            Assert.IsOrBecomesTrue(() => _childTestEventCount == 0, msg: $"Expected 0 got {_childTestEventCount}");
            Assert.IsOrBecomesTrue(() => _grandChildTestEventCount == 0, msg: $"Expected 0 got {_grandChildTestEventCount}");
            // Now unsubscribe (the point of this test)
            _bus.Unsubscribe<ParentTestEvent>(this);


            _bus.Subscribe<ChildTestEvent>(this);
            _bus.Publish(new ChildTestEvent(CorrelatedMessage.NewRoot()));
            Assert.IsOrBecomesTrue(() => _testEventCount == 0, msg: $"Expected 0 got {_testEventCount}");
            // Parent count should not have increased
            Assert.IsOrBecomesTrue(() => _parentTestEventCount == 2, msg: $"Expected 1 got {_parentTestEventCount}");
            Assert.IsOrBecomesTrue(() => _childTestEventCount == 1, msg: $"Expected 1 got {_childTestEventCount}");
            Assert.IsOrBecomesTrue(() => _grandChildTestEventCount == 0, msg: $"Expected 0 got {_grandChildTestEventCount}");
            _bus.Publish(new GrandChildTestEvent(CorrelatedMessage.NewRoot()));
            Assert.IsOrBecomesTrue(() => _testEventCount == 0, msg: $"Expected 0 got {_testEventCount}");
            // Parent count should not have increased
            Assert.IsOrBecomesTrue(() => _parentTestEventCount == 2, msg: $"Expected 1 got {_parentTestEventCount}");
            Assert.IsOrBecomesTrue(() => _childTestEventCount == 2, msg: $"Expected 1 got {_childTestEventCount}");
            Assert.IsOrBecomesTrue(() => _grandChildTestEventCount == 0, msg: $"Expected 0 got {_grandChildTestEventCount}");
            _bus.Unsubscribe<ChildTestEvent>(this);
            _bus.Publish(new ParentTestEvent(CorrelatedMessage.NewRoot()));
            _bus.Publish(new ChildTestEvent(CorrelatedMessage.NewRoot()));
            _bus.Publish(new GrandChildTestEvent(CorrelatedMessage.NewRoot()));
            Assert.IsOrBecomesTrue(() => _testEventCount == 0, msg: $"Expected 0 got {_testEventCount}");
            // Parent count should not have increased
            Assert.IsOrBecomesTrue(() => _parentTestEventCount == 2, msg: $"Expected 1 got {_parentTestEventCount}");
            Assert.IsOrBecomesTrue(() => _childTestEventCount == 2, msg: $"Expected 1 got {_childTestEventCount}");
            Assert.IsOrBecomesTrue(() => _grandChildTestEventCount == 0, msg: $"Expected 0 got {_grandChildTestEventCount}");

        }

        [Fact]
        public void TestHasSubscriber() {
            var sub = _bus.Subscribe<ParentTestEvent>(this);
            Assert.True(_bus.HasSubscriberFor<ParentTestEvent>());
            sub.Dispose();
            Assert.False(_bus.HasSubscriberFor<ParentTestEvent>());

            sub = _bus.Subscribe<TestEvent>(this);
            Assert.True(_bus.HasSubscriberFor<TestEvent>());
            _bus.Unsubscribe<TestEvent>(this);
            Assert.False(_bus.HasSubscriberFor<TestEvent>());

            long _gotAdHoc = 0;
            var sub2 = _bus.Subscribe(new AdHocHandler<TestEvent>(_ => Interlocked.Increment(ref _gotAdHoc)));
            _bus.Publish(new TestEvent(CorrelatedMessage.NewRoot()));
            Assert.True(_bus.HasSubscriberFor<TestEvent>());
            Assert.False(_bus.HasSubscriberFor<Message>());
            Assert.True(_bus.HasSubscriberFor<Message>(true));
            Assert.IsOrBecomesTrue(()=>_gotAdHoc == 1);

            sub = _bus.Subscribe<TestEvent>(this);
            _bus.Publish(new TestEvent(CorrelatedMessage.NewRoot()));
            Assert.True(_bus.HasSubscriberFor<TestEvent>());
            Assert.False(_bus.HasSubscriberFor<Message>());
            Assert.True(_bus.HasSubscriberFor<Message>(true));
            Assert.IsOrBecomesTrue(()=>_gotAdHoc == 2);
            Assert.IsOrBecomesTrue(()=>_testEventCount == 1);

            sub.Dispose();
            _bus.Publish(new TestEvent(CorrelatedMessage.NewRoot()));
            Assert.True(_bus.HasSubscriberFor<TestEvent>());
            Assert.False(_bus.HasSubscriberFor<Message>());
            Assert.True(_bus.HasSubscriberFor<Message>(true));
            Assert.IsOrBecomesTrue(()=>_gotAdHoc == 3);
            Assert.IsOrBecomesTrue(()=>_testEventCount == 1);
            
            sub2.Dispose();
            _bus.Publish(new TestEvent(CorrelatedMessage.NewRoot()));
            Assert.False(_bus.HasSubscriberFor<Message>());
            Assert.False(_bus.HasSubscriberFor<Message>(true));
            Assert.False(_bus.HasSubscriberFor<TestEvent>());
            Assert.IsOrBecomesTrue(()=>_gotAdHoc == 3);
            Assert.IsOrBecomesTrue(()=>_testEventCount == 1);
        }

        [Fact]
        public void TestNoSubscriber() {
            var testEvent = new TestEvent(CorrelatedMessage.NewRoot());
            _bus.Publish(testEvent);
            Assert.IsOrBecomesTrue(() => _testEventCount == 0, msg: $"Expected 0 got {_testEventCount}");
        }

        public void Handle(TestEvent message) => Interlocked.Increment(ref _testEventCount);
        public void Handle(ParentTestEvent message) => Interlocked.Increment(ref _parentTestEventCount);
        public void Handle(ChildTestEvent message) => Interlocked.Increment(ref _childTestEventCount);
        public void Handle(GrandChildTestEvent message) => Interlocked.Increment(ref _grandChildTestEventCount);
    }

    public abstract class TestWrapper : IDisposable {
        // Simple wrapper to assure call to Reset before every test.
        protected TestWrapper() {
            this.Reset();
        }

        abstract protected void Reset();

        void IDisposable.Dispose() {
            // Noop
        }
    }
}
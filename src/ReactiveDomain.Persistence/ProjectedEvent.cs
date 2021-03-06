﻿using System;

namespace ReactiveDomain {
    public class ProjectedEvent:RecordedEvent
    {
        public string ProjectedStream;
        public long ProjectedEventNumber;

        public ProjectedEvent(
                string projectedStream,
                long projectedEventNumber,
                string eventStreamId,
                Guid eventId,
                long eventNumber,
                string eventType,
                byte[] data,
                byte[] metadata,
                bool isJson,
                DateTime created,
                long createdEpoch) : base(  
                                        eventStreamId,
                                        eventId,
                                        eventNumber,
                                        eventType,
                                        data,
                                        metadata,
                                        isJson,
                                        created,
                                        createdEpoch) {
            ProjectedStream = projectedStream;
            ProjectedEventNumber = projectedEventNumber;
        }
    }
}
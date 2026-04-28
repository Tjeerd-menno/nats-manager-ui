namespace NatsManager.Application.Modules.Relationships.Models;

public enum ResourceType
{
    Server, Subject, Stream, Consumer,
    KvBucket, KvKey, ObjectBucket, ObjectStoreObject,
    Service, ServiceEndpoint, Alert, Event, External,
    JetStreamAccount, Client
}

public enum RelationshipType
{
    Contains, ConsumesFrom, PublishesTo, SubscribesTo,
    UsesSubject, BackedByStream, HostedOn, RoutedThrough, HostsJetStream,
    AffectedBy, RelatedEvent, DependsOn, ExternalReference
}

public enum RelationshipDirection { Inbound, Outbound, Bidirectional, Unknown }

public enum ObservationKind { Observed, Inferred }

public enum RelationshipConfidence { High, Medium, Low, Unknown }

public enum RelationshipFreshness { Live, Stale, Partial, Unavailable }

public enum ResourceHealthStatus { Healthy, Warning, Degraded, Stale, Unavailable, Unknown }

public enum RelationshipSourceModule
{
    CoreNats, JetStream, KeyValue, ObjectStore,
    Services, Monitoring, Alerts, Events, Search
}

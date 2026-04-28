namespace NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;

public enum ClusterStatus { Healthy, Degraded, Unavailable, Unknown }

public enum ServerStatus { Healthy, Warning, Stale, Unavailable, Unknown }

public enum RelationshipStatus { Healthy, Warning, Stale, Unavailable, Unknown }

public enum ObservationFreshness { Live, Stale, Partial, Unavailable }

public enum MetricState { Live, Derived, Stale, Unavailable }

public enum TopologyRelationshipType { Route, Gateway, LeafNode, ClusterPeer }

public enum RelationshipDirection { Inbound, Outbound, Bidirectional, Unknown }

public enum MonitoringEndpoint { Healthz, Varz, Jsz, Routez, Gatewayz, Leafz, Other }

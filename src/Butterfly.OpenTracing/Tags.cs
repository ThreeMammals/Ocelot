using System;
using System.Collections.Generic;
using System.Text;

namespace Butterfly.OpenTracing
{
    public static class Tags
    {
        public const string Service = "service.name";
        /// <summary>
        /// The software package, framework, library, or module that generated the associated Span.
        /// E.g., "grpc", "django", "JDBI".
        /// </summary>
        public const string Component = "component";

        /// <summary>
        /// Database instance name. E.g. the "Initial Catalog" value from a SQL connection string.
        /// </summary>
        public const string DbInstance = "db.instance";

        /// <summary>
        /// A database statement for the given database type.
        /// E.g., for db.type="sql", "SELECT * FROM wuser_table"; for db.type="redis", "SET mykey 'WuValue'".
        /// </summary>
        public const string DbStatement = "db.statement";

        /// <summary>
        /// Database type. For any SQL database, "sql". For others, the lower-case database category,
        /// e.g. "cassandra", "hbase", or "redis".
        /// </summary>
        public const string DbType = "db.type";

        /// <summary>
        /// Username for accessing database. E.g., "readonly_user" or "reporting_user".
        /// </summary>
        public const string DbUser = "db.user";

        /// <summary>
        /// <c>true</c> if and only if the application considers the operation represented by the Span to have failed.
        /// </summary>
        public const string Error = "error";

        /// <summary>
        /// HTTP method of the request for the associated Span. E.g., "GET", "POST".
        /// </summary>
        public const string HttpMethod = "http.method";

        /// <summary>
        /// HTTP response status code for the associated Span. E.g., 200, 503, 404.
        /// </summary>
        public const string HttpStatusCode = "http.status_code";

        /// <summary>
        /// URL of the request being handled in this segment of the trace, in standard URI format.
        /// E.g., "https://domain.net/path/to?resource=here".
        /// </summary>
        public const string HttpUrl = "http.url";

        /// <summary>
        /// An address at which messages can be exchanged. E.g. A Kafka record has an associated "topic name"
        /// that can be extracted by the instrumented producer or consumer and stored using this tag.
        /// </summary>
        public const string MessageBusDestination = "message_bus.destination";

        /// <summary>
        /// Remote "address", suitable for use in a networking client library.
        /// This may be a "ip:port", a bare "hostname", a FQDN, or even a JDBC substring like "mysql://prod-db:3306".
        /// </summary>
        public const string PeerAddress = "peer.address";

        /// <summary>
        /// Remote hostname. E.g., "opentracing.io", "internal.dns.name".
        /// </summary>
        public const string PeerHostname = "peer.hostname";

        /// <summary>
        /// Remote IPv4 address as a .-separated tuple. E.g., "127.0.0.1".
        /// </summary>
        public const string PeerIpV4 = "peer.ipv4";

        /// <summary>
        /// Remote IPv6 address as a string of colon-separated 4-char hex tuples.
        /// E.g., "2001:0db8:85a3:0000:0000:8a2e:0370:7334".
        /// </summary>
        public const string PeerIpV6 = "peer.ipv6";

        /// <summary>
        /// Remote port. E.g., 80.
        /// </summary>
        public const string PeerPort = "peer.port";

        /// <summary>
        /// Remote service name (for some unspecified definition of "service").
        /// E.g., "elasticsearch", "a_custom_microservice", "memcache".
        /// </summary>
        public const string PeerService = "peer.service";

        /// <summary>
        /// If greater than 0, a hint to the Tracer to do its best to capture the trace.
        /// If 0, a hint to the trace to not-capture the trace.
        /// If absent, the Tracer should use its default sampling mechanism.
        /// </summary>
        public const string SamplingPriority = "sampling.priority";

        /// <summary>
        /// Either "client" or "server" for the appropriate roles in an RPC,
        /// and "producer" or "consumer" for the appropriate roles in a messaging scenario.
        /// </summary>
        public const string SpanKind = "span.kind";

        /// <summary>
        /// A constant for setting the "span.kind" to indicate that it represents a client span.
        /// </summary>
        public const string SpanKindClient = "client";

        /// <summary>
        /// A constant for setting the "span.kind" to indicate that it represents a consumer span,
        /// in a messaging scenario.
        /// </summary>
        public const string SpanKindConsumer = "consumer";

        /// <summary>
        /// A constant for setting the "span.kind" to indicate that it represents a producer span,
        /// in a messaging scenario.
        /// </summary>
        public const string SpanKindProducer = "producer";

        /// <summary>
        /// A constant for setting the "span.kind" to indicate that it represents a server span.
        /// </summary>
        public const string SpanKindServer = "server";
    }
}

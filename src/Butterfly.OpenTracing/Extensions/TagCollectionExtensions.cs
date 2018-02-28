using System;

namespace Butterfly.OpenTracing
{
    public static class TagCollectionExtensions
    {
        public static TagCollection Set(this TagCollection tagCollection, string key, string value)
        {
            if (tagCollection == null)
            {
                throw new ArgumentNullException(nameof(tagCollection));
            }
            tagCollection[key] = value;
            return tagCollection;
        }

        public static TagCollection Set(this TagCollection tagCollection, string key, bool value)
        {
            return Set(tagCollection, key, value.ToString());
        }

        public static TagCollection Set(this TagCollection tagCollection, string key, int value)
        {
            return Set(tagCollection, key, value.ToString());
        }

        public static TagCollection Set(this TagCollection tagCollection, string key, long value)
        {
            return Set(tagCollection, key, value.ToString());
        }

        public static TagCollection Set(this TagCollection tagCollection, string key, float value)
        {
            return Set(tagCollection, key, value.ToString());
        }

        public static TagCollection Set(this TagCollection tagCollection, string key, double value)
        {
            return Set(tagCollection, key, value.ToString());
        }

        public static TagCollection Service(this TagCollection tagCollection, string service)
        {
            return Set(tagCollection, Tags.Service, service);
        }

        /// <summary>
        /// The software package, framework, library, or module that generated the associated Span.
        /// E.g., "grpc", "Asp.Net Core", "Spring".
        /// </summary>
        public static TagCollection Component(this TagCollection tagCollection, string component)
        {
            return Set(tagCollection, Tags.Component, component);
        }

        /// <summary>
        /// Database instance name. E.g. the "Initial Catalog" value from a SQL connection string.
        /// </summary>
        public static TagCollection DbInstance(this TagCollection tagCollection, string dbInstance)
        {
            return Set(tagCollection, Tags.DbInstance, dbInstance);
        }
        
        /// <summary>
        /// Database instance name. E.g. the "db" value from Redis.
        /// </summary>
        public static TagCollection DbInstance(this TagCollection tagCollection, int db)
        {
            return Set(tagCollection, Tags.DbInstance, db);
        }
        
        /// <summary>
        /// A database statement for the given database type.
        /// E.g., for db.type="sql", "SELECT * FROM wuser_table"; for db.type="redis", "SET mykey 'WuValue'".
        /// </summary>
        public static TagCollection DbStatement(this TagCollection tagCollection, string dbStatement)
        {
            return Set(tagCollection, Tags.DbStatement, dbStatement);
        }
        
        /// <summary>
        /// Database type. For any SQL database, "sql". For others, the lower-case database category,
        /// e.g. "cassandra", "hbase", or "redis".
        /// </summary>
        public static TagCollection DbType(this TagCollection tagCollection, string dbType)
        {
            return Set(tagCollection, Tags.DbType, dbType);
        }
        
        /// <summary>
        /// Username for accessing database. E.g., "readonly_user" or "reporting_user".
        /// </summary>
        public static TagCollection DbUser(this TagCollection tagCollection, string dbUser)
        {
            return Set(tagCollection, Tags.DbUser, dbUser);
        }

        /// <summary>
        /// <c>true</c> if and only if the application considers the operation represented by the Span to have failed.
        /// </summary>
        public static TagCollection Error(this TagCollection tagCollection, bool error)
        {
            return Set(tagCollection, Tags.Error, error);
        }
             
        /// <summary>
        /// HTTP method of the request for the associated Span. E.g., "GET", "POST".
        /// </summary>
        public static TagCollection HttpMethod(this TagCollection tagCollection, string httpMethod)
        {
            return Set(tagCollection, Tags.HttpMethod, httpMethod);
        }
                  
        /// <summary>
        /// HTTP response status code for the associated Span. E.g., 200, 503, 404.
        /// </summary>
        public static TagCollection HttpStatusCode(this TagCollection tagCollection, int httpStatusCode)
        {
            return Set(tagCollection, Tags.HttpStatusCode, httpStatusCode);
        }
                  
        /// <summary>
        /// URL of the request being handled in this segment of the trace, in standard URI format.
        /// E.g., "https://domain.net/path/to?resource=here".
        /// </summary>
        public static TagCollection HttpUrl(this TagCollection tagCollection, string httpUrl)
        {
            return Set(tagCollection, Tags.HttpUrl, httpUrl);
        }
        
        public static TagCollection HttpHost(this TagCollection tagCollection, string host)
        {
            return tagCollection.Set("http.host", host);
        }
        
        public static TagCollection HttpPath(this TagCollection tagCollection, string path)
        {
            return tagCollection.Set("http.path", path);
        }
           
        /// <summary>
        /// An address at which messages can be exchanged. E.g. A Kafka record has an associated "topic name"
        /// that can be extracted by the instrumented producer or consumer and stored using this tag.
        /// </summary>
        public static TagCollection MessageBusDestination(this TagCollection tagCollection, string messageBusDestination)
        {
            return Set(tagCollection, Tags.MessageBusDestination, messageBusDestination);
        }
           
        /// <summary>
        /// Remote "address", suitable for use in a networking client library.
        /// This may be a "ip:port", a bare "hostname", a FQDN, or even a sql connection substring like "mysql://prod-db:3306".
        /// </summary>
        public static TagCollection PeerAddress(this TagCollection tagCollection, string peerAddress)
        {
            return Set(tagCollection, Tags.PeerAddress, peerAddress);
        }
        
        /// <summary>
        /// Remote hostname. E.g., "opentracing.io", "internal.dns.name".
        /// </summary>
        public static TagCollection PeerHostName(this TagCollection tagCollection, string peerHostname)
        {
            return Set(tagCollection, Tags.PeerHostname, peerHostname);
        }
        
        /// <summary>
        /// Remote IPv4 address as a .-separated tuple. E.g., "127.0.0.1".
        /// </summary>
        public static TagCollection PeerIpV4(this TagCollection tagCollection, string peerIpV4)
        {
            return Set(tagCollection, Tags.PeerIpV4, peerIpV4);
        }
        
        /// <summary>
        /// Remote IPv6 address as a string of colon-separated 4-char hex tuples.
        /// E.g., "2001:0db8:85a3:0000:0000:8a2e:0370:7334".
        /// </summary>
        public static TagCollection PeerIpV6(this TagCollection tagCollection, string peerIpV6)
        {
            return Set(tagCollection, Tags.PeerIpV6, peerIpV6);
        }
        
        /// <summary>
        /// Remote port. E.g., 80.
        /// </summary>
        public static TagCollection PeerPort(this TagCollection tagCollection, int peerPort)
        {
            return Set(tagCollection, Tags.PeerPort, peerPort);
        }
        
        /// <summary>
        /// Remote service name (for some unspecified definition of "service").
        /// E.g., "elasticsearch", "a_custom_microservice", "memcache".
        /// </summary>
        public static TagCollection PeerService(this TagCollection tagCollection, string peerService)
        {
            return Set(tagCollection, Tags.PeerService, peerService);
        }
        
        /// <summary>
        /// If greater than 0, a hint to the Tracer to do its best to capture the trace.
        /// If 0, a hint to the trace to not-capture the trace.
        /// If absent, the Tracer should use its default sampling mechanism.
        /// </summary>
        public static TagCollection SamplingPriority(this TagCollection tagCollection, string samplingPriority)
        {
            return Set(tagCollection, Tags.SamplingPriority, samplingPriority);
        }
        
        /// <summary>
        /// Either "client" or "server" for the appropriate roles in an RPC,
        /// and "producer" or "consumer" for the appropriate roles in a messaging scenario.
        /// </summary>
        public static TagCollection SpanKind(this TagCollection tagCollection, string spanKind)
        {
            return Set(tagCollection, Tags.SpanKind, spanKind);
        }
        
        /// <summary>
        /// A constant for setting the "span.kind" to indicate that it represents a "client" span.
        /// </summary>
        public static TagCollection Client(this TagCollection tagCollection)
        {
            return Set(tagCollection, Tags.SpanKind, Tags.SpanKindClient);
        }
        
        /// <summary>
        /// A constant for setting the "span.kind" to indicate that it represents a "server" span.
        /// </summary>
        public static TagCollection Server(this TagCollection tagCollection)
        {
            return Set(tagCollection, Tags.SpanKind, Tags.SpanKindServer);
        }
        
        /// <summary>
        /// A constant for setting the "span.kind" to indicate that it represents a "consumer" span,
        /// in a messaging scenario.
        /// </summary>
        public static TagCollection Consumer(this TagCollection tagCollection)
        {
            return Set(tagCollection, Tags.SpanKind, Tags.SpanKindConsumer);
        }
        
        /// <summary>
        /// A constant for setting the "span.kind" to indicate that it represents a "producer" span,
        /// in a messaging scenario.
        /// </summary>
        public static TagCollection Producer(this TagCollection tagCollection)
        {
            return Set(tagCollection, Tags.SpanKind, Tags.SpanKindProducer);
        }
    }
}
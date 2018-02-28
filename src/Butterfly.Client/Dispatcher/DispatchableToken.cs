using System;

namespace Butterfly.Client
{
    public class DispatchableToken : IEquatable<DispatchableToken>
    {
        private static readonly DispatchableToken _spanToken = "span";

        public static DispatchableToken SpanToken
        {
            get { return _spanToken; }
        }

        private readonly string _token;

        private DispatchableToken(string token)
        {
            _token = token;
        }

        public bool Equals(DispatchableToken other)
        {
            return _token.Equals(other._token);
        }

        public override bool Equals(object obj)
        {
            if (obj is DispatchableToken t)
            {
                return Equals(t);
            }
            if (obj is string str)
            {
                return _token.Equals(str);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return _token.GetHashCode();
        }

        public static implicit operator DispatchableToken(string token)
        {
            return new DispatchableToken(token);
        }

        public static implicit operator string(DispatchableToken token)
        {
            return token._token;
        }

        public static bool operator ==(DispatchableToken obj1, DispatchableToken obj2)
        {
            return obj1._token == obj2._token;
        }

        public static bool operator !=(DispatchableToken obj1, DispatchableToken obj2)
        {
            return obj1._token != obj2._token;
        }

        public static bool operator ==(DispatchableToken obj1, string obj2)
        {
            return obj1._token == obj2;
        }

        public static bool operator !=(DispatchableToken obj1, string obj2)
        {
            return obj1._token != obj2;
        }

        public static bool operator ==(string obj1, DispatchableToken obj2)
        {
            return obj1 == obj2._token;
        }

        public static bool operator !=(string obj1, DispatchableToken obj2)
        {
            return obj1 != obj2._token;
        }
    }
}
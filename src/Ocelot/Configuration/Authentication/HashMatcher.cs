using System;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Ocelot.Configuration.Authentication
{
    public class HashMatcher : IHashMatcher
    {
        public bool Match(string password, string salt, string hash)
        {
            byte[] s = Convert.FromBase64String(salt);

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: s,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

                return hashed == hash;
        }
    }
}
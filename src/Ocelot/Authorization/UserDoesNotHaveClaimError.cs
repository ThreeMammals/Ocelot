﻿namespace Ocelot.Authorization
{
    using Ocelot.Errors;

    public class UserDoesNotHaveClaimError : Error
    {
        public UserDoesNotHaveClaimError(string message)
            : base(message, OcelotErrorCode.UserDoesNotHaveClaimError, 403)
        {
        }
    }
}

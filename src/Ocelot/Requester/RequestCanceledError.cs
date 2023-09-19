using Ocelot.Errors;

namespace Ocelot.Requester
{
    public class RequestCanceledError : Error
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestCanceledError"/> class.
        /// Creates <see cref="RequestCanceledError"/> object by the message.
        /// <para>Status code refer to:</para>
        /// <para>https://stackoverflow.com/questions/46234679/what-is-the-correct-http-status-code-for-a-cancelled-request?answertab=votes#tab-top .</para>
        /// <para>https://httpstatuses.com/499 .</para>
        /// </summary>
        /// <param name="message">The message text.</param>
        public RequestCanceledError(string message)
            : base(message, OcelotErrorCode.RequestCanceled, 499) // https://httpstatuses.com/499
        {
        }
    }
}

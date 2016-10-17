namespace Ocelot.Library.Repository
{
    using Errors;

    public class CannotAddDataError : Error
    {
        public CannotAddDataError(string message) : base(message, OcelotErrorCode.CannotAddDataError)
        {
        }
    }
}

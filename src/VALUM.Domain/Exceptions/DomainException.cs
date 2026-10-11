using System;

namespace VALUM.Domain.Exceptions
{
    public class DomainException : Exception
    {
        public DomainException() : base("A domain rule was violated.") { }
        public DomainException(string message) : base(message) { }
        public DomainException(string message, Exception inner) : base(message, inner) { }
    }
}

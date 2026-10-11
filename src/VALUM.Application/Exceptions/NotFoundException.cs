using System;

namespace VALUM.Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException() : base("The requested resource was not found.") { }
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}

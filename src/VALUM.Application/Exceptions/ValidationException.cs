using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;

namespace VALUM.Application.Exceptions
{
    public class ValidationException : Exception
    {
        private readonly IReadOnlyCollection<ValidationFailure> _errors;

        public IReadOnlyCollection<ValidationFailure> Errors => _errors;

        public ValidationException(IEnumerable<ValidationFailure> failures)
            : base(CreateMessageAndSnapshot(failures, out var snapshot))
        {
            // _errors is the materialized, read-only snapshot created by CreateMessageAndSnapshot
            _errors = snapshot;
        }

        private static string CreateMessageAndSnapshot(IEnumerable<ValidationFailure> failures, out IReadOnlyCollection<ValidationFailure> snapshot)
        {
            var list = failures?.ToList() ?? new List<ValidationFailure>();
            snapshot = list.AsReadOnly();

            if (!list.Any())
            {
                return "One or more validation failures have occurred.";
            }

            return "Validation failed: " + string.Join("; ", list.Select(f => f.ErrorMessage));
        }
    }
}

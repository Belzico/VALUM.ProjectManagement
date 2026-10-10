using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
// NOTE: avoid importing FluentValidation's ValidationException name into this file's scope
// to prevent ambiguity with our custom VALUM.Application.Exceptions.ValidationException.

namespace VALUM.Application.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IValidator<TRequest>[] _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            // materialize validators to avoid multiple enumeration
            _validators = (validators as IValidator<TRequest>[]) ?? validators?.ToArray() ?? Array.Empty<IValidator<TRequest>>();
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Length == 0)
            {
                return await next(cancellationToken);
            }

            var failures = new List<ValidationFailure>();

            // Execute validators sequentially, collecting all failures
            foreach (var validator in _validators)
            {
                var result = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
                if (result?.Errors != null)
                {
                    failures.AddRange(result.Errors.Where(e => e != null));
                }
            }

            if (failures.Count > 0)
            {
                throw new VALUM.Application.Exceptions.ValidationException(failures);
            }

            return await next(cancellationToken);
        }
    }
}

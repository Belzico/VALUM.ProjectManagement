using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Xunit;

namespace VALUM.Tests.Unit.Application.Behaviors
{
    // These tests follow TDD: they describe the expected behavior of
    // VALUM.Application.Behaviors.ValidationBehavior<TRequest, TResponse>
    // and the custom VALUM.Application.Exceptions.ValidationException.
    // The production behavior and exception are not implemented yet.
    public class ValidationBehaviorTests
    {
        private record TestRequest(int Value) : IRequest<TestResponse>;
        private record TestResponse(string Message);

        private class ValidRequestValidator : AbstractValidator<TestRequest>
        {
            public ValidRequestValidator()
            {
                RuleFor(x => x.Value).GreaterThan(0);
            }
        }

        private class InvalidRequestValidator : AbstractValidator<TestRequest>
        {
            public InvalidRequestValidator()
            {
                RuleFor(x => x.Value).GreaterThan(10).WithMessage("Value must be > 10");
            }
        }

        [Fact]
        public async Task ValidRequest_ExecutesNextExactlyOnce()
        {
            // Arrange
            var request = new TestRequest(5);

            var validators = new IValidator<TestRequest>[] { new ValidRequestValidator() };

            // The behavior under test will be implemented in VALUM.Application
            var behavior = new VALUM.Application.Behaviors.ValidationBehavior<TestRequest, TestResponse>(validators);

            var response = new TestResponse("ok");
            var callCount = 0;
            RequestHandlerDelegate<TestResponse> next = ct =>
            {
                callCount++;
                return Task.FromResult(response);
            };

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            Assert.Equal(response, result);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task InvalidRequest_ThrowsValidationException_AndDoesNotInvokeNext()
        {
            // Arrange
            var request = new TestRequest(5);

            var validators = new IValidator<TestRequest>[] { new InvalidRequestValidator() };

            var behavior = new VALUM.Application.Behaviors.ValidationBehavior<TestRequest, TestResponse>(validators);

            var callCount = 0;
            RequestHandlerDelegate<TestResponse> next = ct =>
            {
                callCount++;
                return Task.FromResult(new TestResponse("should not be used"));
            };

            // Act & Assert
            await Assert.ThrowsAsync<VALUM.Application.Exceptions.ValidationException>(async () =>
            {
                await behavior.Handle(request, next, CancellationToken.None);
            });

            Assert.Equal(0, callCount);
        }

        [Fact]
        public async Task NoValidators_ExecutesNextNormally()
        {
            // Arrange
            var request = new TestRequest(0);

            var validators = Enumerable.Empty<IValidator<TestRequest>>();

            var behavior = new VALUM.Application.Behaviors.ValidationBehavior<TestRequest, TestResponse>(validators);

            var response = new TestResponse("ok");
            var callCount = 0;
            RequestHandlerDelegate<TestResponse> next = ct =>
            {
                callCount++;
                return Task.FromResult(response);
            };

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            Assert.Equal(response, result);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task MultipleValidationFailures_AreCollectedInException()
        {
            // Arrange
            var request = new TestRequest(0);

            // Two validators that will each produce a failure
            var validatorA = new InlineValidator<TestRequest>();
            validatorA.RuleFor(x => x.Value).GreaterThan(0).WithMessage("A");

            var validatorB = new InlineValidator<TestRequest>();
            validatorB.RuleFor(x => x.Value).GreaterThan(1).WithMessage("B");

            var validators = new IValidator<TestRequest>[] { validatorA, validatorB };

            var behavior = new VALUM.Application.Behaviors.ValidationBehavior<TestRequest, TestResponse>(validators);

            var callCount = 0;
            RequestHandlerDelegate<TestResponse> next = ct =>
            {
                callCount++;
                return Task.FromResult(new TestResponse("should not be used"));
            };

            // Act
            var ex = await Assert.ThrowsAsync<VALUM.Application.Exceptions.ValidationException>(async () =>
            {
                await behavior.Handle(request, next, CancellationToken.None);
            });

            // The custom ValidationException should expose a structured Errors collection
            Assert.NotNull(ex.Errors);
            var errors = ex.Errors.ToList();

            // Exactly two failures collected
            Assert.Equal(2, errors.Count);

            // Both failures must refer to the 'Value' property
            Assert.All(errors, e => Assert.Equal("Value", e.PropertyName));

            // Both messages "A" and "B" are present among the failures
            Assert.Contains(errors, e => e.ErrorMessage == "A");
            Assert.Contains(errors, e => e.ErrorMessage == "B");

            Assert.Equal(0, callCount);
        }
    }
}

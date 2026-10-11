using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using VALUM.Application;
// Do not import VALUM.Application.Exceptions to avoid ambiguity with FluentValidation.ValidationException
using Xunit;

namespace VALUM.Tests.Integration.Application
{
    // Integration-style tests for the (not-yet-implemented) VALUM.Application.DependencyInjection.AddApplication
    // These tests register test-only handlers and validators and then call AddApplication on the service collection.
    public class ApplicationDependencyInjectionTests
    {
        // Test-only command/response/handler fixtures
        public record ValidCommand(int Value) : IRequest<ValidResponse>;
        public record ValidResponse(string Message);

        public class ValidCommandHandler : IRequestHandler<ValidCommand, ValidResponse>
        {
            public static int CallCount;

            public Task<ValidResponse> Handle(ValidCommand request, CancellationToken cancellationToken)
            {
                CallCount++;
                return Task.FromResult(new ValidResponse("ok"));
            }
        }

        public class ValidCommandValidator : AbstractValidator<ValidCommand>
        {
            public ValidCommandValidator()
            {
                RuleFor(x => x.Value).GreaterThan(0);
            }
        }

        public record InvalidCommand(int Value) : IRequest<InvalidResponse>;
        public record InvalidResponse(string Message);

        public class InvalidCommandHandler : IRequestHandler<InvalidCommand, InvalidResponse>
        {
            public static int CallCount;

            public Task<InvalidResponse> Handle(InvalidCommand request, CancellationToken cancellationToken)
            {
                CallCount++;
                return Task.FromResult(new InvalidResponse("ok"));
            }
        }

        public class InvalidCommandValidator : AbstractValidator<InvalidCommand>
        {
            public InvalidCommandValidator()
            {
                RuleFor(x => x.Value).GreaterThan(10).WithMessage("Must be > 10");
            }
        }

        public record NoValidatorCommand(int Value) : IRequest<NoValidatorResponse>;
        public record NoValidatorResponse(string Message);

        public class NoValidatorHandler : IRequestHandler<NoValidatorCommand, NoValidatorResponse>
        {
            public static int CallCount;

            public Task<NoValidatorResponse> Handle(NoValidatorCommand request, CancellationToken cancellationToken)
            {
                CallCount++;
                return Task.FromResult(new NoValidatorResponse("ok"));
            }
        }

        [Fact]
        public async Task ValidCommand_GoesThroughMediator_ExecutesHandlerOnce()
        {
            var services = new ServiceCollection();

            // Register test-only handler and validator explicitly (AddApplication will scan production assembly)
            services.AddTransient<IRequestHandler<ValidCommand, ValidResponse>, ValidCommandHandler>();
            services.AddTransient<IValidator<ValidCommand>, ValidCommandValidator>();

            // Invoke the application-level composition root (not implemented yet)
            services.AddApplication();
            // Build provider and resolve mediator (will work once AddApplication registers MediatR and pipeline)
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();

            // Isolate counter
            ValidCommandHandler.CallCount = 0;

            var response = await mediator.Send(new ValidCommand(5));

            Assert.Equal("ok", response.Message);
            Assert.Equal(1, ValidCommandHandler.CallCount);
        }

        [Fact]
        public async Task InvalidCommand_ThrowsValidationException_AndDoesNotInvokeHandler()
        {
            var services = new ServiceCollection();

            services.AddTransient<IRequestHandler<InvalidCommand, InvalidResponse>, InvalidCommandHandler>();
            services.AddTransient<IValidator<InvalidCommand>, InvalidCommandValidator>();

            services.AddApplication();
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();

            InvalidCommandHandler.CallCount = 0;

            await Assert.ThrowsAsync<VALUM.Application.Exceptions.ValidationException>(async () =>
            {
                await mediator.Send(new InvalidCommand(5));
            });

            Assert.Equal(0, InvalidCommandHandler.CallCount);
        }

        [Fact]
        public async Task CommandWithoutValidators_ExecutesHandlerNormally()
        {
            var services = new ServiceCollection();

            services.AddTransient<IRequestHandler<NoValidatorCommand, NoValidatorResponse>, NoValidatorHandler>();

            services.AddApplication();
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();

            NoValidatorHandler.CallCount = 0;

            var response = await mediator.Send(new NoValidatorCommand(0));

            Assert.Equal("ok", response.Message);
            Assert.Equal(1, NoValidatorHandler.CallCount);
        }

        [Fact]
        public async Task MultipleValidationFailures_ArePreservedInException_WhenUsingMediator()
        {
            var services = new ServiceCollection();

            // Reuse InvalidCommand but register two validators that both fail
            services.AddTransient<IRequestHandler<InvalidCommand, InvalidResponse>, InvalidCommandHandler>();

            // Validator A
            services.AddTransient<IValidator<InvalidCommand>>(sp =>
            {
                var v = new InlineValidator<InvalidCommand>();
                v.RuleFor(x => x.Value).GreaterThan(0).WithMessage("A");
                return v;
            });

            // Validator B
            services.AddTransient<IValidator<InvalidCommand>>(sp =>
            {
                var v = new InlineValidator<InvalidCommand>();
                v.RuleFor(x => x.Value).GreaterThan(1).WithMessage("B");
                return v;
            });

            services.AddApplication();
            var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();

            InvalidCommandHandler.CallCount = 0;

            var ex = await Assert.ThrowsAsync<VALUM.Application.Exceptions.ValidationException>(async () =>
            {
                await mediator.Send(new InvalidCommand(0));
            });

            // Exception should contain two failures for the 'Value' property with messages A and B
            Assert.NotNull(ex.Errors);
            Assert.Equal(2, ex.Errors.Count);
            Assert.Contains(ex.Errors, e => e.ErrorMessage == "A");
            Assert.Contains(ex.Errors, e => e.ErrorMessage == "B");
            Assert.All(ex.Errors, e => Assert.Equal("Value", e.PropertyName));

            Assert.Equal(0, InvalidCommandHandler.CallCount);
        }
    }
}

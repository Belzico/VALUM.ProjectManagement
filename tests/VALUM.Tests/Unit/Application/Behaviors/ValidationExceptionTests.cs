using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using Xunit;

namespace VALUM.Tests.Unit.Application.Behaviors
{
    public class ValidationExceptionTests
    {
        [Fact]
        public void Errors_AreIndependentOfOriginalList()
        {
            // Arrange
            var original = new List<ValidationFailure>
            {
                new ValidationFailure("Value", "first")
            };

            // Act
            var ex = new VALUM.Application.Exceptions.ValidationException(original);

            // Mutate original after constructing exception
            original.Add(new ValidationFailure("Value", "second"));

            // Assert: exception.Errors should not reflect mutation
            Assert.Equal(1, ex.Errors.Count);
            Assert.Equal("first", ex.Errors.First().ErrorMessage);
        }

        private class SingleEnumerationFailures : IEnumerable<ValidationFailure>
        {
            private readonly ValidationFailure[] _items;
            public int Enumerations { get; private set; }

            public SingleEnumerationFailures(params ValidationFailure[] items)
            {
                _items = items;
            }

            public IEnumerator<ValidationFailure> GetEnumerator()
            {
                Enumerations++;
                foreach (var it in _items)
                {
                    yield return it;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Fact]
        public void Constructor_EnumeratesFailuresOnlyOnce()
        {
            // Arrange
            var source = new SingleEnumerationFailures(
                new ValidationFailure("Value", "A"),
                new ValidationFailure("Value", "B")
            );

            // Act
            var ex = new VALUM.Application.Exceptions.ValidationException(source);

            // Assert: constructor should enumerate the source exactly once
            Assert.Equal(1, source.Enumerations);
            Assert.Equal(2, ex.Errors.Count);
        }
    }
}

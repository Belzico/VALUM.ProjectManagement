using System;
using Xunit;
using Moq;

namespace VALUM.Tests
{
    public class TestingSetupTests
    {
        [Fact]
        public void BasicAssertion_Works()
        {
            Assert.Equal(4, 2 + 2);
        }

        [Fact]
        public void DisposableMock_DisposeCalledOnce()
        {
            var mock = new Mock<IDisposable>(MockBehavior.Strict);
            mock.Setup(m => m.Dispose());
            mock.Object.Dispose();
            mock.Verify(m => m.Dispose(), Times.Exactly(1));
        }
    }
}

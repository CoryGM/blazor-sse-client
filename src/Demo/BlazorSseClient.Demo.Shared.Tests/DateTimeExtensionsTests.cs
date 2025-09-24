using BlazorSseClient.Demo.Shared.Extensions;

namespace BlazorSseClient.Demo.Shared.Tests
{
    [TestClass]
    public sealed class DateTimeExtensionsTests
    {
        //  Create unit tests for the DateTimeExtensions class in the Blazor.Demo.Shared project.
        [TestMethod]
        public void ToReadableDuration_ShouldReturnCorrectFormat()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1, 0, 0, 0);
            var endDate = new DateTime(2023, 1, 2, 2, 3, 4);
            var expected = "1 day, 2 hours, 3 minutes";

            // Act
            var result = startDate.ToReadableDuration(endDate, 3, LabelStyle.Short);

            // Assert
            Assert.AreEqual(expected, result);
        }

    }
}

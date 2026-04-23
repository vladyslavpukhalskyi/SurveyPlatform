using Xunit;
using SurveyPlatform.Core.Entities;

namespace SurveyPlatform.Tests.UnitTests;

public class ValidationTests
{
    [Theory]
    [InlineData("1", true)]
    [InlineData("5", true)]
    [InlineData("0", false)]
    [InlineData("6", false)]
    [InlineData("NotANumber", false)]
    public void Rating_Validation_ShouldWork(string value, bool expectedResult)
    {
        // Simple logic for validation (we'll move this to a service later)
        bool isValid = int.TryParse(value, out int rating) && rating >= 1 && rating <= 5;

        Assert.Equal(expectedResult, isValid);
    }
}
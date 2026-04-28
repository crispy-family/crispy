using System.Net;
using System.Text.Json;
using Crispy.Application.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Crispy.Tests
{
    public class RecipeImportServiceTests
    {
        private readonly Mock<ILogger<RecipeImportService>> _loggerMock;

        public RecipeImportServiceTests()
        {
            _loggerMock = new Mock<ILogger<RecipeImportService>>();
        }

        private HttpClient CreateHttpClientWithMockResponse(string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseContent)
                })
                .Verifiable();

            return new HttpClient(handlerMock.Object);
        }

        [Fact]
        public async Task ImportAsync_WithInvalidUrl_ReturnsNull()
        {
            var httpClient = CreateHttpClientWithMockResponse("");
            var service = new RecipeImportService(httpClient, _loggerMock.Object);

            var result = await service.ImportAsync("invalid-url-here");

            Assert.Null(result);
        }

        [Fact]
        public async Task ImportAsync_WhenHttpServerReturns404_ReturnsNull()
        {
            var httpClient = CreateHttpClientWithMockResponse("Not Found", HttpStatusCode.NotFound);
            var service = new RecipeImportService(httpClient, _loggerMock.Object);
            var validUrl = "https://example.com/recipe/1";

            var result = await service.ImportAsync(validUrl);

            Assert.Null(result);
        }

        [Fact]
        public async Task ImportAsync_WithJsonLd_ParsesSuccessfully()
        {
            var validUrl = "https://example.com/recipe/json-test";
            var jsonLdContent = @"
            <html>
                <head>
                    <script type=""application/ld+json"">
                    {
                        ""@context"": ""https://schema.org"",
                        ""@type"": ""Recipe"",
                        ""name"": ""Тестовий рецепт JSON"",
                        ""description"": ""Опис рецепту з JSON-LD"",
                        ""image"": [""https://example.com/img.jpg""],
                        ""recipeIngredient"": [
                            ""1 шт помідор"",
                            ""200 гр сиру""
                        ]
                    }
                    </script>
                </head>
                <body></body>
            </html>";

            var httpClient = CreateHttpClientWithMockResponse(jsonLdContent);
            var service = new RecipeImportService(httpClient, _loggerMock.Object);

            var result = await service.ImportAsync(validUrl);

            Assert.NotNull(result);
            Assert.Equal("Тестовий рецепт JSON", result.Title);
            Assert.Equal("Опис рецепту з JSON-LD", result.Description);
            Assert.Equal("https://example.com/img.jpg", result.ImageUrl);
            Assert.Equal(validUrl, result.SourceUrl);

            Assert.NotNull(result.Ingredients);
            Assert.Equal(2, result.Ingredients.Count);
        }

        [Fact]
        public async Task ImportAsync_WithOpenGraphMetaTags_ParsesFallbackSuccessfully()
        {
            var validUrl = "https://example.com/recipe/og-test";
            var htmlContent = @"
            <html>
                <head>
                    <title>Звичайний Title</title>
                    <meta property=""og:title"" content=""Тестовий рецепт з OpenGraph"" />
                    <meta property=""og:image"" content=""https://example.com/og-image.jpg"" />
                    <meta name=""description"" content=""Опис в метатегах"" />
                </head>
                <body></body>
            </html>";

            var httpClient = CreateHttpClientWithMockResponse(htmlContent);
            var service = new RecipeImportService(httpClient, _loggerMock.Object);

            var result = await service.ImportAsync(validUrl);

            Assert.NotNull(result);
            Assert.Equal("Тестовий рецепт з OpenGraph", result.Title);
            Assert.Equal("Опис в метатегах", result.Description);
            Assert.Equal("https://example.com/og-image.jpg", result.ImageUrl);
            Assert.Empty(result.Ingredients);
        }

        [Fact]
        public async Task ImportAsync_PageWithoutRecipeData_ReturnsNull()
        {
            var validUrl = "https://example.com/not-a-recipe";
            var htmlContent = @"
            <html>
                <head></head>
                <body>Some content</body>
            </html>";

            var httpClient = CreateHttpClientWithMockResponse(htmlContent);
            var service = new RecipeImportService(httpClient, _loggerMock.Object);

            var result = await service.ImportAsync(validUrl);

            Assert.Null(result);
        }

        // ТЕСТИ НА ПАРСИНГ КРОКІВ ПРИГОТУВАННЯ (ДЛЯ ІНТЕРАКТИВНОГО РЕЖИМУ)

        [Fact]
        public async Task ImportAsync_WithHowToStepArray_ExtractsInstructionsCorrectly()
        {
            var validUrl = "https://example.com/recipe/steps-objects";
            var jsonLdContent = @"
            <html>
                <head>
                    <script type=""application/ld+json"">
                    {
                        ""@context"": ""https://schema.org"",
                        ""@type"": ""Recipe"",
                        ""name"": ""Recipe with Steps"",
                        ""recipeInstructions"": [
                            { ""@type"": ""HowToStep"", ""text"": ""Помити овочі."" },
                            { ""@type"": ""HowToStep"", ""text"": ""Нарізати кубиками."" }
                        ]
                    }
                    </script>
                </head>
                <body></body>
            </html>";

            var httpClient = CreateHttpClientWithMockResponse(jsonLdContent);
            var service = new RecipeImportService(httpClient, _loggerMock.Object);

            var result = await service.ImportAsync(validUrl);

            Assert.NotNull(result);
            var expectedDescription = string.Join(Environment.NewLine, "Помити овочі.", "Нарізати кубиками.");
            Assert.Equal(expectedDescription, result.Description);
        }

        [Fact]
        public async Task ImportAsync_WithStringArray_ExtractsInstructionsCorrectly()
        {
            var validUrl = "https://example.com/recipe/steps-strings";
            var jsonLdContent = @"
            <html>
                <head>
                    <script type=""application/ld+json"">
                    {
                        ""@context"": ""https://schema.org"",
                        ""@type"": ""Recipe"",
                        ""name"": ""Recipe with Strings"",
                        ""recipeInstructions"": [
                            ""Змішати інгредієнти."",
                            ""Випікати 30 хвилин.""
                        ]
                    }
                    </script>
                </head>
                <body></body>
            </html>";

            var httpClient = CreateHttpClientWithMockResponse(jsonLdContent);
            var service = new RecipeImportService(httpClient, _loggerMock.Object);

            var result = await service.ImportAsync(validUrl);

            Assert.NotNull(result);
            var expectedDescription = string.Join(Environment.NewLine, "Змішати інгредієнти.", "Випікати 30 хвилин.");
            Assert.Equal(expectedDescription, result.Description);
        }

        [Fact]
        public async Task ImportAsync_WithSingleString_ExtractsInstructionsCorrectly()
        {
            var validUrl = "https://example.com/recipe/single-string";
            var jsonLdContent = @"
            <html>
                <head>
                    <script type=""application/ld+json"">
                    {
                        ""@context"": ""https://schema.org"",
                        ""@type"": ""Recipe"",
                        ""name"": ""Recipe with Single String"",
                        ""recipeInstructions"": ""Просто приготуйте це.""
                    }
                    </script>
                </head>
                <body></body>
            </html>";

            var httpClient = CreateHttpClientWithMockResponse(jsonLdContent);
            var service = new RecipeImportService(httpClient, _loggerMock.Object);

            var result = await service.ImportAsync(validUrl);

            Assert.NotNull(result);
            Assert.Equal("Просто приготуйте це.", result.Description);
        }
    }
}
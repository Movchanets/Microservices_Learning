using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiGateway.Contracts;
using ApiGateway.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApiGateway.UnitTests.Services;

public sealed class CartBffServiceTests
{
    private static readonly Guid TestSkuId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string TestSkuCode = "SKU-001";
    private static readonly Guid TestProductId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TestStoreId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task GetCartWithDetailsAsync_ShouldPropagateSkuIdAndSkuCode()
    {
        // Arrange — cart-api returns SkuId + SkuCode in JSON
        var rawCartJson = $$"""
        {
            "buyerId": "dddddddd-dddd-dddd-dddd-dddddddddddd",
            "cartId": "11111111-1111-1111-1111-111111111111",
            "items": [
                {
                    "productId": "{{TestProductId}}",
                    "skuId": "{{TestSkuId}}",
                    "skuCode": "{{TestSkuCode}}",
                    "storeId": "{{TestStoreId}}",
                    "quantity": 2,
                    "price": 25.00,
                    "lineTotal": 50.00
                }
            ],
            "totalPrice": 50.00,
            "totalItems": 2,
            "updatedAt": "2025-01-01T00:00:00Z"
        }
        """;

        var cartHandler = new MockHttpMessageHandler(rawCartJson);
        var catalogHandler = new MockHttpMessageHandler("[]"); // no product enrichment needed

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient("cart-api"))
            .Returns(new HttpClient(cartHandler) { BaseAddress = new Uri("http://cart-api") });
        httpClientFactory.Setup(f => f.CreateClient("catalog-api"))
            .Returns(new HttpClient(catalogHandler) { BaseAddress = new Uri("http://catalog-api") });

        var service = new CartBffService(httpClientFactory.Object, Mock.Of<ILogger<CartBffService>>());

        // Act
        var result = await service.GetCartWithDetailsAsync("dddddddd-dddd-dddd-dddd-dddddddddddd", null, null);

        // Assert — SkuId and SkuCode must survive the BFF mapping
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.SkuId.Should().Be(TestSkuId);
        item.SkuCode.Should().Be(TestSkuCode);
    }

    /// <summary>
    /// Minimal handler that returns pre-canned JSON for any request.
    /// </summary>
    private sealed class MockHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}

using Moq;
using Sindika.AspNet.Midtrans.Contracts;
using Sindika.AspNet.Midtrans.Models.Request.Snap;
using Sindika.AspNet.Midtrans.Models.Response.Snap;
using Xunit;

namespace simple_midtrans_test_api.test;

public class PaymentApiUnitTest
{
    [Fact]
    public async Task CreateTransaction_CalculatesGrossAmountCorrectly()
    {
        var mockMidtransClient = new Mock<IMidtransClient>();

        mockMidtransClient
            .Setup(x => x.Snap.CreateTransactionAsync(
                It.IsAny<SnapTransactionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SnapTransactionResponse
            {
                Token = "dummy-snap-token-123",
                RedirectUrl = "https://app.sandbox.midtrans.com/snap/v2/vtweb/dummy-snap-token-123"
            });

        var api = new PaymentAPI(mockMidtransClient.Object);

        var request = new CreateTransactionRequest(
            Product: new List<ProductDto>
            {
                new("P01", "Kopi Susu", "Minuman", 18000, 1, "Ice", new List<string>()),
                new("P02", "Roti Bakar", "Makanan", 15000, 2, "Cokelat", new List<string>())
            },
            Customer: new CustomerDetailsDto("daffa", "Muhammad Daffa")
        );

        var result = await api.CreateTransaction(request, mockMidtransClient.Object);

        Assert.NotNull(result);

        Assert.Equal("dummy-snap-token-123", result.Token);
        Assert.Equal("https://app.sandbox.midtrans.com/snap/v2/vtweb/dummy-snap-token-123", result.RedirectUrl);

        mockMidtransClient.Verify(x => x.Snap.CreateTransactionAsync(
            It.Is<SnapTransactionRequest>(req => req.TransactionDetails.GrossAmount == 48000),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}
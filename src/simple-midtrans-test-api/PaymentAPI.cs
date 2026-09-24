using System.Text.Json;
using Carter;
using Microsoft.AspNetCore.Mvc;
using Sindika.AspNet.Midtrans.Contracts;
using Sindika.AspNet.Midtrans.Enums;
using Sindika.AspNet.Midtrans.Models.Common;
using Sindika.AspNet.Midtrans.Models.Request.Snap;

namespace simple_midtrans_test_api;

public record ProductDto(string ProductId, string ProductName, string ProductCategory, decimal Price, int Amount, string variant, List<string> addons);
public record CustomerDetailsDto(string Username, string Name);

public record CreateTransactionRequest(
    ProductDto Product,
    CustomerDetailsDto Customer
);

public record CheckTransactionRequest(string OrderId);

class PaymentAPI : ICarterModule
{

    private IMidtransClient _midtransClient;
    private Dictionary<string, object> data;

    public PaymentAPI(IMidtransClient midtransClient)
    {
        _midtransClient = midtransClient;

        string resourcesPath = "Resources";
        if (Directory.Exists(Path.Combine(resourcesPath))) Directory.CreateDirectory(Path.Combine(resourcesPath));

        string filePath = Path.Combine("Resources", "data.json");
        if (!File.Exists(filePath)) File.WriteAllText(filePath, "{}");

        data = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(filePath)) ?? new();
    }

    private void SaveToFile()
    {
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine("Resources", "data.json"), json);
    }

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/transaction/create", CreateTransaction);
        app.MapPost("api/transaction/check", CheckTransaction);
    }

    public async Task<IResult> CreateTransaction([FromBody] CreateTransactionRequest req)
    {

        string OrderId = Guid.NewGuid().ToString();
        SnapTransactionRequest snapRequest = new SnapTransactionRequest()
        {
            TransactionDetails = new TransactionDetails()
            {
                OrderId = OrderId,
                Currency = Currency.Idr,
                GrossAmount = req.Product.Amount,
            },

            ItemDetails = new List<ItemDetails>
            {
                new ItemDetails()
                {
                    Id = req.Product.ProductId,
                    Name = req.Product.ProductName,
                    Price = req.Product.Price,
                    Quantity = req.Product.Amount,
                    Category = req.Product.ProductCategory
                }
            },

            CustomerDetails = new CustomerDetails()
            {
                FirstName = req.Customer.Name,
            }
        };

        var response = await _midtransClient.Snap.CreateTransactionAsync(snapRequest);

        data[OrderId] = req;
        SaveToFile();

        return Results.Ok(new
        {
            token = response.Token,
            redirectUrl = response.RedirectUrl
        });
    }

    public async Task<IResult> CheckTransaction([FromBody] CheckTransactionRequest req)
    {
        await Task.CompletedTask;
        if (data.ContainsKey(req.OrderId))
        {
            return Results.Ok(new { message = "Transaksi ditemukan", data = data[req.OrderId] });
        }

        return Results.NotFound(new { message = "Transaksi tidak ditemukan" });
    }


}
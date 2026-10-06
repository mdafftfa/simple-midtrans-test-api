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
    List<ProductDto> Product,
    CustomerDetailsDto Customer
);

public record CreateTransactionResponse(string Token, string RedirectUrl);

public record CheckTransactionRequest(string OrderId);

public class PaymentAPI : ICarterModule
{

    private Dictionary<string, object> data;
    private readonly string _filePath;

    public PaymentAPI(IMidtransClient midtransClient)
    {
        string resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
        _filePath = Path.Combine(resourcesPath, "data.json");

        try
        {
            if (!Directory.Exists(resourcesPath)) Directory.CreateDirectory(resourcesPath);
            if (!File.Exists(_filePath)) File.WriteAllText(_filePath, "{}");

            string jsonContent = File.ReadAllText(_filePath);
            data = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent) ?? new();
        }
        catch
        {
            data = new Dictionary<string, object>();
        }
    }

    private void SaveToFile()
    {
        try
        {
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Fail silently / log warning jika gagal menulis file saat unit test
        }
    }

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/transaction")
            .WithTags("Transaction");

        group.MapPost("/create", CreateTransaction);
        group.MapPost("/check", CheckTransaction);
    }

    public async Task<CreateTransactionResponse> CreateTransaction([FromBody] CreateTransactionRequest req, IMidtransClient _midtransClient)
    {
        string orderId = "ORDER-" + Guid.NewGuid().ToString("N")[..12].ToUpper();

        long grossAmount = Convert.ToInt64(req.Product.Sum(p => p.Price * p.Amount));

        var itemDetailsList = req.Product.Select(p => {
            string safeName = string.IsNullOrWhiteSpace(p.ProductName) ? "Produk" : p.ProductName.Trim();
            if (safeName.Length > 45)
            {
                safeName = safeName.Substring(0, 45);
            }

            string safeId = string.IsNullOrWhiteSpace(p.ProductId) ? Guid.NewGuid().ToString("N")[..8] : p.ProductId.Trim();

            return new ItemDetails()
            {
                Id = safeId,
                Name = safeName,
                Price = Convert.ToInt64(p.Price),
                Quantity = p.Amount <= 0 ? 1 : p.Amount,
                Category = string.IsNullOrWhiteSpace(p.ProductCategory) ? "Umum" : p.ProductCategory
            };
        }).ToList();

        SnapTransactionRequest snapRequest = new SnapTransactionRequest()
        {
            TransactionDetails = new TransactionDetails()
            {
                OrderId = orderId,
                Currency = Currency.Idr,
                GrossAmount = grossAmount,
            },
            ItemDetails = itemDetailsList,
            CustomerDetails = new CustomerDetails()
            {
                FirstName = string.IsNullOrWhiteSpace(req.Customer?.Name) ? "Kasir" : req.Customer.Name,
            }
        };

        var response = await _midtransClient.Snap.CreateTransactionAsync(snapRequest);

        data[orderId] = req;
        SaveToFile();

        return new CreateTransactionResponse(Token: response.Token, RedirectUrl: response.RedirectUrl);
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
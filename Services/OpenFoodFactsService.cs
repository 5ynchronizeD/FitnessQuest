using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FitnessQuest.Models;

namespace FitnessQuest.Services;

/// <summary>
/// Thin client over the free Open Food Facts API. Turns a barcode or a search
/// term into <see cref="FoodItem"/>s with per-100g nutrition.
/// </summary>
public class OpenFoodFactsService
{
    private readonly HttpClient _http;

    public OpenFoodFactsService(HttpClient http)
    {
        _http = http;
        _http.BaseAddress ??= new Uri("https://world.openfoodfacts.org/");
        if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
            _http.DefaultRequestHeaders.Add("User-Agent", "FitnessQuest/1.0 (OBOS internal app)");
    }

    private const string Fields = "code,product_name,brands,image_front_small_url,nutriments";

    public async Task<FoodItem?> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        try
        {
            var url = $"api/v2/product/{Uri.EscapeDataString(barcode)}.json?fields={Fields}";
            var resp = await _http.GetFromJsonAsync<ProductResponse>(url, JsonOpts, ct);
            if (resp is null || resp.Status != 1 || resp.Product is null)
                return null;
            return Map(resp.Product, barcode);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<List<FoodItem>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new();
        try
        {
            var url = $"cgi/search.pl?search_terms={Uri.EscapeDataString(query)}" +
                      $"&search_simple=1&action=process&json=1&page_size=25&fields={Fields}";
            var resp = await _http.GetFromJsonAsync<SearchResponse>(url, JsonOpts, ct);
            if (resp?.Products is null)
                return new();
            return resp.Products
                .Select(p => Map(p, p.Code))
                .Where(f => f is not null && !string.IsNullOrWhiteSpace(f!.Name) && f.KcalPer100g > 0)
                .Select(f => f!)
                .ToList();
        }
        catch (Exception)
        {
            return new();
        }
    }

    private static FoodItem? Map(Product p, string? barcode)
    {
        if (p.Nutriments is null)
            return null;

        return new FoodItem
        {
            Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode,
            Name = string.IsNullOrWhiteSpace(p.ProductName) ? "Okänd produkt" : p.ProductName!.Trim(),
            Brand = FirstBrand(p.Brands),
            ImageUrl = p.ImageUrl,
            KcalPer100g = p.Nutriments.EnergyKcal,
            ProteinPer100g = p.Nutriments.Proteins,
            CarbsPer100g = p.Nutriments.Carbs,
            FatPer100g = p.Nutriments.Fat
        };
    }

    private static string? FirstBrand(string? brands)
    {
        if (string.IsNullOrWhiteSpace(brands)) return null;
        return brands.Split(',')[0].Trim();
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ---- DTOs ----
    private class ProductResponse
    {
        [JsonPropertyName("status")] public int Status { get; set; }
        [JsonPropertyName("product")] public Product? Product { get; set; }
    }

    private class SearchResponse
    {
        [JsonPropertyName("products")] public List<Product>? Products { get; set; }
    }

    private class Product
    {
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("product_name")] public string? ProductName { get; set; }
        [JsonPropertyName("brands")] public string? Brands { get; set; }
        [JsonPropertyName("image_front_small_url")] public string? ImageUrl { get; set; }
        [JsonPropertyName("nutriments")] public Nutriments? Nutriments { get; set; }
    }

    /// <summary>
    /// Open Food Facts returns nutriment values as either numbers or strings
    /// depending on the product, so we parse leniently.
    /// </summary>
    private class Nutriments
    {
        [JsonPropertyName("energy-kcal_100g")] public JsonElement EnergyKcalRaw { get; set; }
        [JsonPropertyName("proteins_100g")] public JsonElement ProteinsRaw { get; set; }
        [JsonPropertyName("carbohydrates_100g")] public JsonElement CarbsRaw { get; set; }
        [JsonPropertyName("fat_100g")] public JsonElement FatRaw { get; set; }

        [JsonIgnore] public double EnergyKcal => ToDouble(EnergyKcalRaw);
        [JsonIgnore] public double Proteins => ToDouble(ProteinsRaw);
        [JsonIgnore] public double Carbs => ToDouble(CarbsRaw);
        [JsonIgnore] public double Fat => ToDouble(FatRaw);

        private static double ToDouble(JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Number => el.GetDouble(),
                JsonValueKind.String when double.TryParse(el.GetString(),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out var v) => v,
                _ => 0
            };
        }
    }
}

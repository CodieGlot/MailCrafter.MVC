using MailCrafter.Domain;
using System.Text.Json;

namespace MailCrafter.MVC.Models;

public class ApiPageQueryRequest
{
    public int Top { get; set; } = 10;
    public int Skip { get; set; }
    public string? Search { get; set; }
    public string? SearchBy { get; set; }
    public string? SortOrder { get; set; }
    public string? SortBy { get; set; }
    public string? NextToken { get; set; }
}

public static class ApiPageQueryRequestExtensions
{
    public static string GenerateNextToken(this ApiPageQueryRequest request)
    {
        var nextObj = new ApiPageQueryRequest
        {
            Top = request.Top,
            Skip = request.Top + request.Skip,
            Search = request.Search,
            SearchBy = request.SearchBy,
            SortOrder = request.SortOrder,
            SortBy = request.SortBy,
        };
        return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(nextObj));
    }
    public static PageQueryDTO ToPageQueryDTO(this ApiPageQueryRequest request)
    {
        PopulateNextToken(request);
        var sortOder = request.SortOrder?.ToLower() switch
        {
            "asc" => SortOrder.Asc,
            "desc" => SortOrder.Desc,
            _ => SortOrder.None,
        };
        return new PageQueryDTO
        {
            Top = request.Top,
            Skip = request.Skip,
            Search = request.Search,
            SearchBy = request.SearchBy,
            SortOrder = sortOder,
            SortBy = request.SortBy,
        };
    }
    public static void PopulateNextToken(this ApiPageQueryRequest request)
    {
        if (!string.IsNullOrEmpty(request.NextToken))
        {
            var param = JsonSerializer.Deserialize<ApiPageQueryRequest>(
                Convert.FromBase64String(request.NextToken ?? ""));

            if (param != null)
            {
                request.Top = param.Top;
                request.Skip = param.Skip;
                request.Search = param.Search;
                request.SearchBy = param.SearchBy;
                request.SortOrder = param.SortOrder;
                request.SortBy = param.SortBy;
            }
        }
    }
}

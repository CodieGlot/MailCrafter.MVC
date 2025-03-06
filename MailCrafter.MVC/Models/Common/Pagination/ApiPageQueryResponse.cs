namespace MailCrafter.MVC.Models;

public class ApiPageQueryResponse<T>
{
    public List<T> Data { get; set; } = [];
    public string? NextToken { get; set; }
    public ApiPageQueryResponse(List<T> data, ApiPageQueryRequest request)
    {
        this.Data = data;
        this.NextToken = request.GenerateNextToken();
    }
}

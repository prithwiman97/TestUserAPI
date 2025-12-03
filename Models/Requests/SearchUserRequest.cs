namespace TestUserAPI.Models.Requests;

public class SearchUserRequest
{
    public string Username { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}


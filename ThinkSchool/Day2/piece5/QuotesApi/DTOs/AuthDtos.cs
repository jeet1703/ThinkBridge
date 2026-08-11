namespace QuotesApi.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Access_Token, string Refresh_Token, int Expires_In);

using QuotesApi.DTOs;
using System.Collections.Generic;

namespace QuotesApi.Validators;

public class CreateQuoteRequestValidator
{
    public (bool IsValid, Dictionary<string, string[]> Errors) Validate(CreateQuoteRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Author))
        {
            errors["author"] = new[] { "Author is required." };
        }
        else
        {
            var trimmedAuthor = request.Author.Trim();
            if (trimmedAuthor.Length < 3 || trimmedAuthor.Length > 50)
            {
                errors["author"] = new[] { "Author must be between 3 and 50 characters." };
            }
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            errors["text"] = new[] { "Text is required." };
        }
        else
        {
            var trimmedText = request.Text.Trim();
            if (trimmedText.Length < 5 || trimmedText.Length > 500)
            {
                errors["text"] = new[] { "Text must be between 5 and 500 characters." };
            }
        }

        return (errors.Count == 0, errors);
    }
}

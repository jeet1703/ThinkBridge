using System;

namespace QuotesApi.Models;

public class Quote
{
    public int Id { get; set; }

    public string Author { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public int? CreatedByUserId { get; set; }

    public static Quote Create(string author, string text, int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(author))
        {
            throw new ArgumentException("Author is required.", nameof(author));
        }

        string trimmedAuthor = author.Trim();
        if (trimmedAuthor.Length < 3 || trimmedAuthor.Length > 50)
        {
            throw new ArgumentException("Author must be between 3 and 50 characters.", nameof(author));
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text is required.", nameof(text));
        }

        string trimmedText = text.Trim();
        if (trimmedText.Length < 5 || trimmedText.Length > 500)
        {
            throw new ArgumentException("Text must be between 5 and 500 characters.", nameof(text));
        }

        return new Quote
        {
            Author = trimmedAuthor,
            Text = trimmedText,
            CreatedByUserId = createdByUserId
        };
    }
}
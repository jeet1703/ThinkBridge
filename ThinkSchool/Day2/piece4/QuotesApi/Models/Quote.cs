using System;

namespace QuotesApi.Models;

public class Quote
{
    public int Id { get; private set; }
    public string Author { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public bool IsDeleted { get; private set; }

    private Quote() { } // EF Core Constructor

    private Quote(string author, string text)
    {
        Author = author;
        Text = text;
    }

    public static Result<Quote> Create(string author, string text)
    {
        if (string.IsNullOrWhiteSpace(author))
        {
            return Result<Quote>.Failure("Author is required.");
        }

        string trimmedAuthor = author.Trim();
        if (trimmedAuthor.Length > 200)
        {
            return Result<Quote>.Failure("Author must be between 1 and 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return Result<Quote>.Failure("Text is required.");
        }

        string trimmedText = text.Trim();
        if (trimmedText.Length > 1000)
        {
            return Result<Quote>.Failure("Text must be between 1 and 1000 characters.");
        }

        return Result<Quote>.Success(new Quote(trimmedAuthor, trimmedText));
    }

    public void Delete()
    {
        IsDeleted = true;
    }
}
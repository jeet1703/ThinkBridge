using FluentAssertions;
using QuotesApi.Models;
using System;
using Xunit;

namespace Quotes.Tests.Unit;

public class QuoteTests
{
    [Fact]
    public void Create_ValidArguments_ShouldReturnQuoteInstance()
    {
        var author = "Martin Fowler";
        var text = "Any fool can write code that a computer can understand.";
        var userId = 42;

        var quote = Quote.Create(author, text, userId);

        quote.Should().NotBeNull();
        quote.Author.Should().Be(author);
        quote.Text.Should().Be(text);
        quote.CreatedByUserId.Should().Be(userId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrWhitespaceAuthor_ShouldThrowArgumentException(string? invalidAuthor)
    {
        var text = "Valid text for the quote.";

        Action act = () => Quote.Create(invalidAuthor!, text);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Author is required*");
    }

    [Fact]
    public void Create_AuthorTooShort_ShouldThrowArgumentException()
    {
        var author = "ab";
        var text = "Valid text for the quote.";

        Action act = () => Quote.Create(author, text);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Author must be between 3 and 50 characters*");
    }

    [Fact]
    public void Create_AuthorTooLong_ShouldThrowArgumentException()
    {
        var author = new string('x', 51);
        var text = "Valid text for the quote.";

        Action act = () => Quote.Create(author, text);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Author must be between 3 and 50 characters*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrWhitespaceText_ShouldThrowArgumentException(string? invalidText)
    {
        var author = "Valid Author";

        Action act = () => Quote.Create(author, invalidText!);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Text is required*");
    }

    [Fact]
    public void Create_TextTooShort_ShouldThrowArgumentException()
    {
        var author = "Valid Author";
        var shortText = "abcd";

        Action act = () => Quote.Create(author, shortText);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Text must be between 5 and 500 characters*");
    }

    [Fact]
    public void Create_TextTooLong_ShouldThrowArgumentException()
    {
        var author = "Valid Author";
        var longText = new string('x', 501);

        Action act = () => Quote.Create(author, longText);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Text must be between 5 and 500 characters*");
    }
}

using FluentAssertions;
using QuotesApi.DTOs;
using QuotesApi.Validators;
using Xunit;

namespace Quotes.Tests.Unit;

public class CreateQuoteRequestValidatorTests
{
    private readonly CreateQuoteRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldBeValid()
    {
        var request = new CreateQuoteRequest
        {
            Author = "Valid Author",
            Text = "This is a valid quote text with sufficient length."
        };

        var (isValid, errors) = _validator.Validate(request);

        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Validate_EmptyOrWhitespaceAuthor_ShouldHaveAuthorRequiredError(string? invalidAuthor)
    {
        var request = new CreateQuoteRequest
        {
            Author = invalidAuthor!,
            Text = "Valid quote text"
        };

        var (isValid, errors) = _validator.Validate(request);

        isValid.Should().BeFalse();
        errors.Should().ContainKey("author");
        errors["author"].Should().ContainSingle().Which.Should().Be("Author is required.");
    }

    [Fact]
    public void Validate_AuthorTooShort_ShouldHaveAuthorLengthError()
    {
        var request = new CreateQuoteRequest
        {
            Author = "ab",
            Text = "Valid quote text"
        };

        var (isValid, errors) = _validator.Validate(request);

        isValid.Should().BeFalse();
        errors.Should().ContainKey("author");
        errors["author"].Should().ContainSingle().Which.Should().Be("Author must be between 3 and 50 characters.");
    }

    [Fact]
    public void Validate_AuthorTooLong_ShouldHaveAuthorLengthError()
    {
        var request = new CreateQuoteRequest
        {
            Author = new string('a', 51),
            Text = "Valid quote text"
        };

        var (isValid, errors) = _validator.Validate(request);

        isValid.Should().BeFalse();
        errors.Should().ContainKey("author");
        errors["author"].Should().ContainSingle().Which.Should().Be("Author must be between 3 and 50 characters.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Validate_EmptyOrWhitespaceText_ShouldHaveTextRequiredError(string? invalidText)
    {
        var request = new CreateQuoteRequest
        {
            Author = "Valid Author",
            Text = invalidText!
        };

        var (isValid, errors) = _validator.Validate(request);

        isValid.Should().BeFalse();
        errors.Should().ContainKey("text");
        errors["text"].Should().ContainSingle().Which.Should().Be("Text is required.");
    }

    [Fact]
    public void Validate_TextTooShort_ShouldHaveTextLengthError()
    {
        var request = new CreateQuoteRequest
        {
            Author = "Valid Author",
            Text = "abcd"
        };

        var (isValid, errors) = _validator.Validate(request);

        isValid.Should().BeFalse();
        errors.Should().ContainKey("text");
        errors["text"].Should().ContainSingle().Which.Should().Be("Text must be between 5 and 500 characters.");
    }

    [Fact]
    public void Validate_TextTooLong_ShouldHaveTextLengthError()
    {
        var request = new CreateQuoteRequest
        {
            Author = "Valid Author",
            Text = new string('x', 501)
        };

        var (isValid, errors) = _validator.Validate(request);

        isValid.Should().BeFalse();
        errors.Should().ContainKey("text");
        errors["text"].Should().ContainSingle().Which.Should().Be("Text must be between 5 and 500 characters.");
    }
}

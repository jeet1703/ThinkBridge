using FluentAssertions;
using QuotesApi.Models;
using System;
using Xunit;

namespace Quotes.Tests.Unit;

public class CollectionTests
{
    [Fact]
    public void Constructor_ValidArguments_ShouldInitializeProperties()
    {
        var name = "Inspiring Quotes";
        var ownerId = "user-123";

        var collection = new Collection(name, ownerId);

        collection.Name.Should().Be(name);
        collection.OwnerId.Should().Be(ownerId);
        collection.Items.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_EmptyOwnerId_ShouldThrowArgumentException()
    {
        var name = "Inspiring Quotes";
        var ownerId = "";

        Action act = () => new Collection(name, ownerId);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Owner ID is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SetName_EmptyOrWhitespaceName_ShouldThrowArgumentException(string? invalidName)
    {
        var collection = new Collection("Valid Name", "owner-123");

        Action act = () => collection.SetName(invalidName!);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Name is required*");
    }

    [Fact]
    public void SetName_TooShortName_ShouldThrowArgumentException()
    {
        var collection = new Collection("Valid Name", "owner-123");
        var shortName = "ab";

        Action act = () => collection.SetName(shortName);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Name must be between 3 and 80 characters*");
    }

    [Fact]
    public void SetName_TooLongName_ShouldThrowArgumentException()
    {
        var collection = new Collection("Valid Name", "owner-123");
        var longName = new string('a', 81);

        Action act = () => collection.SetName(longName);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*Name must be between 3 and 80 characters*");
    }

    [Fact]
    public void AddItem_DuplicateQuoteId_ShouldThrowInvalidOperationException()
    {
        var collection = new Collection("Valid Name", "owner-123");
        collection.AddItem(10);

        Action act = () => collection.AddItem(10);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Quote is already in this collection*");
    }

    [Fact]
    public void AddItem_OverFiftyItems_ShouldThrowInvalidOperationException()
    {
        var collection = new Collection("Valid Name", "owner-123");
        for (int i = 1; i <= 50; i++)
        {
            collection.AddItem(i);
        }

        Action act = () => collection.AddItem(51);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*A collection cannot contain more than 50 items*");
    }

    [Fact]
    public void RemoveItem_NonExistentQuoteId_ShouldThrowInvalidOperationException()
    {
        var collection = new Collection("Valid Name", "owner-123");

        Action act = () => collection.RemoveItem(99);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Quote not found in this collection*");
    }

    [Fact]
    public void AddAndRemoveItem_ValidQuoteId_ShouldLeaveItemsEmpty()
    {
        var collection = new Collection("Valid Name", "owner-123");
        collection.AddItem(42);

        collection.RemoveItem(42);

        collection.Items.Should().BeEmpty();
    }
}

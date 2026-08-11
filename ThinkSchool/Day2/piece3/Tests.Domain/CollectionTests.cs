using FluentAssertions;
using QuotesApi.Models;
using System;
using Xunit;

namespace Tests.Domain;

public class CollectionTests
{
    [Fact]
    public void Constructor_WithEmptyName_ShouldThrowArgumentException()
    {
        Action act = () => new Collection("", "owner-1");
        act.Should().Throw<ArgumentException>().WithMessage("*Name is required*");
    }

    [Fact]
    public void SetName_WithLengthGreaterThan80_ShouldThrowArgumentException()
    {
        var longName = new string('a', 81);
        var collection = new Collection("Valid Name", "owner-1");
        Action act = () => collection.SetName(longName);
        act.Should().Throw<ArgumentException>().WithMessage("*Name must be between 3 and 80 characters*");
    }

    [Fact]
    public void AddItem_WhenCollectionHas50Items_ShouldThrowInvalidOperationException()
    {
        var collection = new Collection("Valid Name", "owner-1");
        for (int i = 1; i <= 50; i++)
        {
            collection.AddItem(i);
        }
        Action act = () => collection.AddItem(51);
        act.Should().Throw<InvalidOperationException>().WithMessage("A collection cannot contain more than 50 items.");
    }

    [Fact]
    public void AddItem_WithDuplicateQuoteId_ShouldThrowInvalidOperationException()
    {
        var collection = new Collection("Valid Name", "owner-1");
        collection.AddItem(10);

        Action act = () => collection.AddItem(10);

        act.Should().Throw<InvalidOperationException>().WithMessage("Quote is already in this collection.");
    }

    [Fact]
    public void RemoveItem_WithNonExistentQuoteId_ShouldThrowInvalidOperationException()
    {
        var collection = new Collection("Valid Name", "owner-1");

        Action act = () => collection.RemoveItem(99);

        act.Should().Throw<InvalidOperationException>().WithMessage("Quote not found in this collection.");
    }

    [Fact]
    public void AddAndRemoveItem_ShouldLeaveCollectionEmpty()
    {
        var collection = new Collection("Valid Name", "owner-1");

        collection.AddItem(42);
        collection.RemoveItem(42);

        collection.Items.Should().BeEmpty();
    }
}

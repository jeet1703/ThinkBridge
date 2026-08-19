using System;
using System.Collections.Generic;
using System.Linq;

namespace QuotesApi.Models;

public class Collection
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string OwnerId { get; private set; } = string.Empty;

    private readonly List<CollectionItem> _items = new();
    public IReadOnlyCollection<CollectionItem> Items => _items.AsReadOnly();

    private Collection() { }

    public Collection(string name, string ownerId)
    {
        SetName(name);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException("Owner ID is required.", nameof(ownerId));
        }
        OwnerId = ownerId;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.");
        }
        
        string trimmed = name.Trim();
        if (trimmed.Length < 3 || trimmed.Length > 80)
        {
            throw new ArgumentException("Name must be between 3 and 80 characters.");
        }
        Name = trimmed;
    }

    public void AddItem(int quoteId)
    {
        if (_items.Count >= 50)
        {
            throw new InvalidOperationException("A collection cannot contain more than 50 items.");
        }

        if (_items.Any(i => i.QuoteId == quoteId))
        {
            throw new InvalidOperationException("Quote is already in this collection.");
        }

        _items.Add(new CollectionItem(quoteId, DateTime.UtcNow));
    }

    public void RemoveItem(int quoteId)
    {
        var item = _items.FirstOrDefault(i => i.QuoteId == quoteId);
        if (item == null)
        {
            throw new InvalidOperationException("Quote not found in this collection.");
        }
        _items.Remove(item);
    }
}

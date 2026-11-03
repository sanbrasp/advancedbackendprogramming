using System.Collections.Concurrent;
using WebApiBasics.Interfaces;
using WebApiBasics.Records;

namespace WebApiBasics.Data;

internal class InMemoryBookRepository : IBookRepository
{
    /// <summary>
    /// Simply for seeding during testing.
    /// </summary>
    private readonly ConcurrentDictionary<int, BookRecord> _books = new()
    {
        [1] = new(1, "Sandra", "A frog's life", "A tale of frogs", 1987)
    };
    

    public IEnumerable<BookRecord> GetAll()
    {
        return _books.Values;
    }

    public bool TryGet(int id, out BookRecord? book)
    {
        return _books.TryGetValue(id, out book);
    }

    public bool TryAdd(BookRecord book)
    {
        return _books.TryAdd(book.Id, book);
    }

    public bool TryRemove(int id, out BookRecord? removed)
    {
        return _books.TryRemove(id, out removed);
    }

    public bool TryUpdate(int id, BookRecord book, out BookRecord? updated)
    {
        var exists = _books.TryGetValue(id, out var existing);
        if (!exists)
        {
            updated = null;
            return false;
        }
        updated = book with { Id = id };

        var success = existing != null && _books.TryUpdate(id, updated, existing);
        if (!success)
        {
            updated = null;
        }
        return success;
    }
}
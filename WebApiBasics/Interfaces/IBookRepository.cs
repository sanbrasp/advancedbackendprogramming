using WebApiBasics.Records;

namespace WebApiBasics.Interfaces;

public interface IBookRepository
{
    IEnumerable<BookRecord> GetAll();
    
    bool TryGet(int id, out BookRecord? book);

    bool TryAdd(BookRecord book);

    bool TryRemove(int id, out BookRecord? removed);

    bool TryUpdate(int id, BookRecord book, out BookRecord? updated);
}
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using WebApiBasics.Records;

namespace WebApiBasics.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private static readonly ConcurrentDictionary<int, BookRecord> _books = new()
        {
            [1] = new(1, "Sandra", "A frog's life", "A tale of frogs", 1987)
        };

        
        [HttpGet]
        public ActionResult<IEnumerable<BookRecord>> GetAll()
        {
            return Ok(_books.Values);
        }

        [HttpGet("{id}")]
        public ActionResult<BookRecord> GetById(int id)
        {
            if (!_books.TryGetValue(id, out var book))
            {
                return NotFound();
            }

            return Ok(book);
        }

        [HttpPost]
        public ActionResult<BookRecord> Create(BookRecord book)
        {
            if (!_books.TryAdd(book.Id, book))
            {
                return Conflict();
            }
            
            return CreatedAtAction(
                nameof(GetById),         // the action that finds the new item 
     new { id = book.Id },    // the route values for that action
                book);                  // the response body
        }

        [HttpPut("{id}")]
        public ActionResult<BookRecord> Edit(int id, BookRecord book)
        {
            var exists =  _books.TryGetValue(id, out var existing);
            if (!exists)
            {
                return NotFound();
            }
            var updated = book with { Id = id };

            if (!_books.TryUpdate(id, updated, existing!))
            {
                return Conflict();
            }
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public ActionResult<BookRecord> Delete(int id)
        {
            if (!_books.TryRemove(id, out var removed))
            {
                return NotFound();
            }
            return Ok(removed);
        }
    }
}

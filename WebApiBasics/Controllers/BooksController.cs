using Microsoft.AspNetCore.Mvc;
using WebApiBasics.Interfaces;
using WebApiBasics.Records;

namespace WebApiBasics.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly IBookRepository _books;
        
        public BooksController(IBookRepository repository)
        {
            _books = repository;
        }
        
        [HttpGet]
        public ActionResult<IEnumerable<BookRecord>> GetAll()
        {
            return Ok(_books.GetAll());
        }

        [HttpGet("{id}")]
        public ActionResult<BookRecord> GetById(int id)
        {
            if (!_books.TryGet(id, out var book))
            {
                return NotFound();
            }

            return Ok(book);
        }

        [HttpPost]
        public ActionResult<BookRecord> Create(BookRecord book)
        {
            if (!_books.TryAdd(book))
            {
                return Conflict();
            }
            
            return CreatedAtAction(
                nameof(GetById),         // the action that finds the new item 
     new { id = book.Id },    // the route values for that action
                book);                  // the response body
        }

        [HttpPut("{id}")]
        public ActionResult<BookRecord> Update(int id, BookRecord book)
        {
            if (!_books.TryUpdate(id, book, out var updated))
            {
                return NotFound();
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

using System;
using Books.Api.Filters;
using Books.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Books.Api.Controllers
{
    [Route("api/synchronousbooks")]
    [ApiController]
    public class SynchronousBooksController : Controller
    {
        private IBooksRepository _booksRepository;

        public SynchronousBooksController(IBooksRepository booksRepository)
        {
            _booksRepository = booksRepository ??
                throw new ArgumentNullException(nameof(booksRepository));
        }

        [HttpGet]
       // [BooksResultFilter]
        public IActionResult GetBooks()
        {
            var bookEntities = _booksRepository.GetBooks();
            return Ok(bookEntities);
        }
    }
}
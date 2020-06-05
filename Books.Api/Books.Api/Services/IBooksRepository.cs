using Books.Api.Entities;
using Books.Api.ExternalModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Books.Api.Services
{
    public interface IBooksRepository
    {
        IEnumerable<Book> GetBooks();

        // Book GetBook(Guid id);

        Task<IEnumerable<Book>> GetBooksAsync();

        Task<IEnumerable<Entities.Book>> GetBooksAsync(IEnumerable<Guid> bookIds);

        Task<Book> GetBookAsync(Guid id);

        void AddBook(Entities.Book bookToAdd);

        Task<bool> SaveChangesAsync();

        Task<BookCover> GetBookCoverAsync(string coverId);
        Task<IEnumerable<BookCover>> GetBookCoversAsync(Guid bookId);
    }
}

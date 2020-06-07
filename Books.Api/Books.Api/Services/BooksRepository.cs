using Books.Api.Contexts;
using Books.Api.Entities;
using Books.Api.ExternalModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Books.Api.Services
{
    public class BooksRepository : IBooksRepository, IDisposable
    {

        private BooksContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger<BooksRepository> _logger;

        public BooksRepository(BooksContext context, IHttpClientFactory httpClientFactory, ILogger<BooksRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Book> GetBookAsync(Guid id)
        {
            //var pageCalculator = new Books.Legacy.ComplicatedPageCalculator();
            //var amountOfPage = pageCalculator.CalculateBookPages();

            // Pitfall #1: using Task.Run() on the server
            _logger.LogInformation($"ThreadId when entering GetBookAsync: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            var bookPages = await GetBookPages();

            return await _context.Books.Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        // Pitfall #1: using Task.Run() on the server
        private Task<int> GetBookPages()
        {
            return Task.Run(() =>
            {
                _logger.LogInformation($"ThreadId when calculating the amount of pages: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

                var pageCalculator = new Books.Legacy.ComplicatedPageCalculator();
                return pageCalculator.CalculateBookPages();
            });
        }

        public async Task<IEnumerable<Book>> GetBooksAsync()
        {
            return await _context.Books.Include(b => b.Author).ToListAsync();
        }

        public async Task<IEnumerable<Entities.Book>> GetBooksAsync(IEnumerable<Guid> bookIds)
        {
            return await _context.Books.Where(b => bookIds.Contains(b.Id))
                .Include(b => b.Author).ToListAsync();
        }

        public IEnumerable<Book> GetBooks()
        {
            return _context.Books.Include(b => b.Author).ToList();
        }

        public void AddBook(Book bookToAdd)
        {
            if (bookToAdd == null)
            {
                throw new ArgumentNullException(nameof(bookToAdd));
            }

            _context.Add(bookToAdd);
        }

        public async Task<bool> SaveChangesAsync()
        {
            // return true if 1 or more entities were changed
            return (await _context.SaveChangesAsync() > 0);
        }

        public async Task<BookCover> GetBookCoverAsync(string coverId)

        {
            var httpClient = _httpClientFactory.CreateClient();
           // var bookCovers = new List<BookCover>();

            var response = await httpClient.GetAsync($"http://localhost:52644/api/bookcovers/{coverId}");

            if (response.IsSuccessStatusCode)
            {
                return System.Text.Json.JsonSerializer.Deserialize<BookCover>(
                    await response.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                    });
            }

            return null;
        }


        public async Task<IEnumerable<BookCover>> GetBookCoversAsync(Guid bookId) 
        {
            var httpClient = _httpClientFactory.CreateClient();
            var bookCovers = new List<BookCover>();
            _cancellationTokenSource = new CancellationTokenSource();

            // create a list of fake bookcovers
            var bookCoverUrls = new[]
            {
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover1",
             //   $"http://localhost:52644/api/bookcovers/{bookId}-dummycover2?returnFault=true",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover2",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover3",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover4",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover5"
            };

            //foreach (var bookCoverUrl in bookCoverUrls)
            //{
            //    var response = await httpClient.GetAsync(bookCoverUrl);

            //    if (response.IsSuccessStatusCode)
            //    {
            //        bookCovers.Add(JsonSerializer.Deserialize<BookCover>(
            //            await response.Content.ReadAsStringAsync(),
            //            new JsonSerializerOptions
            //            {
            //                PropertyNameCaseInsensitive = true,
            //            }));
            //    }
            //}
            //return bookCovers;

            // create the tasks
            var downloadBookCoverTasksQuery =
                 from bookCoverUrl
                 in bookCoverUrls
                 select DownloadBookCoverAsync(httpClient, bookCoverUrl, _cancellationTokenSource.Token);

            // start the tasks
            var downloadBookCoverTasks = downloadBookCoverTasksQuery.ToList();
            try
            {
                return await Task.WhenAll(downloadBookCoverTasks);
            }
            catch (OperationCanceledException operationCanceledException)
            {
                _logger.LogInformation($"{operationCanceledException.Message}");
                foreach (var task in downloadBookCoverTasks)
                {
                    _logger.LogInformation($"Task {task.Id} has status {task.Status}");
                }

                return new List<BookCover>();
            }
            catch (Exception exception)
            {
                _logger.LogError($"{exception.Message}");
                throw;
            }

        }


        private async Task<BookCover> DownloadBookCoverAsync(
        HttpClient httpClient, string bookCoverUrl, CancellationToken cancellationToken)
        {
            // throw new Exception("Cannot download book cover, writer isn't finishing book fast enough.");

            var response = await httpClient.GetAsync(bookCoverUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                //var bookCover = JsonSerializer.Deserialize<BookCover>(
                //    await response.Content.ReadAsStringAsync(),
                //    new JsonSerializerOptions
                //    {
                //        PropertyNameCaseInsensitive = true,
                //    });

                var bookCover = JsonConvert.DeserializeObject<BookCover>(
                    await response.Content.ReadAsStringAsync());
                return bookCover;
            }

            _cancellationTokenSource.Cancel();

            return null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }

            }
        }

    }
}

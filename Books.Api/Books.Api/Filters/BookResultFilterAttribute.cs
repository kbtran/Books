using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Books.Api.Filters
{
    public class BookResultFilterAttribute : ResultFilterAttribute
    {
        private readonly IMapper _mapper;

        public BookResultFilterAttribute(IMapper mapper)
        {
            mapper = _mapper;
        }

        public override async Task OnResultExecutionAsync(ResultExecutingContext context,
            ResultExecutionDelegate next)
        {
            var resultFromAction = context.Result as ObjectResult;
            if (resultFromAction?.Value == null
               || resultFromAction.StatusCode < 200
               || resultFromAction.StatusCode >= 300)
            {
                await next();
                return;
            }

            var mapper = context.HttpContext.RequestServices.GetRequiredService<IMapper>();

            resultFromAction.Value = mapper.Map<Models.Book>(resultFromAction.Value);

            await next();
        }
    }
}

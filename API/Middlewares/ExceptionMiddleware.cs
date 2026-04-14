using Serilog;

namespace WorkManagementSystem.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // ✅ Log lỗi bằng Serilog
                Log.Error(ex, "Unhandled exception: {Message}", ex.Message);

                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                // ✅ Trả về JSON đúng chuẩn
                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = 500,
                    message = ex.Message
                });
            }
        }
    }
}
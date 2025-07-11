using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using nietras.SeparatedValues;

namespace Ensek.PeteForrest.Api.Formatters
{
    public class CsvFormatter<T> : TextInputFormatter
    {
        private readonly ICsvRowConverter<T> _csvRowConverter;

        public CsvFormatter(ICsvRowConverter<T> csvRowConverter)
        {
            _csvRowConverter = csvRowConverter;
            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/csv"));

            this.SupportedEncodings.Add(Encoding.UTF8);
            this.SupportedEncodings.Add(Encoding.Unicode);
        }

        protected override bool CanReadType(Type type) =>
            type.IsAssignableTo(typeof(IAsyncEnumerable<T>));

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context,
            Encoding encoding)
        {
            var httpContext = context.HttpContext;
            var result = ReadRecordsAsync(httpContext.Request.Body);
            return await InputFormatterResult.SuccessAsync(result);
        }

        private async IAsyncEnumerable<T> ReadRecordsAsync(Stream stream)
        {
            using var reader = await new Sep(',').Reader(o => o with
            {
                CultureInfo = CultureInfo.InvariantCulture,
                DisableColCountCheck = true
            }).FromAsync(stream);
            await foreach (var readRow in reader)
            {
                yield return _csvRowConverter.Convert(readRow);
            }
        }
    }
}
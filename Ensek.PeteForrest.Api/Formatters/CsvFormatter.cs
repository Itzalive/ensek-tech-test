using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace Ensek.PeteForrest.Api.Formatters {
    public class CsvFormatter : TextInputFormatter{
        public CsvFormatter()
        {
            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/csv"));
            
            this.SupportedEncodings.Add(Encoding.UTF8);
            this.SupportedEncodings.Add(Encoding.Unicode);
        }

        protected override bool CanReadType(Type type) => type.IsAssignableTo(typeof(IEnumerable)) && type.GetGenericArguments().Length == 1;

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            var httpContext = context.HttpContext;
            var enumeratedType = context.ModelType.GetGenericArguments()[0];
            using var streamReader = new StreamReader(httpContext.Request.Body, encoding);
            var body = await streamReader.ReadToEndAsync();
            var result = typeof(CsvFormatter).GetMethod(nameof(this.ReadRecords), BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(enumeratedType)
                .Invoke(this, [body]);
            return await InputFormatterResult.SuccessAsync(result);
        }

        private IEnumerable<T> ReadRecords<T>(string body)
        {
            using var reader = new StringReader(body);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower()
            };
            using var csv = new CsvReader(reader, config);
            return csv.GetRecords<T>().ToArray();
        }
    }
}

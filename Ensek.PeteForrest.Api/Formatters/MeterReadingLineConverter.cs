using Ensek.PeteForrest.Services.Model;
using nietras.SeparatedValues;

namespace Ensek.PeteForrest.Api.Formatters;

public class MeterReadingLineConverter : ICsvRowConverter<MeterReadingLine>
{
    public MeterReadingLine Convert(SepReader.Row row)
    {
        if (row.ColCount < 3)
        {
            return new MeterReadingLine
            {
                RowId = row.RowIndex,
                ParseErrors = ParseErrors.IncompleteData
            };
        }

        var canParseAccountId = row[0].TryParse<int>(out var accountId);
        return new MeterReadingLine
        {
            RowId = row.RowIndex,
            AccountId = canParseAccountId ? accountId : null,
            MeterReadingDateTime = row[1].ToString(),
            MeterReadValue = row[2].ToString(),
            ParseErrors = canParseAccountId ? ParseErrors.None : ParseErrors.InvalidAccountId
        };
    }
}
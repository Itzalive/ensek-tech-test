using nietras.SeparatedValues;

namespace Ensek.PeteForrest.Api.Formatters;

public interface ICsvRowConverter<out T>
{
    T Convert(SepReader.Row row);
}
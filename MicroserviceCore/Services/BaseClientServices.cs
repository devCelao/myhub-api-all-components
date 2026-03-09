using System.Text.Json;
using System.Text;
using MicroserviceCore.Comunication;

namespace MicroserviceCore.Services;

public abstract class BaseClientServices
{
    protected JsonSerializerOptions options = new (){ PropertyNameCaseInsensitive = true };
    protected static StringContent ObterConteudo(object dado) 
        => new(content: JsonSerializer.Serialize(dado),
                                  encoding: Encoding.UTF8,
                                  mediaType: "application/json");

    protected async Task<T?> DeserializarObjetoResponse<T>(HttpResponseMessage responseMessage)
        => JsonSerializer.Deserialize<T>(await responseMessage.Content.ReadAsStringAsync(), options);

    //protected static bool TrataErrosResponse(HttpResponseMessage responseMessage)
    //{
    //    if (responseMessage.StatusCode == HttpStatusCode.BadRequest) return false;

    //    responseMessage.EnsureSuccessStatusCode();
    //    return true;
    //}

    protected static ResponseResult RetornoOk() => new();

}

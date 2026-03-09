using System.ComponentModel;

namespace DomainObjects.Enums;

public enum SubscriptioId
{
    [Description("TokenCadastroService")]
    TokenCadastroService,
    [Description("SendEmailsService")]
    SendEmailsService,


    ///  Catalogo API
    [Description("CatalogApplication")]
    CatalogApplication
}
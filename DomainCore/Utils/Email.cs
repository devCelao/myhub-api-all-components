using System.Text.RegularExpressions;

namespace DomainObjects.Utils;

public partial class Email
{
    public const int EnderecoMaxLenght = 254;
    public const int EnderecoMinLenght = 5;
    public Regex regexEmail = RegexEmail();
    public string? EnderecoEmail { get; private set; }
    public Email() { }
    public Email(string? email)
    {
        EnderecoEmail = email;
    }
    public bool EmailValido
        => EnderecoEmail != null && regexEmail.IsMatch(EnderecoEmail);

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex RegexEmail();
}

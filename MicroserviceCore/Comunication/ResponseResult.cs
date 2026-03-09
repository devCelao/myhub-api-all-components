namespace MicroserviceCore.Comunication;

public class ResponseResult
{
    public string? Title { get; set; }
    public int Status { get; set; }
    public ResponseErrorMessages Errors { get; set; } = new();
}
public class ResponseErrorMessages
{
    public List<string> Mensagens { get; set; } = [];
}
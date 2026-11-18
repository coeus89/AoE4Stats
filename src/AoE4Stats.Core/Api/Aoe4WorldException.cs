using System.Net;

namespace AoE4Stats.Core.Api;

public sealed class Aoe4WorldException : Exception
{
    public Aoe4WorldException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}

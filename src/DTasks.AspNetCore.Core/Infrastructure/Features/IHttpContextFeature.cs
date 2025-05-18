using System.ComponentModel;
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Infrastructure.Features;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHttpContextFeature
{
    HttpContext HttpContext { get; }
}
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore;

public class DTasksAspNetCoreOptions
{
    public virtual PathString CallbackBasePath { get; set; }
}
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Http;

public delegate DTask AsyncRequestDelegate(HttpContext context);
# Chapter two Let's Run






## Simple resilience HttpClient

* [Better timeout handling with HttpClient](https://www.thomaslevesque.com/2018/02/25/better-timeout-handling-with-httpclient/)




## Polly



* [Transient fault handling and proactive resilience engineering](https://github.com/App-vNext/Polly/wiki/Transient-fault-handling-and-proactive-resilience-engineering)

* [Cancellation in Managed Threads](https://docs.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)



## Handling Validation Errors

```
not so good: duplication
it is not too bad when you don't have much security to check and not many actions in controllers

public class ToyController : ApiController
{
    public async Task<IHttpActionResult> GetAll()
    {
        if (security.IsXssed())
        {
            return BadRequest();
        }

        if (!ModelState.IsValid) 
        {
            return BadRequest(ModelState);
        }
        
        return Ok(await service.GetAll());
    }

    public async Task<IHttpActionResult> GetBy(int id)
    {
        if (security.IsXssed())
        {
            return BadRequest();
        }

        if (!ModelState.IsValid) 
        {
            return BadRequest(ModelState);
        }
        
        return Ok(await service.GetBy(id));
    }

    public async Task<IHttpActionResult> GetBy(string type)
    {
        if (security.IsXssed())
        {
            return BadRequest();
        }

        if (!ModelState.IsValid) 
        {
            return BadRequest(ModelState);
        }
        
        return Ok(await service.GetBy(type));
    }
}
```

Few things to note:

1. the logger is public so that filter attribute can access it from controller context

2. filter attribute should have NO state because they are singlton

3. make sure things we DI in are thread-safe. And there is no easy way to DI a filter unless we seperate filter from attribute as attributes are created at run time by CLR.

4. casting a controller back couples attribute to controller but we can always use a base abastract controller class so that filter can depend on that instead

5. filter vs attribute - they are very different. Filter is where you put your handling logic. Attribute is meta data tag to indicate where filter can be applied. The approach below mix these two concepts due to API platform limitation. Ideally we should separate them.

```
better: things like xss injection is needed in every app, we know then it is a cross-cutting concern should not mix with your application logic in controller.

[XssDetection]
public class ToyController : ApiController
{
    public readonly ILogger logger;

    public ToyController(IToyService service, ILogger logger)
    {
        this.service = service;
        this.logger = logger;
    }

    public async Task<IHttpActionResult> GetAll()
    {
        return Ok(await service.GetAll());
    }

    public async Task<IHttpActionResult> GetBy(int id)
    {
        return Ok(await service.GetBy(id));
    }

    public async Task<IHttpActionResult> GetBy(string type)
    { 
        return Ok(await service.GetBy(type));
    } 
}

public class XssDetectionAttribute : ActionFilterAttribute
{
    private readonly string[] dangerousChars = { "<", ">" };
    
    public override void OnActionExecuting(HttpActionContext context)
    {
        var controller = (ToyController)context
            .ControllerContext
            .Controller;
        
        var logger = controller.logger;

        if (context.ModelState.IsValid == false)
        {
            logger.LogDebug("", "");

            context.Response = context.Request
                .CreateErrorResponse(
                    HttpStatusCode.BadRequest,
                    context.ModelState
                );

            return;
        }

        if (IsXssed(context))
        {
            logger.LogDebug("", "");

            context.Response = context.Request
                .CreateErrorResponse(
                    HttpStatusCode.Forbidden,
                    "Request is invalid"
                );

            return;
        }
    }

    public bool IsXssed(HttpActionContext context)
    {
        var headers = context.Request.Headers.ToString();

        var uri = context.Request.RequestUri.ToString();

        var body = GetBody(context);

        return dangerousChars.Any(
            danger => (body + uri + headers).Contains(danger)
            );
    }

    public string GetBody(HttpActionContext context)
    {
        string content;

        using (var stream = new StreamReader(
            context.Request.Content.ReadAsStreamAsync().Result)
            )
        {
            stream.BaseStream.Seek(0, SeekOrigin.Begin);

            content = stream.ReadToEnd();

            stream.BaseStream.Seek(0, SeekOrigin.Begin);
        }
        
        return content;
    }
}
```

* [separating attribute & filter](https://www.cuttingedge.it/blogs/steven/pivot/entry.php?id=98)

* [Handling Validation Errors](https://docs.microsoft.com/en-us/aspnet/web-api/overview/formats-and-model-binding/model-validation-in-aspnet-web-api)

* [Attribute cache](https://stackoverflow.com/questions/27646196/asp-net-web-api-caches-action-filter-attributes-across-requests)

* [captive dependency](http://blog.ploeh.dk/2014/06/02/captive-dependency/)


## Exception handling

> Exception handling is NOT a feature we sell to the stakeholders, although it is neccessary, it should NOT dominate our code base without a good reason. Exception handling is one thing, business logic is another, they should NOT be mixed. Seperate them as much as you can!

There are three major levels where error could be handled:

1. close to the source of error
2. exception filter
3. globle


Close to the source of error:

> Only do this if we really have something important or specific to action on the exception

```
/* 
not so good: Mixing two responsibilities - getting customer & handling error
*/

public void GetCustomerBy(Guid cifToken)
{
    try 
    {
        var success = Detokenise(cifToken, out int plainCif);

        if (success) 
        {
            var uri = await apiDiscoverer.GetBaseUriAsync(
                Customers.Proxy.Customers.SERVICE_ID
                )
                .ConfigureAwait(false);

            var headers = await headerProvider
                .CreateCustomHeaders()
                .ConfigureAwait(false);

            var customer = await customerDatasource
                .GetCustomerPartialAsync(
                    uri, headers, 
                    cifToken)
                .ConfigureAwait(false);
            
            return Map(customer);
        }
    }
    catch(HttpOperationException)
    {
        // handle
    }
    catch(Exception ex)
    {
        // handle
    }
    finally
    {
        // clean up
    }
}

```

> below approach is ONLY useful when our error handling logic is rather involved and complex.

```
/* 
Good: seperation - now two different concerns can be easily digested.  

GetCustomerBy(Guid cifToken) is fully dedicated to error handling. 

Whereas TryGetCustomerBy(Guid cifToken) for business logic. 

This puts our readers into two different mental modes one at the time. 
*/

public void GetCustomerBy(Guid cifToken)
{
    try 
    {
        TryGetCustomerBy(cifToken);
    }
    catch(Exception ex)
    {
        // handle
    }
}

public void TryGetCustomerBy(Guid cifToken)
{
    var success = Detokenise(cifToken, out int plainCif);

    if (success) 
    {
        var uri = await apiDiscoverer.GetBaseUriAsync(
            Customers.Proxy.Customers.SERVICE_ID
            )
            .ConfigureAwait(false);

        var headers = await headerProvider
            .CreateCustomHeaders()
            .ConfigureAwait(false);

        var customer = await customerDatasource
            .GetCustomerPartialAsync(
                uri, headers, 
                cifToken)
            .ConfigureAwait(false);
        
        return Map(customer);
    }
}
```

If we need try-catch at multiple places, this try-catch setup can quickly become a chore. 

This approach basically does this chore for us, all we need to think about is:
1. how we should handle the error
2. what error we are interested in

```
flavour 1
public static async Task<T> HandleIfException<T>(
    Func<Task<T>> func,

    Func<T> handle,

    params HttpStatusCode[] statusCodesOfInterest
)
{
    try
    {
        return await func().ConfigureAwait(false);
    }
    catch (HttpOperationException httpException)
    {
        if (httpException.Response == null)
        {
            throw;
        }

        if (statusCodesOfInterest.Contains(httpException.Response.StatusCode))
        {
            return handle();
        }

        throw;
    }
}

flavour 2
public static async Task<T> HandleIfError<T>(
    this Func<Task<T>> func,

    Func<HttpOperationException, T> handle
)
{
    try
    {
        return await func().ConfigureAwait(false);
    }
    catch (HttpOperationException httpException)
    {
        if (httpException.Response == null)
        {
            throw;
        }
        
        return handle(httpException);   
    }
}

public async Task<List<TransactionEntity>> GetBy(
    List<string> trueRewardsAcctTokens, 
    int maxDays
)
{
    Func<List<TransactionEntity>> handler = 
    () => new List<TransactionEntity>();

    var errorsToHandle = [
        HttpStatusCode.NotFound
    ];

    return await Utility.HandleIfException(
        async () => await TryGetBy(
            trueRewardsAcctTokens, 
            maxDays
        )
        .ConfigureAwait(false),

        handler,

        errorsToHandle
    )
    .ConfigureAwait(false);
}
```

Exception filter:

> By default, an uncaught exception, doesn't matter from where, Web API will translate that into a 500. 

> Exception filters will be called only if the unhandled exception is NOT an HttpResponseException. 

```
// a 'generic' filter
public class ExceptionHandlerAttribute : ExceptionFilterAttribute 
{ 
    public Type TypeToHandle { get; set; }
    public HttpStatusCode StatusToReturn { get; set; }
 
    public override void OnException(HttpActionExecutedContext context) 
    {
        var ex = context.Exception;

        if (ex.GetType() is Type) 
        {
            var response = context.Request.CreateResponse(
                StatusToReturn, ex.Message
                );

            throw new HttpResponseException(response);
        }
    }
}

public class RCUController : ApiController {
 
    [ExceptionHandler(
        TypeToHandle = typeof(ArgumentNullException), 
        StatusToReturn = HttpStatusCode.BadRequest
        )
    ]

    [ExceptionHandler(
        TypeToHandle = typeof(Exception), 
        StatusToReturn = HttpStatusCode.NotAcceptable
        )
    ]
    public void GetCards(Guid cif) {
        var customer = new GeCustomerBy(cif);

        RCU.GetCards(customer);
    }
}
```

If we have bunch of different errors and we don't want web API to wrap 500, instead we want to forward them to the client

```
bad: duplication
public async Task<MyDto> GetToyByIdAsync(string id)
{
    try
    {
        return await toyRepository.Get(id).ConfigureAwait(false);
    }
    catch (NotFoundException notFoundException)
    {
        throw notFoundException;
    }
    catch (ValidationErrorException validationErrorException)
    {
        throw validationErrorException;
    }
    catch (ForbiddenException forbiddenException)
    {
        throw forbiddenException;
    }
    catch (UnauthorizedException unauthorizedException)
    {
        throw unauthorizedException;
    }
    catch (InternalServerErrorException internalServerException)
    {
        throw internalServerException;
    }
}

public async Task<MyDto> GetToysByTypeAsync(string toyType)
{
    try
    {
        return await toyRepository.Get(toyType).ConfigureAwait(false);
    }
    catch (NotFoundException notFoundException)
    {
        throw notFoundException;
    }
    catch (ValidationErrorException validationErrorException)
    {
        throw validationErrorException;
    }
    catch (ForbiddenException forbiddenException)
    {
        throw forbiddenException;
    }
    catch (UnauthorizedException unauthorizedException)
    {
        throw unauthorizedException;
    }
    catch (InternalServerErrorException internalServerException)
    {
        throw internalServerException;
    }
}
```

```
better:
public class ExceptionHandlerAttribute : ExceptionFilterAttribute
{
    public override void OnException(HttpActionExecutedContext context)
    {
        var ex = context.Exception;

        var statusCode = HttpStatusCode.InternalServerError;

        if (ex is NotFoundException)
        {
            statusCode = HttpStatusCode.NotFound;
        }

        if (ex is ValidationErrorException)
        {
            statusCode = HttpStatusCode.BadRequest;
        }

        if (ex is ForbiddenException)
        {
            statusCode = HttpStatusCode.Forbidden;
        }

        if (ex is UnauthorizedException)
        {
            statusCode = HttpStatusCode.Unauthorized;
        }

        if (ex is HttpOperationException)
        {
            statusCode = (ex as HttpOperationException).Response.StatusCode;
        }
        
        var response = context.Request.CreateResponse(
                statusCode, ex.Message
            );

        throw new HttpResponseException(response);
    }
}

[ExceptionHandler]
public class ToyController : ApiController 
{
    public async Task<MyDto> GetToyByTypeAsync(string toyType)
    {
        return await toyRepository.Get(toyType).ConfigureAwait(false);
    }

    public async Task<MyDto> GetToysByIdAsync(string toyId)
    {
        return await toyRepository.Get(toyId).ConfigureAwait(false);
    }
}
```


## Null coalescing operator ??



## yield v2.0

### yield return

> A yield
return statement effectively pauses the method rather than exiting it

### yield break


### finally in iterator block





## Indexer 

Indexers allow instances of a class or struct to be indexed just like arrays.




## Code tricks

default type inference
```
public LabeledPoint(double x, double y, string label = default)
{
    X = x;
    Y = y;
    this.Label = label;
}
```

Fold knowledge into data structure so our programme can be as dumb as possible.

## Are we really async in foreach ?


## Async in constructor

## out generic modifier


## Reference
* [indexer in C# version](https://stackoverflow.com/questions/47364199/in-which-version-of-c-sharp-indexers-were-introduced/47364214)

* [Stephen Cleary on async await in linq select](https://stackoverflow.com/questions/35011656/async-await-in-linq-select)

* [convariance vs contravariance](http://tomasp.net/blog/variance-explained.aspx/)

* [Stephen Cleary async constructors](http://blog.stephencleary.com/2013/01/async-oop-2-constructors.html)
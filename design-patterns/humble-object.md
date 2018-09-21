
## Why

Sometimes we find ourselves in situation that is close to impossible to test. For example, element position on html, UI visibility toggle, css class, etc. 

In order to test as much logic as possible, we separate our code into two groups: hard to test, and easy to test, these two groups then become what  Gerard Meszaros calls it the humble objects.

Take presenter and view component in a typical web app for example, the whole responsibility of the presenter is to process the data from across the business domain boundary and map it into a view model: button name, button visibility, dropdown list name, dropdown list values, dropdown list visibility, etc. This presenter is a humble object.

The view also a humble object. It simply binds the view model to the UI.

We now separated the hard to test UI and view component from easy to test component the presenter. The key is as their name suggests, humble ojects should be as simple as possible to a point that we can bear the risk of not testing the hard-to-test components say the view component.

However, we can still easily verify if a button is hidden at certain stage by testing the presenter and whether a dropdown is shown in certain way by verifying if a css class being assigned to by the presenter.

## How

Let's see some real code:

If we want to test the code below, we have to mock streamReader and HttpContext. If you haven't tried mock them, you can give it a go, I tried it is really hard.

```
public bool IsXssed()
{
    var isXssed = false;

    var bodyStream = new StreamReader(HttpContext.Current.Request.InputStream);

    bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);

    var body = bodyStream.ReadToEnd();
    
    var uri = context.Request.Url.AbsoluteUri;

    var headers = context.Request.Headers.ToString();

    if (dangerousChars.Any(danger => (body + uri + headers).Contains(danger)))
    {
        logger.LogException(new Exception("Dangerous Characters < or > found in request content"));
        
        isXssed = true;
    }

    bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);

    return isXssed;
}
```

Let's introduce a humble oject RequestContextProvider which only isolates the bits that are hard to test/mock. 

Now we can easily test IsXssed by mocking RequestContextProvider. 

```
public bool IsXssed()
{
    var isXssed = false;

    var body = requestContextProvider.GetBody(); 

    var uri = requestContextProvider.GetUri();

    var headers = requestContextProvider.GetHeaders();

    if (dangerousChars.Any(danger => (body + uri + headers).Contains(danger)))
    {
        logger.LogException(new Exception("Dangerous Characters < or > found in request content"));
        isXssed = true;
    }
    
    return isXssed;
}
```

```
public class RequestContextProvider : IRequestContextProvider
{
    public string GetBody()
    {
        var bodyStream = new StreamReader(HttpContext.Current.Request.InputStream);

        bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);

        var body = String.Copy(bodyStream.ReadToEnd());

        bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);

        return body;
    }

    public string GetHeaders()
    {
        return HttpContext.Current.Request.Headers.ToString();
    }

    public string GetUri()
    {
        return HttpContext.Current.Request.Url.AbsoluteUri;
    }
}

```

## Reference:

* [xUnit](http://xunitpatterns.com/Humble%20Object.html)
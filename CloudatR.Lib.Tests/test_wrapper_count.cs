using CloudatR.Lib.Abstractions;
using CloudatR.Lib.DependencyInjection;
using CloudatR.Lib.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCloudatR(typeof(TestNotification).Assembly);
var provider = services.BuildServiceProvider();

// Use reflection to access the internal cache
var mediatorType = typeof(IMediator);
var mediator = provider.GetRequiredService<IMediator>();

// Try to get the cache through reflection
var mediatorImplType = mediator.GetType();
var cacheField = mediatorImplType.GetField("_handlerCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
if (cacheField != null)
{
    var cache = cacheField.GetValue(mediator);
    var cacheType = cache.GetType();
    var getNotificationHandlersMethod = cacheType.GetMethod("GetNotificationHandlers");
    if (getNotificationHandlersMethod != null)
    {
        var wrappers = getNotificationHandlersMethod.Invoke(cache, new object[] { typeof(TestNotification) });
        var wrappersArray = (System.Collections.IList)wrappers;
        Console.WriteLine($"Number of wrappers in cache: {wrappersArray.Count}");
    }
}

Console.WriteLine("Done");

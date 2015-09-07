# QuickLearn Push Trigger Tools

QuickLearn's Push Trigger Tools provide you with standard interfaces for both your Push Trigger API App, and any client applications where the push events originate, to interface with a storage location for callback URIs, configurations, and credentials. This enables you to write your push trigger actions using standard boilerplate code, focusing instead on implementing on the callback storage mechanism implementation.

Additionally it provides a **Callback** class that allows you to quickly invoke a callback given only the URI.

# What would this look like in my push trigger API App?
To get started, you will need to [install the **QuickLearn.LogicApps.PushTrigger** NuGet package](https://www.nuget.org/packages/QuickLearn.LogicApps.PushTrigger/), and you will need to [install the **TRex** NuGet package](https://www.nuget.org/packages/TRex/).

From there, your controller would need to implement two methods. The first would store the callback information sent from the Logic App, the second would delete stored callbacks by id.

The purpose of adding the **QuickLearn.LogicApps.PushTrigger** NuGet package was to provide a standard interface for implementing the callback store that provides this capability -- providing more focused/boilerplate push trigger development, and enabling easier testing and dependency injection.

```csharp
using TRex.Metadata;
using QuickLearn.LogicApps;

namespace QuickLearn.LogicApps.Controllers
{
    public class CustomPushTriggerController : ApiController
    {

        ICallbackStore<PushTriggerConfiguration> _callbackStore = new Your_Callback_Store_Implementation();

		public CustomPushTriggerController()
        {

        }

		public CustomPushTriggerController(ICallbackStore<PushTriggerConfiguration> callbackStore)
        {
            _callbackStore = callbackStore;
        }

		[Trigger(TriggerType.Push, typeof(PushTriggerOutput))]
        [HttpPut, Route("callback")]
        [Metadata("Trigger Fired", "Custom push trigger was fired by event originating in some external application")]
        public async Task<HttpResponseMessage> RegisterCallbackAsync(
            string triggerId,
            [FromBody]TriggerInput<PushTriggerConfiguration, PushTriggerOutput> parameters)
        {

            var callbackUri = parameters.GetCallback().CallbackUri;

            await _callbackStore.WriteCallbackAsync(triggerId, callbackUri, parameters.inputs);

            return Request.PushTriggerRegistered(parameters.GetCallback());

        }

		[UnregisterCallback()]
        [HttpDelete, Route("callback")]
        public async Task DeleteCallbackAsync(string triggerId)
        {
            await _callbackStore.DeleteCallbackAsync(triggerId);
        }
	}
}

```

# Where are the PushTriggerOutput and PushTriggerConfiguration classes?
Those are just placeholder class names.

You will need to define a class that serves as both the push trigger configuration and the push trigger output.

The configuration model is what will be surfaced to users configuring your push trigger in the Logic App designer.
Maybe your configuration model has members like "FilePattern" or "AllowDuplicates", or "SensorThreshhold" depending
on your needs. As those values are configured, the idea is that you want them to have an effect within the application
that is actually handling the events that cause the push. As a result, you must store those values, and the ICallbackStore
interface assumes this.

The output model is the shape of the data that will be sent to the Logic App when the push trigger fires. In truth, this
may not matter (e.g., maybe you simply care that an event happened, but don't care about the details), but at present the
client side library that is built alongside the **QuickLearn.LogicApps.PushTrigger** package assumes that your push
trigger will have output.

# So how might I implement the IClientCallbackStore interface?
Let's imagine for a moment that you were going to use an Azure Mobile App to provide a nice tidy storage location for
callbacks (that might later be invoked from a mobile device once some sensor condition is met). In that case, you
might end up with an implementation that looks like this:

```csharp
    public class AzureMobileAppCallbackStore : ICallbackStore<PushTriggerConfiguration>
    {

        MobileServiceClient client = new MobileServiceClient(
            mobileAppUri: "URL HERE",
            gatewayUri: "GATEWAY HERE",
            applicationKey: "APP KEY HERE");

        public async Task WriteCallbackAsync(string triggerId, Uri callbackUri, PushTriggerConfiguration triggerConfig)
        {

            var callbackItemsTable = client.GetTable<CallbackItem>();
            var existingCallback = await callbackItemsTable.Where(c => c.TriggerId == triggerId).ToEnumerableAsync();

            if (existingCallback.Any())
            {
                var currentCallbackItem = existingCallback.FirstOrDefault();
                currentCallbackItem.CallbackUri = callbackUri.ToString();
                currentCallbackItem.SuppressDuplicates = triggerConfig.SuppressDuplicates;
                await callbackItemsTable.UpdateAsync(currentCallbackItem);
            }
            else
            {
                await callbackItemsTable.InsertAsync(new CallbackItem()
                {
                    CallbackUri = callbackUri.ToString(),
                    SuppressDuplicates = triggerConfig.SuppressDuplicates,
                    Id = Guid.NewGuid().ToString("N"),
                    TriggerId = triggerId
                });
            }

            return;
        }

        public async Task DeleteCallbackAsync(string triggerId)
        {
            var callbackItemsTable = client.GetTable<CallbackItem>();
            var existingCallback = await callbackItemsTable.Where(c => c.TriggerId == triggerId).ToEnumerableAsync();

            if (existingCallback.Any())
            {
                await callbackItemsTable.DeleteAsync(existingCallback.FirstOrDefault());
            }

            return;
        }

    }

```

# What would this look like in my code in the application where push events originate?

To get started, you will need to [install the **QuickLearn.LogicApps.PushClient** NuGet package](https://www.nuget.org/packages/QuickLearn.LogicApps.PushClient/), and you will need to [install the **TRex** NuGet package](https://www.nuget.org/packages/TRex/).

Just like on the server side (within your push trigger API App), one of the key tasks within your client app will be to
interact with the callback store. In this case, the **IClientCallbackStore** interface is provided. A sample implementation
is shown below (again for connecting to an Azure Mobile App):

```csharp
    public class AzureMobileAppClientCallbackStore : IClientCallbackStore<PushTriggerConfiguration, PushTriggerOutput>
    {

        MobileServiceClient client = new MobileServiceClient(
            mobileAppUri: "URL HERE",
            gatewayUri: "GATEWAY HERE",
            applicationKey: "APP KEY HERE");


        public async Task<IEnumerable<Callback<PushTriggerConfiguration, PushTriggerOutput>>> ReadCallbacksAsync()
        {
            var callbackItemsTable = client.GetTable<CallbackItem>();
            var allCallbacks = await callbackItemsTable.ToEnumerableAsync();

            return from cb in allCallbacks
                   select new Callback<PushTriggerConfiguration, PushTriggerOutput>()
                   {
                       Configuration = new PushTriggerConfiguration()
                       {
                           SuppressDuplicates = cb.SuppressDuplicates
                       },
                       UriWithCredentials = new Uri(cb.CallbackUri)
                   };
        }
    }
```

At this point, your client app has the ability to query for awaiting Logic Apps, and has the ability to invoke them.

```csharp
using QuickLearn.LogicApps;

/*...*/

var clientCallbackStore = new AzureMobileAppClientCallbackStore();
var callbacks = await clientCallbackStore.ReadCallbacksAsync();

foreach (var callback in callbacks)
{
	// Example of how to read from configuration
    if (callback.Configuration.SomeConfigurationValue > 42) continue;

	// Example of how to invoke a callback
    await callback.InvokeAsync(new PushTriggerOutput()
    {
		// This data will be available in the Logic App as @{triggers().outputs.body.SomeProperty}
        SomeProperty = "SomeValue"
    });
}
```

# That's all for now!

At the moment, I must apologize for the state of these documents. These were interfaces/classes put together for
internal use originally, but I've decided to make them more widely available. Feel free to use, not use, fork, customize, and
extend things here as you see fit.

# authorizer <img src="https://badge.ttsalpha.com/api?label=status&status=passing&color=green" alt="status"/> <img src="https://badge.ttsalpha.com/api?label=license&status=MIT" alt="license"/>
The project is intended to enable jwt-token based authorization in ASP.NET Core. The typical example of how does jwt access/refresh
tokens work is explained [here](https://gist.github.com/zmts/802dc9c3510d79fd40f9dc38a12bccfc).

<div align="center">
  <img src="https://img.shields.io/badge/JWT-black?logo=JSON%20web%20tokens" alt="JWT Badge">
  <a href="https://learn.microsoft.com/en-us/aspnet/core/?view=aspnetcore-8.0"><img src="https://img.shields.io/badge/asp.net%20core-420987?style=flat&logo=dotnet&link=https://learn.microsoft.com/en-us/aspnet/core/?view=aspnetcore-8.0" alt="asp.net core" /></a>
  <a href="https://fingerprint.com/blog/browser-fingerprinting-csharp/"><img src="https://img.shields.io/badge/Fingerprint-d94d16?style=flat&link=https://fingerprint.com/blog/browser-fingerprinting-csharp/" alt="Fingerprint" /></a>
  <a href="https://nginx.org/en/docs/"><img src="https://img.shields.io/badge/Nginx-09ad14?style=flat&logo=nginx&link=https://nginx.org/en/docs/" alt="Nginx" /></a>
</div>


## Fingerprint
The source code of the project is here: https://fingerprint.com/blog/browser-fingerprinting-csharp/. 

The project uses MVC pattern but without controller. Instead, each of the view has the corresponding `.cs` file extension, which serves
as the actions in controllers. The `@page` directive is essential in this case, as it is responsible to make the file as direct 
request handler. 

The following line in `Program.cs`  extracts the api key which is provided by [fingerprint dashboard](https://dashboard.fingerprint.com/).
```csharp
// Retrieve the FingerprintApiKey and use it to configure a new `IFingerprintApi` service.
var fingerPrintApiKey = builder.Configuration["FingerprintApiKey"]
?? throw new System.Configuration.ConfigurationErrorsException("The FingerprintApiKey property is required.");
builder.Services.AddSingleton<IFingerprintApi>(new FingerprintApi(new Configuration(fingerPrintApiKey)));
```
The api key serves as an authentication token, allowing the API to verify that the request is coming from a legitimate source.
The Fingerprint server checks the API key against its records. If the key is valid and active, the server processes the request and returns the relevant data.

In the `RegisterModel.cshtml.cs`, we have a subclass called `InputModel` which has 2 properties:
```csharp
public class InputModel
{
     [Required]
     public string VisitorId { get; set; }

     [Required]
     public string RequestId { get; set; }
}
```
These properties get initialized in the `.cshtml`(razor page) itself.
```html
<input asp-for="Input.VisitorId" class="d-none" id="visitorId" />
<input asp-for="Input.RequestId" class="d-none" id="requestId" />
...
<script>
    // Initialize the agent on page load.
    const fpPromise = import('https://fpjscdn.net/v3/Atg9Y1zT02TTNGOQYbkl')
            .then(FingerprintJS => FingerprintJS.load())

    // Get the visitorId when you need it.
    fpPromise
            .then(fp => fp.get())
            .then(result => {
                document.querySelector("#visitorId").value = result.visitorId;
                document.querySelector("#requestId").value = result.requestId;
                document.querySelector("#registerSubmit").disabled = false;
            })
</script>
```
The `js` script dynamically imports the FingerprintJS library from a CDN. Then, it waits for the fpPromise (the promise that resolves when the FingerprintJS library is loaded) to resolve.
When resolved, it calls the `get()` method on the `FingerprintJS` instance (fp). This method generates a unique `visitor ID` and `request ID` based on various browser and device characteristics. 
It returns a promise that resolves with the fingerprint data.

In the `OnPostAsync()` method(which is triggered for handling post request for the corresponding page) of the `Register` page,
we have the following:
```csharp
var fingerprintEvent = await _fingerprintApi.GetEventAsync(Input.RequestId);
```
This method makes an API call to the `Fingerprint` service to retrieve the data associated with a `RequestId`:
* `VisitorId` – A unique identifier for the visitor.
* `Confidence Score `– A measure of how accurately the visitor has been identified.
* `Timestamp` – When the fingerprint was created.
> Internally, The FingerprintJS API receives your `Request ID` and queries its distributed database.

### What is the purpose of `RequestId`, `VisitorId` ?
For every submission of form, a new, unique RequestId is generated. The `VisitorId` is the same for the same browser/device.
* Same Visitor ID for the same browser/device combination signifies that the user is recognized as the same visitor.
* Different Request ID for each submission ensures that each request is treated as a distinct interaction, which adds a layer of security against attempts to reuse or forge requests.
The `RequestId` has a short lifetime(in our code only 2 minutes):
```csharp
var identifiedAt = DateTimeOffset.FromUnixTimeMilliseconds(identification.Timestamp ??
throw new FormatException(
"Missing identification timestamp"));
if (DateTimeOffset.UtcNow - identifiedAt > TimeSpan.FromMinutes(2))
{
    ModelState.AddModelError(string.Empty, "Expired identification timestamp.");
    return Page();
}
```
The `visitor ID` retrieved from the submitted `form` is then compared to `visitor's ID` from the `FingerprintJS API`.
If there's a mismatch, the user gets the model error in the page:
```csharp
// Check that the Visitor ID submitted in the form matches the Visitor ID returned by
// Fingerprint for the RequestId. This prevents users from forging the Visitor ID.
if (identification.VisitorId != Input.VisitorId)
{
    ModelState.AddModelError(string.Empty, "Forged Visitor ID.");
    return Page();
}
```

The code below is important too:
```csharp
// Fingerprint returns a confidence value that represents how accurately the visitor was
// identified. If the identification confidence is less than 90%, then reject the registration
// request.
if (confidence < 0.9f)
{
    ModelState.AddModelError(string.Empty, "Low confidence identification score.");
    return Page();
}
```
But why is the confidence level of **90%** is set ? Because the user's `VisitorId` can change when user changes in the
device or browser environment. For example, when updating his browser from version 130 to 131. 
Also, each user can have at most 4 active sessions(shown below).This prevents user from registering from the same browser, thus
preventing from fake account registrations. 
```csharp
// Query the database and block the registration request if five or more accounts have
// been registered with the same Visitor ID in the last seven days (week).
var startDate = DateTime.UtcNow.AddDays(-7);
if (_applicationDbContext.Users.Count(x => x.Fingerprint == Input.VisitorId && 
                                         x.RegistrationDate >= startDate) >= 5)
{
    ModelState.AddModelError(string.Empty, "You cannot register another account using this browser.");
    return Page();
}
```



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
But why can the confidence score vary? FingerprintJS uses a probabilistic identification approach, meaning it identifies
users based on multiple browser/device signals. These signals include:
* Browser type and version
* Operating system and version
* Screen resolution and timezone
* IP address (if available)
* Installed fonts, plugins, and cookies

The challenge is that not all of these signals are stable. Some of them can change between requests, resulting in a confidence
score that isn’t perfect.

##### Scenarios That Can Affect the Confidence Score

* Browser Updates or Changes

If the browser or its plugins get updated between form submission and validation, some of the signals may change. This can lower the confidence score.
* IP Address Change

If the user switches networks (e.g., moving from Wi-Fi to mobile data), the IP address will change, which affects the fingerprint.

When you call  `_fingerprintApi.GetEventAsync(Input.RequestId)` `FingerprintJS` checks if the `Request ID`
is linked to a specific `Visitor ID`. If a match is found, it compares the current fingerprint signals against the ones that were
collected when the `Request ID` was first generated.

* If most of the signals match, FingerprintJS assigns a high confidence score (e.g., 98%).
* If there are small discrepancies (like a browser update), the score may drop (e.g., 85-90%).

Even though the `Request ID` corresponds to a specific visitor, the confidence score reflects the similarity
between the previously collected signals and the current ones.


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
## How do we implement Fingerprint in our project ?
Unlike the [FingerprintAspNetCore](FingerprintAspNetCore), we don't use `Identity API` and manage the database operations
(e.g. `await _db.SaveChangesAsync()`) and sign in users explicitly without the usage of `SignInManager` and `UserManager`.
When user registers or logs in, javascript client handles the form submission and sets the `access_token` sent by server:
```javascript
 // Handle form submission with JavaScript.
    $('#loginForm').on('submit', function (event) {
        event.preventDefault();  // Prevent default form submission.

        const data = $(this).serialize();  // Serialize form data.

        $.post('/Account/Register', data)  // Send POST request.
            .done(function (response) {
                // Save the access token to localStorage.
                localStorage.setItem('access_token', response.access_token);
                alert('Login successful! Tokens saved to localStorage.');
            })
            .fail(function (xhr) {
                alert('Login failed: ' + xhr.responseText);
            });
    });
```
Besides the `access_token`, server sets the `refresh_token` in cookies in `HttpOnly` mode, thus preventing client to access
the token. 
```csharp
var randomNumber = new byte[32];
using var rng = RandomNumberGenerator.Create();
rng.GetBytes(randomNumber);
var refreshToken = Convert.ToBase64String(randomNumber);
Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions()
{
    HttpOnly = true,
    Secure = true,
    SameSite = SameSiteMode.Strict,
    MaxAge = TimeSpan.FromDays(7)
});
```
Eventually, server sends both `access_token` and `refresh_token` to client. Actually, this is the optional behaviour i was just
trying to follow the [right implementation](https://gist.github.com/zmts/802dc9c3510d79fd40f9dc38a12bccfc). In our code, 
the `refreshToken` is set in server-side explicitly(watch the code above). 
All the code in `Login` and `Register` actions are almost the same. We are actually interested in `Refresh` action's 
implementation. Firstly, `Index` is the action that serves as the page for "authorized" users only. Before user accesses
the page, the client checks if user's token is expired:
```javascript
// Function to check if the token is expired.
function isTokenExpired(token) {
   try {
       const payload = JSON.parse(atob(token.split('.')[1])); // Decode token payload.
       const expiry = payload.exp * 1000; // Convert expiry to milliseconds.
       return Date.now() > expiry; // Check if the token is expired.
   } catch (e) {
       console.error('Invalid token:', e);
       return true; // Treat invalid token as expired.
   }
}
```
if it is expired, the request to `/Account/Refresh` endpoint is sent:
```javascript
const response = await fetch('/Account/Refresh', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
    },
    body: JSON.stringify(visitorId) //send fingerprint
});
```
Then, the `Refresh` action in `AccountController` retrieves the `fingerprint` along with `refreshToken` from `cookies`.
We retrieve the corresponding row in database, and upon success, delete the row with the session and insert the newly generated
session record with update the cookies with the `new refreshToken`:
```csharp
var session = await _db.RefreshSessions
                .Include(u => u.User)
                .FirstOrDefaultAsync(r => r.RefreshToken == refreshToken);
_db.RefreshSessions.Remove(session);
var newSession = new RefreshSession()
{
    User = user,
    Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
    UA = Request.Headers["User-Agent"].ToString(),
    RefreshToken = newRefreshToken,
    ExpiresIn = newExpiresIn,
    Fingerprint = fingerprint
 };
_db.RefreshSessions.Add(newSession);
await _db.SaveChangesAsync();
Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
{
     HttpOnly = true,
     Secure = true,   // Use HTTPS only.
     SameSite = SameSiteMode.Strict, 
     Expires = DateTimeOffset.FromUnixTimeMilliseconds(newExpiresIn)
});
return Ok(new { access_token = newAccessToken });
```
If the refresh token is expired, browser forces the user to re-login:
```csharp
var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
if (session.ExpiresIn < now)
{
    return Unauthorized("Refresh token expired.");
}
```

```javascript
console.error('Error refreshing token:', error);
alert('Session expired. Please log in again.');
localStorage.removeItem('access_token'); // Clear tokens on failure.
window.location.href = '/Account/Login'; // Redirect to login page.
```
The log out process is simple, backend just retrieves the record with the current `refreshToken` from db and deletes it.
```csharp
if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
{
    var session = await _db.RefreshSessions.FirstOrDefaultAsync(r => r.RefreshToken == refreshToken);
    if (session != null)
    {
        _db.RefreshSessions.Remove(session);
        await _db.SaveChangesAsync();
    }
    else
    {
        return BadRequest(new { message = "Session not found." }); // Handle case where session does not exist
    }
}
```
It also clears the cookies:
```csharp
Response.Cookies.Delete("refreshToken");
```
The client removes the `access_token` from localStorage and redirects user to `Login` action:
```javascript
localStorage.removeItem('access_token');
window.location.href = '/Account/Login';
```




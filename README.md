# authorizer <img src="https://badge.ttsalpha.com/api?label=status&status=passing&color=green" alt="status"/> <a href="https://github.com/blendereru/authorizer?tab=MIT-1-ov-file"><img src="https://badge.ttsalpha.com/api?label=license&status=MIT" alt="license"/></a>
The project is intended to enable jwt-token based authorization in ASP.NET Core. The typical example of how does jwt access/refresh
tokens work is explained [here](https://gist.github.com/zmts/802dc9c3510d79fd40f9dc38a12bccfc).

<div align="center">
  <a href="https://gist.github.com/zmts/802dc9c3510d79fd40f9dc38a12bccfc"><img src="https://img.shields.io/badge/JWT-black?logo=JSON%20web%20tokens" alt="JWT Badge"></a>
  <a href="https://learn.microsoft.com/en-us/aspnet/core/?view=aspnetcore-8.0"><img src="https://img.shields.io/badge/asp.net%20core-420987?style=flat&logo=dotnet&link=https://learn.microsoft.com/en-us/aspnet/core/?view=aspnetcore-8.0" alt="asp.net core" /></a>
  <a href="https://fingerprint.com/blog/browser-fingerprinting-csharp/"><img src="https://img.shields.io/badge/Fingerprint-d94d16?style=flat&logo=javascript&logoColor=black&link=https://fingerprint.com/blog/browser-fingerprinting-csharp" alt="Fingerprint" /></a>
</div> 

## Description
The explanation of code in both projects(1 - FingerprintAspNetCore, 2 - JwtService) is provided [here](workflow.md).
The project uses `.net` of version `8.0`. The main project is JwtService, whereas the [FingerprintAspNetCore](FingerprintAspNetCore)
is the project taken from [fingerprint](https://fingerprint.com/blog/browser-fingerprinting-csharp/) documentation, just to
show you how the fingerprint is typically implemented in your c# application.

## Package versions
```csproj
<PackageReference Include="FingerprintPro.ServerSdk" Version="7.0.0-test.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10">
```

## How to Run the Project

Follow these steps to get the project up and running on your local machine:

1. **Open Git Bash**
    - If you don't have Git Bash installed, you can download it from [here](https://www.atlassian.com/git/tutorials/git-bash).
    - Once installed, open Git Bash.

2. **Select the Path for Cloning**
    - Use the `cd` command to navigate to the directory where you want to clone the repository. For example:
      ```bash
      cd /path/to/your/directory
      ```
    - Replace `/path/to/your/directory` with your desired path.

3. **Clone the Repository**
    - Paste the following command in Git Bash to clone the repository:
      ```bash
      git clone https://github.com/blendereru/authorizer.git
      ```

4. **Navigate into the Project Directory**
    - Change into the project directory that was just created:
      ```bash
      cd authorizer
      ```

5. **Install Dependencies**
    - If your project requires any dependencies, install them by running:
      ```bash
      dotnet restore
      ```
    - This will restore the NuGet packages specified in your project file.

6. **Run the Project**
    - To run the project, use the following command:
      ```bash
      dotnet run
      ```
    - This will start the application, and you can access it as specified in your project's documentation (usually in a web browser).

7. **Open in a Browser**
    - If it's a web application, open your browser and go to `http://localhost:5000` (or whichever port your application is running on) to see it in action.

### Troubleshooting
- If you encounter issues while running the project, check the following:
    - Ensure you have the correct version of the .NET SDK installed.
    - Review any error messages for guidance on what might be wrong.






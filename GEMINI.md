# Project Standard Practices

## Microservice Scripts

When creating a new microservice, the following practices should be followed for the run script:

*   The script should be placed in the `./bin` directory.
*   The script should be named `run-<service-name>`.
*   The script must be executable (`chmod +x`).
*   The script should print the IP address and port the service is binding to.
*   The script should use the `--no-launch-profile` flag when running .NET projects to ensure it uses the configuration from the `.env` file.

## .NET Project Configuration

All .NET projects in this solution must adhere to the following configuration practices:

*   Configuration should be loaded from a `.env` file in the root of the project.
*   The `DotNetEnv` NuGet package should be used to load the `.env` file.
*   Sensitive information, such as credentials, and environment-specific settings, like ports and binding IPs, should be stored in the `.env` file.
*   The binding IP and port should be configured separately using `APP_IP` and `APP_PORT` variables in the `.env` file. The run script will then construct the `ASPNETCORE_URLS` variable from these.

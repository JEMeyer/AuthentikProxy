# AuthentikProxy

A lightweight OIDC proxy service designed to simplify OIDC authentication for client applications with Authentik identity provider.

## Overview

AuthentikProxy acts as an intermediary between your client applications and your Authentik server, allowing you to:

1. Centralize OIDC client configurations in one place
2. Keep client secrets secure by not exposing them to frontend applications
3. Support multiple client applications through a single proxy instance
4. Simplify client-side authentication implementation

## Why Use AuthentikProxy?

While Authentik is a powerful identity provider, there are scenarios where a proxy can be beneficial:

- **Security**: Client secrets shouldn't be embedded in frontend code. AuthentikProxy keeps these secrets server-side.
- **Simplified Configuration**: Configure multiple client applications in one place.
- **Development Workflow**: Use the same authentication flow across different environments.
- **Private Applications**: For applications you build yourself, this proxy provides a streamlined authentication experience.

## How It Works

The proxy exposes standard OIDC endpoints that client applications can use:

- `/Authentik/authorize` - Initiates the authorization flow
- `/Authentik/token` - Exchanges authorization codes for tokens
- `/Authentik/userinfo` - Retrieves user information using access tokens
- `/Authentik/callback` - Handles the callback from Authentik

Behind the scenes, AuthentikProxy forwards these requests to your Authentik server, handling all the necessary token exchanges and client authentication.

## Use Cases

- **Personal Projects**: Simplify authentication for applications you build yourself
- **Microservices**: Use as a central authentication service for your microservices architecture
- **Development Environment**: Provide a consistent authentication mechanism across development, staging, and production

## Docker Deployment

The easiest way to deploy AuthentikProxy is using the Docker image:

```bash
docker run -d \
  --name authentik-proxy \
  -p 8080:8080 \
  -e Authentik__Url=https://your-authentik-server \
  -e Authentik__Clients__0__ClientId=your-client-id \
  -e Authentik__Clients__0__ClientSecret=your-client-secret \
  -e Authentik__Clients__0__Slug=app1 \
  -e CorsSettings__AllowedOrigins__0=https://your-app-domain \
  your-docker-username/authentik-proxy:latest
```

## Configuration

AuthentikProxy can be configured through environment variables or by mounting a custom appsettings.json file:

```json
{
  "Authentik": {
    "Url": "https://your-authentik-server",
    "Clients": [
      {
        "ClientId": "client-id-1",
        "ClientSecret": "client-secret-1",
        "Slug": "app1"
      },
      {
        "ClientId": "client-id-2",
        "ClientSecret": "client-secret-2",
        "Slug": "app2"
      }
    ]
  },
  "CorsSettings": {
    "AllowedOrigins": [
      "https://app1.example.com",
      "https://app2.example.com"
    ]
  }
}
```

## Difference from Direct Authentik Integration

AuthentikProxy is designed for applications you build yourself that need a simplified OIDC flow. For third-party applications that already have built-in OIDC support, connecting them directly to your Authentik server is often the better approach.

## License

This project is licensed under the terms of the included LICENSE file.

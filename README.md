# AadApiGatekeeper
The AadApiGatekeeper protects your API with Azure Active Directory.

## Description
The here implemented Gatekeeper can be used as a Reverse Proxy in front of your API.
Protecting an API using Azure Active Directory can often be anoying, especially if you are using programming languages
where no Azure Active Directory integration libraries are available or if you don't find any examples in the internet.
The AadApiGatekeeper is implemented in ASP.NET Core and can be used on every platfrom that supports the .NET Core runtime.

### Azure Active Directory Integration
The integration in AAD is very simple. You have to create an AAD application in your tenant and configure the Gatekeeper with the application's ClientId and ClientSecret.
Creating a new AAD application is shown here:
https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-v1-integrate-apps-with-azure-ad

### Features
- ReverseProxy 
- Get user's claims with a single call to http://<reverseproxy>/me
- Acquire tokens for other protected AAD resources for On-Behalof-Of flow
- Define public accessable routes in your API
- Simple Kubernetes integration ([SideCar example](doc/SideCarExample.md))
- Token caching

### Configuration

### Overview

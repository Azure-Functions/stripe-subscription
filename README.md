# Stripe Subscription Start Azure Function

This repo contains a single Azure Functions v2 function that can receive a form post and setup an appropriate subscription for it in Stripe.

The app includes just one function:

* `StartSubscription` - receives form POST submission and creates a subscription via Stripe

## Setup

To set this up, you'll need to have an [Azure Portal account](https://portal.azure.com).


1. Fork this repository
2. [Create a **v2** Azure Function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function)
3. [Configure your function to deploy from your fork](https://docs.microsoft.com/en-us/azure/azure-functions/scripts/functions-cli-create-function-app-github-continuous)
4. Set up the following [App Settings for your Azure Function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings)

| Setting | Value
| -------- | -------
| `SubscriptionSuccessUrl` | where to redirect a users browser to if the subscription was **successful**
| `SubscriptionFailureUrl` | where to redirect a users browser to if the subscription **failed**
| `StripeApiSecretKey` | your **secret key**  from the *Developers > API keys* in your Stripe dashboard

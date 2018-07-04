using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;

namespace StripeSubscription
{
    public static class StartSubscriptionFunction
    {
        private static readonly Regex validEmail = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$"); // Simplest form of email validation

        [FunctionName("StartSubscription")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequest request, TraceWriter log, ExecutionContext context)
        {
            var config = Config.CreateFrom(context);
            if (!config.IsValid(out var configErrorMessage))
                return new BadRequestErrorMessageResult(configErrorMessage);

            var form = SubscriptionForm.CreateFrom(request);
            if (!form.IsValid(out string errors))
                return new BadRequestErrorMessageResult(errors);

            try
            {
                StripeConfiguration.SetApiKey(config.StripeApiSecretKey);
                await CreateCustomerSubscriptionAtStripe(form);

                return new RedirectResult(config.SubscriptionSuccessUrl);
            }
            catch
            {
                return new RedirectResult(config.SubscriptionFailureUrl);
            }
        }

        private static async Task<string> CreateCustomerSubscriptionAtStripe(SubscriptionForm subscriptionForm)
        {
            var customer = await new StripeCustomerService()
                .CreateAsync(new StripeCustomerCreateOptions { Email = subscriptionForm.StripeEmail, SourceToken = subscriptionForm.StripeToken });

            var subscription = await new StripeSubscriptionService().CreateAsync(customer.Id, new StripeSubscriptionCreateOptions
            {
                Items = new List<StripeSubscriptionItemOption>
                {
                    new StripeSubscriptionItemOption { PlanId = subscriptionForm.PlanId }
                }
            });

            return subscription.Id;
        }

        private static T ToInstance<T>(this IEnumerable<KeyValuePair<string, string>> source)
        {
            var dictionary = source.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
            var result = Activator.CreateInstance<T>();
            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (dictionary.TryGetValue(field.Name, out var value))
                    field.SetValue(result, value);
            return result;
        }

        public class Config
        {
            public string StripeApiSecretKey;
            public string SubscriptionSuccessUrl;
            public string SubscriptionFailureUrl;

            public static Config CreateFrom(ExecutionContext context)
            {
                return new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", true)
                    .AddEnvironmentVariables()
                    .Build()
                    .AsEnumerable()
                    .ToInstance<Config>();
            }

            public bool IsValid(out string errorMessages)
            {
                var errors = new StringBuilder();

                if (String.IsNullOrWhiteSpace(StripeApiSecretKey))
                    errors.AppendFormat("Stripe API secret key not defined in Azure Function setting '{0}'\n", nameof(StripeApiSecretKey));

                if (String.IsNullOrWhiteSpace(SubscriptionSuccessUrl))
                    errors.AppendFormat("Success url not defined in Azure Function setting '{0}'\n", nameof(SubscriptionSuccessUrl));
                else if (Uri.TryCreate(SubscriptionSuccessUrl, UriKind.Absolute, out var _))
                    errors.AppendFormat("Success url not valid url format in Azure Function setting '{0}'\n", nameof(SubscriptionSuccessUrl));

                if (String.IsNullOrWhiteSpace(SubscriptionFailureUrl))
                    errors.AppendFormat("Failure url not defined in Azure Function setting '{0}'\n", nameof(SubscriptionFailureUrl));
                else if (Uri.TryCreate(SubscriptionFailureUrl, UriKind.Absolute, out var _))
                    errors.AppendFormat("Failure url not defined in Azure Function setting '{0}'\n", nameof(SubscriptionFailureUrl));

                errorMessages = errors.ToString();

                return errors.Length == 0;
            }
        }

        public class SubscriptionForm
        {
            public string StripeEmail;
            public string StripeToken;
            public string PlanId;

            public static SubscriptionForm CreateFrom(HttpRequest request)
            {
                return request.Form.ToDictionary(k => k.Key, v => v.Value.FirstOrDefault()).ToInstance<SubscriptionForm>();
            }

            public bool IsValid(out string errorMessages)
            {
                var errors = new StringBuilder();

                if (String.IsNullOrWhiteSpace(StripeEmail))
                    errors.AppendFormat("Subscriber Email not found in '{0}' form value\n", nameof(StripeEmail));
                else
                    if (!validEmail.IsMatch(StripeEmail))
                    errors.AppendFormat("Subscriber Email does not match user@domain.com format in '{0}' form value\n", nameof(StripeEmail));

                if (String.IsNullOrWhiteSpace(StripeToken))
                    errors.AppendFormat("Public Stripe token not found in '{0}|' form value\n", nameof(StripeToken));

                if (String.IsNullOrWhiteSpace(PlanId))
                    errors.AppendFormat("Subscription plan ID not found in '{0}' form value\n", nameof(PlanId));

                errorMessages = errors.ToString();

                return errors.Length == 0;
            }
        }
    }
}
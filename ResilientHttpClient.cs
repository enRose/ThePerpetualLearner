using Newtonsoft.Json;
using Polly;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Linq;
using System.Net;

namespace ASB.API.TrueRewardsRedemptionExp
{
    public interface IResilientHttpClientSingleton
    {
        void UseTheseInOrder(params PolicyType[] policyTypes);

        Task<HttpResponseMessage> DoPostPutAsync<T>(
            HttpMethod method,
            string requestUrl,
            T payload,
            string authorizationToken = null,
            string requestId = null,
            string authorizationMethod = "Bearer");
    }

    public class ResilientHttpClientSingleton : IResilientHttpClientSingleton
    {
        private readonly HttpClient client;
        private PolicyStore policyStore;
        private PolicyWrap PolicyWrap { get; set; }
        private List<HttpStatusCode> serverSideErrorCodes;

        public ResilientHttpClientSingleton()
        {
            // should we lock it?
            client = new HttpClient();

            policyStore = new PolicyStore();

            serverSideErrorCodes = new List<HttpStatusCode> {
                HttpStatusCode.InternalServerError,
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.GatewayTimeout
                };
        }

        public void UseTheseInOrder(params PolicyType[] policyTypes)
        {
            var policies = policyTypes.ToList().Select(policyType =>
                 policyStore.Get(policyType)
            );

            PolicyWrap = Policy.WrapAsync(policies.ToArray());
        }

        public Task<HttpResponseMessage> DoPostPutAsync<T>(
            HttpMethod method,
            string requestUrl,
            T payload,
            string authorizationToken = null,
            string requestId = null,
            string authorizationMethod = "Bearer")
        {
            return CallHttp(async () =>
            {
                var requestMessage = new HttpRequestMessage(method, requestUrl);

                SetAuthorizationHeader(requestMessage);

                requestMessage.Content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                    );

                if (authorizationToken != null)
                {
                    requestMessage.Headers.Authorization =
                        new AuthenticationHeaderValue(
                            authorizationMethod,
                            authorizationToken
                        );
                }

                if (requestId != null)
                {
                    requestMessage.Headers.Add("x-requestid", requestId);
                }

                var response = await client.SendAsync(requestMessage).ConfigureAwait(false);

                if (serverSideErrorCodes.Contains(response.StatusCode) 
                    || 
                    IsTooManyRequests(response.StatusCode))
                {
                    throw new HttpRequestException();
                }

                return response;
            });
        }

        private bool IsTooManyRequests(HttpStatusCode httpStatusCode)
        {
            return httpStatusCode.ToString() == "429";
        }

        private async Task<T> CallHttp<T>(Func<Task<T>> action)
        {
            return await PolicyWrap.ExecuteAsync(action).ConfigureAwait(false);
        }

        private void SetAuthorizationHeader(HttpRequestMessage requestMessage)
        {
            // tied up to win OS IIS
            var authorizationHeader = HttpContext.Current.Request.Headers["Authorization"];

            if (!string.IsNullOrWhiteSpace(authorizationHeader))
            {
                requestMessage.Headers.Add(
                    "Authorization",
                    new List<string>() { authorizationHeader }
                    );
            }
        }
    }
}
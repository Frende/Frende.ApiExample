using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Tavis.UriTemplates;

namespace Frende.ApiExample
{
    class Program
    {
        static string issuer = "https://externaltest-login.frende.no/identityserver";
        static string apiUri = "https://externaltest-api.frende.no";
        private static string clientId = "";
        private static string certificateThumbprint = "";
        
        public static void Main(string[] args)
        {
            AsyncMain(args).GetAwaiter().GetResult();
        }

        static async Task AsyncMain(string[] args)
        {
            string birthNumber = "";

            var token = await FetchAccessToken(birthNumber);

            // Console.WriteLine($"Got access token: {token}");

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("X-Frende-ClientId", $"Frende.ApiExample.{clientId}");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpClient.BaseAddress = new Uri(apiUri);

                Guid customerId = await GetCustomerId(httpClient, birthNumber);
                var agreementsOverview = await GetAgreements(httpClient, customerId);

                foreach (var insurance in agreementsOverview.Insurances)
                {
                    Console.WriteLine($"Customer has {insurance.GetType().Name}");
                    switch (insurance.GetType().Name)
                    {
                        case "CarInsurance":
                            var carInsurance = (CarInsurance) insurance;
                            Console.WriteLine($"\tInsurance is {carInsurance.StandardInsuranceStatus.ToString()}");
                        break;
                    }

                }
            }

            Thread.Sleep(10000);
        }

        private static async Task<TResult> GetAndDeserialize<TResult>(HttpClient client, String uri)
        {
            return JsonConvert.DeserializeObject<TResult>(
                await client.GetStringAsync(uri));
        }
        private static async Task<TResult> PostAndDeserialize<TResult>(HttpClient client, String uri, object body)
        {

            var parameters = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var result = await client.PostAsync(uri, parameters);
            if (!result.IsSuccessStatusCode)
                throw new Exception($"Could not post to {uri}: " + result.ReasonPhrase);
            return JsonConvert.DeserializeObject<TResult>(await result.Content.ReadAsStringAsync());
        }
        private static async Task<AgreementsOverviewResource> GetAgreements(HttpClient httpClient, Guid customerId)
        {
            var agreementsRoot = await GetAndDeserialize<AgreementsRootResource>(httpClient, "/agreement");

            var overviewLink = new UriTemplate(agreementsRoot.OverviewLink.Href)
                .AddParameter("customerId", customerId).Resolve();

            var agreementsOverview = await GetAndDeserialize<AgreementsOverviewResource>(httpClient, overviewLink);
            return agreementsOverview;
        }

        private static async Task<Guid> GetCustomerId(HttpClient httpClient, string birthNumber)
        {

            var customerInformationRoot = await GetAndDeserialize<CustomerInformationRootResource>(httpClient, "/customerinformation");

            var lookupResponse = await PostAndDeserialize<CustomerLookupResponse>(httpClient,
                customerInformationRoot.CustomerLookup.Href, new
                {
                    BirthNumber = birthNumber
                });
            return lookupResponse.CustomerId;
        }
        
        private static async Task<string> FetchAccessToken(string birthNumber)
        {
            using (HttpClient client = new HttpClient())
            {
                // First we get the OpenID Connect discovery document:
                var discovery = await client.GetDiscoveryDocumentAsync(issuer);
                if (discovery.IsError) throw new Exception(discovery.Error);

                // The token endpoint is where we authenticate ourselves to get an access token.
                var tokenEndpoint = discovery.TokenEndpoint;
                var signingAlgorithm =
                    discovery.TryGetStringArray(OidcConstants.Discovery.TokenEndpointAuthSigningAlgorithmsSupported)
                        .FirstOrDefault() ?? "RS256";

                // Fetch certificate used when signing the assertion. We need to have access to the private key to do this. 
                var certs = X509.LocalMachine.My.Thumbprint.Find(certificateThumbprint, false);
                if (!certs.Any())
                    throw new Exception($"Could not find certificate with thumbprint {certificateThumbprint}");
                var cert = certs.Single();

                // Build an assertion to be signed by the pre-shared certificate.
                var assertion = new JwtSecurityToken(clientId, tokenEndpoint, claims: new List<Claim>
                    {
                        new Claim("sub", clientId),
                        new Claim("jti", new Random().Next().ToString()) // Should be unique for each assertion.
                    },
                    expires: DateTime.Now.Add(TimeSpan.FromMinutes(1)),
                    signingCredentials: new SigningCredentials(new X509SecurityKey(cert), signingAlgorithm));
                assertion.Header.Remove(JwtHeaderParameterNames
                    .Kid); // This is important due to an incompatibility between IdentityModel 4 and 5.

                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                // Fetch an access token to be used for a single customer
                var response = await client.RequestTokenAsync(new ClientCredentialsTokenRequest()
                {
                    Address = tokenEndpoint,
                    ClientId = clientId,
                    ClientAssertion = new ClientAssertion
                    {
                        Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                        Value = tokenHandler.WriteToken(assertion),
                    },
                    GrantType = "affiliation",
                    Parameters =
                    {
                        {
                            "scope", "agreement.read customerinformation"
                        }, // The list of scopes is supplied by frende. Invalid scope list will give an error.
                        {
                            "customer_birth_number", birthNumber
                        } // This must be a birth number of an affiliated customer
                    }
                });
                if (response.IsError) throw new Exception(response.Error);

                var token = response.AccessToken;
                return token;
            }
        }
        
    }
}

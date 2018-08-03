using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Frende.ApiExample
{

    public class Link
    {
        [JsonProperty("href")]
        public string Href;

        [JsonProperty("templated")]
        public bool Templated;
    }

    public class CustomerInformationRootResource
    {
        [JsonProperty("_links")]
        private IDictionary<string, Link> links;
        public Link CustomerLookup
        {
            get => links["customer_lookup"];
        }
    }

    public class AgreementsRootResource
    {
        [JsonProperty("_links")]
        private IDictionary<string, Link> links;
        public Link OverviewLink
        {
            get => links["overview"];
        }
    }

    public class AgreementsOverviewResource
    {
        [JsonProperty("_embedded")]
        private AgreementsEmbedded embedded;

        public IEnumerable<IInsurance> Insurances
        {
            get => embedded.Insurances;
        }
    }

    public class AgreementsEmbedded
    {

        [JsonConverter(typeof(InsuranceConverter))]
        [JsonProperty("insurances")]
        public IEnumerable<IInsurance> Insurances { get; set; }
    }

    public class CustomerLookupResponse
    {
        [JsonProperty]
        public Guid CustomerId;
    }

    public interface IInsurance
    {
    }

    public class PetInsurance : IInsurance
    {
        public int MonthlyPrice { get; set; }
        public int YearlyPrice { get; set; }

        [JsonConverter(typeof(StandardInsuranceStatusConverter))]
        public IStandardInsuranceStatus StandardInsuranceStatus { get; set; }
    }

    public class CarInsurance : IInsurance
    {
        public int MonthlyPrice { get; set; }
        public int YearlyPrice { get; set; }

        [JsonConverter(typeof(StandardInsuranceStatusConverter))]
        public IStandardInsuranceStatus StandardInsuranceStatus { get; set; }
    }

    public class UnimplementedInsurance : IInsurance
    {
    }

    public interface IStandardInsuranceStatus
    {
        string ToString();
    }

    public class StandardActive : IStandardInsuranceStatus
    {
        public DateTime ActivatedAt { get; set; }

        public string ToString()
        {
            return $"Active (Activated at {ActivatedAt})";
        }
    }

    public class StandardFuture : IStandardInsuranceStatus
    {
        public DateTime ActiveFrom { get; set; }
        public string ToString()
        {
            return $"Not yet active (Activated at {ActiveFrom})";
        }
    }

    public class StandardUnknown : IStandardInsuranceStatus
    {
        public string ToString()
        {
            return "Unknown status";
        }
    }
}
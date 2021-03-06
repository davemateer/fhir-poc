using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace FhirQuestionnairePoc.Pages
{
    public class QuestionnaireModel : PageModel
    {
        private readonly IMemoryCache cache;

        public QuestionnaireModel(IMemoryCache cache)
        {
            this.cache = cache;
        }

        [BindProperty(SupportsGet = true)]
        public string AccessToken { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Patient { get; set; }

        [BindProperty(SupportsGet = true)]
        public string State { get; set; }

        public decimal? SystolicBloodPressure { get; set; }

        public decimal? DiastolicBloodPressure { get; set; }

        public Condition[] Conditions { get; set; }

        public void OnGet()
        {
            string iss = cache.Get<string>($"state:{this.State}");
            FhirClient client = new(iss);
            //Patient ehrPatient = client.Read<Patient>($"Patient/{this.Patient}");

            this.GetBloodPressure(client);
            this.GetConditions(client);
        }

        private void GetBloodPressure(FhirClient client)
        {
            SearchParams query = new();
            query = query.Where($"patient=Patient/{this.Patient}").Where("category=vital-signs").Where("code=55284-4");
            query = query.OrderBy("_lastUpdated", SortOrder.Descending);
            query = query.LimitTo(1);

            Bundle searchResults = client.Search<Observation>(query);

            if (searchResults.Entry.Any())
            {
                var bloodPressure = searchResults.Entry[0].Resource as Observation;
                var systolic = bloodPressure.Component.First(_ => _.Code.Coding.Any(_ => _.Code == "8480-6")).Value as Quantity;
                var diastolic = bloodPressure.Component.First(_ => _.Code.Coding.Any(_ => _.Code == "8462-4")).Value as Quantity;
                this.SystolicBloodPressure = systolic.Value;
                this.DiastolicBloodPressure = diastolic.Value;
            }
        }

        private void GetConditions(FhirClient client)
        {
            SearchParams query = new();
            query = query.Where($"patient=Patient/{this.Patient}");

            Bundle searchResults = client.Search<Condition>(query);
            this.Conditions = searchResults.Entry.Select(_ => _.Resource).Cast<Condition>().ToArray();
        }
    }
}

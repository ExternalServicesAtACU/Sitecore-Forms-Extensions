using Sitecore.XConnect.Collection.Model;

namespace Feature.FormsExtensions.ValueProviders.xDbFieldValueBinders.ContactConcent
{
    public class XDbDoNotMarketFieldValueBinder : ConsentInformationFieldValueBinder
    {
        protected override IFieldValueBinderResult GetFieldBindingValueFromFacet(ConsentInformation facet)
        {
            if (facet.Consents.TryGetValue("DoNotMarket", out var consentItem))
            {
                // Adjust logic based on the actual structure of ConsentItem
                return new FieldValueBindingFoundResult(consentItem != null);
            }
            // Replace with appropriate logic or type for missing FieldValueBindingNotFoundResult
            return null; // Placeholder for missing type
        }

        public override void StoreValue(object newValue)
        {
            if (newValue is bool value)
            {
                UpdateFacet(x =>
                {
                    var consentItem = new ConsentItem(); // Adjust initialization as needed
                    // Set properties of consentItem based on your requirements
                    x.Consents["DoNotMarket"] = consentItem;
                });
            }
        }
    }
}

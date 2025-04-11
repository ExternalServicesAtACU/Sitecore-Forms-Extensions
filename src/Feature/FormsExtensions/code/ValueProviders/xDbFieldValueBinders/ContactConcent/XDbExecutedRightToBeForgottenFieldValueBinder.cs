using System;
using Sitecore.XConnect.Collection.Model;

namespace Feature.FormsExtensions.ValueProviders.xDbFieldValueBinders.ContactConcent
{
    public class XDbExecutedRightToBeForgottenFieldValueBinder : ConsentInformationFieldValueBinder
    {
        private const string RightToBeForgottenKey = "RightToBeForgotten";

        protected override IFieldValueBinderResult GetFieldBindingValueFromFacet(ConsentInformation facet)
        {
            bool hasConsent = facet != null && facet.Consents.ContainsKey(RightToBeForgottenKey);
            return new FieldValueBindingFoundResult(hasConsent);
        }

        public override void StoreValue(object newValue)
        {
            if (newValue is bool value)
            {
                UpdateFacet(x =>
                {
                    if (value)
                    {
                        x.Consents[RightToBeForgottenKey] = new ConsentItem();
                    }
                    else
                    {
                        x.Consents.Remove(RightToBeForgottenKey);
                    }
                });
            }
        }
    }
}
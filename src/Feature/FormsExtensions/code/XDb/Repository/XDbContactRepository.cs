using System;
using System.Threading.Tasks;
using Feature.FormsExtensions.XDb.Model;
using Sitecore.Analytics;
using Sitecore.Analytics.Model;
using Sitecore.Analytics.Tracking;
using Sitecore.XConnect;
using Sitecore.XConnect.Client;
using Sitecore.XConnect.Client.Configuration;
using Sitecore.XConnect.Collection.Model;

// Aliases to avoid ambiguity
using XConnectContact = Sitecore.XConnect.Contact;
using TrackingContact = Sitecore.Analytics.Tracking.Contact;

namespace Feature.FormsExtensions.XDb.Repository
{
    public class XDbContactRepository : IXDbContactRepository
    {
        [Obsolete("Use UpdateXDbContactEmailAsync to avoid potential deadlocks.")]
        public void UpdateXDbContactEmail(IXDbContactWithEmail basicContact)
        {
            UpdateXDbContactEmailAsync(basicContact).GetAwaiter().GetResult();
        }

        public async Task UpdateXDbContactEmailAsync(IXDbContactWithEmail basicContact)
        {
            using (var client = SitecoreXConnectClientConfiguration.GetClient())
            {
                var reference = new IdentifiedContactReference(basicContact.IdentifierSource, basicContact.IdentifierValue);
                var expandOptions = new ContactExpandOptions(CollectionModel.FacetKeys.PersonalInformation, CollectionModel.FacetKeys.EmailAddressList);
                var xDbContact = await client.GetAsync<XConnectContact>(reference, new ContactExecutionOptions(expandOptions));

                if (xDbContact != null)
                {
                    SetEmail(xDbContact, basicContact, client);
                    await client.SubmitAsync();
                }
            }
        }

        [Obsolete("Use GetContactIdAsync to avoid potential deadlocks.")]
        public Guid? GetContactId(IdentifiedContactReference reference)
        {
            return GetContactIdAsync(reference).GetAwaiter().GetResult();
        }

        public async Task<Guid?> GetContactIdAsync(IdentifiedContactReference reference)
        {
            using (var client = SitecoreXConnectClientConfiguration.GetClient())
            {
                var expandOptions = new ContactExpandOptions();
                var contact = await client.GetAsync<XConnectContact>(reference, new ContactExecutionOptions(expandOptions));
                return contact?.Id;
            }
        }

        [Obsolete("Use UpdateContactFacetAsync to avoid potential deadlocks.")]
        public void UpdateContactFacet<T>(IdentifiedContactReference reference, ContactExpandOptions expandOptions, Action<T> updateFacets, Func<T> createFacet) where T : Facet
        {
            UpdateContactFacetAsync(reference, expandOptions, updateFacets, createFacet).GetAwaiter().GetResult();
        }

        public async Task UpdateContactFacetAsync<T>(IdentifiedContactReference reference, ContactExpandOptions expandOptions, Action<T> updateFacets, Func<T> createFacet) where T : Facet
        {
            using (var client = SitecoreXConnectClientConfiguration.GetClient())
            {
                var xDbContact = await client.GetAsync<XConnectContact>(reference, new ContactExecutionOptions(expandOptions));

                if (xDbContact != null)
                {
                    MakeContactKnown(client, xDbContact);
                    var facet = xDbContact.GetFacet<T>() ?? createFacet();
                    updateFacets(facet);
                    client.SetFacet(xDbContact, facet);
                    await client.SubmitAsync();
                }
            }
        }

        public void SaveNewContactToCollectionDb(TrackingContact contact)
        {
            if (CreateContactManager() is ContactManager manager)
            {
                contact.ContactSaveMode = ContactSaveMode.AlwaysSave;
                manager.SaveContactToCollectionDb(contact);
            }
        }

        private static void MakeContactKnown(IXdbContext client, XConnectContact contact)
        {
            if (contact.IsKnown)
            {
                return;
            }
            if (!Sitecore.Configuration.Settings.GetBoolSetting("MakeContactKnownOnFormsUpdate", true))
            {
                return;
            }
            client.AddContactIdentifier(contact, new ContactIdentifier("scformsextension-known", Guid.NewGuid().ToString("N"), ContactIdentifierType.Known));
        }

        private static object CreateContactManager()
        {
            return Sitecore.Configuration.Factory.CreateObject("tracking/contactManager", true);
        }

        public void ReloadContactDataIntoSession()
        {
            if (Tracker.Current?.Contact == null)
                return;
            if (CreateContactManager() is ContactManager manager)
            {
                manager.RemoveFromSession(Tracker.Current.Contact.ContactId);
                Tracker.Current.Session.Contact = manager.LoadContact(Tracker.Current.Contact.ContactId);
            }
        }

        [Obsolete("Use UpdateOrCreateXDbServiceContactWithEmailAsync to avoid potential deadlocks.")]
        public void UpdateOrCreateXDbServiceContactWithEmail(IXDbContactWithEmail serviceContact)
        {
            UpdateOrCreateXDbServiceContactWithEmailAsync(serviceContact).GetAwaiter().GetResult();
        }

        public async Task UpdateOrCreateXDbServiceContactWithEmailAsync(IXDbContactWithEmail serviceContact)
        {
            using (var client = SitecoreXConnectClientConfiguration.GetClient())
            {
                var reference = new IdentifiedContactReference(serviceContact.IdentifierSource, serviceContact.IdentifierValue);
                var expandOptions = new ContactExpandOptions(CollectionModel.FacetKeys.EmailAddressList);
                var contact = await client.GetAsync<XConnectContact>(reference, new ContactExecutionOptions(expandOptions));

                if (contact == null)
                {
                    var newContact = new XConnectContact(new ContactIdentifier(reference.Source, reference.Identifier, ContactIdentifierType.Known));
                    SetEmail(newContact, serviceContact, client);
                    client.AddContact(newContact);
                    await client.SubmitAsync();
                }
                else if (contact.Emails()?.PreferredEmail.SmtpAddress != serviceContact.Email)
                {
                    SetEmail(contact, serviceContact, client);
                    await client.SubmitAsync();
                }
            }
        }

        private static void SetEmail(XConnectContact contact, IXDbContactWithEmail xDbContact, IXdbContext client)
        {
            if (string.IsNullOrEmpty(xDbContact.Email))
            {
                return;
            }
            var emailFacet = contact.Emails();
            if (emailFacet == null)
            {
                emailFacet = new EmailAddressList(new EmailAddress(xDbContact.Email, false), "Preferred");
            }
            else
            {
                if (emailFacet.PreferredEmail?.SmtpAddress == xDbContact.Email)
                {
                    return;
                }
                emailFacet.PreferredEmail = new EmailAddress(xDbContact.Email, false);
            }
            client.SetEmails(contact, emailFacet);
        }
    }
}

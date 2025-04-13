using Feature.FormsExtensions.SubmitActions.SendEmail.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;
using Sitecore.EmailCampaign.Cd.Actions;
using Sitecore.EmailCampaign.Cd.Services;
using Sitecore.EmailCampaign.Model.Messaging;
using Sitecore.EmailCampaign.Model.Messaging.Buses;
using Sitecore.ExM.Framework.Diagnostics;
using Sitecore.ExperienceForms.Models;
using Sitecore.ExperienceForms.Processing;
using Sitecore.ExperienceForms.Processing.Actions;
using Sitecore.Framework.Messaging;
using Sitecore.XConnect;
using System;
using System.Collections.Generic;

namespace Feature.FormsExtensions.SubmitActions.SendEmail
{
    public abstract class SendEmailBase<T> : SubmitActionBase<T> where T : SendEmailData
    {
        private readonly IClientApiService _clientApiService;
        private readonly ILogger _logger;
        private readonly IMailTokenBuilder _mailTokenBuilder;

        protected SendEmailBase(ISubmitActionData submitActionData, ILogger logger, IClientApiService clientApiService, IMailTokenBuilder mailTokenBuilder) 
            : base(submitActionData)
        {
            _logger = logger;
            _clientApiService = clientApiService;
            _mailTokenBuilder = mailTokenBuilder;
        }

        protected override bool Execute(T data, FormSubmitContext formSubmitContext)
        {
            if (data.MessageId == Guid.Empty)
            {
                _logger.LogWarn("Empty message id");
                return false;
            }

            var toContacts = GetToContacts(data, formSubmitContext);
            if (toContacts == null || toContacts.Count == 0)
            {
                return false;
            }

#if !DEBUG
            try
            {
#endif
                var customTokens = BuildCustomTokens(data, formSubmitContext);
                foreach (var to in toContacts)
                {
                    SendMail(to, customTokens, data.MessageId);
                }
#if !DEBUG
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
                return false;
            }
#endif
            return true;
        }

        protected virtual void SendMail(ContactIdentifier toContact, Dictionary<string, object> customTokens, Guid messageId)
        {
            var automatedMessage = new AutomatedMessage
            {
                ContactIdentifier = toContact,
                MessageId = messageId,
                CustomTokens = customTokens,
                TargetLanguage = Sitecore.Context.Language.Name
            };
            SendAutomatedMessage(automatedMessage);
        }

        private void SendAutomatedMessage(AutomatedMessage automatedMessage)
        {
            if (_clientApiService != null)
            {
                _clientApiService.SendAutomatedMessage(automatedMessage);
            }
            else
            {
                var automatedMessageBus = ServiceLocator.ServiceProvider.GetService<IMessageBus<AutomatedMessagesBus>>();
                automatedMessageBus.Send(automatedMessage);
            }
        }

        protected virtual Dictionary<string, object> BuildCustomTokens(T data, FormSubmitContext formSubmitContext)
        {
            return _mailTokenBuilder.BuildTokens(data.FieldsTokens, formSubmitContext);
        }

        protected abstract IList<ContactIdentifier> GetToContacts(T data, FormSubmitContext formSubmitContext);
    }
}

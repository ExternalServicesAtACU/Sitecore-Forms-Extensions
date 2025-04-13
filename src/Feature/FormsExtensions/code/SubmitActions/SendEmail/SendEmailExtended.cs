using Feature.FormsExtensions.SubmitActions.SendEmail.FileAttachment;
using Feature.FormsExtensions.SubmitActions.SendEmail.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;
using Sitecore.EmailCampaign.Cd.Services;
using Sitecore.ExM.Framework.Diagnostics;
using Sitecore.ExperienceForms.Models;
using Sitecore.ExperienceForms.Processing;
using Sitecore.Mvc.Extensions;
using Sitecore.XConnect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Feature.FormsExtensions.SubmitActions.SendEmail
{
    public class SendEmailExtended : SendEmailBase<SendEmailExtendedData>
    {
        private readonly FixedAddressContactIdentifierHandler _fixedAddressHandler;
        private readonly FieldValueContactIdentifierHandler _fieldValueContactIdentifierHandler;
        private readonly CurrentContactContactIdentifierHandler _currentContactContactIdentifierHandler;
        private readonly FileAttachmentTokenBuilder _fileAttachmentTokenBuilder;
        private readonly IMailTokenBuilder _mailTokenBuilder;

        public SendEmailExtended(ISubmitActionData submitActionData)
            : this(
                  submitActionData,
                  ServiceLocator.ServiceProvider.GetService<ILogger>(),
                  ServiceLocator.ServiceProvider.GetService<IClientApiService>(),
                  ServiceLocator.ServiceProvider.GetService<IMailTokenBuilder>(),
                  ServiceLocator.ServiceProvider.GetService<FixedAddressContactIdentifierHandler>(),
                  ServiceLocator.ServiceProvider.GetService<FieldValueContactIdentifierHandler>(),
                  ServiceLocator.ServiceProvider.GetService<CurrentContactContactIdentifierHandler>(),
                  ServiceLocator.ServiceProvider.GetService<FileAttachmentTokenBuilder>())
        { }

        public SendEmailExtended(ISubmitActionData submitActionData,
            ILogger logger,
            IClientApiService clientApiService,
            IMailTokenBuilder mailTokenBuilder,
            FixedAddressContactIdentifierHandler fixedAddressHandler,
            FieldValueContactIdentifierHandler fieldValueContactIdentifierHandler,
            CurrentContactContactIdentifierHandler currentContactContactIdentifierHandler,
            FileAttachmentTokenBuilder fileAttachmentTokenBuilder)
                : base(submitActionData, logger, clientApiService, mailTokenBuilder)
        {
            _fixedAddressHandler = fixedAddressHandler;
            _fieldValueContactIdentifierHandler = fieldValueContactIdentifierHandler;
            _currentContactContactIdentifierHandler = currentContactContactIdentifierHandler;
            _fileAttachmentTokenBuilder = fileAttachmentTokenBuilder;
            _mailTokenBuilder = mailTokenBuilder;
        }

        protected override IList<ContactIdentifier> GetToContacts(SendEmailExtendedData data, FormSubmitContext formSubmitContext)
        {
            var handler = GetExtractSendToContactIdentierHandler(data.Type);
            return handler.GetContacts(data, formSubmitContext);
        }

        public IExtractSendToContactIdentifierHandler GetExtractSendToContactIdentierHandler(string name)
        {
            switch (name)
            {
                case "fixedAddress": return _fixedAddressHandler;
                case "currentContact": return _currentContactContactIdentifierHandler;
                case "fieldValue": return _fieldValueContactIdentifierHandler;
            }
            throw new Exception($"Unknown sendToType: {name}");
        }

        protected override Dictionary<string, object> BuildCustomTokens(SendEmailExtendedData data, FormSubmitContext formSubmitContext)
        {
            var tokens = base.BuildCustomTokens(data, formSubmitContext);
            var attachmentTokens = _fileAttachmentTokenBuilder.BuildFileAttachmentTokens(data, formSubmitContext);
            tokens.AddRange(attachmentTokens);
            if (data.GenerateAllFieldsToken)
            {
                var formFields = _mailTokenBuilder.BuildAllTokens(formSubmitContext);
                tokens.Add(Sitecore.Configuration.Settings.GetSetting("FormExtensions.AllFormFieldsTokenName", "AllFormFields"), ConvertTokensToSingleString(formFields));
            }
            return tokens;
        }

        protected string ConvertTokensToSingleString(Dictionary<string, object> tokens)
        {
            return tokens.Aggregate("", (current, value) => current + Environment.NewLine + value.Key + " : " + value.Value);
        }
    }
}

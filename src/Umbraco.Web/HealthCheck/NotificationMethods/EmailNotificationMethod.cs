﻿using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Configuration.HealthChecks;
using Umbraco.Core.Services;

namespace Umbraco.Web.HealthCheck.NotificationMethods
{
    [HealthCheckNotificationMethod("email")]
    public class EmailNotificationMethod : NotificationMethodBase, IHealthCheckNotificatationMethod
    {
        private readonly ILocalizedTextService _textService;

        /// <summary>
        /// Default constructor which is used in the provider model
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="failureOnly"></param>
        /// <param name="verbosity"></param>
        /// <param name="recipientEmail"></param>
        public EmailNotificationMethod(bool enabled, bool failureOnly, HealthCheckNotificationVerbosity verbosity,
                string recipientEmail)
            : this(enabled, failureOnly, verbosity, recipientEmail, ApplicationContext.Current.Services.TextService)
        {            
        }

        /// <summary>
        /// Constructor that could be used for testing
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="failureOnly"></param>
        /// <param name="verbosity"></param>
        /// <param name="recipientEmail"></param>
        /// <param name="textService"></param>
        internal EmailNotificationMethod(bool enabled, bool failureOnly, HealthCheckNotificationVerbosity verbosity,
            string recipientEmail, ILocalizedTextService textService)
            : base(enabled, failureOnly, verbosity)
        {
            if (textService == null) throw new ArgumentNullException("textService");
            if (string.IsNullOrWhiteSpace(recipientEmail)) throw new ArgumentException("Value cannot be null or whitespace.", "recipientEmail");
            _textService = textService;
            RecipientEmail = recipientEmail;
            Verbosity = verbosity;
        }

        public string RecipientEmail { get; private set; }        

        public async Task SendAsync(HealthCheckResults results)
        {
            if (ShouldSend(results) == false)
            {
                return;
            }

            if (string.IsNullOrEmpty(RecipientEmail))
            {
                return;
            }

            var message = _textService.Localize("healthcheck/scheduledHealthCheckEmailBody", new[]
            {
                DateTime.Now.ToShortDateString(),
                DateTime.Now.ToShortTimeString(),
                results.ResultsAsHtml(Verbosity)
            });

            var subject = _textService.Localize("healthcheck/scheduledHealthCheckEmailSubject");

            using (var client = new SmtpClient())
            using (var mailMessage = new MailMessage(UmbracoConfig.For.UmbracoSettings().Content.NotificationEmailAddress,
                RecipientEmail,
                string.IsNullOrEmpty(subject) ? "Umbraco Health Check Status" : subject,
                message)
            {
                IsBodyHtml = message.IsNullOrWhiteSpace() == false
                             && message.Contains("<") && message.Contains("</")
            })
            {
                if (client.DeliveryMethod == SmtpDeliveryMethod.Network)
                {
                    await client.SendMailAsync(mailMessage);
                }
                else
                {
                    client.Send(mailMessage);
                }
            }
        }
    }
}

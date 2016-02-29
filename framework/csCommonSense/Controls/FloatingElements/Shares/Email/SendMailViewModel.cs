#region

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using Caliburn.Micro;
using csShared.Documents;
using csShared.FloatingElements.Shares.Email;
using csShared.Utils;
using EndPoint = csShared.FloatingElements.Classes.EndPoint;

#endregion

namespace csShared
{
    public class SendMailViewModel : Screen
    {
        private Document doc;
        private string comment;
        private string to;
        private string subject;
        private EndPoint endPoint;

        [ImportingConstructor]
        public SendMailViewModel() {
            To = AppState.Config.Get(@"EmailShare.ToAddress", "tno.presenter@tno.nl");
            Subject = AppState.Config.Get(@"EmailShare.Subject", "Share");
        }

        public Document Doc {
            get { return doc; }
            set {
                doc = value;
                NotifyOfPropertyChange(() => Doc);
            }
        }

        public AppStateSettings AppState {
            get { return AppStateSettings.Instance; }
        }


        public EndPoint EndPoint {
            get { return endPoint; }
            set {
                endPoint = value;
                if (endPoint != null)
                    Comment = GenerateBody();
            }
        }

        public string To {
            get { return to; }
            set {
                to = value;
                NotifyOfPropertyChange(() => To);
            }
        }

        public string Subject {
            get { return subject; }
            set {
                subject = value;
                NotifyOfPropertyChange(() => Subject);
            }
        }

        public string Comment {
            get { return comment; }
            set {
                comment = value;
                NotifyOfPropertyChange(() => Comment);
            }
        }

        public bool IsAttachment {
            get {
                return EndPoint != null && string.Equals(EndPoint.ContractType, "document", StringComparison.CurrentCultureIgnoreCase);
            }
        }

        public void Send() {
            if (EndPoint == null) return;

            AppState.Config.Set(@"EmailShare.ToAddress", To);
            ThreadPool.QueueUserWorkItem(delegate {
                try {
                    var fromAddress =
                        new MailAddress(AppState.Config.Get(@"EmailShare.FromAddress", "tno.presenter@gmail.com"),
                                        AppState.Config.Get(@"EmailShare.FromName", "TnoPresenter"));
                    var toAddress = new MailAddress(To, To);
                    var fromPassword = AppState.Config.Get(@"EmailShare.Password", "na");
                    //var subject = AppState.Config.Get(@"EmailShare.Subject", "Share");
                    var body = comment;

                    var smtp = new SmtpClient {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                    };
                    using (var message = new MailMessage(fromAddress, toAddress) { Subject = subject, Body = body }) {
                        switch (EndPoint.ContractType)
                        {
                            case "link":
                                break;
                            case "document":
                                var fileName = endPoint.Value.ToString();
                                message.Attachments.Add(new Attachment(fileName, MimeTypeHelper.GetMimeType(Path.GetExtension(fileName))));
                                break;
                        }

                        smtp.Send(message);
                    }

                    AppState.TriggerNotification("Sending email completed");
                }
                catch (Exception e) {
                    Logger.Log("EmailShareContract", "Error sending email", e.Message, Logger.Level.Error);
                    AppState.TriggerNotification("Error sending email");
                }
            });
            if (Element != null) Element.UnFlip();
        }

        private string GenerateBody() {
            var body = new StringBuilder();
            body.AppendLine(AppState.Config.Get(@"EmailShare.BodyHeader", ""));
            body.AppendLine();
            switch (EndPoint.ContractType) {
                case "link":
                    body.AppendLine(EndPoint.Value + Environment.NewLine);
                    break;
                case "document":
                    body.AppendLine("Document is attached.");
                    body.AppendLine();
                    break;
            }
            body.AppendLine(AppState.Config.Get("EmailShare.BodyFooter", ""));
            return body.ToString();
        }

        public FloatingElement Element { get; set; }
    }
}
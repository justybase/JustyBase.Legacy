using System.Net.Mail;
using System.Net.Mime;

namespace AppBase.Services.Utilities;

public sealed class SendMail : ISendMail
{
    public string Server { get; set; }
    public string MailFrom { get; set; }
    public string MailTo { get; set; }
    public string MailCC { get; set; }
    public string Subject { get; set; }
    public string[] Attachments { get; set; }

    public string MessageBody;

    public void Send()
    {
        SmtpClient client = new SmtpClient(Server);
        // Credentials are necessary if the server requires the client
        // to authenticate before it will send email on the client's behalf.
        client.UseDefaultCredentials = true;
        try
        {
            MailMessage mailWithImg = BuildMailMessage();
            client.Send(mailWithImg);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            client.Dispose();
        }
    }

    public MailMessage BuildMailMessage()
    {
        MailMessage mail = new MailMessage();
        mail.IsBodyHtml = true;

        var (htmlBody, imagePaths) = SendMailHtmlHelper.BuildHtmlBody(MessageBody);
        AlternateView alternateView = AlternateView.CreateAlternateViewFromString(htmlBody, null, MediaTypeNames.Text.Html);
        foreach (var imagePath in imagePaths)
        {
            var res = new LinkedResource(imagePath);
            res.ContentId = Guid.NewGuid().ToString();
            alternateView.LinkedResources.Add(res);
        }
        mail.AlternateViews.Add(alternateView);

        if (Attachments is not null && Attachments.Length > 0)
        {
            foreach (var item in Attachments)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    mail.Attachments.Add(new Attachment(item));
                }
            }
        }

        mail.From = new MailAddress(MailFrom);
        mail.To.Add(MailTo);
        if (!string.IsNullOrWhiteSpace(MailCC) && MailCC.Contains('@'))
        {
            mail.CC.Add(MailCC);
        }

        mail.Subject = Subject;
        return mail;
    }
}

public static class SendMailHtmlHelper
{
    private static readonly System.Text.RegularExpressions.Regex ImageRegex =
        new(@"\#IMAGE\#\#\[(?<imagePath>[^\]\[]*)\]", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// Parses #IMAGE##[path] markers in the message body and builds HTML with embedded image references.
    /// Returns the HTML body and the list of extracted image file paths.
    /// </summary>
    public static (string Html, string[] ImagePaths) BuildHtmlBody(string? messageBody)
    {
        if (string.IsNullOrEmpty(messageBody))
        {
            return (string.Empty, []);
        }

        var matches = ImageRegex.Matches(messageBody);

        List<string> imagePaths = new List<string>();
        List<string> contentIds = new List<string>();

        if (matches.Count > 0)
        {
            int prevIndex = 0;
            var sb = new System.Text.StringBuilder();
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                var imagePath = m.Groups["imagePath"].Value;
                var contentId = Guid.NewGuid().ToString();

                sb.Append(messageBody[prevIndex..m.Index]);
                sb.Append(@"<img src='cid:");
                sb.Append(contentId);
                sb.Append(@"'/>");

                prevIndex = m.Index + m.Length;
                imagePaths.Add(imagePath);
                contentIds.Add(contentId);
            }
            sb.Append(messageBody[prevIndex..]);

            return (sb.ToString(), imagePaths.ToArray());
        }

        return (messageBody, []);
    }
}

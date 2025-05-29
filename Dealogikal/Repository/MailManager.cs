using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using Dealogikal.Utils;

namespace Dealogikal.Repository
{
    public class MailManager
    {
        private string MailSender { get; set; }

        private string MailAppPassword { get; set; }

        public MailManager()
        {
            MailSender = ConfigurationManager.AppSettings["MailSender"];
            MailAppPassword = ConfigurationManager.AppSettings["MailSenderAppPassword"];
        }

        public bool SendEmail(string szRecipient, string subject, string szMsgBody, ref string errResponse)
        {
            try
            {
                using (var message = new MailMessage())
                {
                    var smtp = new SmtpClient();
                    message.From = new MailAddress(MailSender);
                    message.To.Add(new MailAddress(szRecipient));
                    message.Subject = subject;
                    message.IsBodyHtml = true; 
                    message.Body = szMsgBody;

                    smtp.Port = 587;
                    smtp.Host = "smtp.gmail.com"; 
                    smtp.EnableSsl = true;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(MailSender, MailAppPassword);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                    smtp.Send(message);

                    errResponse = "Message Sent";

                    return true;
                }
            }
            catch (Exception ex)
            {
                errResponse = ex.Message;

                return false;
            }
        }

        public ErrorCode LeaveApprovalEmail(string email, string firstName, string leaveRequestType, DateTime leaveStart, DateTime leaveEnd, int status, ref string errMsg, string corporation = "Dealogikal")
        {
            try
            {
                string errorMessage = "";
                var mailManager = new MailManager();
                string subject = "Your Leave Request Status";

                string logoUrl = corporation.ToLower() == "kpec"
                   ? "http://knotticalpower.com/wp-content/uploads/2023/03/kpec-logo.png"
                   : "https://www.dealogikal.com/images/dealogikal_dark.png";
                // Convert status int to label and color
                string statusText = status == 1 ? "Approved" : "Rejected";
                string statusColor = status == 1 ? "#28a745" : "#dc3545";

                string body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                      <meta charset='UTF-8'>
                      <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                      <title>Leave Request Update</title>
                    </head>
                    <body style='margin: 0; padding: 0; background-color: #f4f4f4; font-family: Arial, sans-serif;'>
                      <table align='center' cellpadding='0' cellspacing='0' width='100%' style='padding: 20px 0;'>
                        <tr>
                          <td align='center'>
                            <table cellpadding='0' cellspacing='0' width='600' style='background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
                              <tr>
                                <td align='center' style='background-color: #cfd0d1; padding: 30px;'>
                                    <img src='{logoUrl}' alt='{corporation} Logo' width='180' style='display: block;' />
                                </td>
                              </tr>

                              <tr>
                                <td style='padding: 40px 30px 20px 30px;'>
                                  <h2 style='color: #333333; text-align: center; margin-bottom: 10px;'>Leave Request Status</h2>
                                  <p style='color: #555555; font-size: 16px; text-align: center;'>Hello {firstName},</p>
                                  <p style='color: #666666; font-size: 16px; text-align: center;'>
                                    Your leave request has been <strong style='color: {statusColor};'>{statusText}</strong>.
                                  </p>
                                </td>
                              </tr>

                              <tr>
                                <td style='padding: 20px 30px;'>
                                  <table cellpadding='10' cellspacing='0' width='100%' style='border: 1px solid #ddd; border-radius: 6px; background-color: #f9f9f9;'>
                                    <tr>
                                      <td style='font-size: 16px; color: #555;'>Leave Type:</td>
                                      <td style='font-size: 16px; color: #333;'><strong>{leaveRequestType}</strong></td>
                                    </tr>
                                    <tr>
                                      <td style='font-size: 16px; color: #555;'>Start Date:</td>
                                      <td style='font-size: 16px; color: #333;'><strong>{leaveStart:MMMM dd, yyyy}</strong></td>
                                    </tr>
                                    <tr>
                                      <td style='font-size: 16px; color: #555;'>End Date:</td>
                                      <td style='font-size: 16px; color: #333;'><strong>{leaveEnd:MMMM dd, yyyy}</strong></td>
                                    </tr>
                                  </table>
                                </td>
                              </tr>

                              <tr>
                                <td style='padding: 20px 30px;'>
                                  <p style='color: #888888; font-size: 14px; line-height: 1.6;'>
                                    If you have any questions or need further clarification, feel free to contact your Department Head Or The HR Department.
                                  </p>
                                  <p style='color: #888888; font-size: 14px;'>Thank you,<br /><strong>The {corporation} Department Head</strong></p>
                                </td>
                              </tr>

                              <tr>
                                <td align='center' style='background-color: #cfd0d1; padding: 15px; font-size: 12px; color: #333333;'>
                                  &copy; {DateTime.Now.Year} Dealogikal. All rights reserved.
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                      </table>
                    </body>
                    </html>";

                bool isSent = SendEmail(email, subject, body, ref errorMessage);
                if (isSent)
                {
                    errMsg = "Email sent successfully.";
                    return ErrorCode.Success;
                }
                else
                {
                    errMsg = errorMessage;
                    return ErrorCode.Error;
                }
            }
            catch (Exception ex)
            {
                errMsg = $"An error occurred: {ex.Message}";
                return ErrorCode.Error;
            }
        }
        public ErrorCode DHLeaveEmail(string firstName, string lastName, string leaveRequestType, DateTime leaveStart, DateTime leaveEnd, int status, ref string errMsg, string corporation = "Dealogikal")
        {
            try
            {
                var email = "isk@dealogikal.com";
                string errorMessage = "";
                var mailManager = new MailManager();
                string subject = "Leave Request From " + firstName;
                string leavetype = leaveRequestType.ToLower() == "leavewithoutpay"
                    ? "Leave Without Pay"
                    : "Leave With Pay";
                string logoUrl = corporation.ToLower() == "dealogikal"
                   ?"https://www.dealogikal.com/images/dealogikal_dark.png" 
                   :"http://knotticalpower.com/wp-content/uploads/2023/03/kpec-logo.png";
                // Convert status int to label and color
                string body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                      <meta charset='UTF-8'>
                      <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                      <title>Leave Request Update</title>
                    </head>
                    <body style='margin: 0; padding: 0; background-color: #f4f4f4; font-family: Arial, sans-serif;'>
                      <table align='center' cellpadding='0' cellspacing='0' width='100%' style='padding: 20px 0;'>
                        <tr>
                          <td align='center'>
                            <table cellpadding='0' cellspacing='0' width='600' style='background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
                              <tr>
                                <td align='center' style='background-color: #cfd0d1; padding: 30px;'>
                                    <img src='{logoUrl}' alt='{corporation} Logo' width='180' style='display: block;' />
                                </td>
                              </tr>

                              <tr>
                                <td style='padding: 40px 30px 20px 30px;'>
                                  <h2 style='color: #333333; text-align: center; margin-bottom: 10px;'>Leave Request</h2>
                                  <p style='color: #555555; font-size: 16px; text-align: center;'>Good Day <strong>Sir Ira</strong></p>
                                  <p style='color: #666666; font-size: 16px; text-align: center;'>
                                   <strong>{firstName} {lastName}</strong> submitted a leave request
                                  </p>
                                </td>
                              </tr>

                              <tr>
                                <td style='padding: 20px 30px;'>
                                  <table cellpadding='10' cellspacing='0' width='100%' style='border: 1px solid #ddd; border-radius: 6px; background-color: #f9f9f9;'>
                                    <tr>
                                      <td style='font-size: 16px; color: #555;'>Leave Type:</td>
                                      <td style='font-size: 16px; color: #333;'><strong>{leavetype}</strong></td>
                                    </tr>
                                    <tr>
                                      <td style='font-size: 16px; color: #555;'>Start Date:</td>
                                      <td style='font-size: 16px; color: #333;'><strong>{leaveStart:MMMM dd, yyyy}</strong></td>
                                    </tr>
                                    <tr>
                                      <td style='font-size: 16px; color: #555;'>End Date:</td>
                                      <td style='font-size: 16px; color: #333;'><strong>{leaveEnd:MMMM dd, yyyy}</strong></td>
                                    </tr>
                                  </table>
                                </td>
                              </tr>

                              <tr>
                                <td style='padding: 20px 30px;'>
                                  <p style='color: #888888; font-size: 14px; line-height: 1.6;'>
                                    If you have any questions or need further clarification, feel free to contact the IT Department or HR Department.
                                  </p>
                                  <p style='color: #888888; font-size: 14px;'>Thank you,<br /><strong>The {corporation} Department Head</strong></p>
                                </td>
                              </tr>

                              <tr>
                                <td align='center' style='background-color: #cfd0d1; padding: 15px; font-size: 12px; color: #333333;'>
                                  &copy; 2023 Dealogikal. All rights reserved.
                                </td>
                              </tr>
                            </table>
                          </td>
                        </tr>
                      </table>
                    </body>
                    </html>";

                bool isSent = SendEmail(email, subject, body, ref errorMessage);
                if (isSent)
                {
                    errMsg = "Email sent successfully.";
                    return ErrorCode.Success;
                }
                else
                {
                    errMsg = errorMessage;
                    return ErrorCode.Error;
                }
            }
            catch (Exception ex)
            {
                errMsg = $"An error occurred: {ex.Message}";
                return ErrorCode.Error;
            }
        }
    }
}
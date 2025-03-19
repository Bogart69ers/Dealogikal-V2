using System;
using System.Collections.Generic;
using System.Linq;
using Dealogikal.Utils;
using Dealogikal.Database;

namespace Dealogikal.Repository
{
    public class NotificationManager
    {
        private readonly BaseRepository<notification> _notif;

        public NotificationManager()
        {
            _notif = new BaseRepository<notification>();
        }

        public notification GetNotificationById(int id)
        {
            return _notif.Get(id);
        }

        public List<notification> GetNotificationByemployeeId(string employeeId)
        {
            return _notif._table.Where(m => m.employeeId == employeeId).OrderByDescending(m => m.createdAt).ToList();
        }

        public ErrorCode MarkAllAsRead(string employeeId, ref string errMsg)
        {
            try
            {
                var notifications = _notif._table
                    .Where(n => n.employeeId == employeeId && n.isRead == false);

                if (!notifications.Any())
                {
                    errMsg = "No unread notifications found.";
                    return ErrorCode.Success;
                }

                foreach (var notif in notifications)
                {
                    notif.isRead = true;
                }

                return _notif.Save(out errMsg); // ✅ Make sure your repository Save method matches this!
            }
            catch (Exception ex)
            {
                errMsg = $"An error occurred: {ex.Message}";
                return ErrorCode.Error;
            }
        }




        public ErrorCode MarkAsRead(int notificationId, ref string errMsg)
        {
            try
            {
                var notification = _notif.Get(notificationId);
                if (notification == null)
                {
                    errMsg = "Notification not found";
                    return ErrorCode.Error;
                }

                notification.isRead = true;

                return _notif.Update(notificationId, notification, out errMsg);
            }
            catch (Exception ex)
            {
                errMsg = $"An error occurred: {ex.Message}";
                return ErrorCode.Error; ;
            }
        }


        public ErrorCode CreateNotification(string employeeId, string senderId, string title, string message, ref string errorMessage)
        {
            try
            {
                var notification = new notification
                {
                    employeeId = employeeId,
                    senderId = senderId,
                    title = title,
                    message = message,
                    isRead = false,
                    createdAt = DateTime.Now
                };

                return _notif.Create(notification, out errorMessage);
            }
            catch (Exception ex)
            {
                errorMessage = $"An error occurred: {ex.Message}";
                return ErrorCode.Error;
            }
        }

        public ErrorCode UpdateNotification(notification nt, out string errMsg)
        {
            // You pass in the entity's ID, etc.
            return _notif.Update(nt.id, nt, out errMsg);
        }
    }
}

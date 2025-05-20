using System;
using System.Linq;
using System.Web.Mvc;
using Dealogikal.Database;
using Dealogikal.Repository;
using Dealogikal.ViewModel;

namespace Dealogikal.Controllers
{
    public class BaseController : Controller
    {
        // GET: Base
        public String ErrorMessage;
        public BaseRepository<userAccount> _userAcc;
        public AccountManager _AccManager;
        public DtrManager _DtrManager;
        public RequestManager _RequestManager;
        public ImageManager _ImgManager;
        public FeedbackManager _FeedbackManager;
        public NotificationManager _NotifManager;
        public MailManager _MailManager;
        public BaseController()
        {
            _userAcc = new BaseRepository<userAccount>();
            _AccManager = new AccountManager();
            _DtrManager = new DtrManager();
            _RequestManager = new RequestManager();
            _ImgManager = new ImageManager();
            _FeedbackManager = new FeedbackManager();
            _NotifManager = new NotificationManager();
            _MailManager = new MailManager();
        }

        private string GetAvatarUrl(string employeeId)
        {
            // 1) If userId = 0 or invalid, return a default image or handle as needed
            if (employeeId == null)
                return Url.Content("~/Assets/img/profile.jpg");

            // 2) Use your image manager to get user’s image
            var userImage = _ImgManager.ListImageByEmployeeId(employeeId).FirstOrDefault();
            if (userImage == null || string.IsNullOrEmpty(userImage.imageFile))
            {
                // fallback to default
                return Url.Content("~/Assets/img/profile.jpg");
            }
            // 3) Otherwise return the actual path
            return Url.Content("~/UploadedFiles/" + userImage.imageFile);
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            var userAgent = filterContext.HttpContext.Request.UserAgent?.ToLower() ?? "";


            if (User.Identity.IsAuthenticated)
            {
                var userAccount = _AccManager.GetUserByEmployeeId(User.Identity.Name);
                if (userAccount != null)
                {
                    var employeeInfo = _AccManager.CreateOrRetrieve(userAccount.employeeId, ref ErrorMessage);
                    var notifications = _NotifManager.GetNotificationByemployeeId(userAccount.employeeId);

                    var notificationViewModels = notifications
                        .Select(n => new AccountViewModel
                        {
                            notif = n,
                            AvatarUrl = GetAvatarUrl((string)n.senderId)
                        })
                        .ToList();
                    // Create the AccountViewModel instance
                    var accountViewModel = new AccountViewModel
                    {
                        userAccount = userAccount,
                        employeeInfo = employeeInfo
                    };

                    // Pass the model data to all views using ViewBag
                    ViewBag.AccountViewModel = accountViewModel;
                    // Optionally, you can pass the employee info directly if your layout references it:
                    ViewBag.EmployeeInfo = employeeInfo;

                    ViewBag.Notifications = notificationViewModels;

                    var profileImage = _ImgManager.ListImageByEmployeeId(employeeInfo.employeeId).FirstOrDefault();
                    ViewBag.ProfilePicture = profileImage != null ? profileImage.imageFile : null;

                }
            }
        }
    }
}
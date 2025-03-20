using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Dealogikal.Database;
using Dealogikal.Repository;
using Dealogikal.Utils;
using Dealogikal.ViewModel;


namespace Dealogikal.Controllers
{
    [Authorize(Roles = "Employee")]
    public class HomeController : BaseController
    {
        [AllowAnonymous]
        public ActionResult Index()
        {
            var user = _AccManager.GetUserByEmployeeId(User.Identity.Name);

            if (user == null)
            {
                return RedirectToAction("Login", "Home"); // Redirect to login if no user is found
            }

            switch (user.role1.roleName)
            {
                case Constant.Role_HR:
                    return RedirectToAction("AdminDashboard", "Admin");

                case Constant.Role_Employee:
                    return RedirectToAction("Dashboard", "Home");

                default:
                    return RedirectToAction("Login", "Home"); // Handle unexpected roles
            }
        }


        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index");
            }

            ViewBag.Error = string.Empty;
            ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Login(string employeeId, string password, string returnUrl, bool rememberMe = false)
        {
            if (_AccManager == null)
            {
                ViewBag.Error = "Account manager is not initialized";
                return View();
            }

            if (_AccManager.SignIn(employeeId, password, ref ErrorMessage) == ErrorCode.Success)
            {
                var user = _AccManager.GetUserByEmployeeId(employeeId);
                if (user == null)
                {
                    ViewBag.Error = "User not found";
                    return View();
                }

                var info = _AccManager.GetEmployeebyEmployeeId(employeeId);
                if (info == null)
                {
                    ViewBag.Error = "Employee information not found";
                    return View();
                }

                // Check if account is inactive
                if (info.status == 0)
                {
                    return RedirectToAction("InActiveAccount", "Home");
                }

                // Create Authentication Ticket
                FormsAuthenticationTicket ticket = new FormsAuthenticationTicket(
                    1,
                    employeeId,
                    DateTime.Now,
                    rememberMe ? DateTime.Now.AddDays(30) : DateTime.Now.AddMinutes(30), // Remember me logic
                    rememberMe,
                    "",
                    FormsAuthentication.FormsCookiePath
                );

                // Encrypt the ticket
                string encryptedTicket = FormsAuthentication.Encrypt(ticket);

                // Create authentication cookie
                HttpCookie authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                {
                    Expires = rememberMe ? ticket.Expiration : DateTime.MinValue, // Expire when session ends if not persistent
                    HttpOnly = true, // Prevent JavaScript access
                    Secure = Request.IsSecureConnection // Ensures HTTPS-only transmission
                };

                Response.Cookies.Add(authCookie);

                // Redirect if returnUrl is valid
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Validate User Role
                if (user.role1 == null)
                {
                    ViewBag.Error = "User role is not defined.";
                    return View();
                }

                // Redirect Based on Role
                return user.role == 1 ? RedirectToAction("AdminDashboard", "Admin") : RedirectToAction("Dashboard", "Home");
            }

            ViewBag.Error = ErrorMessage;
            return View();
        }


        [Authorize]
        public ActionResult Dashboard()
        {
            var user = _AccManager.GetEmployeebyEmployeeId(User.Identity.Name);
            var dtrRec = _DtrManager.GetRecordsByEmployeeId(user.employeeId);

            var currentDtr = _DtrManager.GetAllDtr().FirstOrDefault(r => r.employeeId == user.employeeId && r.date == DateTime.Now.Date);

            ViewBag.Name = user.firstName;

            var model = new AccountViewModel
            {
                employeeInfos = _AccManager.GetAllEmployee(),
                leaveRequests = _RequestManager.GetLeaveRequestByEmployeeId(User.Identity.Name),
                overtimeRequests = _RequestManager.GetOvertimeRequestByEmployeeId(User.Identity.Name),
                dtr = currentDtr,
                dtrRecords = _DtrManager.GetAllDtr()
            };

            return View(model);

        }

        [Authorize]
        public ActionResult Dtr()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult Dtr(dtrRecords dtr, int? recordId, string action)
        {
            var currentUser = User.Identity.Name;
            string errMsg = string.Empty;
            ErrorCode result;

            if (action == "TimeIn")
            {
                // Create a new record for the morning Time In.
                result = _DtrManager.CreateDtr(dtr, currentUser, ref errMsg);
                if (result != ErrorCode.Success)
                {
                    ViewBag.Error = "Error Creating DTR: " + errMsg;
                    return RedirectToAction("Dashboard");
                }
            }
            else if (action == "BreakIn")
            {
                // Update the current record with Break In time.
                if (recordId.HasValue)
                {
                    result = _DtrManager.UpdateBreakIn(currentUser, recordId.Value, ref errMsg);
                    if (result != ErrorCode.Success)
                    {
                        ViewBag.Error = "Error Updating Break In: " + errMsg;
                        return RedirectToAction("Dashboard");
                    }
                }
                else
                {
                    ViewBag.Error = "Record ID is missing for Break In.";
                    return RedirectToAction("Dashboard");
                }
            }
            else if (action == "BreakOut")
            {
                result = _DtrManager.UpdateBreakOut(currentUser, recordId.Value, dtr.workMode, ref errMsg);
                if (result != ErrorCode.Success)
                {
                    ViewBag.Error = "Error Updating Break Out: " + errMsg;
                    return RedirectToAction("Dashboard");
                }
            }
            else if (action == "TimeOut")
            {
                // Update the current record with Time Out.
                if (recordId.HasValue)
                {
                    result = _DtrManager.UpdateTimeOut(currentUser, recordId.Value, ref errMsg);
                    if (result != ErrorCode.Success)
                    {
                        ViewBag.Error = "Error Updating Time Out: " + errMsg;
                        return RedirectToAction("Dashboard");
                    }
                }
                else
                {
                    ViewBag.Error = "Record ID is missing for Time Out.";
                    return RedirectToAction("Dashboard");
                }
            }

            return RedirectToAction("Dashboard");
        }


        [Authorize]
        public ActionResult LeaveRequest()
        {
            var user = _AccManager.GetEmployeebyEmployeeId(User.Identity.Name);
            var currentUserId = User.Identity.Name;

            int leaveCount = user?.leaveCount ?? 0;

            // Pending leavewithpay requests
            var pendingLPRequests = _RequestManager.GetLeaveRequestByEmployeeId(currentUserId)
                .Where(r => r.status == 0 && r.leaveType == "leavewithpay")
                .ToList();

            int pendingLPDays = pendingLPRequests.Sum(r =>
            {
                if (!r.leaveStart.HasValue || !r.leaveEnd.HasValue)
                    return 0;
                return (r.leaveEnd.Value - r.leaveStart.Value).Days + 1;
            });

            int availableLPDays = leaveCount - pendingLPDays;

            ViewBag.LeaveCount = leaveCount;     
            ViewBag.AvailableLPDays = availableLPDays; 
            ViewBag.PendingLPDays = pendingLPDays;     

            var requests = _RequestManager.GetLeaveRequestByEmployeeId(currentUserId);

            var model = new AccountViewModel
            {
                leaveRequests = requests
            };

            return View(model);
        }


        [Authorize]
        [HttpPost]
        public ActionResult LeaveRequest(leaveRequest lr)
        {
            try
            {
                var user = User.Identity.Name;
                var userInfo = _AccManager.GetEmployeebyEmployeeId(user);
                string errMsg = string.Empty;

                if (userInfo == null)
                {
                    ViewBag.ErrorMessage = "User not found.";

                    var fallbackRequests = _RequestManager.GetLeaveRequestByEmployeeId(user);
                    var fallbackModel = new AccountViewModel
                    {
                        leaveRequests = fallbackRequests
                    };

                    return View("LeaveRequest", fallbackModel);
                }

                if (lr.leaveType == "leavewithpay")
                {
                    var pendingLPRequests = _RequestManager.GetLeaveRequestByEmployeeId(user)
                        .Where(r => r.status == 0 && r.leaveType == "leavewithpay")
                        .ToList();

                    int reservedDays = pendingLPRequests.Sum(r =>
                    {
                        if (!r.leaveStart.HasValue || !r.leaveEnd.HasValue)
                            return 0;
                        return (r.leaveEnd.Value - r.leaveStart.Value).Days + 1;
                    });

                    int availableLeaveDays = (userInfo.leaveCount ?? 0) - reservedDays;

                    if (!lr.leaveStart.HasValue || !lr.leaveEnd.HasValue)
                    {
                        ViewBag.ErrorMessage = "Leave Start and End dates are required.";

                        var fallbackRequests = _RequestManager.GetLeaveRequestByEmployeeId(user);
                        var fallbackModel = new AccountViewModel
                        {
                            leaveRequests = fallbackRequests
                        };

                        return View("LeaveRequest", fallbackModel);
                    }

                    int requestedDays = (lr.leaveEnd.Value - lr.leaveStart.Value).Days + 1;

                    if (requestedDays > availableLeaveDays)
                    {
                        ViewBag.ErrorMessage = $"You only have {availableLeaveDays} Leave With Pay days remaining after pending requests. Please adjust your leave dates.";

                        var fallbackRequests = _RequestManager.GetLeaveRequestByEmployeeId(user);
                        var fallbackModel = new AccountViewModel
                        {
                            leaveRequests = fallbackRequests
                        };

                        return View("LeaveRequest", fallbackModel);
                    }
                }

                if (_RequestManager.CreateLeave(lr, user, ref errMsg) != ErrorCode.Success)
                {
                    ViewBag.ErrorMessage = errMsg;

                    var fallbackRequests = _RequestManager.GetLeaveRequestByEmployeeId(user);
                    var fallbackModel = new AccountViewModel
                    {
                        leaveRequests = fallbackRequests
                    };

                    return View("LeaveRequest", fallbackModel);
                }

                var notifManager = new NotificationManager();
                var deptHead = _AccManager.GetDepartmentHeadByDepartment(userInfo.department);

                if (deptHead != null)
                {
                    notifManager.CreateNotification(
                        deptHead.employeeId,
                        userInfo.employeeId,
                        "Pending Leave Request",
                        $"{userInfo.firstName} {userInfo.lastName} has submitted a leave request.",
                        ref errMsg
                    );
                }

                return RedirectToAction("LeaveRequest");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;

                var fallbackRequests = _RequestManager.GetLeaveRequestByEmployeeId(User.Identity.Name);
                var fallbackModel = new AccountViewModel
                {
                    leaveRequests = fallbackRequests
                };

                return View("LeaveRequest", fallbackModel);
            }
        }





        [Authorize]
        public ActionResult OvertimeRequest()
        {
            var user = _AccManager.GetEmployeebyEmployeeId(User.Identity.Name);
            var currentUserId = User.Identity.Name;

            ViewBag.LeaveCount = user.leaveCount;

            var requests = _RequestManager.GetOvertimeRequestByEmployeeId(currentUserId);

            var model = new AccountViewModel
            {
                overtimeRequests = requests,

            };

            return View(model);
        }


        [Authorize]
        [HttpPost]
        public ActionResult OvertimeRequest(overtimeRequest ot)
        {
            try
            {
                var user = User.Identity.Name;
                var userInfo = _AccManager.GetEmployeebyEmployeeId(user);
                string errMsg = string.Empty;

                if (userInfo == null)
                {
                    ViewBag.ErrorMessage = "User not found.";
                    return View("OvertimeRequest");
                }

                if (_RequestManager.CreateOvertime(ot, user, ref errMsg) != ErrorCode.Success)
                {
                    ViewBag.ErrorMessage = errMsg;
                    return View("OvertimeRequest");
                }

                // 🔔 Notify Department Head
                var notifManager = new NotificationManager();
                var deptHead = _AccManager.GetDepartmentHeadByDepartment(userInfo.department);

                if (deptHead != null)
                {
                    notifManager.CreateNotification(
                        deptHead.employeeId,                    // receiver
                        userInfo.employeeId,                    // sender (the employee making the request)
                        "Pending Overtime Request",
                        $"{userInfo.firstName} {userInfo.lastName} has submitted an overtime request.",
                        ref errMsg
                    );

                }
                else
                {
                    Console.WriteLine("No department head found for department: " + userInfo.department);
                }


                return RedirectToAction("OvertimeRequest");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("OvertimeRequest");
            }
        }


        [Authorize]
        public ActionResult DTRHistory()
        {
            var currentUserId = User.Identity.Name;
            var dtrHistory = _DtrManager.GetDtrHistoryByEmployeeId(currentUserId);

            var model = new AccountViewModel
            {
                dtrRecords = dtrHistory
            };

            return View(model);
        }


        [AllowAnonymous]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();

            if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
            {
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName)
                {
                    Expires = DateTime.Now.AddDays(-1),
                    HttpOnly = true
                };
                Response.Cookies.Add(authCookie);
            }

            return RedirectToAction("Login", "Home");
        }

        [AllowAnonymous]
        public ActionResult PageNotFound()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult InActiveAccount()
        {
            return View();
        }

        [Authorize]
        public ActionResult LeaveApproval()
        {
            var currentUserId = User.Identity.Name;

            var deptHead = _AccManager.GetEmployeebyEmployeeId(currentUserId);

            var employeesInDepartment = _AccManager.GetAllEmployee()
                .Where(e => e.department == deptHead.department)
                .ToList();

            var leaveRequestsInDepartment = _RequestManager.GetAllLeaveRequestsDesc()
                .Where(lr => employeesInDepartment.Any(emp => emp.employeeId == lr.employeeId))
                .ToList();

            var model = new AccountViewModel
            {
                employeeInfos = employeesInDepartment,
                leaveRequests = leaveRequestsInDepartment
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult LeaveApproval(leaveRequest lr, string employeeId, int requestId, string action)
        {
            string errMsg = string.Empty;
            ErrorCode result;

            if (action == "Accept")
            {
                var request = _RequestManager.GetLeaveRequestByRequestId(requestId);
                if (request == null)
                {
                    ViewBag.Error = "Leave request not found.";
                    return RedirectToAction("LeaveApproval");
                }

                if (request.leaveType == "leavewithpay")
                {
                    var userInfo = _AccManager.GetEmployeebyEmployeeId(request.employeeId);

                    if (userInfo == null)
                    {
                        ViewBag.Error = "Employee not found.";
                        return RedirectToAction("LeaveApproval");
                    }

                    // Get other pending leavewithpay requests (excluding the one we're approving)
                    var otherPendingLPRequests = _RequestManager.GetLeaveRequestByEmployeeId(request.employeeId)
                        .Where(r => r.status == 0 && r.leaveType == "leavewithpay" && r.requestId != requestId)
                        .ToList();

                    // Safely calculate reserved days
                    int reservedDays = otherPendingLPRequests.Sum(r =>
                    {
                        if (!r.leaveStart.HasValue || !r.leaveEnd.HasValue)
                            return 0;

                        return (r.leaveEnd.Value - r.leaveStart.Value).Days + 1;
                    });

                    // ✅ Safely get leave count (handle nulls)
                    int leaveCount = userInfo.leaveCount ?? 0;
                    int availableLPDays = leaveCount - reservedDays;

                    // ✅ Validate leave start and end dates
                    if (!request.leaveStart.HasValue || !request.leaveEnd.HasValue)
                    {
                        ViewBag.Error = "Leave request dates are incomplete.";
                        return RedirectToAction("LeaveApproval");
                    }

                    int requestedLPDays = (request.leaveEnd.Value - request.leaveStart.Value).Days + 1;

                    // ✅ Revalidate before approving
                    if (requestedLPDays > availableLPDays)
                    {
                        ViewBag.Error = $"Cannot approve request. Employee only has {availableLPDays} Leave With Pay days remaining after other pending requests.";
                        return RedirectToAction("LeaveApproval");
                    }

                    // ✅ Deduct leave days from employee's leaveCount
                    var deductResult = _AccManager.UpdateEmployeeLeaveCount(request.employeeId, requestedLPDays, ref errMsg);
                    if (deductResult != ErrorCode.Success)
                    {
                        ViewBag.Error = "Error deducting Leave With Pay balance: " + errMsg;
                        return RedirectToAction("LeaveApproval");
                    }
                }

                // ✅ Approve the leave request
                result = _RequestManager.ApproveLeaveRequest(employeeId, requestId, ref errMsg);
                if (result != ErrorCode.Success)
                {
                    ViewBag.Error = "Error approving leave request: " + errMsg;
                    return RedirectToAction("LeaveApproval");
                }

                // ✅ Notify the employee
                _NotifManager.CreateNotification(
                    request.employeeId,
                    User.Identity.Name,
                    "Leave Request Approved",
                    "Your leave request has been approved by the Department Head.",
                    ref errMsg
                );
            }
            else if (action == "Decline")
            {
                var request = _RequestManager.GetLeaveRequestByRequestId(requestId);

                if (request == null)
                {
                    ViewBag.Error = "Leave request not found.";
                    return RedirectToAction("LeaveApproval");
                }

                result = _RequestManager.DeclineLeaveRequest(employeeId, requestId, ref errMsg);
                if (result != ErrorCode.Success)
                {
                    ViewBag.Error = "Error declining leave request: " + errMsg;
                    return RedirectToAction("LeaveApproval");
                }

                // ✅ Notify employee of decline
                _NotifManager.CreateNotification(
                    request.employeeId,
                    User.Identity.Name,
                    "Leave Request Declined",
                    "Your leave request has been declined by the Department Head.",
                    ref errMsg
                );
            }

            return RedirectToAction("LeaveApproval");
        }




        [Authorize]
        public ActionResult OvertimeApproval()
        {
            var currentUserId = User.Identity.Name;

            var deptHead = _AccManager.GetEmployeebyEmployeeId(currentUserId);

            var employeesInDepartment = _AccManager.GetAllEmployee()
                .Where(e => e.department == deptHead.department)
                .ToList();

            var OvertimeRequestsInDepartment = _RequestManager.GetAllOvertimeRequestsDesc()
                .Where(lr => employeesInDepartment.Any(emp => emp.employeeId == lr.employeeId))
                .ToList();

            var model = new AccountViewModel
            {
                employeeInfos = employeesInDepartment,
                overtimeRequests = OvertimeRequestsInDepartment
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult OvertimeApproval(overtimeRequest or, string employeeId, int requestId, string action)
        {
            string errMsg = string.Empty;
            ErrorCode result;

            var notifManager = new NotificationManager();

            if (action == "Accept")
            {
                // ✅ Approve the overtime request
                result = _RequestManager.ApproveOvertimeRequest(employeeId, requestId, ref errMsg);
                if (result != ErrorCode.Success)
                {
                    ViewBag.Error = "Error Updating Overtime Request: " + errMsg;
                    return RedirectToAction("OvertimeApproval");
                }

                // ✅ Notify Employee of Approval
                _NotifManager.CreateNotification(
                    employeeId,                            // receiver
                    User.Identity.Name,                    // sender (the department head approving)
                    "Overtime Request Approved",
                    "Your overtime request has been approved by the Department Head.",
                    ref errMsg
                );

            }
            else if (action == "Decline")
            {
                // ✅ Decline the overtime request
                result = _RequestManager.DeclineOvertimeRequest(employeeId, requestId, ref errMsg);
                if (result != ErrorCode.Success)
                {
                    ViewBag.Error = "Error Updating Overtime Request: " + errMsg;
                    return RedirectToAction("OvertimeApproval");
                }

                // ✅ Notify Employee of Decline
                _NotifManager.CreateNotification(
                    employeeId,                            // receiver
                    User.Identity.Name,                    // sender (the department head declining)
                    "Overtime Request Declined",
                    "Your overtime request has been declined by the Department Head.",
                    ref errMsg
                );

            }

            TempData["SuccessMessage"] = "Overtime request updated successfully!";
            return RedirectToAction("OvertimeApproval");
        }


        [Authorize]
        public ActionResult MyProfile()
        {
            var currentUser = User.Identity.Name;
            var employee = _AccManager.GetEmployeebyEmployeeId(currentUser);
            var user = _AccManager.GetUserByEmployeeId(currentUser);

            var model = new AccountViewModel
            {
                employeeInfo = employee,
                userAccount = user
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult MyProfile(string phone, string email, string address, string barangay, string city, HttpPostedFileBase profilePicture)
        {
            if (ModelState.IsValid)
            {
                var currentUser = User.Identity.Name;

                // Retrieve the existing employee and user records
                var image = _ImgManager.GetImagebyEmployeeId(currentUser);
                var employee = _AccManager.GetEmployeebyEmployeeId(currentUser);
                var user = _AccManager.GetUserByEmployeeId(currentUser);

                if (employee == null || user == null)
                {
                    ModelState.AddModelError(String.Empty, "User not found.");
                    return View();
                }

                // Profile Picture Upload Handling
                if (profilePicture != null && profilePicture.ContentLength > 0)
                {
                    var uploadsFolderPath = Server.MapPath("~/UploadedFiles/");
                    if (!Directory.Exists(uploadsFolderPath))
                        Directory.CreateDirectory(uploadsFolderPath);

                    string fileExtension = Path.GetExtension(profilePicture.FileName);
                    string profileFileName = $"{employee.employeeId}_{DateTime.Now.ToString("yyyyMMdd")}{fileExtension}";
                    string profileSavePath = Path.Combine(uploadsFolderPath, profileFileName);

                    if (image != null && !string.IsNullOrEmpty(image.imageFile))
                    {
                        var oldImagePath = Path.Combine(uploadsFolderPath, image.imageFile);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath); // **Deletes old profile picture**
                        }
                    }
                    profilePicture.SaveAs(profileSavePath);

                    if (image != null)
                    {
                        image.imageFile = profileFileName;
                        if (_ImgManager.UpdateImg(image, ref ErrorMessage) == ErrorCode.Error)
                        {
                            ModelState.AddModelError(String.Empty, ErrorMessage);
                            return View();
                        }
                    }
                    else
                    {
                        images img = new images
                        {
                            imageFile = profileFileName,
                            employeeId = employee.employeeId
                        };

                        if (_ImgManager.CreateImg(img, ref ErrorMessage) == ErrorCode.Error)
                        {
                            ModelState.AddModelError(String.Empty, ErrorMessage);
                            return View();
                        }
                    }
                }

                // Update Employee Information ONLY IF new values are provided
                employee.phone = !string.IsNullOrEmpty(phone) ? phone : employee.phone;
                employee.email = !string.IsNullOrEmpty(email) ? email : employee.email;
                employee.address = !string.IsNullOrEmpty(address) ? address : employee.address;
                employee.barangay = !string.IsNullOrEmpty(barangay) ? barangay : employee.barangay;
                employee.city = !string.IsNullOrEmpty(city) ? city : employee.city;

                if (_AccManager.UpdateEmployeeInformation(employee, ref ErrorMessage) == ErrorCode.Error)
                {
                    ModelState.AddModelError(String.Empty, ErrorMessage);
                    return View();
                }

                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction("MyProfile");
            }

            return View();
        }

        [Authorize]
        [HttpPost]
        public JsonResult CreateFeedback(feedback fb)
        {
            try
            {
                if (fb == null)
                {
                    return Json(new { success = false, message = "Feedback data is null." });
                }

                // Manually set the dateCreated since it is not submitted from the form
                fb.dateCreated = DateTime.Now;
                fb.status = 0;

                if (_FeedbackManager.CreateFeedback(fb, ref ErrorMessage) != ErrorCode.Success)
                {
                    return Json(new { success = false, message = "Feedback Failed to create." });
                }

                return Json(new { success = true, message = "Thank you for your feedback!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }



        [Authorize]
        public ActionResult ViewFeedback()
        {
            var currentUser = User.Identity.Name;
            var employee = _AccManager.GetEmployeebyEmployeeId(currentUser);
            var user = _AccManager.GetUserByEmployeeId(currentUser);
            var feed = _FeedbackManager.GetAllFeedback();

            var model = new AccountViewModel
            {
                employeeInfo = employee,
                userAccount = user,
                feedbacks = feed
            };

            return View(model);
        }


        [HttpGet]
        public JsonResult GetUnreadNotificationCount()
        {
            var employee = User.Identity.Name;
            var account = _AccManager.GetEmployeebyEmployeeId(employee);
            var notification = _NotifManager.GetNotificationByemployeeId(employee);

            var unreadCount = notification.Count(n => n.isRead == false);

            return Json(new { unreadCount }, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        [HttpPost]
        public JsonResult UpdateFeedback(int id)
        {
            string errorMessage = string.Empty;

            if (_FeedbackManager.UpdateFeedbackStatus(id, ref errorMessage) == ErrorCode.Error)
            {
                return Json(new { success = false, message = errorMessage });
            }

            return Json(new { success = true, message = "Feedback successfully updated!" });
        }


        [Authorize]
        [HttpPost]
        public JsonResult MarkAllNotificationsAsRead()
        {
            string errorMessage = string.Empty;
            var currentUserId = User.Identity.Name;

            var result = _NotifManager.MarkAllAsRead(currentUserId, ref errorMessage);

            if (result == ErrorCode.Error)
            {
                return Json(new { success = false, message = errorMessage });
            }

            return Json(new { success = true, message = "All notifications marked as read!" });
        }

        [HttpPost]
        [Authorize]
        public ActionResult ChangePassword(string OldPassword, string NewPassword, string ConfirmNewPassword)
        {
            var employeeId = User.Identity.Name; // This is your EmployeeID
            string errorMsg = string.Empty;

            if (string.IsNullOrWhiteSpace(OldPassword) || string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmNewPassword))
            {
                TempData["Error"] = "All fields are required.";
                return RedirectToAction("MyProfile");
            }

            if (NewPassword != ConfirmNewPassword)
            {
                TempData["Error"] = "New password and confirmation do not match.";
                return RedirectToAction("MyProfile");
            }

            var userAccount = _AccManager.GetUserByEmployeeId(employeeId);

            if (userAccount == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("MyProfile");
            }

            // ✅ Verify old password (check if it is hashed or plain)
            bool isOldPasswordCorrect = false;

            if (userAccount.password.StartsWith("$2")) // bcrypt hashed
            {
                isOldPasswordCorrect = BCrypt.Net.BCrypt.Verify(OldPassword, userAccount.password);
            }
            else // Plaintext fallback (in case you have legacy passwords)
            {
                isOldPasswordCorrect = userAccount.password == OldPassword;
            }

            if (!isOldPasswordCorrect)
            {
                TempData["Error"] = "Old password is incorrect.";
                return RedirectToAction("MyProfile");
            }

            // ✅ Hash and update the new password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            userAccount.password = hashedPassword;

            if (_AccManager.UpdateUser(userAccount, ref errorMsg) == ErrorCode.Success)
            {
                TempData["Success"] = "Password changed successfully.";
            }
            else
            {
                TempData["Error"] = errorMsg;
            }

            return RedirectToAction("MyProfile");
        }

        [Authorize]
        public ActionResult Faq()
        {
            return View();

        }

        [AllowAnonymous]     
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ForgotPassword(string email)
        {
            var user = _AccManager.GetEmployeebyemail(email);

            if (user == null)
            {
                TempData["Error"] = "There is no account associated with that email. Try again.";
                return RedirectToAction("ForgotPassword");
            }

            var userAcc = _AccManager.GetEmployeebyEmployeeId(user.employeeId);

            if (userAcc != null)
            {
                // Generate a verification code (e.g., 6 digits)
                var random = new Random();
                string verificationCode = random.Next(100000, 999999).ToString();

                // Save the code and employeeId temporarily
                Session["VerificationCode"] = verificationCode;
                Session["VerificationUserId"] = user.employeeId;
                Session["VerificationEmail"] = email;
                Session["VerificationCodeExpiresAt"] = DateTime.Now.AddMinutes(5); 


                // Send email
                string errorMessage = "";
                var mailManager = new MailManager();  // Your MailManager instance
                string subject = "Your Password Reset Verification Code";
                string body = $@"
                            <!DOCTYPE html>
                            <html>
                            <head>
                              <meta charset='UTF-8'>
                              <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                              <title>Verification Code</title>
                            </head>
                            <body style='margin: 0; padding: 0; background-color: #f4f4f4; font-family: Arial, sans-serif;'>
                              <table align='center' border='0' cellpadding='0' cellspacing='0' width='600' style='border-collapse: collapse; background-color: #ffffff; margin-top: 50px; box-shadow: 0 0 10px rgba(0,0,0,0.1);'>
                                <tr>
                                  <td align='center' bgcolor='#cfd0d1' style='padding: 30px 0;'>
                                    <img src='https://www.dealogikal.com/images/dealogikal_dark.png' alt='Dealogikal Logo' width='200' style='display: block;' />
                                  </td>
                                </tr>
                                <tr>
                                  <td style='padding: 40px 30px 20px 30px;'>
                                    <h2 style='color: #333333; text-align: center;'>Password Reset Verification</h2>
                                    <p style='color: #666666; font-size: 16px; text-align: center;'>Hello {user.firstName},</p>
                                    <p style='color: #666666; font-size: 16px; text-align: center;'>
                                      We received a request to reset your password. Use the verification code below to proceed:
                                    </p>
                                  </td>
                                </tr>
                                <tr>
                                  <td align='center' style='padding: 20px 30px;'>
                                    <p style='font-size: 32px; color: #ffffff; background-color: #2596be; display: inline-block; padding: 15px 25px; border-radius: 5px; letter-spacing: 5px;'>
                                      {verificationCode}
                                    </p>
                                  </td>
                                </tr>
                                <tr>
                                  <td style='padding: 20px 30px;'>
                                    <p style='color: #666666; font-size: 14px;'>
                                      If you did not request a password reset, you can safely ignore this email. This code will expire in 5 minutes.
                                    </p>
                                    <p style='color: #666666; font-size: 14px;'>Thank you,<br />The Dealogikal Team</p>
                                  </td>
                                </tr>
                                <tr>
                                  <td bgcolor='#cfd0d1' style='padding: 20px; color: #ffffff; text-align: center; font-size: 12px;'>
                                    &copy; {DateTime.Now.Year} Dealogikal. All rights reserved.
                                  </td>
                                </tr>
                              </table>
                            </body>
                            </html>";

                bool isSent = mailManager.SendEmail(email, subject, body, ref errorMessage);

                if (!isSent)
                {
                    TempData["Error"] = "Failed to send verification email: " + errorMessage;
                    return RedirectToAction("ForgotPassword");
                }

                TempData["Success"] = "Verification code sent to your email. Please check your inbox.";
                return RedirectToAction("VerifyCode");
            }

            TempData["Error"] = "Something went wrong.";
            return RedirectToAction("ForgotPassword");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ResendCode()
        {
            var employeeId = Session["VerificationUserId"] as string;
            var email = Session["VerificationEmail"] as string;

            if (string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Session expired. Please try again.";
                return RedirectToAction("ForgotPassword");
            }

            var user = _AccManager.GetEmployeebyEmployeeId(employeeId);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("ForgotPassword");
            }

            var random = new Random();
            string verificationCode = random.Next(100000, 999999).ToString();

            Session["VerificationCode"] = verificationCode;
            Session["VerificationCodeExpiresAt"] = DateTime.Now.AddMinutes(5); // 5 min expiry

            string errorMessage = "";
            var mailManager = new MailManager();
            string subject = "Your New Verification Code";
            string body = $@"
                        <!DOCTYPE html>
                        <html>
                        <head>
                          <meta charset='UTF-8'>
                          <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                          <title>Verification Code</title>
                        </head>
                        <body style='margin: 0; padding: 0; background-color: #f4f4f4; font-family: Arial, sans-serif;'>
                          <table align='center' border='0' cellpadding='0' cellspacing='0' width='600' style='border-collapse: collapse; background-color: #ffffff; margin-top: 50px; box-shadow: 0 0 10px rgba(0,0,0,0.1);'>
                            <tr>
                              <td align='center' bgcolor='#cfd0d1' style='padding: 30px 0;'>
                                <img src='https://www.dealogikal.com/images/dealogikal_dark.png' alt='Dealogikal Logo' width='200' style='display: block;' />
                              </td>
                            </tr>
                            <tr>
                              <td style='padding: 40px 30px 20px 30px;'>
                                <h2 style='color: #333333; text-align: center;'>Password Reset Verification</h2>
                                <p style='color: #666666; font-size: 16px; text-align: center;'>Hello {user.firstName},</p>
                                <p style='color: #666666; font-size: 16px; text-align: center;'>
                                  We received a request to reset your password. Use the verification code below to proceed:
                                </p>
                              </td>
                            </tr>
                            <tr>
                              <td align='center' style='padding: 20px 30px;'>
                                <p style='font-size: 32px; color: #ffffff; background-color: #2596be; display: inline-block; padding: 15px 25px; border-radius: 5px; letter-spacing: 5px;'>
                                  {verificationCode}
                                </p>
                              </td>
                            </tr>
                            <tr>
                              <td style='padding: 20px 30px;'>
                                <p style='color: #666666; font-size: 14px;'>
                                  If you did not request a password reset, you can safely ignore this email. This code will expire in 5 minutes.
                                </p>
                                <p style='color: #666666; font-size: 14px;'>Thank you,<br />The Dealogikal Team</p>
                              </td>
                            </tr>
                            <tr>
                              <td bgcolor='#cfd0d1' style='padding: 20px; color: #ffffff; text-align: center; font-size: 12px;'>
                                &copy; {DateTime.Now.Year} Dealogikal. All rights reserved.
                              </td>
                            </tr>
                          </table>
                        </body>
                        </html>";

            bool isSent = mailManager.SendEmail(email, subject, body, ref errorMessage);

            if (!isSent)
            {
                TempData["Error"] = "Failed to resend code: " + errorMessage;
            }
            else
            {
                TempData["Success"] = "A new verification code has been sent to your email.";
            }

            return RedirectToAction("VerifyCode");
        }



        [AllowAnonymous]
        public ActionResult VerifyCode()
        {
            var expiresAt = Session["VerificationCodeExpiresAt"] as DateTime?;

            if (Session["VerificationUserId"] == null)
            {
                TempData["Error"] = "Session expired. Please try again.";
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.ExpiresAt = expiresAt.Value.ToString("o"); 
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult VerifyCode(string code)
        {
            string sessionCode = Session["VerificationCode"] as string;
            string employeeId = Session["VerificationUserId"] as string;
            DateTime? expiresAt = Session["VerificationCodeExpiresAt"] as DateTime?;


            if (string.IsNullOrEmpty(sessionCode) || string.IsNullOrEmpty(employeeId) || !expiresAt.HasValue)
            {
                TempData["Error"] = "Session expired. Please try again.";
                return RedirectToAction("ForgotPassword");
            }

            if (DateTime.Now > expiresAt.Value)
            {
                TempData["Error"] = "Your verification code has expired. Please request a new one.";
                // Optionally clear session here
                Session.Remove("VerificationCode");
                Session.Remove("VerificationUserId");
                Session.Remove("VerificationCodeExpiresAt");
                return RedirectToAction("ForgotPassword");
            }

            // Check if the code matches
            if (code != sessionCode)
            {
                TempData["Error"] = "Invalid verification code. Please try again.";
                return RedirectToAction("VerifyCode");
            }

            // Code is valid, clear the session if you like
            Session.Remove("VerificationCode"); // Optional: remove the code after successful verification
            Session.Remove("VerificationCodeExpiresAt");

            // Proceed to confirmation password page
            return RedirectToAction("ConfirmationPassword", new { id = employeeId });
        }



        [AllowAnonymous]
        public ActionResult ConfirmationPassword(string id)
        {
            var sessionUserId = Session["VerificationUserId"] as string;

            if (string.IsNullOrEmpty(sessionUserId) || sessionUserId != id)
            {
                TempData["Error"] = "Unauthorized access or session expired.";
                return RedirectToAction("ForgotPassword");
            }

            var model = new AccountViewModel
            {
                userAccount = _AccManager.GetUserByEmployeeId(id)
            };

            ViewBag.UserId = id;

            return View(model);
        }


        [HttpPost]
        [AllowAnonymous]
        public ActionResult ConfirmationPassword(string employeeId, string password, string confirmpassword)
        {
            string errorMsg = string.Empty;

            // Basic validation
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(confirmpassword))
            {
                TempData["Error"] = "Password and Confirm Password are required.";
                return RedirectToAction("ConfirmationPassword", new { id = employeeId });
            }

            if (password != confirmpassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return RedirectToAction("ConfirmationPassword", new { id = employeeId });
            }

            // Get the user account
            var user = _AccManager.GetUserByEmployeeId(employeeId);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("ForgotPassword");
            }

            // Hash the new password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            user.password = hashedPassword;

            // Update the user in the database
            var result = _AccManager.UpdateUser(user, ref errorMsg);

            if (result == ErrorCode.Success)
            {
                TempData["Success"] = "Password reset successful. You may now log in with your new password.";
                return RedirectToAction("ConfirmationPassword", new { id = employeeId });
            }
            else
            {
                TempData["Error"] = "Failed to reset password: " + errorMsg;
                return RedirectToAction("ConfirmationPassword", new { id = employeeId });
            }
        }


    }
}
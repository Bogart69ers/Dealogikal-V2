using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ClosedXML.Excel;
using Dealogikal.Database;
using Dealogikal.Utils;
using Dealogikal.ViewModel;


namespace Dealogikal.Controllers
{
    [Authorize(Roles = "HR")]
    public class AdminController : BaseController
    {

        [Authorize]
        public ActionResult Index()
        {
            return RedirectToAction("AdminDashboard");
        }

        [Authorize]
        public ActionResult AdminDashboard()
        {
            var user = _AccManager.GetEmployeebyEmployeeId(User.Identity.Name);
            var empId = user.employeeId.Trim();

            var currentDtr = _DtrManager.GetAllDtr()
                .AsEnumerable() // to allow .Date
                .FirstOrDefault(r => r.employeeId.Trim() == empId && r.date.Date == DateTime.Now.Date);


            ViewBag.Name = user.firstName;

            var today = DateTime.Now.Date;
            var lateThreshold = new TimeSpan(8, 0, 0); 

            var lateEmployeesCount = _DtrManager.GetAllDtr()
                 .Where(dtr => dtr.date == today &&
                               dtr.timeIn.HasValue &&
                               dtr.timeIn.Value.TimeOfDay > lateThreshold)
                 .Select(dtr => dtr.employeeId)
                 .Distinct()
                 .Count();


            ViewBag.LateEmployeesCount = lateEmployeesCount;

            var model = new AccountViewModel
            {
                employeeInfos = _AccManager.GetAllEmployee(),
                leaveRequests = _RequestManager.GetAllLeaveRequest(),
                overtimeRequests = _RequestManager.GetAllOvertimeRequest(),
                dtr = currentDtr,
                dtrRecords = _DtrManager.GetAllDtr()
            };

            return View(model);
        }



        [Authorize]
        public ActionResult CreateAccount()
        {
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
        [HttpPost]
        public ActionResult CreateAccount(userAccount ua, string email, DateTime? birthdate, string firstName, string lastName, string department, string position, string address, string barangay, string city, string phone, DateTime dateHired, string corporation)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("CreateAccount");
                }

                ua.employeeId = ua.employeeId?.Replace(" ", "").Trim();

                ua.password = BCrypt.Net.BCrypt.HashPassword(ua.password);


                if (_AccManager.EmployeeInfoSignup(birthdate, position, department, ua.employeeId, email, firstName, lastName, phone, address, city, barangay, dateHired, corporation, ref ErrorMessage) != ErrorCode.Success)
                {
                    ViewBag.ErrorMessage = ErrorMessage;
                    return View("CreateAccount");
                }


                if (_AccManager.CreateEmployee(ua, department, ref ErrorMessage) != ErrorCode.Success)
                {
                    ViewBag.ErrorMessage = "Employee Already Exist";
                    return View("CreateAccount");
                }


                TempData["SuccessMessage"] = "Account created successfully.";

                return RedirectToAction("CreateAccount");

            }
            catch (Exception ex)
            {

                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                return View("CreateAccount");
            }
        }




        [Authorize]
        public ActionResult Accounts()
        {
            var employees = _AccManager.GetAllEmployee();
            var images = _ImgManager.GetAllImages();

            var model = new AccountViewModel
            {
                employeeInfos = employees ?? new List<employeeInfo>(),  
                images = images ?? new List<images>()                   
            };

            return View(model);
        }

        [HttpGet]
        public JsonResult GetEmployeeDetails(int id)
        {
            try
            {
                var employee = _AccManager.GetEmployeebyEmployeeId(id.ToString());

                if (employee == null)
                {
                    return Json(new { error = "Employee not found" }, JsonRequestBehavior.AllowGet);
                }

                var employeeDetails = new
                {
                    Email = employee.email,
                    Phone = employee.phone,
                    Address = $"{employee.address}, {employee.barangay}, {employee.city}",
                    Birthdate = employee.birthdate?.ToString("yyyy-MM-dd")
                };

                return Json(employeeDetails, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [Authorize]
        public ActionResult Dtr()
        {
            var currentUserId = User.Identity.Name;
            var dtrHistory = _DtrManager.GetDtrHistoryByEmployeeId(currentUserId);

            var model = new AccountViewModel
            {
                dtrRecords = dtrHistory
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Dtr(dtrRecords dtr, int? recordId, string dtrAction)
        {
            var currentUser = User.Identity.Name;
            string errMsg = string.Empty;
            ErrorCode result;

            if (dtrAction == "TimeIn")
            {
                result = _DtrManager.CreateDtr(dtr, currentUser, ref errMsg);
                if (result != ErrorCode.Success)
                {
                    ViewBag.Error = "Error Creating DTR: " + errMsg;
                    return RedirectToAction("AdminDashboard");
                }
            }
            else if (dtrAction == "BreakIn")
            {
                if (recordId.HasValue)
                {
                    result = _DtrManager.UpdateBreakIn(currentUser, recordId.Value, ref errMsg);
                    if (result != ErrorCode.Success)
                    {
                        ViewBag.Error = "Error Updating Break In: " + errMsg;
                        return RedirectToAction("AdminDashboard");
                    }
                }
                else
                {
                    ViewBag.Error = "Record ID is missing for Break In.";
                    return RedirectToAction("AdminDashboard");
                }
            }
            else if (dtrAction == "BreakOut")
            {
                result = _DtrManager.UpdateBreakOut(currentUser, recordId.Value, dtr.workMode, ref errMsg);
                if (result != ErrorCode.Success)
                {
                    ViewBag.Error = "Error Updating Break Out: " + errMsg;
                    return RedirectToAction("AdminDashboard");
                }
            }
            else if (dtrAction == "TimeOut")
            {
                if (recordId.HasValue)
                {
                    result = _DtrManager.UpdateTimeOut(currentUser, recordId.Value, ref errMsg);
                    if (result != ErrorCode.Success)
                    {
                        ViewBag.Error = "Error Updating Time Out: " + errMsg;
                        return RedirectToAction("AdminDashboard");
                    }
                }
                else
                {
                    ViewBag.Error = "Record ID is missing for Time Out.";
                    return RedirectToAction("AdminDashboard");
                }
            }

            return RedirectToAction("AdminDashboard");
        }

        [Authorize]
        public ActionResult EmployeeDtr()
        {
            var model = new AccountViewModel
            {
                employeeInfos = _AccManager.GetAllEmployee(),
                dtrRecords = _DtrManager.GetAllDtrDesc()
            };

            return View(model);
        }

       
        [Authorize]
        public ActionResult LeaveRequest()
        {
            var model = new AccountViewModel
            {
                employeeInfos = _AccManager.GetAllEmployee(),
                leaveRequests = _RequestManager.GetAllLeaveRequestsDesc()
            };

            return View(model);
        }

        [Authorize]
        public ActionResult OvertimeRequests()
        {
            var model = new AccountViewModel
            {
                employeeInfos = _AccManager.GetAllEmployee(),
                overtimeRequests = _RequestManager.GetAllOvertimeRequestsDesc()
            };

            return View(model);
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

                var image = _ImgManager.GetImagebyEmployeeId(currentUser);
                var employee = _AccManager.GetEmployeebyEmployeeId(currentUser);
                var user = _AccManager.GetUserByEmployeeId(currentUser);

                if (employee == null || user == null)
                {
                    ModelState.AddModelError(String.Empty, "User not found.");
                    return View();
                }

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
                            System.IO.File.Delete(oldImagePath);
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
        public ActionResult DownloadEmployeeDTRExcel(string employeeId, int month, string cutoff)
        {
            var dtrRecords = _DtrManager.GetEmployeeDTR(employeeId, month, cutoff);
            var employee = _AccManager.GetEmployeebyEmployeeId(employeeId);

            if (employee == null)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToAction("EmployeeDtr");
            }

            string initials = $"{employee.firstName?.FirstOrDefault()}{employee.lastName?.FirstOrDefault()}".ToUpper();
            string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

            List<DateTime> cutoffDates = GenerateCutoffDates(month, cutoff);
            int year = cutoffDates.First().Year;

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add(initials);

                worksheet.Range("B1:J1").Merge().Value = "Bi-Weekly TimeSheet Calculator";
                worksheet.Cell("B1").Style.Font.SetBold().Font.FontSize = 16;
                worksheet.Cell("B1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var corporationName = employee.corporation == "KPEC"
                    ? "Knotical Power and Energy Corporation"
                    : "DEALOGIKAL CORP.";

                worksheet.Range("B2:J2").Merge().Value = corporationName;
                worksheet.Cell("B2").Style.Font.SetBold();
                worksheet.Cell("B2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                worksheet.Cell("C3").Value = "Employee Name:";
                worksheet.Range("D3:H3").Merge().Value = $"{employee.firstName} {employee.lastName}";

                worksheet.Cell("C4").Value = "Department:";
                worksheet.Range("D4:H4").Merge().Value = employee.department;

                worksheet.Cell("C5").Value = "Paid Overtime:";
                worksheet.Range("D5:H5").Merge().Value = "No";

                worksheet.Cell("C7").Value = "Year";
                worksheet.Cell("C8").Value = year;

                worksheet.Cell("D7").Value = "Month";
                worksheet.Cell("D8").Value = monthName;

                worksheet.Cell("E7").Value = "Weekend";
                worksheet.Cell("E8").Value = "Sat & Sun";

                worksheet.Cell("E7").Style.Fill.BackgroundColor = XLColor.FromHtml("#C5D9F1");
                worksheet.Range(7, 3, 7, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Range(8, 3, 8, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Range(7, 3, 7, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7");
                worksheet.Range(7, 3, 8, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range(7, 3, 8, 5).Style.Border.InsideBorder = XLBorderStyleValues.Thin;



                int startRow = 12;
                worksheet.Range("D11:E11").Merge().Value = "Morning";
                worksheet.Range("F11:G11").Merge().Value = "Afternoon";

                worksheet.Cell(startRow, 2).Value = "Day";
                worksheet.Cell(startRow, 3).Value = "Date";
                worksheet.Cell(startRow, 4).Value = "Time In";
                worksheet.Cell(startRow, 5).Value = "Time Out";
                worksheet.Cell(startRow, 6).Value = "Time In";
                worksheet.Cell(startRow, 7).Value = "Time Out";
                worksheet.Cell(startRow, 8).Value = "Break";

                worksheet.Range(startRow, 2, startRow, 8).Style.Font.SetBold().Font.FontColor = XLColor.DarkBlue;
                worksheet.Range(startRow, 2, startRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Range(startRow, 2, startRow, 8).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range(startRow, 2, startRow, 8).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range("B11:G11").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range("B11:G11").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                worksheet.Range("B11:G11").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;



                int row = startRow + 1;

                var dtrDict = dtrRecords.ToDictionary(d => d.date.Date, d => d);

                foreach (var date in cutoffDates)
                {
                    worksheet.Cell(row, 2).Value = date.DayOfWeek.ToString();
                    worksheet.Cell(row, 3).Value = date.Day.ToString("00");

                    bool isSaturday = date.DayOfWeek == DayOfWeek.Saturday;
                    bool isSunday = date.DayOfWeek == DayOfWeek.Sunday;

                    dtrRecords dtr;

                    if (isSunday)
                    {
                        worksheet.Cell(row, 4).Value = "WEEKEND";
                        worksheet.Cell(row, 5).Value = "--";
                        worksheet.Cell(row, 6).Value = "--";
                        worksheet.Cell(row, 7).Value = "--";
                        worksheet.Cell(row, 8).Value = "--";

                        worksheet.Range(row, 2, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#C5D9F1");
                    }
                    else if (isSaturday)
                    {
                        if (dtrDict.TryGetValue(date.Date, out dtr))
                        {
                            worksheet.Cell(row, 4).Value = dtr.timeIn.HasValue ? dtr.timeIn.Value : (DateTime?)null;
                            worksheet.Cell(row, 5).Value = dtr.breakIn.HasValue ? dtr.breakIn.Value : (DateTime?)null;
                            worksheet.Cell(row, 6).Value = dtr.breakOut.HasValue ? dtr.breakOut.Value : (DateTime?)null;
                            worksheet.Cell(row, 7).Value = dtr.timeOut.HasValue ? dtr.timeOut.Value : (DateTime?)null;

                            worksheet.Cell(row, 8).Value = "1.0";

                            if (dtr.timeIn.HasValue)
                                worksheet.Cell(row, 4).Style.DateFormat.Format = "HH:mm";
                            if (dtr.breakIn.HasValue)
                                worksheet.Cell(row, 5).Style.DateFormat.Format = "HH:mm";
                            if (dtr.breakOut.HasValue)
                                worksheet.Cell(row, 6).Style.DateFormat.Format = "HH:mm";
                            if (dtr.timeOut.HasValue)
                                worksheet.Cell(row, 7).Style.DateFormat.Format = "HH:mm";
                        }
                        else
                        {
                            worksheet.Cell(row, 4).Value = "WEEKEND";
                            worksheet.Cell(row, 5).Value = "--";
                            worksheet.Cell(row, 6).Value = "--";
                            worksheet.Cell(row, 7).Value = "--";
                            worksheet.Cell(row, 8).Value = "--";
                        }

                        worksheet.Range(row, 2, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#C5D9F1");
                    }
                    else
                    {
                        if (dtrDict.TryGetValue(date.Date, out dtr))
                        {
                            worksheet.Cell(row, 4).Value = dtr.timeIn.HasValue ? dtr.timeIn.Value : (DateTime?)null;
                            worksheet.Cell(row, 5).Value = dtr.breakIn.HasValue ? dtr.breakIn.Value : (DateTime?)null;
                            worksheet.Cell(row, 6).Value = dtr.breakOut.HasValue ? dtr.breakOut.Value : (DateTime?)null;
                            worksheet.Cell(row, 7).Value = dtr.timeOut.HasValue ? dtr.timeOut.Value : (DateTime?)null;
                            worksheet.Cell(row, 8).Value = "1.0";

                            if (dtr.timeIn.HasValue)
                                worksheet.Cell(row, 4).Style.DateFormat.Format = "HH:mm";
                            if (dtr.breakIn.HasValue)
                                worksheet.Cell(row, 5).Style.DateFormat.Format = "HH:mm";
                            if (dtr.breakOut.HasValue)
                                worksheet.Cell(row, 6).Style.DateFormat.Format = "HH:mm";
                            if (dtr.timeOut.HasValue)
                                worksheet.Cell(row, 7).Style.DateFormat.Format = "HH:mm";
                        }
                        else
                        {
                            worksheet.Cell(row, 4).Value = "ABSENT";
                            worksheet.Cell(row, 5).Value = "--";
                            worksheet.Cell(row, 6).Value = "--";
                            worksheet.Cell(row, 7).Value = "--";
                            worksheet.Cell(row, 8).Value = "--";

                            worksheet.Range(row, 2, row, 8).Style.Fill.BackgroundColor = XLColor.LightPink;
                        }
                    }

                    worksheet.Range(row, 2, row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    int endRow = row - 1;
                    worksheet.Range("B11:H27").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Range("B11:H27").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    row++;
                }


                worksheet.Columns().AdjustToContents();

                worksheet.Column(4).Width += 5;
                worksheet.Column(5).Width += 5; 
                worksheet.Column(6).Width += 5; 
                worksheet.Column(7).Width += 5; 
                worksheet.Column(8).Width += 3; 


                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    string fileName = $"DTR_{employee.lastName}_{monthName}_{year}_Cutoff-{cutoff}.xlsx";

                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
        private List<DateTime> GenerateCutoffDates(int month, string cutoff)
        {
            List<DateTime> dates = new List<DateTime>();
            int year = DateTime.Now.Year; 

            if (cutoff == "9-23")
            {
                for (int day = 9; day <= 23; day++)
                {
                    dates.Add(new DateTime(year, month, day));
                }
            }
            else if (cutoff == "24-8")
            {
                int daysInCurrentMonth = DateTime.DaysInMonth(year, month);
                for (int day = 24; day <= daysInCurrentMonth; day++)
                {
                    dates.Add(new DateTime(year, month, day));
                }

                int nextMonth = month == 12 ? 1 : month + 1;
                int nextYear = month == 12 ? year + 1 : year;

                for (int day = 1; day <= 8; day++)
                {
                    dates.Add(new DateTime(nextYear, nextMonth, day));
                }
            }

            return dates;
        }

        [Authorize]
        public ActionResult DownloadAllEmployeeDTR(int month, string cutoff)
        {
            var allEmployees = _AccManager.GetAllEmployee(); 
            if (allEmployees == null || !allEmployees.Any())
            {
                TempData["Error"] = "No employees found.";
                return RedirectToAction("EmployeeDtr");
            }

            string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
            int year = DateTime.Now.Year;

            using (var workbook = new XLWorkbook())
            {
                foreach (var employee in allEmployees)
                {
                    var dtrRecords = _DtrManager.GetEmployeeDTR(employee.employeeId, month, cutoff);

                    var cutoffDates = GenerateCutoffDates(month, cutoff);
                    int sheetYear = cutoffDates.First().Year;

                    string initials = $"{employee.firstName?.FirstOrDefault()}{employee.lastName?.FirstOrDefault()}".ToUpper();

                    var sheetName = initials;
                    int counter = 1;
                    while (workbook.Worksheets.Any(ws => ws.Name == sheetName))
                    {
                        sheetName = initials + counter;
                        counter++;
                    }

                    var worksheet = workbook.Worksheets.Add(sheetName);

                    var dtrDict = dtrRecords.ToDictionary(d => d.date.Date, d => d);

                    worksheet.Range("B1:J1").Merge().Value = "Bi-Weekly TimeSheet Calculator";
                    worksheet.Cell("B1").Style.Font.SetBold().Font.FontSize = 16;
                    worksheet.Cell("B1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    var corporationName = employee.corporation == "KPEC"
                        ? "Knotical Power and Energy Corporation"
                        : "DEALOGIKAL CORP.";

                    worksheet.Range("B2:J2").Merge().Value = corporationName;
                    worksheet.Cell("B2").Style.Font.SetBold();
                    worksheet.Cell("B2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Cell("C3").Value = "Employee Name:";
                    worksheet.Range("D3:H3").Merge().Value = $"{employee.firstName} {employee.lastName}";

                    worksheet.Cell("C4").Value = "Department:";
                    worksheet.Range("D4:H4").Merge().Value = employee.department;

                    worksheet.Cell("C5").Value = "Paid Overtime:";
                    worksheet.Range("D5:H5").Merge().Value = "No";

                    worksheet.Cell("C7").Value = "Year";
                    worksheet.Cell("C8").Value = sheetYear;

                    worksheet.Cell("D7").Value = "Month";
                    worksheet.Cell("D8").Value = monthName;

                    worksheet.Cell("E7").Value = "Weekend";
                    worksheet.Cell("E8").Value = "Sat & Sun";

                    worksheet.Cell("E7").Style.Fill.BackgroundColor = XLColor.FromHtml("#C5D9F1");
                    worksheet.Range(7, 3, 7, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Range(8, 3, 8, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Range(7, 3, 7, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#DDEBF7");


                    int startRow = 12;
                    worksheet.Range("D11:E11").Merge().Value = "Morning";
                    worksheet.Range("F11:G11").Merge().Value = "Afternoon";
                    worksheet.Cell(startRow, 2).Value = "Day";
                    worksheet.Cell(startRow, 3).Value = "Date";
                    worksheet.Cell(startRow, 4).Value = "Time In";
                    worksheet.Cell(startRow, 5).Value = "Time Out";
                    worksheet.Cell(startRow, 6).Value = "Time In";
                    worksheet.Cell(startRow, 7).Value = "Time Out";
                    worksheet.Cell(startRow, 8).Value = "Break";

                    worksheet.Range(startRow, 2, startRow, 8).Style.Font.SetBold().Font.FontColor = XLColor.DarkBlue;
                    worksheet.Range(startRow, 2, startRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Range("B11:G11").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Range("B11:G11").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Range("B11:G11").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;


                    int row = startRow + 1;

                    foreach (var date in cutoffDates)
                    {
                        worksheet.Cell(row, 2).Value = date.DayOfWeek.ToString();
                        worksheet.Cell(row, 3).Value = date.Day.ToString("00");

                        bool isSaturday = date.DayOfWeek == DayOfWeek.Saturday;
                        bool isSunday = date.DayOfWeek == DayOfWeek.Sunday;

                        dtrRecords dtr;

                        if (isSunday)
                        {
                            worksheet.Cell(row, 4).Value = "WEEKEND";
                            worksheet.Cell(row, 5).Value = "--";
                            worksheet.Cell(row, 6).Value = "--";
                            worksheet.Cell(row, 7).Value = "--";
                            worksheet.Cell(row, 8).Value = "--";
                            worksheet.Range(row, 2, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#C5D9F1");
                        }
                        else if (isSaturday)
                        {
                            if (dtrDict.TryGetValue(date.Date, out dtr))
                            {
                                worksheet.Cell(row, 4).Value = dtr.timeIn.HasValue ? dtr.timeIn.Value : (DateTime?)null;
                                worksheet.Cell(row, 5).Value = dtr.breakIn.HasValue ? dtr.breakIn.Value : (DateTime?)null;
                                worksheet.Cell(row, 6).Value = dtr.breakOut.HasValue ? dtr.breakOut.Value : (DateTime?)null;
                                worksheet.Cell(row, 7).Value = dtr.timeOut.HasValue ? dtr.timeOut.Value : (DateTime?)null;
                                worksheet.Cell(row, 8).Value = "1.0";

                                if (dtr.timeIn.HasValue)
                                    worksheet.Cell(row, 4).Style.DateFormat.Format = "HH:mm";
                                if (dtr.breakIn.HasValue)
                                    worksheet.Cell(row, 5).Style.DateFormat.Format = "HH:mm";
                                if (dtr.breakOut.HasValue)
                                    worksheet.Cell(row, 6).Style.DateFormat.Format = "HH:mm";
                                if (dtr.timeOut.HasValue)
                                    worksheet.Cell(row, 7).Style.DateFormat.Format = "HH:mm";
                            }
                            else
                            {
                                worksheet.Cell(row, 4).Value = "WEEKEND";
                                worksheet.Cell(row, 5).Value = "--";
                                worksheet.Cell(row, 6).Value = "--";
                                worksheet.Cell(row, 7).Value = "--";
                                worksheet.Cell(row, 8).Value = "--";
                            }
                            worksheet.Range(row, 2, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#C5D9F1");
                        }
                        else
                        {
                            if (dtrDict.TryGetValue(date.Date, out dtr))
                            {
                                worksheet.Cell(row, 4).Value = dtr.timeIn.HasValue ? dtr.timeIn.Value : (DateTime?)null;
                                worksheet.Cell(row, 5).Value = dtr.breakIn.HasValue ? dtr.breakIn.Value : (DateTime?)null;
                                worksheet.Cell(row, 6).Value = dtr.breakOut.HasValue ? dtr.breakOut.Value : (DateTime?)null;
                                worksheet.Cell(row, 7).Value = dtr.timeOut.HasValue ? dtr.timeOut.Value : (DateTime?)null;
                                worksheet.Cell(row, 8).Value = "1.0";

                                if (dtr.timeIn.HasValue)
                                    worksheet.Cell(row, 4).Style.DateFormat.Format = "HH:mm";
                                if (dtr.breakIn.HasValue)
                                    worksheet.Cell(row, 5).Style.DateFormat.Format = "HH:mm";
                                if (dtr.breakOut.HasValue)
                                    worksheet.Cell(row, 6).Style.DateFormat.Format = "HH:mm";
                                if (dtr.timeOut.HasValue)
                                    worksheet.Cell(row, 7).Style.DateFormat.Format = "HH:mm";
                            }
                            else
                            {
                                worksheet.Cell(row, 4).Value = "ABSENT";
                                worksheet.Cell(row, 5).Value = "--";
                                worksheet.Cell(row, 6).Value = "--";
                                worksheet.Cell(row, 7).Value = "--";
                                worksheet.Cell(row, 8).Value = "--";
                                worksheet.Range(row, 2, row, 8).Style.Fill.BackgroundColor = XLColor.LightPink;
                            }
                        }

                        worksheet.Range(row, 2, row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        row++;
                    }

                    int endRow = row - 1;
                    worksheet.Range($"B11:H{endRow}").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Range($"B11:H{endRow}").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    worksheet.Columns().AdjustToContents();

                    worksheet.Column(4).Width += 5; 
                    worksheet.Column(5).Width += 5;
                    worksheet.Column(6).Width += 5; 
                    worksheet.Column(7).Width += 5; 
                    worksheet.Column(8).Width += 3; 

                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    string fileName = $"All_Employees_DTR_{monthName}_{year}_Cutoff-{cutoff}.xlsx";

                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
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
        public ActionResult EditEmployee(string employeeId, string firstName, string lastName, string phone, string email, string address, string barangay, string city, DateTime? birthdate, DateTime? dateHired, string department, string position, string corporation, HttpPostedFileBase profilePicture)
        {
            if (string.IsNullOrEmpty(employeeId))
            {
                ModelState.AddModelError(string.Empty, "Employee ID is required.");
                return RedirectToAction("Accounts");
            }

            try
            {
                var employee = _AccManager.GetEmployeebyEmployeeId(employeeId);
                if (employee == null)
                {
                    ModelState.AddModelError(string.Empty, "Employee not found.");
                    return RedirectToAction("Accounts");
                }

                if (profilePicture != null && profilePicture.ContentLength > 0)
                {
                    var uploadsFolderPath = Server.MapPath("~/UploadedFiles/");
                    if (!Directory.Exists(uploadsFolderPath))
                        Directory.CreateDirectory(uploadsFolderPath);

                    var profileFileName = Path.GetFileName(profilePicture.FileName);
                    var profileSavePath = Path.Combine(uploadsFolderPath, profileFileName);
                    profilePicture.SaveAs(profileSavePath);

                    var existingImage = _ImgManager.ListImageByEmployeeId(employee.employeeId).FirstOrDefault();
                    if (existingImage != null)
                    {
                        existingImage.imageFile = profileFileName;
                        if (_ImgManager.UpdateImg(existingImage, ref ErrorMessage) == ErrorCode.Error)
                        {
                            ModelState.AddModelError(string.Empty, ErrorMessage);
                            return RedirectToAction("Accounts");
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
                            ModelState.AddModelError(string.Empty, ErrorMessage);
                            return RedirectToAction("Accounts");
                        }
                    }
                }

                employee.firstName = !string.IsNullOrEmpty(firstName) ? firstName : employee.firstName;
                employee.lastName = !string.IsNullOrEmpty(lastName) ? lastName : employee.lastName;
                employee.phone = !string.IsNullOrEmpty(phone) ? phone : employee.phone;
                employee.email = !string.IsNullOrEmpty(email) ? email : employee.email;
                employee.address = !string.IsNullOrEmpty(address) ? address : employee.address;
                employee.barangay = !string.IsNullOrEmpty(barangay) ? barangay : employee.barangay;
                employee.city = !string.IsNullOrEmpty(city) ? city : employee.city;
                employee.corporation = !string.IsNullOrEmpty(corporation) ? corporation : employee.corporation;
                employee.department = !string.IsNullOrEmpty(department) ? department : employee.department;
                employee.position = !string.IsNullOrEmpty(position) ? position : employee.position;
                if (birthdate.HasValue)
                {
                    employee.birthdate = birthdate.Value;
                }

                if (dateHired.HasValue)
                {
                    employee.dateHired = dateHired.Value;
                }

                if (_AccManager.UpdateEmployeeInformation(employee, ref ErrorMessage) == ErrorCode.Error)
                {
                    ModelState.AddModelError(string.Empty, ErrorMessage);
                    return RedirectToAction("Accounts");
                }

                TempData["SuccessMessage"] = "Employee info updated successfully.";
                return RedirectToAction("Accounts");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                return RedirectToAction("Accounts");
            }
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
            var employeeId = User.Identity.Name; 
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

            bool isOldPasswordCorrect = false;

            if (userAccount.password.StartsWith("$2"))
            {
                isOldPasswordCorrect = BCrypt.Net.BCrypt.Verify(OldPassword, userAccount.password);
            }
            else 
            {
                isOldPasswordCorrect = userAccount.password == OldPassword;
            }

            if (!isOldPasswordCorrect)
            {
                TempData["Error"] = "Old password is incorrect.";
                return RedirectToAction("MyProfile");
            }

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

        [Authorize]
        [HttpPost]
        public JsonResult UpdateEmployeeDTR(dtrRecords model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.employeeId) || model.date == null)
                {
                    return Json(new { success = false, message = "Invalid DTR data provided." });
                }

                // Find the existing DTR record
                var existingDtr = _DtrManager.GetAllDtr()
                    .FirstOrDefault(d => d.employeeId.Trim() == model.employeeId.Trim() && d.date.Date == model.date.Date);

                if (existingDtr == null)
                {
                    return Json(new { success = false, message = "DTR record not found." });
                }

                // Update the fields
                existingDtr.timeIn = model.timeIn;
                existingDtr.breakIn = model.breakIn;
                existingDtr.breakOut = model.breakOut;
                existingDtr.timeOut = model.timeOut;

                string errorMsg = string.Empty;
                var updateResult = _DtrManager.UpdateDtr(existingDtr, ref errorMsg);

                if (updateResult != ErrorCode.Success)
                {
                    return Json(new { success = false, message = "Failed to update DTR: " + errorMsg });
                }

                return Json(new { success = true, message = "DTR updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}
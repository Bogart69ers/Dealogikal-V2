using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dealogikal.Utils;
using Dealogikal.Database;
using BCrypt.Net;
using System.Net;

namespace Dealogikal.Repository
{
    public class AccountManager
    {
        private BaseRepository<userAccount> _userAcc;
        private BaseRepository<employeeInfo> _employeeInf;
        private BaseRepository<loginLogs> _loginLogs;

        public AccountManager()
        {
            _userAcc = new BaseRepository<userAccount>();
            _employeeInf = new BaseRepository<employeeInfo>();
            _loginLogs = new BaseRepository<loginLogs>();
        }

        public userAccount GetUserByUserId(int userId)
        {
            return _userAcc.Get(userId);
        }

        public userAccount GetUserByEmployeeId(string employeeId)
        {
            return _userAcc._table.FirstOrDefault(m => m.employeeId == employeeId);
        }

        public List<userAccount> GetAllUsers()
        {
            return _userAcc.GetAll();
        }

        public List<employeeInfo> GetAllEmployee()
        {
            return _employeeInf.GetAll();
        }
        public employeeInfo GetDepartmentHeadByDepartment(string department)
        {
            return _employeeInf._table
                .FirstOrDefault(e => e.department == department && e.position == "Department Head");
        }

        public employeeInfo GetEmployeebyEmployeeId(string employeeId)
        {
            return _employeeInf._table.FirstOrDefault(m => m.employeeId == employeeId);
        }

        public employeeInfo GetEmployeebyEmployeeIdOrEmail(string employeeId)
        {
            return _employeeInf._table.FirstOrDefault(m => m.employeeId == employeeId || m.email == employeeId);
        }

        public List<employeeInfo> GetEmployeebyEmployeeIdDesc(string employeeId)
        {
            return _employeeInf._table.Where(m => m.employeeId == employeeId).OrderByDescending(e => e.createdAt).ToList();

        }
        public employeeInfo GetEmployeebyemail(string email)
        {
            return _employeeInf._table.FirstOrDefault(e => e.email == email);
        }


        public employeeInfo CreateOrRetrieve(String employeeId, ref String err)
        {
            var user = GetUserByEmployeeId(employeeId);
            if (user == null)
            {
                err = "User does not exist";
                return null;
            }

            var employeeInfo = GetEmployeebyEmployeeId(user.employeeId);
            if (employeeInfo != null)
            {
                return employeeInfo;
            }

            employeeInfo = new employeeInfo();
            employeeInfo.employeeId = user.employeeId;
            employeeInfo.createdAt = DateTime.Now;
            employeeInfo.status = (int)Status.Active;

            _employeeInf.Create(employeeInfo, out err);

            return GetEmployeebyEmployeeId(user.employeeId);
        }
        public ErrorCode CreateLoginLogs(loginLogs logs ,ref string errMsg)
        {
            try
            {           
                _loginLogs.Create(logs, out errMsg);
                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }

        public ErrorCode UpdateAccountStatus(string employeeId, int newStatus, ref string errMsg)
        {
            try
            {
                var user = GetEmployeebyEmployeeId(employeeId);
                if (user == null)
                {
                    errMsg = "Employee not found";
                    return ErrorCode.Error;
                }

                // Update status directly on the existing user object
                user.status = newStatus;

                return _employeeInf.Update(user.employeeId, user, out errMsg);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }


        public ErrorCode DeleteUser(int userId, ref string errMsg)
        {
            try

            {
                var user = GetUserByUserId(userId);
                if (user == null)
                {
                    errMsg = "User not found";
                    return ErrorCode.Error;
                }
                return _userAcc.Delete(userId, out errMsg);
            }
            catch (Exception ex)
            {
                errMsg = $"An error occurred: {ex.Message}";
                return ErrorCode.Error; ;
            }
        }
        public ErrorCode UpdateEmployeeLeaveCount(string employeeId, int daysToDeduct, ref string errMsg)
        {
            try
            {
                var userInfo = GetEmployeebyEmployeeId(employeeId);
                if (userInfo == null)
                {
                    errMsg = "No Employee Information found.";
                    return ErrorCode.Error;
                }

                if (userInfo.leaveCount < daysToDeduct)
                {
                    errMsg = "Insufficient leave balance.";
                    return ErrorCode.Error;
                }

                userInfo.leaveCount -= daysToDeduct;

                return _employeeInf.Update(employeeId, userInfo, out errMsg);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }


        public ErrorCode UpdateEmployeeInformation(employeeInfo empinf, ref string errMsg)
        {
            return _employeeInf.Update(empinf.employeeId, empinf, out errMsg);
        }

        public ErrorCode UpdateUser(userAccount userac, ref string errMsg)
        {
            return _userAcc.Update(userac.userId, userac, out errMsg);
        }

        public ErrorCode SignIn(string employeeId, string password, ref string errMsg)
        {

            userAccount userSignIn = null;

            userSignIn = GetUserByEmployeeId(employeeId);
            if (userSignIn == null)
            {
                // Try finding the employee info by email
                var empInfo = GetEmployeebyemail(employeeId);
                if (empInfo != null)
                {
                    // Now find the account using employeeId from empInfo
                    userSignIn = GetUserByEmployeeId(empInfo.employeeId);
                }
            }


            if (userSignIn == null)
            {
                errMsg = "Employee ID or Email or Password is incorrect";
                return ErrorCode.Error;
            }

            bool isPasswordCorrect = false;

            if (userSignIn.password.StartsWith("$2"))
            {
                isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, userSignIn.password);
            }
            else
            {
                isPasswordCorrect = userSignIn.password.Equals(password);

                if (isPasswordCorrect)
                {
                    userSignIn.password = BCrypt.Net.BCrypt.HashPassword(password);
                    string updateMsg = "";
                    if (UpdateUser(userSignIn, ref updateMsg) != ErrorCode.Success)
                    {
                        errMsg = "Failed to upgrade password security.";
                        return ErrorCode.Error;
                    }
                }
            }

            if (!isPasswordCorrect)
            {
                errMsg = "Employee ID or Password is incorrect";
                return ErrorCode.Error;
            }

            errMsg = "Login successful";
            return ErrorCode.Success;
        }


        public ErrorCode CreateEmployee(userAccount ua, string department, ref string errMsg)
        {
            try
            {
                if (department == "HR")
                {
                    ua.role = 1;
                    ua.createdAt = DateTime.Now;
                }
                else
                {
                    ua.role = 2;
                    ua.createdAt = DateTime.Now;
                }

                if (GetUserByEmployeeId(ua.employeeId) != null)
                {
                    errMsg = "Employee already exists";
                    return ErrorCode.Error;
                }

                if (_userAcc.Create(ua, out errMsg) != ErrorCode.Success)
                {
                    return ErrorCode.Error;
                }

                return ErrorCode.Success;

            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }

        public ErrorCode EmployeeInfoSignup(DateTime? birthdate, string position, string department, string employeeId, string email, string firstname, string lastname, string phone, string address, string city, string barangay, DateTime dateHired, string corporation, ref String err)
        {
            try
            {
                var empInfo = GetEmployeebyEmployeeId(employeeId);
                if (empInfo != null) 
                {
                    if (empInfo.employeeId == employeeId && empInfo.firstName == firstname && empInfo.lastName == lastname)
                    {
                        err = "Employee Already Exists";
                        return ErrorCode.Error;
                    }
                    else if (empInfo.employeeId == employeeId)
                    {
                        err = "Employee ID Already Used";
                        return ErrorCode.Error;
                    }
                }
                empInfo = new employeeInfo();
                empInfo.employeeId = employeeId;
                empInfo.email = email;
                empInfo.status = (int)Status.Active;
                empInfo.dateHired = dateHired;
                empInfo.createdAt = DateTime.Now;
                empInfo.position = position;
                empInfo.department = department;
                empInfo.firstName = firstname;
                empInfo.lastName = lastname;
                empInfo.address = address;
                empInfo.barangay = barangay;
                empInfo.city = city;
                empInfo.phone = phone;
                empInfo.birthdate = birthdate;
                empInfo.leaveCount = 7;
                empInfo.corporation = corporation;

                _employeeInf.Create(empInfo, out err);

                return ErrorCode.Success;
            }
            catch (Exception ex)
            {

                err = ex.Message;
                return ErrorCode.Error;
            }

        }
        public ErrorCode ResetAnnualLeaveCount(ref string errMsg)
        {
            try
            {
                var allEmployees = _employeeInf.GetAll();

                foreach (var emp in allEmployees)
                {
                    emp.leaveCount = 7;
                    if (_employeeInf.Update(emp.employeeId, emp, out errMsg) != ErrorCode.Success)
                    {
                        return ErrorCode.Error;
                    }
                }

                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }

    }
}
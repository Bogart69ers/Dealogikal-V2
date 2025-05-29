using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dealogikal.Utils;
using Dealogikal.Database;

namespace Dealogikal.Repository
{
    public class RequestManager
    {
        private BaseRepository<leaveRequest> _leaveReq;
        private BaseRepository<overtimeRequest> _overtReq;
        private BaseRepository<obRequest> _obReq;

        public RequestManager()
        {
            _leaveReq = new BaseRepository<leaveRequest>();
            _overtReq = new BaseRepository<overtimeRequest>();
            _obReq = new BaseRepository<obRequest>();
        }
        public obRequest GetObRequestById(int obId)
        {
            return _obReq.Get(obId);
        }

        public List<obRequest> GetObRequestByEmployeeId(string employeeId)
        {
            return _obReq._table.Where(o => o.employeeId == employeeId).OrderByDescending(o => o.dateFiled).ToList();
        }

        public List<obRequest> GetAllObRequestsDesc()
        {
            return _obReq.GetAll()
                          .OrderBy(l => l.status != 0)
                          .ThenByDescending(l => l.dateFiled)
                          .ToList();
        }
      
        public leaveRequest GetLeaveRequestbyRequestId(int requestId)
        {
            return _leaveReq.Get(requestId);
        }

        public List<leaveRequest> GetLeaveRequestByEmployeeId(string employeeId)
        {
            return _leaveReq._table.Where(l => l.employeeId == employeeId).OrderByDescending(l => l.dateFiled).ToList();
        }

        public List<leaveRequest> GetAllLeaveRequestsDesc()
        {
            return _leaveReq.GetAll()
                            .OrderBy(l => l.status != 0)
                            .ThenByDescending(l => l.dateFiled)
                            .ToList();
        }

        public List<leaveRequest> GetAllLeaveRequest()
        {
            return _leaveReq.GetAll();
        }

        public overtimeRequest GetOvertimeRequestbyRequestId(int requestId)
        {
            return _overtReq.Get(requestId);
        }

        public List<overtimeRequest> GetOvertimeRequestByEmployeeId(string employeeId)
        {
            return _overtReq._table.Where(o => o.employeeId == employeeId).OrderByDescending(o => o.dateFiled).ToList();
        }

        public List<overtimeRequest> GetAllOvertimeRequest()
        {
            return _overtReq.GetAll();
        }
        public List<overtimeRequest> GetAllOvertimeRequestsDesc() 
        {
            return _overtReq.GetAll()
                .OrderBy(l => l.status != 0) 
                .ThenByDescending(l => l.dateFiled) 
                .ToList();
        }
       

        public leaveRequest GetLeaveRequestByRequestId(int requestId)
        {
            return _leaveReq.Get(requestId);
        }

        public overtimeRequest GetOvertimeByRequestdId(int requestId)
        {
            return _overtReq.Get(requestId);
        }

        public ErrorCode CreateObReq(obRequest ob, string employeeId, ref string errMsg)
        {
            try
            {
                DateTime serverTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Singapore Standard Time");
                ob.dateFiled = serverTime;
                ob.employeeId = employeeId;
                ob.status = 0;
                if (_obReq.Create(ob, out errMsg) != ErrorCode.Success)
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

        public ErrorCode ApproveObRequest(int obId, ref string errMsg)
        {
            try
            {
                var request = GetObRequestById(obId);
                if (request == null)
                {
                    errMsg = "No request found";
                    return ErrorCode.Error;
                }
                request.status = 1; // Approve status
                return _obReq.Update(obId, request, out errMsg);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }

        public ErrorCode DeclineObRequest(int obId, ref string errMsg)
        {
            try
            {
                var request = GetObRequestById(obId);
                if (request == null)
                {
                    errMsg = "No request found";
                    return ErrorCode.Error;
                }
                request.status = 2; // Decline status
                return _obReq.Update(obId, request, out errMsg);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }

        public ErrorCode CreateLeave(leaveRequest lr, string employeeId, ref string errMsg)
        {
            try
            {
                DateTime serverTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Singapore Standard Time");

                lr.dateFiled = serverTime;
                lr.employeeId = employeeId;
                lr.status = 0;

                if (_leaveReq.Create(lr, out errMsg) != ErrorCode.Success)
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

        public ErrorCode CreateOvertime(overtimeRequest or, string employeeId, ref string errMsg)
        {
            try
            {
                DateTime serverTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Singapore Standard Time");

                or.dateFiled = serverTime;
                or.employeeId = employeeId;
                or.status = 0;

                if (_overtReq.Create(or, out errMsg) != ErrorCode.Success)
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

        public ErrorCode ApproveLeaveRequest(int requestId, ref string errMsg)
        {
            try
            {
                var request = GetLeaveRequestByRequestId(requestId);
                if (request == null)
                {
                    errMsg = "No request found";
                    return ErrorCode.Error;
                }
                request.status = 1;
                return _leaveReq.Update(requestId, request, out errMsg);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }
        public ErrorCode ApproveOvertimeRequest(int requestId, ref string errMsg)
        {
            try
            {
                var request = GetOvertimeByRequestdId(requestId);
                if (request == null)
                {
                    errMsg = "No request found";
                    return ErrorCode.Error;
                }
                request.status = 1;
                return _overtReq.Update(requestId, request, out errMsg);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }

        public ErrorCode DeclineLeaveRequest(int requestId, ref string errMsg)
        {
            try
            {
                var request = GetLeaveRequestByRequestId(requestId);
                if (request == null)
                {
                    errMsg = "No request found";
                    return ErrorCode.Error;
                }
                request.status = 2;
                return _leaveReq.Update(requestId, request, out errMsg);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }
        public ErrorCode DeclineOvertimeRequest(int requestId, ref string errMsg)
        {
            try
            {
                var request = GetOvertimeByRequestdId(requestId);
                if (request == null)
                {
                    errMsg = "No request found";
                    return ErrorCode.Error;
                }
                request.status = 2;
                return _overtReq.Update(requestId, request, out errMsg);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }
    }
}
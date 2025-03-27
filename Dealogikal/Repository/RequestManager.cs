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

        public RequestManager()
        {
            _leaveReq = new BaseRepository<leaveRequest>();
            _overtReq = new BaseRepository<overtimeRequest>();
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

        public ErrorCode ApproveLeaveRequest(string employeeId, int requestId, ref string errMsg)
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
        public ErrorCode ApproveOvertimeRequest(string employeeId, int requestId, ref string errMsg)
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

        public ErrorCode DeclineLeaveRequest(string employeeId, int requestId, ref string errMsg)
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
        public ErrorCode DeclineOvertimeRequest(string employeeId, int requestId, ref string errMsg)
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
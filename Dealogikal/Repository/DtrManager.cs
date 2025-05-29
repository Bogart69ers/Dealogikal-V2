using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dealogikal.Database;
using Dealogikal.Utils;

namespace Dealogikal.Repository
{
    public class DtrManager
    {
        private BaseRepository<dtrRecords> _dtrRecords;


        public DtrManager()
        {
            _dtrRecords = new BaseRepository<dtrRecords>();
        }
        public dtrRecords GetRecordsByRecordId(int recordId)
        {
            return _dtrRecords.Get(recordId);
        }
        public dtrRecords GetRecordsByEmployeeId(string employeeId)
        {
            return _dtrRecords._table.FirstOrDefault(e => e.employeeId == employeeId);
        }

        public dtrRecords GetCurrentRecord(int recordId)
        {
            return _dtrRecords._table.FirstOrDefault(r => r.recordId == recordId && r.date == DateTime.Now);
        }
        public List<dtrRecords> GetAllDtr()
        {
            return _dtrRecords.GetAll();
        }
        public List<dtrRecords> GetAllDtrDesc()
        {
            return _dtrRecords.GetAll().OrderByDescending(d => d.createdAt).ToList();
        }

        public List<dtrRecords> GetDtrHistoryByEmployeeId(string employeeId)
        {
            return _dtrRecords._table.Where(e => e.employeeId == employeeId).OrderByDescending(e => e.date).ToList();
        }

        public List<dtrRecords> GetEmployeeDTR(string employeeId, int month, string cutoff)
        {
            int year = DateTime.Now.Year;

            var startDate = new DateTime(year, month, 1);

            DateTime cutoffStartDate;
            DateTime cutoffEndDate;

            if (cutoff == "9-23")
            {
                cutoffStartDate = new DateTime(startDate.Year, startDate.Month, 9);
                cutoffEndDate = new DateTime(startDate.Year, startDate.Month, 23);
            }
            else
            {
                cutoffStartDate = new DateTime(startDate.Year, startDate.Month, 24);

                if (month == 12)
                {
                    cutoffEndDate = new DateTime(startDate.Year + 1, 1, 8);
                }
                else
                {
                    cutoffEndDate = new DateTime(startDate.Year, startDate.Month + 1, 8);
                }
            }

            var records = _dtrRecords._table
                .Where(d => d.employeeId == employeeId &&
                            d.date >= cutoffStartDate &&
                            d.date <= cutoffEndDate)
                .OrderBy(d => d.date)
                .ToList();

            return records;
        }


        public ErrorCode UpdateDtr(dtrRecords dtr, ref string errMsg)
        {
            return _dtrRecords.Update(dtr.recordId, dtr, out errMsg);
        }
        public ErrorCode CreateDtr(dtrRecords dtr, string employeeId, ref string errMsg)
        {
            try
            {
                DateTime serverTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Singapore Standard Time");

                dtr.employeeId = employeeId;
                dtr.createdAt = serverTime.Date;
                dtr.date = serverTime.Date;
                dtr.timeIn = serverTime.Add(new TimeSpan(0,3,29));

                if (_dtrRecords.Create(dtr, out errMsg) != ErrorCode.Success)
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
        public ErrorCode UpdateBreakIn(string employeeId, int recordId, ref string errMsg)
        {
            try
            {
                DateTime serverTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Singapore Standard Time");

                var record = GetRecordsByRecordId(recordId);
                if (record == null)
                {
                    errMsg = "No record found for Break In.";
                    return ErrorCode.Error;
                }
                record.breakIn = serverTime.Add(new TimeSpan(0, 3, 29));
                return _dtrRecords.Update(recordId, record, out errMsg);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }

        public ErrorCode UpdateBreakOut(string employeeId, int recordId, string workmode, ref string errMsg)
        {
            try
            {
                DateTime serverTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Singapore Standard Time");

                dtrRecords record = null;
                if (recordId > 0)
                {
                    record = GetRecordsByRecordId(recordId);
                }

                if (record == null)
                {
                    var newRecord = new dtrRecords();
                    newRecord.employeeId = employeeId;
                    newRecord.createdAt = serverTime.Date;
                    newRecord.date = serverTime.Date;
                    newRecord.workMode = workmode;
                    newRecord.breakOut = serverTime.Add(new TimeSpan(0, 3, 29));

                    return _dtrRecords.Create(newRecord, out errMsg);
                }
                else
                {
                    record.breakOut = serverTime;
                    return _dtrRecords.Update(record.recordId, record, out errMsg);
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }


        public ErrorCode UpdateTimeOut(string employeeId, int recordId, ref string errMsg)
        {
            try
            {
                DateTime serverTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Singapore Standard Time");

                var record = GetRecordsByRecordId(recordId);

                if (record == null)
                {
                    errMsg = "No Time In record found for today.";
                    return ErrorCode.Error;
                }

                record.timeOut = serverTime.Add(new TimeSpan(0, 3, 29));

                return _dtrRecords.Update(recordId, record, out errMsg);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                return ErrorCode.Error;
            }
        }

    }
}
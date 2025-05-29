using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dealogikal.Database;

namespace Dealogikal.ViewModel
{
	public class AccountViewModel
	{
        public userAccount userAccount { get; set; }

        public List<userAccount> userAccounts { get; set; } = new List<userAccount>();
        public employeeInfo employeeInfo { get; set; }

        public List<employeeInfo> employeeInfos { get; set; } = new List<employeeInfo>();

        public dtrRecords dtr { get; set; }

        public List<dtrRecords> dtrRecords { get; set; } = new List<dtrRecords>();

        public leaveRequest leaveRequest { get; set; }

        public List<leaveRequest> leaveRequests { get; set; } = new List<leaveRequest>();

        public overtimeRequest overtimeRequest { get; set; }

        public List<overtimeRequest> overtimeRequests { get; set; } = new List<overtimeRequest>();

        public feedback feedback { get; set; }

        public List<feedback> feedbacks { get; set; } = new List<feedback>();

        public images image { get; set; }

        public List<images> images { get; set; }

        public notification notif { get; set; }

        public List<notification> notifs { get; set; }

        public List<obRequest> obreq { get; set; }

        public obRequest obreqs { get; set; }

        public string AvatarUrl { get; set; }
    }
}
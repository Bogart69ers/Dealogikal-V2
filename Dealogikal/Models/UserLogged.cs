using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dealogikal.Database;

namespace Dealogikal.Models
{
	public class UserLogged
	{
        public userAccount UserAccount { get; set; }
        public employeeInfo employeeInfo { get; set; }
    }
}
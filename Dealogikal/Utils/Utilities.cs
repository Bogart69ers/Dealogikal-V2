using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dealogikal.Repository;
using Dealogikal.Database;
using System.Web.Mvc; 

namespace Dealogikal.Utils
{
    public enum ErrorCode
    {
        Success,
        Error,
        NotFound
    }
    public enum RoleType
    {
        HR,
        Employee

    }
    public enum Status
    {
        InActive,
        Active
    }
    public class Constant
    {
        public const string Role_HR = "HR";
        public const string Role_Employee = "Employee";
        public const string Role_DepartmentHead = "DepartmentHead";

        public const int ERROR = 1;
        public const int SUCCESS = 0;
    }
    public class Utilities
    {
        public static List<SelectListItem> ListRole
        {
            get
            {
                BaseRepository<role> role = new BaseRepository<role>();
                var list = new List<SelectListItem>();
                foreach (var item in role.GetAll())
                {
                    var r = new SelectListItem
                    {
                        Text = item.roleName,
                        Value = item.roleId.ToString()
                    };

                    list.Add(r);
                }

                return list;
            }
        }
    }
}
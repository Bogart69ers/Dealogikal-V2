//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Dealogikal.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class userAccount
    {
        public int userId { get; set; }
        public string employeeId { get; set; }
        public string password { get; set; }
        public int role { get; set; }
        public Nullable<System.DateTime> createdAt { get; set; }
    
        public virtual employeeInfo employeeInfo { get; set; }
        public virtual role role1 { get; set; }
    }
}

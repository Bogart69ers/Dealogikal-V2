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
    
    public partial class feedback
    {
        public int id { get; set; }
        public string name { get; set; }
        public string feedbackType { get; set; }
        public string feedbackMessage { get; set; }
        public Nullable<System.DateTime> dateCreated { get; set; }
        public Nullable<int> status { get; set; }
    }
}

// CustomReport.cs
using System;
using System.Collections.Generic;

namespace LibraryAccounting.Models
{
    [Serializable]
    public class CustomReport
    {
        public int ReportId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ReportType { get; set; }
        public List<string> SelectedFields { get; set; }
        public string FilterCriteria { get; set; }
        public string SortOrder { get; set; }
        public int Limit { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedByUserId { get; set; }
        public bool IsFavorite { get; set; }
        public string QueryString { get; set; }
    }
}
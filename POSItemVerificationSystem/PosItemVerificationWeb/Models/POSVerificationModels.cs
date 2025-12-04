using Azure.Core;
using Azure;


using Microsoft.EntityFrameworkCore.Metadata.Internal;
using PosItemVerificationWeb.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Channels;
using System.Threading;
using System;
using System.Globalization;
// Models/POSVerificationModels.cs - Updated models
using System.ComponentModel.DataAnnotations;

namespace PosItemVerificationWeb.Models
{
    
        public class POSVerificationItem
        {
            public string DataType { get; set; }
            public string POSDataSetBrandKey { get; set; }
            public string EffectiveFrom { get; set; }
            public string PointOfSale { get; set; }
            public string POSDataSet { get; set; }
            public string POSDataSetVersion { get; set; }
            public string POSCode { get; set; }
            public string POSName { get; set; }
            public string POSDTAB { get; set; }
            public string POSPrice { get; set; }
            public string POSItemKey { get; set; }
            public string POSItemStatus { get; set; }
            public string SyncAction { get; set; }
            public string SelectAction { get; set; }
            public string CreatedOn { get; set; }
            public string LastRecordedSale { get; set; }
            public string AuditKey { get; set; }
            public int AuditOrder { get; set; }
        }

        public class POSVerificationGroup
        {
            public string AuditKey { get; set; }
            public string Brand { get; set; }
            public string POSCode { get; set; }

            public string POSItemKey { get; set; }
            public POSVerificationItem NewUpdate { get; set; }
            public POSVerificationItem Current { get; set; }
            public List<POSVerificationItem> Others { get; set; } = new List<POSVerificationItem>();
            public string SelectedAction { get; set; } = "Update";

            // Computed properties
            public bool HasPOSDeleted => Others.Any(o => o.POSItemStatus == "POSDeleted");
            public POSVerificationItem POSDeletedItem => Others.FirstOrDefault(o => o.POSItemStatus == "POSDeleted");
        public decimal NewPrice => decimal.TryParse(NewUpdate?.POSPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) ? price : 0;
        public decimal CurrentPrice => decimal.TryParse(Current?.POSPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) ? price : 0;
        public DateTime? LastSaleDate => DateTime.TryParse(Current?.LastRecordedSale, out var date) ? date : null;
            public string Status => NewUpdate?.POSItemStatus ?? Current?.POSItemStatus ?? "";
        }

        public class POSVerificationSummary
        {
            public int TotalGroups { get; set; }
            public int UpdateActions { get; set; }
            public int CreateNewActions { get; set; }
            public int ItemsWithPOSDeleted { get; set; }
            public DateTime GeneratedAt { get; set; } = DateTime.Now;
        }

        //public class SubmitActionsRequest
        //{
        //    public List<ActionSelection> Actions { get; set; } = new List<ActionSelection>();
        //    public string EmailAddress { get; set; } = "";
        //}

        public class ActionSelection
        {
            public string AuditKey { get; set; }
            public string Brand { get; set; }
            public string POSCode { get; set; }
            public string Action { get; set; }
            public string POSItemKey { get; set; }
    }

    


 
        public class SubmitActionsRequest
        {
            public List<ActionSelection> Actions { get; set; } = new List<ActionSelection>();
            public string EmailAddress { get; set; } = "";
            public string SubmittedBy { get; set; } = "";
        }

       

         
    public class ActionUpdateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TotalItemsUpdated { get; set; }
        public int UpdateActions { get; set; }
        public int CreateActions { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public Guid SessionId { get; set; } // Add this for rollback tracking
    }

}





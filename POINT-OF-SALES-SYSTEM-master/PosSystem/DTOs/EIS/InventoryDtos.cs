using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PosSystem.DTOs.EIS
{
    [DataContract]
    public class InventoryItemDto
    {
        [DataMember(Name = "productCode")]
        public string ProductCode { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "quantity")]
        public decimal Quantity { get; set; }

        [DataMember(Name = "unitCost")]
        public decimal UnitCost { get; set; }
    }

    [DataContract]
    public class InventoryAdjustmentRequest
    {
        [DataMember(Name = "referenceNo")]
        public string ReferenceNo { get; set; }

        [DataMember(Name = "items")]
        public List<InventoryItemDto> Items { get; set; }
    }

    [DataContract]
    public class InventoryResponseData
    {
        [DataMember(Name = "referenceNo")]
        public string ReferenceNo { get; set; }

        [DataMember(Name = "status")]
        public string Status { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }
    }
}

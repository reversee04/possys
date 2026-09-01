using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PosSystem.DTOs.EIS
{
    [DataContract]
    public class SubmitSalesRequest
    {
        [DataMember(Name = "invoiceNumber")]
        public string InvoiceNumber { get; set; }

        [DataMember(Name = "transactionDate")]
        public DateTime TransactionDate { get; set; }

        [DataMember(Name = "tin")]
        public string Tin { get; set; }

        [DataMember(Name = "totalAmount")]
        public decimal TotalAmount { get; set; }

        [DataMember(Name = "items")]
        public List<SaleItemDto> Items { get; set; }
    }

    [DataContract]
    public class SaleItemDto
    {
        [DataMember(Name = "productCode")]
        public string ProductCode { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "quantity")]
        public decimal Quantity { get; set; }

        [DataMember(Name = "unitPrice")]
        public decimal UnitPrice { get; set; }

        [DataMember(Name = "taxAmount")]
        public decimal TaxAmount { get; set; }
    }
}

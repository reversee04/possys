using System.Runtime.Serialization;

namespace PosSystem.DTOs.EIS
{
    [DataContract]
    public class SubmitSalesResponseData
    {
        [DataMember(Name = "invoiceNumber")]
        public string InvoiceNumber { get; set; }

        [DataMember(Name = "mraReference")]
        public string MraReference { get; set; }

        [DataMember(Name = "validationData")]
        public string ValidationData { get; set; }

        [DataMember(Name = "qrData")]
        public string QrData { get; set; }
    }
}

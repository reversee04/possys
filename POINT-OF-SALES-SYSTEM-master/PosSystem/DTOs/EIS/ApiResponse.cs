using System.Runtime.Serialization;

namespace PosSystem.DTOs.EIS
{
    [DataContract]
    public class ApiResponse<T>
    {
        [DataMember(Name = "statusCode")]
        public int StatusCode { get; set; }

        [DataMember(Name = "remark")]
        public string Remark { get; set; }

        [DataMember(Name = "data")]
        public T Data { get; set; }
    }
}

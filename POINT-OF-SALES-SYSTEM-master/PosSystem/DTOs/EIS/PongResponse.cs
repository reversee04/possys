using System.Runtime.Serialization;

namespace PosSystem.DTOs.EIS
{
    [DataContract]
    public class PongResponse
    {
        [DataMember(Name = "message")]
        public string Message { get; set; }
    }
}

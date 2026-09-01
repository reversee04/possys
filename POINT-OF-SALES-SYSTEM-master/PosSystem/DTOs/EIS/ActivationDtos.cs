using System.Runtime.Serialization;

namespace PosSystem.DTOs.EIS
{
    [DataContract]
    public class ActivationRequest
    {
        [DataMember(Name = "tac")]
        public string Tac { get; set; }

        [DataMember(Name = "terminalId")]
        public string TerminalId { get; set; }
    }

    [DataContract]
    public class ActivationResponse
    {
        [DataMember(Name = "terminalToken")]
        public string TerminalToken { get; set; }

        [DataMember(Name = "terminalConfig")]
        public object TerminalConfig { get; set; }
    }

    [DataContract]
    public class ActivationConfirmationRequest
    {
        [DataMember(Name = "terminalId")]
        public string TerminalId { get; set; }

        [DataMember(Name = "confirmationReference")]
        public string ConfirmationReference { get; set; }
    }
}

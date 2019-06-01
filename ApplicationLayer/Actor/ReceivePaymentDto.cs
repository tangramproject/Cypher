using System;
using System.ComponentModel.DataAnnotations;
using TangramCypher.ApplicationLayer.Wallet;
using TangramCypher.Model;

namespace TangramCypher.ApplicationLayer.Actor
{
    public class ReceivePaymentDto
    {
        public CredentialsDto Credentials { get; set; }
        [Required]
        public string FromAddress { get; set; }
        public MessageDto RedemptionMessage { get; set; }
    }
}

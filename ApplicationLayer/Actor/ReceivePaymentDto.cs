using System;
using System.ComponentModel.DataAnnotations;
using TangramCypher.ApplicationLayer.Wallet;

namespace TangramCypher.ApplicationLayer.Actor
{
    public class ReceivePaymentDto
    {
        public CredentialsDto Credentials { get; set; }
        [Required]
        public string FromAddress { get; set; }
        public NotificationDto RedemptionMessage { get; set; }
    }
}

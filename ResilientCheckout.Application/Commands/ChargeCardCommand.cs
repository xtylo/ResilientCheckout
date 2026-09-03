using ResilientCheckout.Domain.Payments;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResilientCheckout.Application.Commands
{
    public class ChargeCardCommand
    {
        public int OrderId { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public string CardOwner { get; set; } = string.Empty;
        public string ExpirationDate { get; set; } = string.Empty;
        public int SecurityCode { get; set; }

        public ChargeInstruction ToChargeInstruction() => new()
        {
            OrderId = OrderId
        };
    }
}

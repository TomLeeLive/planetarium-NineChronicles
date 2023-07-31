using System.Text.Json.Serialization;

namespace NineChronicles.ExternalServices.ArenaService.Runtime.Models
{
    public class ArenaInfoSchema
    {
        [JsonPropertyName("championship")]
        public int Championship { get; set; }

        [JsonPropertyName("round")]
        public int Round { get; set; }

        [JsonPropertyName("addr")]
        public string Addr { get; set; }

        [JsonPropertyName("win")]
        public int Win { get; set; }

        [JsonPropertyName("lose")]
        public int Lose { get; set; }

        [JsonPropertyName("ticket")]
        public int Ticket { get; set; }

        [JsonPropertyName("ticket_reset_count")]
        public int TicketResetCount { get; set; }

        [JsonPropertyName("purchased_ticket_count")]
        public int PurchasedTicketCount { get; set; }
    }
}

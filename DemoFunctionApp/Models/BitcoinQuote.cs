﻿using System.Text.Json.Serialization;

namespace DemoFunctionApp.Models
{
    public class BitcoinQuote
    {
        [JsonPropertyName("c")]
        public decimal CurrentPrice { get; set; }

        [JsonPropertyName("d")]
        public decimal Change { get; set; }

        [JsonPropertyName("dp")]
        public decimal PercentChange { get; set; }

        [JsonPropertyName("h")]
        public decimal HighPrice { get; set; }

        [JsonPropertyName("l")]
        public decimal LowPrice { get; set; }

        [JsonPropertyName("o")]
        public decimal OpenPrice { get; set; }

        [JsonPropertyName("pc")]
        public decimal PreviousClose { get; set; }

        [JsonPropertyName("t")]
        public long Timestamp { get; set; }

    }
}

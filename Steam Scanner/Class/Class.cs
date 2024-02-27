using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SteamScanner
{
    public partial class Program
    {
        public class IDefault
        {
            #region App

            public class IApp
            {
                public uint ID { get; set; }

                [JsonIgnore]
                public decimal Price { get; set; }

                public string Default { get; set; }

                public List<IValue> ValueList { get; set; } = new List<IValue>();

                public decimal TradingCard() => ValueList.Where(x => x.Type == IValue.EType.TradingCard).Sum(x => x.Price);

                [JsonIgnore]
                public decimal Margin
                {
                    get => ValueList.Count > 0
                        ? Math.Round(ValueList.Sum(x => x.Margin), 2)
                        : 0m;
                }

                [JsonIgnore]
                public uint Success { get; set; }

                [JsonIgnore]
                public byte Retry { get; set; } = 1;

                public IApp(uint ID)
                {
                    this.ID = ID;
                }
            }

            #endregion

            public IApp App { get; set; }

            public IDefault(IApp App)
            {
                this.App = App;
            }

            #region Bundle

            public class IBundle
            {
                public uint ID { get; set; }

                public string Type { get; set; }

                [JsonIgnore]
                public decimal Price { get; set; }

                public string Default { get; set; }

                public List<IApp> AppList { get; set; } = new List<IApp>();

                [JsonIgnore]
                public List<ISub> SubList { get; set; } = new List<ISub>();

                #region Sub

                public class ISub
                {
                    public uint ID { get; set; }

                    [JsonIgnore]
                    public uint Success { get; set; }

                    public ISub(uint ID)
                    {
                        this.ID = ID;
                    }
                }

                #endregion

                public IBundle(uint ID, string Type)
                {
                    this.ID = ID;
                    this.Type = Type;
                }
            }

            #endregion

            public IBundle Bundle { get; set; }

            public IDefault(IBundle Bundle)
            {
                this.Bundle = Bundle;
            }
        }

        public class IValue
        {
            [JsonIgnore]
            public uint Success { get; set; }

            [JsonIgnore]
            public byte Retry { get; set; } = 1;

            public string Name { get; set; }

            [JsonIgnore]
            public string HashName { get; set; }

            [JsonIgnore]
            public decimal Price { get; set; }

            public string Default { get; set; }

            [JsonIgnore]
            public decimal Average { get; set; }

            [JsonIgnore]
            public decimal Margin { get; set; }

            public uint Quantity { get; set; }

            public uint Sale { get; set; }

            [JsonIgnore]
            public string ClassID { get; set; }

            public enum EType
            {
                Unknown,
                FoilTradingCard,
                TradingCard,
                ProfileBackground,
                Emoticon
            }

            [JsonIgnore]
            public EType Type { get; set; }

            public enum ERare
            {
                Common,
                Uncommon,
                Rare
            }

            [JsonIgnore]
            public ERare Rare { get; set; }
        }

        public class ISteamInventoryHelper
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("data")]
            public List<IData> Data { get; set; }

            public class IData
            {
                [JsonProperty("appid")]
                public uint ID { get; set; }
            }
        }

        public class IBadge
        {
            [JsonProperty("response")]
            public IResponse Response { get; set; }

            public class IResponse
            {
                [JsonProperty("badges")]
                public List<IBadges> Badges { get; set; }

                public class IBadges
                {
                    [JsonProperty("appid")]
                    public uint AppID { get; set; }
                }
            }
        }

        public class IApp
        {
            [JsonProperty]
            public Dictionary<uint, string> Result { get; set; }

            [JsonProperty]
            public string Message { get; set; }

            [JsonProperty]
            public bool Success { get; set; }
        }

        public class IAppPrice
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("data")]
            public object Data { get; set; }

            public class Response
            {
                [JsonProperty("price_overview")]
                public IPriceOverview Overview { get; set; }

                public class IPriceOverview
                {
                    [JsonProperty("final_formatted")]
                    public string Price { get; set; }
                }
            }
        }

        public class IRender
        {
            [JsonProperty("results")]
            public List<IValue> Value { get; set; }

            public class IValue
            {
                [JsonProperty("name")]
                public string Name { get; set; }

                [JsonProperty("hash_name")]
                public string HashName { get; set; }

                [JsonProperty("sell_listings")]
                public uint Quantity { get; set; }

                [JsonProperty("sell_price")]
                public decimal? Price { get; set; }

                public class IAsset
                {
                    [JsonProperty("classid")]
                    public string ClassID { get; set; }

                    [JsonProperty("type")]
                    public string Type { get; set; }
                }

                [JsonProperty("asset_description")]
                public IAsset Asset { get; set; }
            }
        }

        public class IPriceHistory
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("prices")]
            public List<List<string>> List { get; set; }
        }
    }
}

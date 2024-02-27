using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace SteamScanner
{
    public class IConfig
    {
        [JsonIgnore]
        private static string File { get; set; }

        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

        [JsonProperty]
        public int Thread { get; set; } = 1;

        public bool ShouldSerializeThread() => Thread != 1;

        [JsonProperty]
        public uint After { get; set; } = 25;

        public bool ShouldSerializeAfter() => After != 25;

        [JsonProperty("Max Trading Card Price")]
        public uint MaxTradingCardPrice { get; set; } = 0;

        public bool ShouldSerializeMaxTradingCardPrice() => MaxTradingCardPrice > 0;

        [JsonIgnore]
        public uint MaxStorePage { get; set; } = 15;

        [JsonProperty("Min Level Badge")]
        public uint MinLevelBadge { get; set; } = 5;

        public bool ShouldSerializeMinLevelBadge() => MinLevelBadge < 5;

        public enum EMedian
        {
            Month,
            Day
        }

        [JsonProperty]
        public EMedian Median { get; set; } = EMedian.Month;

        public bool ShouldSerializeMedian() => Median != EMedian.Month;

        [JsonProperty]
        public int Duration { get; set; } = 1;

        public bool ShouldSerializeDuration() => Duration != 1;

        public enum ECurrency : byte
        {
            RUB = 5,
            USD = 1,
            TRY = 17
        }

        [JsonProperty]
        public ECurrency Currency { get; set; } = ECurrency.USD;

        public bool ShouldSerializeCurrency() => Currency != ECurrency.USD;

        [JsonProperty]
        public CultureInfo Culture { get; set; } = CultureInfo.GetCultureInfo("en-US");

        public bool ShouldSerializeCulture() => Culture != CultureInfo.GetCultureInfo("en-US");

        [JsonProperty]
        public string SteamID { get; set; }

        public bool ShouldSerializeSteamID() => !string.IsNullOrEmpty(SteamID);

        public class IASF
        {
            [JsonProperty]
            public string IP { get; set; }

            public bool ShouldSerializeIP() => !string.IsNullOrEmpty(IP);

            [JsonProperty]
            public string Index { get; set; }

            public bool ShouldSerializeIndex() => !string.IsNullOrEmpty(Index);

            [JsonProperty]
            public string Password { get; set; }

            public bool ShouldSerializePassword() => !string.IsNullOrEmpty(Password);
        }

        [JsonProperty]
        public IASF ASF { get; set; } = new IASF();

        public bool ShouldSerializeASF() => ASF.ShouldSerializeIP() || ASF.ShouldSerializeIndex() || ASF.ShouldSerializePassword();

        public class ICustom
        {
            public bool Any { get; set; }

            public List<string> List { get; set; } = new List<string>();
        }

        [JsonIgnore]
        public ICustom Custom { get; set; } = new ICustom();

        public enum ECookie
        {
            Community,
            Store
        }

        public partial class ICookie
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }

            [JsonProperty("path")]
            public string Path { get; set; }

            [JsonProperty("domain")]
            public string Domain { get; set; }
        }

        [JsonIgnore]
        public Dictionary<ECookie, List<ICookie>> Cookie { get; set; } = new Dictionary<ECookie, List<ICookie>>();

        public static (string ErrorMessage, IConfig Config) Load(string Directory, string _File)
        {
            if (!string.IsNullOrEmpty(Directory) && !System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.CreateDirectory(Directory);
            }

            File = _File;

            if (!string.IsNullOrEmpty(File) && !System.IO.File.Exists(File))
            {
                System.IO.File.WriteAllText(File, JsonConvert.SerializeObject(new IConfig(), Formatting.Indented));
            }

            string Json;

            try
            {
                Json = System.IO.File.ReadAllText(File);
            }
            catch (Exception e)
            {
                return (e.Message, null);
            }

            if (string.IsNullOrEmpty(Json) || Json.Length == 0)
            {
                return ("Данные равны нулю!", null);
            }

            IConfig Config;

            try
            {
                Config = JsonConvert.DeserializeObject<IConfig>(Json);
            }
            catch (Exception e)
            {
                return (e.Message, null);
            }

            if (Config == null)
            {
                return ("Глобальный конфиг равен нулю!", null);
            }

            Config.Save();

            return (null, Config);
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(File) || (this == null)) return;

            string JSON = JsonConvert.SerializeObject(this, Formatting.Indented);
            string _ = File + ".new";

            Semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                System.IO.File.WriteAllText(_, JSON);

                if (System.IO.File.Exists(File))
                {
                    System.IO.File.Replace(_, File, null);
                }
                else
                {
                    System.IO.File.Move(_, File);
                }
            }
            catch
            {

            }
            finally
            {
                Semaphore.Release();
            }
        }
    }
}

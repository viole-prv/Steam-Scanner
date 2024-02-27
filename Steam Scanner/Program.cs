using HtmlAgilityPack;
using MoreLinq;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using Viole_Logger_Interactive;

namespace SteamScanner
{
    partial class Program
    {
        private readonly static Logger Logger = new Logger();

        public static IConfig Config;

        private static readonly string ConfigDirectory = "config";
        private static readonly string ConfigFile = Path.Combine(ConfigDirectory, "config.json");

        public static List<IDefault> Default = new List<IDefault>();

        private static readonly object Lock = new object();

        public enum ETag : byte
        {
            Any,
            Anime
        }

        private static ETag Tag = ETag.Any;

        public enum EAction : byte
        {
            None,
            Badge,
            TradingCard,
            Background,
            Emoticon,
            Bundle
        }

        private static EAction Action = EAction.None;

        public static void Main()
        {
            Console.Title = string.Empty;

            _ = Init();

            Console.ReadLine();
        }

        private static async Task Init()
        {
            (string ErrorMessage, IConfig Config) = IConfig.Load(ConfigDirectory, ConfigFile);

            if (Config == null)
            {
                Logger.LogGenericWarning(ErrorMessage);

                return;
            }
            else
            {
                Program.Config = Config;
            }

            Currency();

        Init:

            Console.Clear();
            Console.WriteLine("\n\n");

            int Case = -1;

            if (Action > 0)
            {
                var Selection = new List<string>
                {
                    "Настройки",
                    "Старт"
                };

                Case = Support.Table(">", Selection, Position: Console.CursorTop - 1);
            }

            switch (Case)
            {
                case -1:
                    Console.Title = string.Empty;

                    Console.Clear();
                    Console.WriteLine("\n\n");

                    int bAction = Support.Table(">", new List<string>
                    {
                        "Дешевые значки",
                        "Карты",
                        "Фон",
                        "Смайлик",
                        "Наборы с карточками"
                    }, false, Console.CursorTop - 1);

                    if (bAction > -1)
                    {
                        Action = (EAction)bAction + 1;

                        Console.Title = $" $ {(Action == EAction.TradingCard ? "Trading Card" : Action.ToString().ToUpper())}";
                    }

                    goto Init;

                case 0:

                Retry:

                    Console.Clear();
                    Console.WriteLine("\n\n");

                    var Selection = new List<string>
                    {
                        "Стим"
                    };

                    if (Action == EAction.Badge || Action == EAction.Background || Action == EAction.Emoticon || (!Config.Custom.Any && (Action == EAction.TradingCard || Action == EAction.Bundle)))
                    {
                        Selection.Add("Магазин");
                    }

                    Selection.Add("Программа");

                    Case = Support.Table(">", Selection, Position: Console.CursorTop - 1);

                    if (Case == -1) goto Init;

                    switch (Selection[Case])
                    {
                        case "Стим":
                            Console.Clear();
                            Console.WriteLine("\n\n");

                            Selection = new List<string>();

                            if (Action == EAction.TradingCard || Action == EAction.Background || Action == EAction.Emoticon || Action == EAction.Bundle)
                            {
                                Selection.Add($"Валюта ({Config.Currency.ToString().ToUpper()})");
                            }

                            if (Action == EAction.TradingCard || Action == EAction.Background || Action == EAction.Emoticon)
                            {
                                Selection.Add($"ASF IP-Адрес ({(string.IsNullOrEmpty(Config.ASF.IP) ? "FALSE" : Config.ASF.IP)})");
                                Selection.Add($"ASF Индекс ({(string.IsNullOrEmpty(Config.ASF.Index) ? "FALSE" : Config.ASF.Index)})");
                                Selection.Add($"ASF Пароль ({(string.IsNullOrEmpty(Config.ASF.Password) ? "FALSE" : Config.ASF.Password)})");
                            }

                            if (Action == EAction.Badge)
                            {
                                Selection.Add($"Идентификатор ({(string.IsNullOrEmpty(Config.SteamID) ? "FALSE" : Config.SteamID)})");
                            }

                            Selection.Add($"Доступ в аккаунт [COMMUNITY] ({(Config.Cookie.Any(x => x.Key == IConfig.ECookie.Community) ? "TRUE" : "FALSE")})");

                            if (Action == EAction.TradingCard || Action == EAction.Background || Action == EAction.Emoticon || Action == EAction.Bundle)
                            {
                                Selection.Add($"Доступ в аккаунт [STORE] ({(Config.Cookie.Any(x => x.Key == IConfig.ECookie.Store) ? "TRUE" : "FALSE")})");
                            }

                            Case = Support.Table(">", Selection, Position: Console.CursorTop - 1);

                            if (Case == -1) goto Retry;

                            Console.Clear();

                            if (Selection[Case].Contains("Валюта"))
                            {
                                var List = Enum
                                    .GetValues(typeof(IConfig.ECurrency))
                                    .Cast<IConfig.ECurrency>()
                                    .ToList();

                                Console.Clear();
                                Console.WriteLine("\n\n");

                                Case = Support.Table(">", List, Position: Console.CursorTop - 1);

                                if (Case == -1) goto case "Стим";

                                Console.Clear();

                                Config.Currency = List[Case];

                                Currency();
                            }
                            else if (Selection[Case].Contains("Идентификатор"))
                            {
                                Console.Write("Значение идентификатора: ");
                                string Line = Console.ReadLine();

                                if (!string.IsNullOrEmpty(Line))
                                {
                                    Config.SteamID = Line.Trim();
                                    Config.Save();
                                }
                            }
                            else if (Selection[Case].Contains("ASF IP-Адрес"))
                            {
                                Console.Write("ASF IP-Адрес: ");
                                string Line = Console.ReadLine();

                                if (!string.IsNullOrEmpty(Line))
                                {
                                    if (Line[Line.Length - 1] == '/' && Regex.IsMatch(Line[Line.Length - 2].ToString(), @"^\d+$"))
                                    {
                                        Line = Line.Remove(Line.Length - 1);
                                    }

                                    Config.ASF.IP = Line.Trim();
                                    Config.Save();
                                }
                            }
                            else if (Selection[Case].Contains("ASF Индекс"))
                            {
                                Console.Write("ASF Индекс: ");
                                string Line = Console.ReadLine();

                                if (!string.IsNullOrEmpty(Line))
                                {
                                    Config.ASF.Index = Line.Trim();
                                    Config.Save();
                                }
                            }
                            else if (Selection[Case].Contains("ASF Пароль"))
                            {
                                Console.Write("ASF Пароль: ");
                                string Line = Console.ReadLine();

                                if (!string.IsNullOrEmpty(Line))
                                {
                                    Config.ASF.Password = Line.Trim();
                                    Config.Save();
                                }
                            }
                            else if (Selection[Case].Contains("Доступ в аккаунт [COMMUNITY]"))
                            {
                                Config.Cookie.Remove(IConfig.ECookie.Community);

                                Console.Write("Значение доступа в аккаунт Steam: ");

                                var Thread = new Thread(() =>
                                {
                                    string _ = Clipboard.GetText();

                                    if (Logger.Helper.IsValidJson(_))
                                    {
                                        var Cookie = JsonConvert.DeserializeObject<List<IConfig.ICookie>>(_);

                                        if (Cookie != null && Cookie.Count > 0)
                                        {
                                            Config.Cookie.Add(IConfig.ECookie.Community, Cookie);
                                            Config.Save();
                                        }
                                    }
                                });

                                Thread.SetApartmentState(ApartmentState.STA);
                                Thread.Start();
                                Thread.Join();
                            }
                            else if (Selection[Case].Contains("Доступ в аккаунт [STORE]"))
                            {
                                Config.Cookie.Remove(IConfig.ECookie.Store);

                                Console.Write("Значение доступа в аккаунт Steam: ");

                                var Thread = new Thread(() =>
                                {
                                    string _ = Clipboard.GetText();

                                    if (Logger.Helper.IsValidJson(_))
                                    {
                                        var Cookie = JsonConvert.DeserializeObject<List<IConfig.ICookie>>(_);

                                        if (Cookie != null && Cookie.Count > 0)
                                        {
                                            Config.Cookie.Add(IConfig.ECookie.Store, Cookie);
                                            Config.Save();
                                        }
                                    }
                                });

                                Thread.SetApartmentState(ApartmentState.STA);
                                Thread.Start();
                                Thread.Join();
                            }

                            goto case "Стим";

                        case "Магазин":
                            Console.Clear();
                            Console.WriteLine("\n\n");

                            Selection = new List<string>();

                            if (Action == EAction.Badge)
                            {
                                Selection.Add($"Цена всех карточек ({Config.MaxTradingCardPrice})");
                            }

                            if (!Config.Custom.Any && (Action == EAction.TradingCard || Action == EAction.Background || Action == EAction.Emoticon || Action == EAction.Bundle))
                            {
                                Selection.Add($"Количество страниц в магазине ({Config.MaxStorePage})");
                            }

                            if (Action == EAction.Background || Action == EAction.Emoticon)
                            {
                                Selection.Add($"Уровень значка ({Config.MinLevelBadge})");
                            }

                            Case = Support.Table(">", Selection, Position: Console.CursorTop - 1);

                            if (Case == -1) goto Retry;

                            Console.Clear();

                            if (Selection[Case].Contains("Цена всех карточек"))
                            {
                                Console.Write("Максимальная цена всех карточек: ");
                                string Line = Console.ReadLine();

                                if (!string.IsNullOrEmpty(Line) && uint.TryParse(Line, out uint X) && X > 0)
                                {
                                    Config.MaxTradingCardPrice = X;
                                    Config.Save();
                                }
                            }
                            else if (Selection[Case].Contains("Количество страниц в магазине"))
                            {
                                Console.Write("Максимальное количество страниц в магазине: ");
                                string Line = Console.ReadLine();

                                if (!string.IsNullOrEmpty(Line) && uint.TryParse(Line, out uint X) && X > 0)
                                {
                                    Config.MaxStorePage = X;
                                    Config.Save();
                                }
                            }
                            else if (Selection[Case].Contains("Уровень значка"))
                            {
                                Console.Write("Минимальный левел крафта значка: ");
                                string Line = Console.ReadLine();

                                if (!string.IsNullOrEmpty(Line) && uint.TryParse(Line, out uint X) && X > 0)
                                {
                                    Config.MinLevelBadge = X;
                                    Config.Save();
                                }
                            }

                            goto case "Магазин";

                        case "Программа":
                            Console.Clear();
                            Console.WriteLine("\n\n");

                            Selection = new List<string>
                            {
                                $"Время ожидание одного запроса ({Config.After})",
                                $"Количество потоков ({Config.Thread})",
                                $"Пользовательский контроль ({Config.Custom.Any.ToString().ToUpper()})",
                            };

                            if (Action == EAction.TradingCard || Action == EAction.Bundle)
                            {
                                Selection.Add($"Формат среднего значения ({(Config.Median == IConfig.EMedian.Month ? "Месяц" : Config.Median == IConfig.EMedian.Day ? "День" : "FALSE")})");
                                Selection.Add($"Продолжительность среднего значения ({Config.Duration})");
                            }

                            Case = Support.Table(">", Selection, Position: Console.CursorTop - 1);

                            if (Case == -1) goto Retry;

                            Console.Clear();

                            if (Selection[Case].Contains("Время ожидание одного запроса"))
                            {
                                Console.Write("Максимальное время ожидание одного запроса: ");
                                string Line = Console.ReadLine();

                                if (!string.IsNullOrEmpty(Line) && uint.TryParse(Line, out uint X) && X > 0)
                                {
                                    Config.After = X;
                                    Config.Save();
                                }
                            }
                            else if (Selection[Case].Contains("Количество потоков"))
                            {
                                Console.Write("Максимальное количество потоков: ");
                                string Line = Console.ReadLine();

                                if (!string.IsNullOrEmpty(Line) && int.TryParse(Line, out int X) && X > 0)
                                {
                                    Config.Thread = X;
                                    Config.Save();
                                }
                            }
                            else if (Selection[Case].Contains("Пользовательский контроль"))
                            {
                                Config.Custom.Any = !Config.Custom.Any;
                            }
                            else if (Selection[Case].Contains("Формат среднего значения"))
                            {
                                var List = Enum
                                    .GetValues(typeof(IConfig.EMedian))
                                    .Cast<IConfig.EMedian>()
                                    .ToList();

                                Console.WriteLine("\n\n");

                                Case = Support.Table(">", List, Position: Console.CursorTop - 1);

                                if (Case == -1) goto case "Программа";

                                Console.Clear();

                                Config.Median = List[Case];
                                Config.Save();
                            }
                            else if (Selection[Case].Contains("Продолжительность среднего значения"))
                            {
                                Console.Write("Продолжительность среднего значения: ");
                                string Line = Console.ReadLine();

                                if (!string.IsNullOrEmpty(Line) && int.TryParse(Line, out int X) && X > 0)
                                {
                                    Config.Duration = X;
                                    Config.Save();
                                }
                            }

                            goto case "Программа";

                        default:
                            goto Init;
                    }

                case 1:
                    Console.Clear();

                    if (Config.Custom.Any)
                    {
                        Console.WriteLine("Список: ");

                        while (Support.Read(out string Line))
                        {
                            if (string.IsNullOrEmpty(Line)) break;

                            Config.Custom.List.Add(Line);
                        }

                        Console.Clear();
                    }

                    if (Action == EAction.Badge)
                    {
                        if (await GetSteamInventoryHelper())
                        {
                            Console.Clear();

                            if (await GetBadge())
                            {
                                Console.Clear();

                                Overview(AppList);
                            }
                        }
                    }
                    else if (Action == EAction.TradingCard || Action == EAction.Background || Action == EAction.Emoticon)
                    {
                        if (await GetAppList())
                        {
                            Console.Clear();

                            if (await GetProfile())
                            {
                                Console.Clear();

                                await GetPrice(AppList);

                                AppList.RemoveAll(x => x.Price <= 0m);

                                Console.Clear();

                                if (AppList.Count > 0)
                                {
                                    Overview(AppList);
                                }
                            }
                        }
                    }
                    else if (Action == EAction.Bundle)
                    {
                        if (await GetBundleList())
                        {
                            Console.Clear();

                            foreach (var Bundle in BundleList)
                            {
                                await GetPrice(Bundle.AppList);

                                Bundle.AppList.RemoveAll(v => v.Price <= 0m);

                                Console.Clear();
                            }

                            BundleList.RemoveAll(x => x.AppList.Count == 0);

                            if (BundleList.Count > 0)
                            {
                                Overview(BundleList);
                            }
                        }
                    }

                    break;

                default:

                    goto Init;
            }
        }

        public static void Currency()
        {
            var CultureList = CultureInfo
                .GetCultures(CultureTypes.SpecificCultures)
                .Select(x => (x.Name, Region: new RegionInfo(x.Name)))
                .Where(x => x.Region.ISOCurrencySymbol == Config.Currency.ToString())
                .Select(x => CultureInfo.GetCultureInfo(x.Name))
                .ToList();

            if (CultureList.Contains(Config.Culture))
            {

            }
            else
            {
                if (CultureList.Count == 1)
                {
                    Config.Culture = CultureList[0];
                }
                else
                {
                    Console.Clear();
                    Console.WriteLine("\n\n");

                    int Case = Support.Table(">", CultureList, Position: Console.CursorTop - 1);

                    if (Case == -1)
                    {
                        Config.Culture = CultureList[0];
                    }
                    else
                    {
                        Config.Culture = CultureList[Case];
                    }
                }

                Config.Save();
            }

            CultureInfo.CurrentCulture = Config.Culture;
        }

        private static async Task<bool> GetSteamInventoryHelper()
        {
            byte Retry = 0;

            while (++Retry <= 25)
            {
                try
                {
                    Logger.LogGenericDebug($"Начинаю сканировать базу игр ({Retry}/25)");

                    var Client = new RestClient(
                        new RestClientOptions()
                        {
                            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36",
                            MaxTimeout = 300000
                        });

                    var Request = new RestRequest($"https://sih.gainskins.com/api/sih/steam/badges");

                    for (byte i = 0; i < 3; i++)
                    {
                        try
                        {
                            var Execute = await Client.ExecuteGetAsync(Request);

                            if ((int)Execute.StatusCode == 429)
                            {
                                Logger.LogGenericWarning("Слишком много запросов!");

                                await Task.Delay(TimeSpan.FromMinutes(2.5));

                                continue;
                            }

                            if (string.IsNullOrEmpty(Execute.Content))
                            {
                                if (Execute.StatusCode == 0 || Execute.StatusCode == HttpStatusCode.OK)
                                {
                                    Logger.LogGenericWarning("Ответ пуст!");
                                }
                                else
                                {
                                    Logger.LogGenericWarning($"Ошибка: {Execute.StatusCode}.");
                                }
                            }
                            else
                            {
                                if (Execute.StatusCode == 0 || Execute.StatusCode == HttpStatusCode.OK)
                                {
                                    if (Logger.Helper.IsValidJson(Execute.Content))
                                    {
                                        try
                                        {
                                            var JSON = JsonConvert.DeserializeObject<ISteamInventoryHelper>(Execute.Content);

                                            if (JSON == null || !JSON.Success || JSON.Data == null)
                                            {
                                                Logger.LogGenericWarning($"Ошибка: {Execute.Content}.");
                                            }
                                            else
                                            {
                                                AppList = JSON.Data
                                                    .DistinctBy(x => x.ID)
                                                    .Select(x => new IDefault.IApp(x.ID))
                                                    .ToList();

                                                Logger.LogGenericDebug($"В базе было найдено {AppList.Count} игр{(AppList.Count == 1 ? "а" : "")}.");

                                                return true;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Logger.LogGenericException(e);
                                        }
                                    }
                                    else
                                    {
                                        Logger.LogGenericWarning($"Ошибка: {Execute.Content}");
                                    }
                                }
                                else
                                {
                                    Logger.LogGenericWarning($"Ошибка: {Execute.StatusCode}.");
                                }
                            }

                            await Task.Delay(2500);
                        }
                        catch (Exception e)
                        {
                            Logger.LogGenericException(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogGenericException(e);
                }

                await Task.Delay(5000);
            }

            return false;
        }

        private static async Task<bool> GetBadge()
        {
            if (string.IsNullOrEmpty(Config.SteamID))
            {
                return true;
            }

            Logger.LogGenericDebug("Начинаю сканировать Steam значки.");

            try
            {
                var Client = new RestClient(
                    new RestClientOptions()
                    {
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36",
                        MaxTimeout = 300000
                    });

                var Request = new RestRequest($"https://api.steampowered.com/IPlayerService/GetBadges/v1/?key=5F29CD149CCB59631047ED5B12A4E0E1&steamid={Config.SteamID}");

                for (byte i = 0; i < 3; i++)
                {
                    try
                    {
                        var Execute = await Client.ExecuteGetAsync(Request);

                        if ((int)Execute.StatusCode == 429)
                        {
                            Logger.LogGenericWarning("Слишком много запросов!");

                            await Task.Delay(TimeSpan.FromMinutes(2.5));

                            continue;
                        }

                        if (string.IsNullOrEmpty(Execute.Content))
                        {
                            if (Execute.StatusCode == 0 || Execute.StatusCode == HttpStatusCode.OK)
                            {
                                Logger.LogGenericWarning("Ответ пуст!");
                            }
                            else
                            {
                                Logger.LogGenericWarning($"Ошибка: {Execute.StatusCode}.");
                            }
                        }
                        else
                        {
                            if (Execute.StatusCode == 0 || Execute.StatusCode == HttpStatusCode.OK)
                            {
                                if (Logger.Helper.IsValidJson(Execute.Content))
                                {
                                    try
                                    {
                                        var JSON = JsonConvert.DeserializeObject<IBadge>(Execute.Content);

                                        if (JSON == null || JSON.Response == null || JSON.Response.Badges == null || JSON.Response.Badges.Count == 0)
                                        {
                                            Logger.LogGenericWarning($"Ошибка: {Execute.Content}.");
                                        }
                                        else
                                        {
                                            var BadgeIDs = JSON.Response.Badges
                                                .Where(x => x.AppID > 0)
                                                .Select(x => x.AppID).ToList();

                                            AppList.RemoveAll(x => BadgeIDs.Any(v => v == x.ID));

                                            return AppList.Count > 0;
                                        }

                                        break;
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.LogGenericException(e);
                                    }
                                }
                                else
                                {
                                    Logger.LogGenericWarning($"Ошибка: {Execute.Content}");
                                }
                            }
                            else
                            {
                                Logger.LogGenericWarning($"Ошибка: {Execute.StatusCode}.");
                            }
                        }

                        await Task.Delay(2500);
                    }
                    catch (Exception e)
                    {
                        Logger.LogGenericException(e);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogGenericException(e);
            }

            return false;
        }

        private static async Task<bool> GetProfile()
        {
            if (string.IsNullOrEmpty(Config.ASF.IP) || string.IsNullOrEmpty(Config.ASF.Index))
            {
                return true;
            }

            Logger.LogGenericDebug("Начинаю сканировать Steam профиль.");

            try
            {
                var Client = new RestClient(
                    new RestClientOptions()
                    {
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36",
                        MaxTimeout = 300000
                    });

                var Request = new RestRequest($"{Config.ASF.IP}/Api/Annex/{Config.ASF.Index}/App");

                if (!string.IsNullOrEmpty(Config.ASF.Password))
                {
                    Request.AddHeader("Authentication", Config.ASF.Password);
                }

                for (byte i = 0; i < 3; i++)
                {
                    try
                    {
                        var Execute = await Client.ExecuteGetAsync(Request);

                        if ((int)Execute.StatusCode == 429)
                        {
                            Logger.LogGenericWarning("Слишком много запросов!");

                            await Task.Delay(TimeSpan.FromMinutes(2.5));

                            continue;
                        }

                        if (string.IsNullOrEmpty(Execute.Content))
                        {
                            if (Execute.StatusCode == 0 || Execute.StatusCode == HttpStatusCode.OK)
                            {
                                Logger.LogGenericWarning("Ответ пуст!");
                            }
                            else
                            {
                                Logger.LogGenericWarning($"Ошибка: {Execute.StatusCode}.");
                            }
                        }
                        else
                        {
                            if (Execute.StatusCode == 0 || Execute.StatusCode == HttpStatusCode.OK)
                            {
                                try
                                {
                                    var JSON = JsonConvert.DeserializeObject<IApp>(Execute.Content);

                                    if (JSON == null)
                                    {
                                        Logger.LogGenericWarning($"Ошибка: {Execute.Content}.");
                                    }
                                    else
                                    {
                                        if (JSON.Success)
                                        {
                                            if (JSON.Result.Count > 0)
                                            {
                                                var AppIDs = JSON.Result
                                                    .Select(x => x.Key)
                                                    .ToList();

                                                if (AppIDs.Count > 0)
                                                {
                                                    AppList.RemoveAll(x => AppIDs.Any(v => v == x.ID));

                                                    return AppList.Count > 0;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Logger.LogGenericWarning($"Ошибка: {JSON.Message}");
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    Logger.LogGenericException(e);
                                }
                            }
                            else
                            {
                                Logger.LogGenericWarning($"Ошибка: {Execute.StatusCode}.");
                            }
                        }

                        await Task.Delay(2500);
                    }
                    catch (Exception e)
                    {
                        Logger.LogGenericException(e);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogGenericException(e);
            }

            return true;
        }

        #region App

        public static List<IDefault.IApp> AppList = new List<IDefault.IApp>();

        private static async Task<bool> GetAppList()
        {
            try
            {
                if (Config.Custom.Any)
                {
                    if (Config.Custom.List == null || Config.Custom.List.Count == 0)
                    {
                        Logger.LogGenericWarning("Данные отсутствуют.");
                    }
                    else
                    {
                        foreach (string _ in Config.Custom.List)
                        {
                            (uint ID, string Type) = Helper.ToAnalyze(_);

                            if (ID > 0)
                            {
                                AppList.Add(new IDefault.IApp(ID));
                            }
                        }
                    }
                }
                else
                {
                    Console.Clear();
                    Console.WriteLine("\n\n");

                    Tag = (ETag)Support.Table(">", new List<string>
                    {
                        "Все игры",
                        "Только игры с жанром - Аниме"
                    }, false, Console.CursorTop - 1);

                    Console.Clear();

                    Logger.LogGenericDebug("Начинаю сканировать Steam магазин.");

                    string URL = "https://store.steampowered.com/search/?sort_by=Price_ASC&category1=998&category2=29";

                    if (Tag == ETag.Any)
                        URL += "&specials=1";
                    else if (Tag == ETag.Anime)
                        URL += "&specials=1&tags=4085";

                    URL += "&ignore_preferences=1&ndl=1";

                    if (string.IsNullOrEmpty(URL))
                    {
                        Logger.LogGenericWarning("Тип не выбран.");
                    }
                    else
                    {
                        for (uint i = 1; i < (Config.MaxStorePage + 1); i++)
                        {
                            uint Count = 0;

                            var Document = await GetDocument($"{URL}&page={i}", IConfig.ECookie.Store);
                            var Anchor = Document.DocumentNode
                                .SelectNodes("//a")
                                .ToList();

                            foreach (var _ in Anchor)
                            {
                                if (_.Attributes.Contains("href"))
                                {
                                    (uint ID, string Type) = Helper.ToAnalyze(_.Attributes["href"].Value);

                                    if (ID == 0) continue;

                                    AppList.Add(new IDefault.IApp(ID));

                                    ++Count;
                                }
                            }

                            Logger.LogGenericDebug($"Просмотрено страниц: {i}/{Config.MaxStorePage} ({(AppList.Count > 0 ? AppList.Count : 0)})");

                            if (Count == 0) break;

                            await Task.Delay(2500);
                        }
                    }
                }

                if (AppList.Count > 0)
                {
                    AppList = AppList
                        .DistinctBy(x => x.ID)
                        .ToList();

                    Logger.LogGenericDebug($"Найдено всего игр: {AppList.Count}.");

                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.LogGenericException(e);
            }

            return false;
        }

        #endregion

        #region Bundle

        public static List<IDefault.IBundle> BundleList = new List<IDefault.IBundle>();

        private static async Task<bool> GetBundleList()
        {
            try
            {
                if (Config.Custom.Any)
                {
                    if (Config.Custom.List == null || Config.Custom.List.Count == 0)
                    {
                        Logger.LogGenericWarning("Данные отсутствуют.");
                    }
                    else
                    {
                        foreach (string _ in Config.Custom.List)
                        {
                            (uint ID, string Type) = Helper.ToAnalyze(_);

                            if (ID == 0) continue;

                            await Resolve(ID, Type);
                        }
                    }
                }
                else
                {
                    Console.Clear();
                    Console.WriteLine("\n\n");

                    Tag = (ETag)Support.Table(">", new List<string>
                    {
                        "Все игры",
                        "Только игры с жанром - Аниме"
                    }, false, Console.CursorTop - 1);

                    Console.Clear();

                    Logger.LogGenericDebug("Начинаю сканировать Steam магазин.");

                    string URL = "https://store.steampowered.com/search/?sort_by=Price_ASC&category1=996";

                    if (Tag == ETag.Any)
                        URL += "&specials=1";
                    else if (Tag == ETag.Anime)
                        URL += "&specials=1&tags=4085";

                    URL += "&ignore_preferences=1&ndl=1";

                    if (string.IsNullOrEmpty(URL))
                    {
                        Logger.LogGenericWarning("Тип не выбран.");
                    }
                    else
                    {
                        for (uint i = 1; i < (Config.MaxStorePage + 1); i++)
                        {
                            uint NULL = 0;

                            var Document = await GetDocument($"{URL}&page={i}", IConfig.ECookie.Store);
                            var Anchor = Document.DocumentNode
                                .SelectNodes("//a")
                                .ToList();

                            foreach (var _ in Anchor)
                            {
                                if (_.Attributes.Contains("href"))
                                {
                                    (uint ID, string Type) = Helper.ToAnalyze(_.Attributes["href"].Value);

                                    if (ID == 0) continue;

                                    if (await Resolve(ID, Type))
                                    {
                                        ++NULL;
                                    }
                                }
                            }

                            Logger.LogGenericDebug($"Просмотрено страниц: {i}/{Config.MaxStorePage} ({BundleList.Count})");

                            if (NULL == 0) break;

                            await Task.Delay(5000);
                        }
                    }
                }

                async Task<bool> Resolve(uint ID, string Type, IDefault.IBundle Bundle = null)
                {
                    var Dictionary = new Dictionary<uint, string>();
                    var Document = await GetDocument($"https://store.steampowered.com/{Type}/{ID}/", IConfig.ECookie.Store);

                    if (Document == null || Document.DocumentNode.SelectSingleNode(".//div[@class='page_title_area game_title_area']") == null) return false;

                    var Anchor = Document.DocumentNode
                        .SelectNodes("//a")
                        .ToList();

                    foreach (var _ in Anchor
                        .Where(x => x.HasClass("tab_item_overlay"))
                        .Where(x => x.Attributes.Contains("href"))
                        .ToList())
                    {
                        (uint bID, string bType) = Helper.ToAnalyze(_.Attributes["href"].Value);

                        if (bID == 0 || Dictionary.ContainsKey(bID)) continue;

                        Dictionary.Add(bID, bType);
                    }

                    if (Dictionary.Count > 0)
                    {
                        IDefault.IBundle BundleData = Bundle ?? new IDefault.IBundle(ID, Type);

                        foreach (KeyValuePair<uint, string> Pair in Dictionary)
                        {
                            switch (Pair.Value.ToUpper())
                            {
                                case "SUB":
                                    BundleData.SubList.Add(new IDefault.IBundle.ISub(Pair.Key));

                                    break;

                                case "APP":
                                    BundleData.AppList.Add(new IDefault.IApp(Pair.Key));

                                    break;
                            }
                        }

                        if (Bundle == null)
                        {
                            var Price = Document.DocumentNode.SelectSingleNode(".//div[@class='discount_final_price']");

                            if (Price == null) return false;

                            BundleData.Default = Price.InnerText;

                            var _ = Helper.ToPrice(BundleData.Default);

                            if (_ == null) return false;

                            BundleData.Price = _.Value;

                            BundleList.Add(BundleData);
                        }

                        return true;
                    }

                    return false;
                }

                BundleList.RemoveAll(x => x.Price <= 0m);

                if (BundleList.Count > 0)
                {
                    BundleList = BundleList
                        .DistinctBy(x => x.ID)
                        .ToList();

                    Logger.LogGenericDebug($"Найдено всего наборов: {BundleList.Count}.");

                    foreach (var Bundle in BundleList
                        .Where(x => x.SubList.Count > 0)
                        .ToList())
                    {
                        while (Bundle.SubList.Any(x => x.Success == 0))
                        {
                            foreach (var Sub in Bundle.SubList
                                .Where(x => x.Success == 0)
                                .ToList())
                            {
                                if (await Resolve(Sub.ID, "sub", Bundle))
                                {
                                    Sub.Success = 200;
                                }
                            }

                            Bundle.SubList.RemoveAll(x => x.Success == 200);

                            await Task.Delay(2500);
                        }
                    }

                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.LogGenericException(e);
            }

            return false;
        }

        #endregion

        private static async Task GetPrice(List<IDefault.IApp> List)
        {
            try
            {
                var Batch = List
                    .Batch(500)
                    .Select((x, i) => (Value: x, Index: i += 1))
                    .ToList();

                Logger.LogGenericDebug($"Найдено {List.Count} {Lang.Declination(new string[] { "игру", "игры", "игр" }, List.Count)}.");
                Logger.LogGenericDebug($"Обрабатываю {Batch.Count} {Lang.Declination(new string[] { "страница", "страницы", "страниц" }, Batch.Count)}.");

                foreach ((IEnumerable<IDefault.IApp> Enumerable, int Index) in Batch)
                {
                    Logger.LogGenericDebug($"Загружаю {Index} страницу.");

                    try
                    {
                        string URL = $"https://store.steampowered.com/api/appdetails?appids={string.Join(",", Enumerable.Select(x => x.ID.ToString()))}&cc={Config.Culture.TwoLetterISOLanguageName}&filters=price_overview";

                        var Client = new RestClient(
                            new RestClientOptions()
                            {
                                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36",
                                MaxTimeout = 300000
                            });

                        var Request = new RestRequest(URL);

                        for (byte i = 0; i < 3; i++)
                        {
                            try
                            {
                                var Execute = await Client.ExecuteGetAsync(Request);

                                if ((int)Execute.StatusCode == 429)
                                {
                                    Logger.LogGenericWarning("Слишком много запросов!");

                                    await Task.Delay(TimeSpan.FromMinutes(2.5));

                                    continue;
                                }

                                if (string.IsNullOrEmpty(Execute.Content))
                                {
                                    if (Execute.StatusCode == 0 || Execute.StatusCode == HttpStatusCode.OK)
                                    {
                                        Logger.LogGenericWarning("Ответ пуст!");
                                    }
                                    else
                                    {
                                        Logger.LogGenericWarning($"Ошибка: {Execute.StatusCode}.");
                                    }
                                }
                                else
                                {
                                    if (Execute.StatusCode == 0 || Execute.StatusCode == HttpStatusCode.OK)
                                    {
                                        if (Logger.Helper.IsValidJson(Execute.Content))
                                        {
                                            try
                                            {
                                                var JSON = JsonConvert.DeserializeObject<Dictionary<string, IAppPrice>>(Execute.Content);

                                                if (JSON == null)
                                                {
                                                    Logger.LogGenericWarning($"Ошибка: {Execute.Content}.");
                                                }
                                                else
                                                {
                                                    foreach (var App in Enumerable)
                                                    {
                                                        var X = JSON[App.ID.ToString()];

                                                        if (X == null) continue;

                                                        if (X.Success)
                                                        {
                                                            string Data = X.Data.ToString();

                                                            if (Data == "[]") continue;

                                                            var Response = JsonConvert.DeserializeObject<IAppPrice.Response>(Data);

                                                            if (Response == null || Response.Overview == null) continue;

                                                            App.Default = Response.Overview.Price;

                                                            var _ = Helper.ToPrice(App.Default);

                                                            if (_ == null) continue;

                                                            App.Price = _.Value;
                                                        }
                                                    }
                                                }

                                                break;
                                            }
                                            catch (Exception e)
                                            {
                                                Logger.LogGenericException(e);
                                            }
                                        }
                                        else
                                        {
                                            Logger.LogGenericWarning($"Ошибка: {Execute.Content}");
                                        }
                                    }
                                    else
                                    {
                                        Logger.LogGenericWarning($"Ошибка: {Execute.StatusCode}.");
                                    }
                                }

                                await Task.Delay(2500);
                            }
                            catch (Exception e)
                            {
                                Logger.LogGenericException(e);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogGenericException(e);
                    }

                    await Task.Delay(2500);
                }
            }
            catch (Exception e)
            {
                Logger.LogGenericException(e);
            }
        }

        private static Thread[] ThreadList;

        private static void Overview<T>(List<T> List)
        {
            Console.Clear();

            Logger.LogGenericDebug($"Найдено {List.Count} значений.");

            if (List.Count < Config.Thread)
                Config.Thread = List.Count;

            ThreadList = new Thread[Config.Thread];

            for (int i = 0; i < Config.Thread; i++)
            {
                ThreadList[i] = new Thread(new ThreadStart(Overview))
                {
                    IsBackground = true
                };

                ThreadList[i].Start();
            }

            while (true)
            {
                Console.Title = $"QUEUE: {Completed}/{List.Count} | SUCCESS: {Success} | TIME: {(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString("hh\\:mm\\:ss", CultureInfo.InvariantCulture)}";

                if (Completed >= List.Count) break;

                Thread.Sleep(1000);
            }
        }

        private static async Task<HtmlDocument> GetDocument(string URL, IConfig.ECookie Key)
        {
            byte Retry = 0;

            while (++Retry <= 5)
            {
                try
                {
                    var Client = new RestClient(
                        new RestClientOptions()
                        {
                            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36",
                            MaxTimeout = 300000
                        });

                    var Request = new RestRequest(URL);

                    foreach (var Cookie in Config.Cookie
                        .Where(x => x.Key == Key)
                        .SelectMany(x => x.Value)
                        .ToList())
                    {
                        try
                        {
                            Client.AddCookie(Cookie.Name, Cookie.Value, Cookie.Path, Cookie.Domain);
                        }
                        catch { }
                    }

                    for (byte i = 0; i < 3; i++)
                    {
                        try
                        {
                            var Source = new CancellationTokenSource();
                            Source.CancelAfter(TimeSpan.FromSeconds(60));

                            var Execute = await Client.ExecuteGetAsync(Request, Source.Token);

                            if ((int)Execute.StatusCode == 429)
                            {
                                Logger.LogGenericWarning("Слишком много запросов!");

                                await Task.Delay(TimeSpan.FromMinutes(2.5));

                                continue;
                            }

                            if (string.IsNullOrEmpty(Execute.Content))
                            {
                                if (Execute.StatusCode == 0 || Execute.StatusCode == HttpStatusCode.OK)
                                {
                                    Logger.LogGenericWarning("Ответ пуст!");
                                }
                                else
                                {
                                    Logger.LogGenericWarning($"Ошибка: {Execute.StatusCode}.");
                                }
                            }
                            else
                            {
                                if (Execute.StatusCode == 0 || Execute.StatusCode == HttpStatusCode.OK)
                                {
                                    var Document = new HtmlDocument();
                                    Document.LoadHtml(Execute.Content);

                                    return Document;
                                }
                                else
                                {
                                    Logger.LogGenericWarning($"Ошибка: {Execute.StatusCode}.");
                                }
                            }

                            await Task.Delay(2500);
                        }
                        catch (Exception e)
                        {
                            Logger.LogGenericException(e);
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    Logger.LogGenericException(e);
                }
            }

            return null;
        }

        public static int Checked = 0;
        public static int Completed = 0;

        public static int Success = 0;

        public static async void Overview()
        {
            if (Action == EAction.Badge || Action == EAction.TradingCard || Action == EAction.Background || Action == EAction.Emoticon)
            {
                while (Checked < AppList.Count)
                {
                    try
                    {
                        if (Checked > AppList.Count) break;

                        IDefault.IApp App = null;

                        lock (Lock)
                        {
                            App = AppList[Checked];

                            Checked++;
                        }

                        if (App == null) continue;

                        var Logger = new Logger(App.ID.ToString());

                        await Resolve(Logger, App);

                        if (App.ValueList.Count == 0) continue;

                        if (Action == EAction.Badge)
                        {
                            decimal _ = App.TradingCard();

                            if (_ > 0 &&
                                _ >= Config.MaxTradingCardPrice)
                            {
                                Logger.LogGenericInfo("Игру не добавляем в список, так как цена не подходит.");
                            }
                            else
                            {
                                Logger.LogGenericDebug(JsonConvert.SerializeObject(new
                                {
                                    App.ID,
                                    Price = App.Default,
                                    List = App.ValueList.ToDictionary(x => x.Name, x => x.Default)
                                }, Formatting.Indented));

                                lock (Lock)
                                {
                                    Default.Add(new IDefault(App));

                                    Save();
                                }
                            }
                        }
                        else if (Action == EAction.TradingCard)
                        {
                            byte Average = Helper.ToAverage(App.ValueList.Count);

                            bool P = IPrice();

                            if (P)
                            {
                                bool A = await IAverage();

                                if (A)
                                {
                                    Logger.LogGenericDebug(JsonConvert.SerializeObject(new
                                    {
                                        App = new
                                        {
                                            App.ID,
                                            Price = App.Default,
                                            Margin = Helper.ToSummary(App.Margin),
                                            Count = new
                                            {
                                                Value = App.ValueList.Count,
                                                Max = Average
                                            }
                                        },
                                        List =
                                            App.ValueList.ToDictionary(
                                                x => x.Name,
                                                x => new
                                                {
                                                    Price = x.Default,
                                                    x.Average,
                                                    Margin = Helper.ToSummary(x.Margin)
                                                })
                                    }, Formatting.Indented));

                                    lock (Lock)
                                    {
                                        Default.Add(new IDefault(App));

                                        Save();
                                    }
                                }
                            }

                            bool IPrice()
                            {
                                decimal _ = Math.Round(App.ValueList.Average(v => v.Price) * Average, 2);

                                if (_ >= App.Price)
                                {
                                    decimal Sum = Math.Round(App.ValueList.Sum(x => x.Price), 2);
                                    decimal Max = Math.Round(App.ValueList.Max(x => x.Price), 2);
                                    decimal Min = Math.Round(Sum - Max, 2);

                                    if (Max >= Min)
                                    {
                                        Logger.LogGenericInfo($"Игру не сохраняю, так как карты переоценены. ({Max} >= {Min})", "Price");
                                    }
                                    else
                                    {
                                        Logger.LogGenericDebug($"Цена прошла проверку! | Значение: {_} | Сумма: {Sum} | Макс: {Max} | Мин: {Min}", "Price");

                                        return true;
                                    }
                                }
                                else
                                {
                                    Logger.LogGenericInfo($"Игру не сохраняю, так как карты стоят меньше игры. ({_} >= {App.Price})", "Price");
                                }

                                return false;
                            }

                            async Task<bool> IAverage()
                            {
                                await GetPriceHistory(Logger, App.ValueList.ToList());

                                decimal _ = Math.Round(App.ValueList.Average(v => v.Average) * Average, 2);

                                if (_ >= App.Price)
                                {
                                    decimal Sum = Math.Round(App.ValueList.Sum(x => x.Average), 2);
                                    decimal Max = Math.Round(App.ValueList.Max(x => x.Average), 2);
                                    decimal Min = Math.Round(Sum - Max, 2);

                                    if (Max >= Min)
                                    {
                                        Logger.LogGenericInfo($"Игру не сохраняю, так как карты переоценены. ({Max} >= {Min})", "Average");
                                    }
                                    else
                                    {
                                        Logger.LogGenericDebug($"Цена прошла проверку! | Значение: {_} | Сумма: {Sum} | Макс: {Max} | Мин: {Min}", "Average");

                                        return true;
                                    }
                                }
                                else
                                {
                                    Logger.LogGenericInfo($"Игру не сохраняю, так как карты стоят меньше игры. ({_} >= {App.Price})", "Average");
                                }

                                return false;
                            }
                        }
                        else if (Action == EAction.Background)
                        {
                            if (App.ValueList
                                    .Where(x => x.Type == IValue.EType.ProfileBackground)
                                    .Any(x => x.Price >= (App.TradingCard() * Config.MinLevelBadge)))
                            {
                                Logger.LogGenericDebug(JsonConvert.SerializeObject(new
                                {
                                    App.ID,
                                    Price = App.Default,
                                    TradingCard =
                                        App.ValueList
                                            .Where(x => x.Type == IValue.EType.TradingCard)
                                            .ToDictionary(x => x.Name, x => x.Default),
                                    ProfileBackground =
                                        App.ValueList
                                            .Where(x => x.Type == IValue.EType.ProfileBackground)
                                            .ToDictionary(
                                                x => x.Name,
                                                x => new
                                                {
                                                    Price = x.Default,
                                                    x.Rare
                                                })
                                }, Formatting.Indented));


                                lock (Lock)
                                {
                                    Default.Add(new IDefault(App));

                                    Save();
                                }
                            }
                            else
                            {
                                Logger.LogGenericInfo("Игру не сохраняю, так как карты стоят дороже фонов.");
                            }
                        }
                        else if (Action == EAction.Emoticon)
                        {
                            if (App.ValueList
                                    .Where(x => x.Type == IValue.EType.Emoticon)

                                    .Any(x => x.Price >= (App.TradingCard() * Config.MinLevelBadge)))
                            {
                                Logger.LogGenericDebug(JsonConvert.SerializeObject(new
                                {
                                    App.ID,
                                    Price = App.Default,
                                    TradingCard =
                                        App.ValueList
                                            .Where(x => x.Type == IValue.EType.TradingCard)
                                            .ToDictionary(x => x.Name, x => x.Default),
                                    Emoticon =
                                        App.ValueList
                                            .Where(x => x.Type == IValue.EType.Emoticon)
                                            .ToDictionary(
                                                x => x.Name,
                                                x => new
                                                {
                                                    Price = x.Default,
                                                    x.Rare
                                                })
                                }, Formatting.Indented));

                                lock (Lock)
                                {
                                    Default.Add(new IDefault(App));

                                    Save();
                                }
                            }
                            else
                            {
                                Logger.LogGenericInfo("Игру не сохраняю, так как карты стоят дороже смайликов.");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogGenericException(e);
                    }
                    finally
                    {
                        lock (Lock)
                        {
                            ++Completed;
                        }

                        await Task.Delay(1000);
                    }
                }
            }
            else if (Action == EAction.Bundle)
            {
                while (Checked < BundleList.Count)
                {
                    try
                    {
                        if (Checked > BundleList.Count) break;

                        IDefault.IBundle Bundle = null;

                        lock (Lock)
                        {
                            Bundle = BundleList[Checked];

                            Checked++;
                        }

                        if (Bundle == null) continue;

                        var Logger = new Logger(Bundle.ID.ToString());

                        foreach (var App in Bundle.AppList)
                        {
                            await Resolve(Logger, App);

                            await Task.Delay(Config.Thread * 2500);
                        }

                        Bundle.AppList.RemoveAll(v => v.ValueList.Count == 0);

                        if (Math.Round(Bundle.AppList.Sum(x => x.ValueList.Average(v => v.Price) * Helper.ToAverage(x.ValueList.Count)), 2) >= Bundle.Price)
                        {
                            foreach ((IDefault.IApp App, int Index) in Bundle.AppList
                                .Select((x, i) => (Value: x, Index: i))
                                .ToList())
                            {
                                await GetPriceHistory(Logger, App.ValueList);

                                await Task.Delay(Config.Thread * 2500);
                            }

                            if (Math.Round(Bundle.AppList.Sum(x => x.ValueList.Average(v => v.Average) * Helper.ToAverage(x.ValueList.Count)), 2) >= Bundle.Price)
                            {
                                Logger.LogGenericDebug(JsonConvert.SerializeObject(new
                                {
                                    Bundle.ID,
                                    Price = Bundle.Default,
                                    List =
                                        Bundle.AppList
                                            .Select(App => new
                                            {
                                                App.ID,
                                                Price = App.Default,
                                                List = App.ValueList.ToDictionary(
                                                    x => x.Name,
                                                    x => new
                                                    {
                                                        Price = x.Default,
                                                        x.Average,
                                                        Margin = Helper.ToSummary(x.Margin)
                                                    })
                                            })
                                }, Formatting.Indented));

                                lock (Lock)
                                {
                                    Default.Add(new IDefault(Bundle));

                                    Save();
                                }
                            }
                        }
                        else
                        {
                            Logger.LogGenericInfo("Набор не сохраняю, так как карты стоят меньше набора.");
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogGenericException(e);
                    }
                    finally
                    {
                        lock (Lock)
                        {
                            ++Completed;
                        }

                        await Task.Delay(1000);
                    }
                }
            }

            async Task Resolve(Logger Logger, IDefault.IApp App)
            {
                string URL = string.Concat(
                    $"https://steamcommunity.com/market/search/render/?query=&start=0&count=100&norender=1&appid=753&category_753_Game[]=tag_app_{App.ID}",
                    Action == EAction.Background
                        ? "&category_753_item_class[]=tag_item_class_2&category_753_item_class[]=tag_item_class_3" // Background + Trading Card
                        : Action == EAction.Emoticon
                            ? "&category_753_item_class[]=tag_item_class_2&category_753_item_class[]=tag_item_class_4" // Emoticon + Trading Card
                            : "&category_753_item_class[]=tag_item_class_2&category_753_cardborder[]=tag_cardborder_0" // Trading Card
                    );

            Retry:

                while (App.Success == 0)
                {
                    Logger.LogGenericDebug($"URL: {URL}");
                    Logger.LogGenericDebug($"{(App.Price > 0 ? $"Стоимость игры: {App.Price:C} | " : "")}Получаю данные. ({App.Retry}/3)");

                    try
                    {
                        var Client = new RestClient(
                            new RestClientOptions()
                            {
                                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36",
                                MaxTimeout = 300000
                            });

                        var Request = new RestRequest(URL);

                        foreach (var Cookie in Config.Cookie
                            .Where(x => x.Key == IConfig.ECookie.Community)
                            .SelectMany(x => x.Value)
                            .ToList())
                        {
                            try
                            {
                                Client.AddCookie(Cookie.Name, Cookie.Value, Cookie.Path, Cookie.Domain);
                            }
                            catch { }
                        }

                        for (byte i = 0; i < 10; i++)
                        {
                            try
                            {
                                var Source = new CancellationTokenSource();
                                Source.CancelAfter(TimeSpan.FromSeconds(Config.After));

                                var Execute = await Client.ExecuteGetAsync(Request, Source.Token);

                                if ((int)Execute.StatusCode == 429)
                                {
                                    Logger.LogGenericWarning("Слишком много запросов!");

                                    await Task.Delay(TimeSpan.FromMinutes(2.5));

                                    continue;
                                }

                                if (string.IsNullOrEmpty(Execute.Content))
                                {
                                    if (Execute.StatusCode == 0 || Execute.StatusCode == HttpStatusCode.OK)
                                    {
                                        Logger.LogGenericWarning("Ответ пуст!");

                                        await Task.Delay(TimeSpan.FromMinutes(1));

                                        continue;
                                    }
                                    else
                                    {
                                        Logger.LogGenericWarning($"Ошибка: {Execute.StatusCode}.");

                                        App.Success = (uint)Execute.StatusCode;
                                    }
                                }
                                else
                                {
                                    if (Execute.StatusCode == 0 || Execute.StatusCode == HttpStatusCode.OK)
                                    {
                                        if (Logger.Helper.IsValidJson(Execute.Content))
                                        {
                                            try
                                            {
                                                var JSON = JsonConvert.DeserializeObject<IRender>(Execute.Content);

                                                if (JSON == null || JSON.Value == null || JSON.Value.Count == 0)
                                                {
                                                    Logger.LogGenericWarning($"Ошибка: {Execute.Content}.");

                                                    App.Success = 600;
                                                }
                                                else
                                                {
                                                    if (App.ValueList.Count > 0)
                                                    {
                                                        App.ValueList.Clear();
                                                    }

                                                    foreach (var X in JSON.Value)
                                                    {
                                                        if (string.IsNullOrEmpty(X.Name) || X.Price == null || X.Asset == null) continue;

                                                        if (App.ValueList.Count(x => x.ClassID == X.Asset.ClassID) == 0)
                                                        {
                                                            var Value = new IValue
                                                            {
                                                                Success = 1,
                                                                Name = X.Name.TrimStart().TrimEnd(),
                                                                HashName = HttpUtility.UrlEncode(X.HashName.TrimStart().TrimEnd()),
                                                                Price = X.Price.Value,
                                                                Quantity = X.Quantity,
                                                                ClassID = X.Asset.ClassID,
                                                                Type =
                                                                    X.Asset.Type.Contains("Foil Trading Card") ? IValue.EType.FoilTradingCard :
                                                                    X.Asset.Type.Contains("Trading Card") ? IValue.EType.TradingCard :
                                                                    X.Asset.Type.Contains("Profile Background") ? IValue.EType.ProfileBackground :
                                                                    X.Asset.Type.Contains("Emoticon") ? IValue.EType.Emoticon : IValue.EType.Unknown,

                                                            };

                                                            if (Value.Type == IValue.EType.FoilTradingCard || Value.Type == IValue.EType.Unknown) continue;

                                                            if (Action == EAction.Background || Action == EAction.Emoticon)
                                                            {
                                                                Value.Rare =
                                                                    X.Asset.Type.Contains(Value.Type == IValue.EType.ProfileBackground ? "Uncommon Profile Background" : "Uncommon Emoticon") ? IValue.ERare.Uncommon :
                                                                    X.Asset.Type.Contains(Value.Type == IValue.EType.ProfileBackground ? "Rare Profile Background" : "Rare Emoticon") ? IValue.ERare.Rare : IValue.ERare.Common;
                                                            }

                                                            App.ValueList.Add(Value);
                                                        }
                                                    }

                                                    Logger.LogGenericDebug($"Полученно {App.ValueList.Count(x => x.Success == 1)} валидных значений.");

                                                    App.ValueList.ForEach(x => x.Success = 0);

                                                    App.Success = 200;
                                                }

                                                break;
                                            }
                                            catch (Exception e)
                                            {
                                                Logger.LogGenericException(e);
                                            }
                                        }
                                        else
                                        {
                                            Logger.LogGenericWarning($"Ошибка: {Execute.Content}");

                                            App.Success = 600;
                                        }
                                    }
                                    else
                                    {
                                        Logger.LogGenericWarning($"Ошибка: {Execute.StatusCode}.");

                                        App.Success = (uint)Execute.StatusCode;
                                    }
                                }

                                await Task.Delay(2500);
                            }
                            catch (OperationCanceledException) { }
                            catch (Exception e)
                            {
                                Logger.LogGenericException(e);

                                App.Success = 600;
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception e)
                    {
                        Logger.LogGenericException(e);

                        App.Success = 600;
                    }

                    await Task.Delay(Config.Thread * 2500);
                }

                if (App.Success != 200)
                {
                    Logger.LogGenericWarning($"Ошибка: {(App.Success == 429 ? "слишком много запросов" : App.Success == 600 ? "лоты на торговой площадке не найдены" : $"код состояния: {App.Success}")}.");

                    if (++App.Retry <= 3)
                    {
                        await Task.Delay(App.Success == 429
                            ? TimeSpan.FromMinutes(App.Retry * 5)
                            : TimeSpan.FromSeconds(App.Success == 600
                                ? Action == EAction.Bundle ? 2.5 : 5
                                : 25));

                        App.Success = 0;

                        goto Retry;
                    }
                }
            }
        }

        private static async Task GetPriceHistory(Logger Logger, List<IValue> ValueList)
        {
            try
            {
                var Now = DateTime.Now;

                switch (Config.Median)
                {
                    case IConfig.EMedian.Month:
                        Now = Now.AddMonths(-Config.Duration);

                        break;
                    case IConfig.EMedian.Day:
                        Now = Now.AddDays(-Config.Duration);

                        break;
                }

                foreach (var Value in ValueList)
                {
                    string URL = $"https://steamcommunity.com/market/pricehistory?country={Config.Currency}&currency={(byte)Config.Currency}&appid=753&market_hash_name={Value.HashName}";

                Retry:

                    while (Value.Success == 0)
                    {
                        Logger.LogGenericDebug($"URL: {URL}");
                        Logger.LogGenericDebug($"Получаю данные. ({Value.Retry}/3)");

                        try
                        {
                            var Client = new RestClient(
                                new RestClientOptions()
                                {
                                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.159 Safari/537.36",
                                    MaxTimeout = 300000
                                });

                            var Request = new RestRequest(URL);

                            foreach (var Cookie in Config.Cookie
                                .Where(x => x.Key == IConfig.ECookie.Community)
                                .SelectMany(x => x.Value)
                                .ToList())
                            {
                                try
                                {
                                    Client.AddCookie(Cookie.Name, Cookie.Value, Cookie.Path, Cookie.Domain);
                                }
                                catch { }
                            }

                            for (byte i = 0; i < 10; i++)
                            {
                                try
                                {
                                    var Source = new CancellationTokenSource();
                                    Source.CancelAfter(TimeSpan.FromSeconds(Config.After));

                                    var Execute = await Client.ExecuteGetAsync(Request, Source.Token);

                                    if ((int)Execute.StatusCode == 429)
                                    {
                                        Logger.LogGenericWarning("Слишком много запросов!");

                                        await Task.Delay(TimeSpan.FromMinutes(2.5));

                                        continue;
                                    }

                                    if (string.IsNullOrEmpty(Execute.Content))
                                    {
                                        if (Execute.StatusCode == 0 || Execute.StatusCode == HttpStatusCode.OK)
                                        {
                                            Logger.LogGenericWarning("Ответ пуст!");

                                            await Task.Delay(TimeSpan.FromMinutes(1));

                                            continue;
                                        }
                                        else
                                        {
                                            Logger.LogGenericWarning($"Ошибка: {Execute.StatusCode}.");

                                            Value.Success = (uint)Execute.StatusCode;
                                        }
                                    }
                                    else
                                    {
                                        if (Execute.StatusCode == 0 || Execute.StatusCode == HttpStatusCode.OK)
                                        {
                                            if (Logger.Helper.IsValidJson(Execute.Content))
                                            {
                                                try
                                                {

                                                    var JSON = JsonConvert.DeserializeObject<IPriceHistory>(Execute.Content);

                                                    if (JSON == null || !JSON.Success || JSON.List == null || JSON.List.Count == 0)
                                                    {
                                                        Logger.LogGenericWarning($"Ошибка: {Execute.Content}.");

                                                        Value.Success = 600;
                                                    }
                                                    else
                                                    {
                                                        var A = JSON.List
                                                            .Where(x => x.Count > 1)
                                                            .Where(x =>
                                                            {
                                                                return DateTime.TryParseExact(x[0].Replace(": +0", ""), "MMM dd yyyy HH", CultureInfo.InvariantCulture, DateTimeStyles.None, out var V) &&
                                                                V >= Now;
                                                            })
                                                            .Select(x =>
                                                            {
                                                                if (decimal.TryParse(x[1], out decimal V))
                                                                {
                                                                    try
                                                                    {
                                                                        if (x.Count > 2 && uint.TryParse(x[2], NumberStyles.Integer, CultureInfo.CurrentCulture, out uint O))
                                                                        {
                                                                            Value.Sale += O;
                                                                        }
                                                                    }
                                                                    catch (Exception e)
                                                                    {
                                                                        Logger.LogGenericException(e);
                                                                    }

                                                                    return V;
                                                                }

                                                                return -1;
                                                            })
                                                            .Where(x => x > -1)
                                                            .ToList();

                                                        if (A.Count > 0)
                                                        {
                                                            decimal Average = A.Average(x => x);

                                                            Value.Average = Math.Round(Average, 2);
                                                            Value.Margin = Math.Round(((Value.Average - Value.Price) / Value.Price) * 100, 2);

                                                            Logger.LogGenericDebug($"Название: {Value.Name} | Количество: {Value.Quantity} | Продаж: {Value.Sale} | Цена: {Value.Price:C} | Средняя цена за месяц: {Value.Average} | Процентное изменение:{Helper.ToSummary(Value.Margin, true)}");
                                                        }
                                                        else
                                                        {
                                                            Logger.LogGenericDebug($"Название: {Value.Name} | За установленный период продаж не наблюдалось.");
                                                        }

                                                        Value.Success = 200;
                                                    }

                                                    break;
                                                }
                                                catch (Exception e)
                                                {
                                                    Logger.LogGenericException(e);

                                                    Value.Success = 600;
                                                }
                                            }
                                            else
                                            {
                                                Logger.LogGenericWarning($"Ошибка: {Execute.Content}");

                                                Value.Success = 600;
                                            }
                                        }
                                        else
                                        {
                                            Logger.LogGenericWarning($"Ошибка: {Execute.StatusCode}.");

                                            Value.Success = (uint)Execute.StatusCode;
                                        }
                                    }

                                    await Task.Delay(2500);
                                }
                                catch (OperationCanceledException) { }
                                catch (Exception e)
                                {
                                    Logger.LogGenericException(e);
                                }
                            }
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception e)
                        {
                            Logger.LogGenericException(e);

                            Value.Success = 600;
                        }

                        await Task.Delay(Config.Thread * 2500);
                    }

                    if (Value.Success != 200)
                    {
                        Logger.LogGenericWarning($"Ошибка: {(Value.Success == 429 ? "слишком много запросов" : (Value.Success == 600 ? "получения данных" : $"код состояния: {Value.Success}"))}.");

                        if (++Value.Retry <= 3)
                        {
                            await Task.Delay(Value.Success == 429
                                ? TimeSpan.FromMinutes(Value.Retry * 5)
                                : TimeSpan.FromSeconds(Value.Success == 500
                                    ? Action == EAction.Bundle ? 2.5 : 5
                                    : 25));

                            Value.Success = 0;

                            goto Retry;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogGenericException(e);
            }
        }

        private static void Save()
        {
            ++Success;

            string _ = string.Empty;

            if (Action == EAction.Badge)
            {
                _ = string.Join("\n", Default
                    .OrderBy(x => x.App.TradingCard())
                    .Select(x =>
                    {
                        string URL = "https://steamcommunity.com/market/multibuy?appid=753";

                        x.App.ValueList.ForEach(v => URL += $"&items[]={v.HashName}&qty[]=1");

                        return $"Сет: {x.App.TradingCard():C} | Ссылка: {Uri.EscapeUriString(URL).Replace("!", "%21").Replace("(", "%28").Replace(")", "%29")}";
                    }));
            }
            else if (Action == EAction.TradingCard)
            {
                _ = string.Join("\n", Default
                    .OrderBy(x => x.App.Price)
                    .Select(x =>
                    {
                        string URL = $"https://store.steampowered.com/app/{x.App.ID}/";

                        byte Average = Helper.ToAverage(x.App.ValueList.Count);

                        return $"Игра: {x.App.Price:C} | Примерное значение: {Math.Round(x.App.ValueList.Average(v => v.Price) * Average, 2):C} | Среднее значение: {Math.Round(x.App.ValueList.Average(v => v.Average) * Average, 2):C} | Прибыль:{Helper.ToSummary(x.App.Margin, true)} | Ссылка: {URL}";
                    }));
            }
            else if (Action == EAction.Background || Action == EAction.Emoticon)
            {
                _ = string.Join("\n", Default
                    .OrderBy(x => x.App.TradingCard())
                    .Select(x =>
                    {
                        string URL = $"https://www.steamcardexchange.net/index.php?gamepage-appid-{x.App.ID}/";

                        var List = x.App.ValueList

                            .Where(v => v.Price >= x.App.TradingCard() * Config.MinLevelBadge)
                            .GroupBy(v => v.Rare)
                            .Select(v => new { v.Key, List = v.ToList().OrderBy(o => o.Price).Reverse() })
                            .Select(v => $"{v.Key}: {string.Join(", ", v.List.Select(o => o.Price.ToString("C")))}");

                        return $"Сет: {x.App.TradingCard():C} | {string.Join(" | ", List)} | Ссылка: {URL}";
                    }));
            }
            else if (Action == EAction.Bundle)
            {
                _ = string.Join("\n", Default
                    .OrderBy(x => x.Bundle.Price)
                    .Select(x =>
                    {
                        string URL = $"https://store.steampowered.com/{x.Bundle.Type}/{x.Bundle.ID}/";

                        decimal P = 0m;
                        decimal A = 0m;
                        decimal M = 0m;

                        x.Bundle.AppList
                            .Where(v => v.ValueList.Count > 0)
                            .ForEach(v =>
                            {
                                byte Average = Helper.ToAverage(v.ValueList.Count);

                                P += v.ValueList.Average(o => o.Price) * Average;
                                A += v.ValueList.Average(o => o.Average) * Average;
                                M += v.Margin;
                            });

                        return $"Набор: {x.Bundle.Price:C} | Примерное значение: {Math.Round(P, 2):C} | Среднее значение: {Math.Round(A, 2):C} | Прибыль:{Helper.ToSummary(M, true)} | Ссылка: {URL}";
                    }));
            }

            File.WriteAllText($"$ {string.Format((Config.Custom.Any ? "Custom [{0}]" : "{0}"), $"{(Action == EAction.TradingCard ? "Trading Card" : Action.ToString())}{(Tag == ETag.Any ? "" : " - Anime")}")}.txt", _);
        }
    }
}
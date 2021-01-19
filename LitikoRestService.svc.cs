
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Hosting;
using IDev.Hub.MSSSender;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using KyivstarMobileID;

namespace LitikoRestService
{
    
    public class Settings
    {
        private static string JSONSettings = Path.Combine(Path.GetDirectoryName(HostingEnvironment.ApplicationPhysicalPath), @"Settings.json");
        public string CertPhoneFullName = JObject.Parse(File.ReadAllText(JSONSettings))["CertPhoneFullName"].ToString();
        public string UseProxy = JObject.Parse(File.ReadAllText(JSONSettings))["UseProxy"].ToString();
        public string ProxyScheme = JObject.Parse(File.ReadAllText(JSONSettings))["ProxyScheme"].ToString();
        public string ProxyHost = JObject.Parse(File.ReadAllText(JSONSettings))["ProxyHost"].ToString();
        public string ProxyUserDomain = JObject.Parse(File.ReadAllText(JSONSettings))["ProxyUserDomain"].ToString();
        public string ProxyUserName = JObject.Parse(File.ReadAllText(JSONSettings))["ProxyUserName"].ToString();
        public string ProxyUserPassword = JObject.Parse(File.ReadAllText(JSONSettings))["ProxyUserPassword"].ToString();
        public int ProxyPort = int.Parse(JObject.Parse(File.ReadAllText(JSONSettings))["ProxyPort"].ToString());
        public string VodafoneUri = JObject.Parse(File.ReadAllText(JSONSettings))["VodafoneUri"].ToString();
        public string VodafoneAppId = JObject.Parse(File.ReadAllText(JSONSettings))["VodafoneAppId"].ToString();
        public string VodafoneAppPass = JObject.Parse(File.ReadAllText(JSONSettings))["VodafoneAppPass"].ToString();
        public string KyivstarUri = JObject.Parse(File.ReadAllText(JSONSettings))["KyivstarUri"].ToString();
        public string KyivstarAppId = JObject.Parse(File.ReadAllText(JSONSettings))["KyivstarAppId"].ToString();
        public string KyivstarAppPass = JObject.Parse(File.ReadAllText(JSONSettings))["KyivstarAppPass"].ToString();
        public MSSSender vodafoneSender;
        public KyivstarClient kyivstarClient;
        internal List<string> VodafoneCodes;
        internal List<string> KyivstarCodes;

        public Settings()
        {
            vodafoneSender = GetVodafoneSender();
            kyivstarClient = GetKyivstarClient();
            VodafoneCodes = new List<string> {"50", "66", "95", "99"};
            KyivstarCodes = new List<string> {"67", "68", "96", "97", "98"};
        }
        
        private KyivstarClient GetKyivstarClient()
        {
            if (bool.Parse(UseProxy))
            {
                var uriBuilder = new UriBuilder(ProxyScheme, ProxyHost, ProxyPort);
                if (string.IsNullOrWhiteSpace(ProxyUserName))
                    return new KyivstarClient(uriBuilder.Uri, KyivstarAppId, KyivstarAppPass);
                else
                    return new KyivstarClient(uriBuilder.Uri,
                                              ProxyUserDomain,
                                              ProxyUserName,
                                              ProxyUserPassword,
                                              KyivstarAppId,
                                              KyivstarAppPass);
            }
            else
                return new KyivstarClient(KyivstarAppId, KyivstarAppPass);
        }
        private MSSSender GetVodafoneSender()
        {
            if (bool.Parse(UseProxy))
            {
                var uriBuilder = new UriBuilder(ProxyScheme, ProxyHost, ProxyPort);
                if (string.IsNullOrWhiteSpace(ProxyUserName))
                    return new MSSSender(VodafoneUri, VodafoneAppId, VodafoneAppPass, uriBuilder.Uri);
                else
                    return new MSSSender(mssUrl: VodafoneUri,
                                         appId: VodafoneAppId,
                                         appPassword: VodafoneAppPass,
                                         proxyUri: uriBuilder.Uri,
                                         userDomain: ProxyUserDomain,
                                         userName: ProxyUserName,
                                         password: ProxyUserPassword);
            }
            else
                return new MSSSender(VodafoneUri, VodafoneAppId, VodafoneAppPass);
        }

    }
    public class LitikoRestService : ILitikoRestService
    {
        Settings settings = null;

        public Response GetOperatorByPhone(string PhoneNumber)
        {
            if (settings == null)
                settings = new Settings();
            //Check phone length
            if (PhoneNumber.Length != 12)
                return new Response() { ErrorMessage = "Длина номера телефона должна быть равна 12 символам ! (PhoneNumber = " + PhoneNumber + ")", ResponseResult = string.Empty };
            if (settings.VodafoneCodes.Contains(PhoneNumber.Substring(3, 2)))
                return new Response() { ErrorMessage = string.Empty, ResponseResult = "Vodafone" };
            if (settings.KyivstarCodes.Contains(PhoneNumber.Substring(3, 2)))
                return new Response() { ErrorMessage = string.Empty, ResponseResult = "Kyivstar" };
            return new Response() { ErrorMessage = "Не смог определить оператора по номеру телефона (PhoneNumber = " + PhoneNumber + ")", ResponseResult = string.Empty };
        }

        public Response GetPhoneByCertThumbprint(string CertificateThumbprint)
        {
            if (settings == null)
                settings = new Settings();

            string configPath = settings.CertPhoneFullName;
            //Если в настройках сервиса тоже нет пути к файлу - даем ошибку
            if (string.IsNullOrEmpty(configPath))
                return new Response() { ErrorMessage = "Не задан полный путь к конфигурационному файлу соответствий сертификатов к номерам телефонов. Путь задается в настройках сервиса или же в параметрах метода.", ResponseResult = string.Empty };
            //считываем конфиг в словарь
            try
            {
                
                string config = File.ReadAllText(configPath);
                Dictionary<string, string> configIthems = JsonConvert.DeserializeObject<Dictionary<string, string>>(config);
                //получаем из словаря номер телефона по отпечатку
                string phoneNumber = configIthems.FirstOrDefault(x => String.Equals(x.Key, CertificateThumbprint, StringComparison.OrdinalIgnoreCase)).Value;
                if (!string.IsNullOrEmpty(phoneNumber))
                    return new Response() { ErrorMessage = string.Empty, ResponseResult = phoneNumber };
                else
                    return new Response() { ErrorMessage = string.Format("В конфигурационном файле {0} не был найден телефон, соответствующий сертификату {1}", configPath, CertificateThumbprint), ResponseResult = string.Empty };
            }
            catch(Exception ex)
            {
                return new Response() { ErrorMessage = ex.Message + Environment.NewLine + ex.StackTrace };
            }
        }
        public Response GetAllPositions(string PhoneNumber)
        {
            try
            {
                if (settings == null)
                    settings = new Settings();
                var resp = settings.vodafoneSender.GetPosition(PhoneNumber);
                var data = resp.Data;
                var positions = JsonConvert.SerializeObject(data);

                return new Response() { ErrorMessage = string.Empty, ResponseResult = positions};
            }
            catch (Exception e)
            {
                return new Response() { ErrorMessage = e.Message + Environment.NewLine + "Trace : " + e.StackTrace, ResponseResult = string.Empty };
            }
        }

        public Response SignData(string HashData, string PhoneNumber, int PositionId = 0, string Service = "", string DisplayMessage = "Підписання даних в Директум")
        {
            var getOperatorResult = GetOperatorByPhone(PhoneNumber);
            if (string.IsNullOrWhiteSpace(getOperatorResult.ErrorMessage))
            {
                if(getOperatorResult.ResponseResult == "Vodafone")
                {
                    return SignVodafone(HashData, PhoneNumber, PositionId, Service, DisplayMessage);
                }
                if (getOperatorResult.ResponseResult == "Kyivstar")
                    return SignKyivstar(HashData, PhoneNumber);

                return new Response() { ErrorMessage = "Метод GetOperatorByPhone вернул неопознанного оператора", ResponseResult = string.Empty };
            }
            else
            {
                return new Response() { ErrorMessage = getOperatorResult.ErrorMessage, ResponseResult = string.Empty };
            }
        }


        public Response SignVodafone(string HashData, string PhoneNumber, int PositionId, string Service, string DisplayMessage = "Підписання даних в Директум")
        {
            try
            {
                if (settings == null)
                    settings = new Settings();
                var signatureParameters = new SignatureParameters
                {
                    DisplayData = DisplayMessage,
                    Position = PositionId,
                    Data = HashData,
                    Service = Service,
                    RandomValue = Guid.NewGuid().ToString("N").Substring(0, 4)
                };
            
                var res = settings.vodafoneSender.DoAction(PhoneNumber, signatureParameters);
                return new Response() { ErrorMessage = string.Empty, ResponseResult = Convert.ToBase64String(res.Data) };
            }
            catch (Exception e)
            {
                return new Response() { ErrorMessage = e.Message + Environment.NewLine + "Trace : " + e.StackTrace, ResponseResult = string.Empty };
            }
        }
        public Response SignKyivstar(string HashData, string PhoneNumber)
        {
            try
            {
                if (settings == null)
                    settings = new Settings();

                if (!PhoneNumber.StartsWith("+"))
                    PhoneNumber = "+" + PhoneNumber;

                if (settings.kyivstarClient.SendRequest(settings.KyivstarUri, PhoneNumber, HashData, out string res))
                    return new Response() { ErrorMessage = string.Empty, ResponseResult = res };
                else
                    return new Response() { ErrorMessage = "Запрос подписи вернул ошибку : " + res, ResponseResult = string.Empty };
            }
            catch (Exception e)
            {
                return new Response() { ErrorMessage = e.Message + Environment.NewLine + "Trace : " + e.StackTrace, ResponseResult = string.Empty };
            }
        }

        public string Test1()
        {
            if (settings == null)
                settings = new Settings();
            return string.Join(",",settings.VodafoneCodes);
        }

        public string Secret()
        {
            try
            {
                return File.ReadAllText(settings.CertPhoneFullName).Any().ToString();
            }
            catch (Exception e)
            {
                return "Секрет не удался " + e.Message + Environment.NewLine + e.StackTrace;
            }
        }
    }
}
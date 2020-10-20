﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Hosting;
using IDev.Hub.MSSSender;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public MSSSender sender;

        public Settings()
        {
            sender = GetSender();
        }
        
        private MSSSender GetSender()
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
                var resp = settings.sender.GetPosition(PhoneNumber);
                var data = resp.Data;
                var positions = JsonConvert.SerializeObject(data);

                return new Response() { ErrorMessage = string.Empty, ResponseResult = positions};
            }
            catch (Exception e)
            {
                return new Response() { ErrorMessage = e.Message + Environment.NewLine + "Trace : " + e.StackTrace, ResponseResult = string.Empty };
            }
        }

        public Response SignData(string HashData, string PhoneNumber, int PositionId, string Service, string DisplayMessage = "Підписання даних в Директум")
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
            
                var res = settings.sender.DoAction(PhoneNumber, signatureParameters);
                return new Response() { ErrorMessage = string.Empty, ResponseResult = Convert.ToBase64String(res.Data) };
            }
            catch (Exception e)
            {
                return new Response() { ErrorMessage = e.Message + Environment.NewLine + "Trace : " + e.StackTrace, ResponseResult = string.Empty };
            }
        }

        public string Test1()
        {
            return settings.CertPhoneFullName;
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
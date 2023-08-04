using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace WorkerService
{
    public abstract class BaseLocalMachineConfig<T> where T : BaseLocalMachineConfig<T>, new()
    {
        private const string DateTimeConfigFormat = "yyyy/MM/dd HH:mm:ss.fff";

        /// <summary>
        /// 	Logger
        /// </summary>
        private static readonly ILog _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public class LocalMachineDataSet
        {
            public List<LocalMachineData> ListLocalMachineData { get; set; }

            public LocalMachineDataSet()
            {
                this.ListLocalMachineData = new List<LocalMachineData>();
            }
        }

        public class LocalMachineData
        {
            public string Key { get; set; }
            public string Data { get; set; }
        }

        protected static T _instance = null;

        private Dictionary<string, LocalMachineData> _mapSystemConfigValues = new Dictionary<string, LocalMachineData>();

        protected string LocalMachineConfigPath { get; set; }

        protected static AutoResetEvent _lockExclusiveAccess = new AutoResetEvent(true); //initial unlock
        private static AutoResetEvent _lockExclusiveMappingConfig = new AutoResetEvent(true); // initial unlock
        private static AutoResetEvent _lockExclusiveSavingFile = new AutoResetEvent(true); // initial unlock

        public static T GetConfig()
        {
            try
            {
                _lockExclusiveAccess.WaitOne();
                if (_instance == null)
                {
                    _instance = new T();
                    _instance.ReloadConfig();
                }
                return _instance;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                throw;
            }
            finally
            {
                _lockExclusiveAccess.Set();
            }
        }

        private string Serialize()
        {
            string result = null;
            XmlSerializer serializer = new XmlSerializer(typeof(LocalMachineDataSet));
            using (MemoryStream memoryStream = new MemoryStream())
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = UTF8Encoding.UTF8;
                settings.Indent = true;
                settings.OmitXmlDeclaration = true;
                using (XmlWriter xmlTextWriter = XmlWriter.Create(memoryStream, settings))
                {
                    LocalMachineDataSet dataSet = new LocalMachineDataSet();
                    dataSet.ListLocalMachineData = new List<LocalMachineData>(this._mapSystemConfigValues.Values);

                    serializer.Serialize(xmlTextWriter, dataSet);
                }

                result = Encoding.UTF8.GetString(memoryStream.ToArray());
            }

            return result;
        }

        private List<LocalMachineData> Deserialize(string xmlContent)
        {
            List<LocalMachineData> result = null;

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(LocalMachineDataSet));
                using (MemoryStream readStream = new MemoryStream(UTF8Encoding.UTF8.GetBytes(xmlContent)))
                {
                    XmlReader reader = new XmlTextReader(readStream);
                    var dataSet = (LocalMachineDataSet)serializer.Deserialize(reader);
                    result = dataSet.ListLocalMachineData;
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(string.Format("Lỗi khi đọc file XML: {0} - thử reload lại với chế độ cũ", ex.Message), ex);

                XmlSerializer serializer = new XmlSerializer(typeof(List<LocalMachineData>));
                using (MemoryStream readStream = new MemoryStream(UTF8Encoding.UTF8.GetBytes(xmlContent)))
                {
                    XmlReader reader = new XmlTextReader(readStream);
                    result = (List<LocalMachineData>)serializer.Deserialize(reader);
                }
                return result;
            }
        }

        private string GetConfigPath()
        {
            if (string.IsNullOrEmpty(this.LocalMachineConfigPath))
            {
                throw new Exception("Local config path is not set !!!");
            }
            else
            {
                var homeDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var configPath = string.Format(@"{0}\{1}", homeDirectory, this.LocalMachineConfigPath);

                return configPath;
            }
        }

        public void ReloadConfig()
        {
            try
            {
                _lockExclusiveMappingConfig.WaitOne();
                _mapSystemConfigValues.Clear();

                var configPath = GetConfigPath();
                if (!string.IsNullOrEmpty(configPath))
                {
                    if (!File.Exists(configPath))
                    {
                        using (FileStream fileStream = File.Create(configPath))
                        {
                            string data = this.Serialize();
                            byte[] rawData = UTF8Encoding.UTF8.GetBytes(data);
                            fileStream.Write(rawData, 0, rawData.Length);
                        }
                    }

                    string xmlData = string.Empty;
                    using (TextReader readFileStream = new StreamReader(configPath))
                    {
                        xmlData = readFileStream.ReadToEnd();
                    }

                    var listConfig = Deserialize(xmlData);
                    foreach (var config in listConfig)
                    {
                        _mapSystemConfigValues.Add(config.Key, config);
                    }
                    SaveConfigToFile(); //save lại lần nữa, trong trường hợp bản cũ
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                throw;
            }
            finally
            {
                _lockExclusiveMappingConfig.Set();
            }
        }

        protected void SaveConfigValue(string key, string value)
        {
            try
            {
                _lockExclusiveMappingConfig.WaitOne();

                bool saveUpdate = false;

                LocalMachineData checkData = null;
                if (!_mapSystemConfigValues.TryGetValue(key, out checkData))
                {
                    checkData = new LocalMachineData()
                    {
                        Key = key,
                        Data = value
                    };
                    _mapSystemConfigValues.Add(checkData.Key, checkData);
                    saveUpdate = true;
                }
                else if (checkData.Data != value)
                {
                    checkData.Data = value;
                    saveUpdate = true;
                }

                if (saveUpdate)
                {
                    SaveConfigToFile();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                throw;
            }
            finally
            {
                _lockExclusiveMappingConfig.Set();
            }
        }

        protected void SaveConfigValue(string key, int value)
        {
            string valueRaw = value.ToString();
            SaveConfigValue(key, valueRaw);
        }

        protected void SaveConfigValue(string key, bool value)
        {
            string valueRaw = value.ToString();
            SaveConfigValue(key, valueRaw);
        }

        protected void SaveConfigValue(string key, DateTime value)
        {
            string valueRaw = value.ToString(DateTimeConfigFormat);
            SaveConfigValue(key, valueRaw);
        }

        protected void SaveConfigValue(string key, float value)
        {
            string valueRaw = value.ToString();
            SaveConfigValue(key, valueRaw);
        }

        protected void SaveConfigValue(string key, double value)
        {
            string valueRaw = value.ToString();
            SaveConfigValue(key, valueRaw);
        }

        private void SaveConfigToFile()
        {
            try
            {
                _lockExclusiveSavingFile.WaitOne();

                string data = this.Serialize();
                var configPath = GetConfigPath();
                if (!string.IsNullOrEmpty(configPath))
                {
                    using (var fileStream = new StreamWriter(configPath, false))
                    {
                        fileStream.Write(data);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                throw;
            }
            finally
            {
                _lockExclusiveSavingFile.Set();
            }
        }

        protected string GetConfigValue(string key, string defaultValue)
        {
            try
            {
                _lockExclusiveMappingConfig.WaitOne();

                LocalMachineData localConfig = null;
                if (!_mapSystemConfigValues.TryGetValue(key, out localConfig))
                {
                    localConfig = new LocalMachineData()
                    {
                        Key = key,
                        Data = defaultValue
                    };
                    _mapSystemConfigValues.Add(localConfig.Key, localConfig);

                    SaveConfigToFile();

                    return defaultValue;
                }

                return localConfig.Data;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                throw;
            }
            finally
            {
                _lockExclusiveMappingConfig.Set();
            }
        }

        protected int GetConfigValue(string key, int defaultValue)
        {
            string defaultValueRaw = defaultValue.ToString();
            string resultRaw = GetConfigValue(key, defaultValueRaw);
            int result = defaultValue;
            if (!int.TryParse(resultRaw, out result))
            {
                result = defaultValue;
                SaveConfigValue(key, defaultValue);
            }

            return result;
        }

        protected bool GetConfigValue(string key, bool defaultValue)
        {
            string defaultValueRaw = defaultValue.ToString();
            string resultRaw = GetConfigValue(key, defaultValueRaw);
            bool result = defaultValue;
            if (!bool.TryParse(resultRaw, out result))
            {
                result = defaultValue;
                SaveConfigValue(key, defaultValue);
            }

            return result;
        }

        protected DateTime GetConfigValue(string key, DateTime defaultValue)
        {
            string defaultValueRaw = defaultValue.ToString(DateTimeConfigFormat);
            string resultRaw = GetConfigValue(key, defaultValueRaw);
            DateTime result = defaultValue;
            if (!DateTime.TryParseExact(resultRaw, DateTimeConfigFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                result = defaultValue;
                SaveConfigValue(key, defaultValue);
            }

            return result;
        }

        protected double GetConfigValue(string key, double defaultValue)
        {
            string defaultValueRaw = defaultValue.ToString();
            string resultRaw = GetConfigValue(key, defaultValueRaw);
            double result = defaultValue;
            if (!double.TryParse(resultRaw, out result))
            {
                result = defaultValue;
                SaveConfigValue(key, defaultValue);
            }

            return result;
        }

        protected float GetConfigValue(string key, float defaultValue)
        {
            string defaultValueRaw = defaultValue.ToString();
            string resultRaw = GetConfigValue(key, defaultValueRaw);
            float result = defaultValue;
            if (!float.TryParse(resultRaw, out result))
            {
                result = defaultValue;
                SaveConfigValue(key, defaultValue);
            }

            return result;
        }

    }
}

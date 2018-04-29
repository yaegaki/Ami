using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Ami
{
    public class SettingService
    {
        public string AccessToken { get; private set; } = string.Empty;
        public string AccessTokenSecret { get; private set; } = string.Empty;

        private static string CryptoKey { get; } = "ohapippi-";
        private static string CryptoSalt { get; } = "amiamiamiamaimai";

        // 実行しないととりあえずわからないようにしておく
        private static string EncryptedConsumerKey { get; } = "Ae0KDc+3XTemO4dnBnDCU5EI9Ujclzzl0hx8cxc2Aac=";
        private static string EncryptedConsumerSecret { get; } = "EzbLJyVxFfkYble9TURwHcgDWaPRY/iaiKz8ywZNdWC4mHzprOMfygn2PcpoC6wTDCcjTuEcz9LEuC2iTmyG7w==";

        public static string ConsumerKey => Decrypt(EncryptedConsumerKey);
        public static string ConsumerSecret => Decrypt(EncryptedConsumerSecret);

        private static XName RootElementTagName { get; } = XName.Get("AmiSetting");
        private static XName TwitterSettingElementTagName { get; } = XName.Get("TwitterSetting");
        private static XName TwitterAccessTokenAttributeName { get; } = XName.Get("AccessToken");
        private static XName TwitterAccessTokenSecretAttributeName { get; } = XName.Get("AccessTokenSecret");

        public bool Load()
        {
            try
            {
                var root = XElement.Load(GetSettingFilePath());
                if (root.Name != RootElementTagName)
                {
                    return false;
                }

                ParseTwitterSetting(root);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Save()
        {
            try
            {
                var root = new XElement(RootElementTagName);
                root.Add(CreateTwitterSettingElement());

                root.Save(GetSettingFilePath());
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetSettingFilePath()
        {
            var location = System.Reflection.Assembly.GetEntryAssembly().Location;
            var dir = Path.GetDirectoryName(location);
            var name = Path.GetFileNameWithoutExtension(location) + ".conf";
            return Path.Combine(dir, name);
        }

        public void UpdateOAuthToken(string accessToken, string accessTokenSecret)
        {
            this.AccessToken = accessToken;
            this.AccessTokenSecret = accessTokenSecret;
        }

        private void ParseTwitterSetting(XElement root)
        {
            var elem = root.Element(TwitterSettingElementTagName);
            if (elem == null)
            {
                this.AccessToken = string.Empty;
                this.AccessTokenSecret = string.Empty;
                return;
            }

            var encryptedAccessToken = elem.Attribute(TwitterAccessTokenAttributeName)?.Value;
            var encryptedAccessTokenSecret = elem.Attribute(TwitterAccessTokenSecretAttributeName)?.Value;

            this.AccessToken = Decrypt(encryptedAccessToken);
            this.AccessTokenSecret = Decrypt(encryptedAccessTokenSecret);
        }

        private XElement CreateTwitterSettingElement()
        {
            var elem = new XElement(TwitterSettingElementTagName);

            if (string.IsNullOrEmpty(this.AccessToken) || string.IsNullOrEmpty(this.AccessTokenSecret))
            {
                return elem;
            }

            elem.SetAttributeValue(TwitterAccessTokenAttributeName, Encrypt(this.AccessToken));
            elem.SetAttributeValue(TwitterAccessTokenSecretAttributeName, Encrypt(this.AccessTokenSecret));

            return elem;
        }

        #region crypto

        private static string Encrypt(string value)
        {
            // 気休め程度

            try
            {
                if (string.IsNullOrEmpty(value))
                {
                    return string.Empty;
                }

                var aes = CreateAESManaged();
                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    var input = Encoding.UTF8.GetBytes(value);
                    var encrypted = encryptor.TransformFinalBlock(input, 0, input.Length);

                    return Convert.ToBase64String(encrypted);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string Decrypt(string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                {
                    return string.Empty;
                }

                var encrypted = Convert.FromBase64String(value);
                var aes = CreateAESManaged();
                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    var decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

                    return Encoding.UTF8.GetString(decrypted);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static AesManaged CreateAESManaged()
        {
            var deriveBytes = new Rfc2898DeriveBytes(CryptoKey, Encoding.UTF8.GetBytes(CryptoSalt));
            var key = deriveBytes.GetBytes(32);
            var iv = deriveBytes.GetBytes(16);

            var aes = new AesManaged()
            {
                BlockSize = 128,
                KeySize = 256,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                Key = key,
                IV = iv,
            };

            return aes;
        }

        #endregion
    }
}

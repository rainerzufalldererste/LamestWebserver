using System;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace LamestWebserver.Security
{
    public class Password : IXmlSerializable, ISerializable
    {
        private byte[] hash;
        private byte[] salt;

        /// <summary>
        /// Only used for deserialisation
        /// </summary>
        public Password()
        {

        }

        public Password(SerializationInfo info, StreamingContext context)
        {
            hash = (byte[])info.GetValue(nameof(this.hash), typeof(byte[]));
            salt = (byte[])info.GetValue(nameof(this.salt), typeof(byte[]));
        }

        public Password(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new InvalidOperationException("You have to set a password.");

            salt = SessionContainer.getComplexHash(new UTF8Encoding().GetBytes(SessionContainer.generateHash() + SessionContainer.generateHash() + SessionContainer.generateHash() + SessionContainer.generateHash()));

            hash = generateSaltedHash(password, salt);
        }

        public bool IsValid(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new InvalidOperationException("You have to set a password to check against.");

            byte[] sha3 = generateSaltedHash(password, salt);

            if (sha3.Length != hash.Length)
                return false;

            for (int i = 0; i < sha3.Length; i++)
                if (sha3[i] != hash[i]) return false;

            return true;
        }

        private byte[] generateSaltedHash(string password, byte[] salt)
        {
            byte[] bytes = new UTF8Encoding().GetBytes(password);
            byte[] hash = SessionContainer.getComplexHash(bytes);

            System.Diagnostics.Debug.Assert(hash.Length == salt.Length, "Hash and Salt are of different lengths.");

            for (int i = 0; i < hash.Length; i++)
            {
                hash[i] ^= salt[i];
            }

            hash = SessionContainer.getComplexHash(hash);

            return hash;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();

            hash = reader.ReadElement<byte[]>("hash");
            salt = reader.ReadElement<byte[]>("salt");

            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteElement("hash", hash);
            writer.WriteElement("salt", salt);
        }

        /// <inheritdoc />
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(this.hash), hash);
            info.AddValue(nameof(this.salt), salt);
        }
    }
}

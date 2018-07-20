using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using LamestWebserver;
using LamestWebserver.Collections;
using LamestWebserver.Core;
using LamestWebserver.Serialization;
using System.Threading;

namespace LamestWebserver.Collections
{
    [Serializable]
    public class OutOfCoreHashmap<TKey, TValue> : Core.NullCheckable, IDictionary<TKey, TValue> where TKey : IEquatable<TKey>, IComparable
    {
        private string _filename;
        private AVLHashMap<TKey, long?> _keys;
        private long nextIndex = 0;
        public bool SaveKeysAutomatically = true;

        public OutOfCoreHashmap(int size, string filename)
        {
            _filename = filename;
            _keys = new AVLHashMap<TKey, long?>(size);
        }

        public OutOfCoreHashmap(string loadFromFilename)
        {
            _filename = loadFromFilename;
            _keys = Serializer.ReadJsonData<AVLHashMap<TKey, long?>>(_filename);

            if(!System.IO.File.Exists(GetCompressedFile()))
            {
                Logger.LogInformation("Updating OutOfCoreHashmap values...");

                foreach (var key in _keys)
                {
                    long keyIndex = key.Value.Value;
                    TValue data = (TValue)Serializer.ReadJsonData($"{GetFileNameOld(keyIndex)}", typeof(TValue));
                    System.IO.File.WriteAllBytes(GetFileName(keyIndex), Compression.GZipCompression.CompressString(Serializer.WriteJsonDataInMemory(data, false)));
                }

                System.IO.File.WriteAllText(GetCompressedFile(), "compressed: gzip");

                foreach (var key in _keys)
                {
                    long keyIndex = key.Value.Value;
                    TValue data = (TValue)Serializer.ReadJsonData($"{GetFileNameOld(keyIndex)}", typeof(TValue));
                    System.IO.File.Move(GetFileNameOld(keyIndex), GetFileNameOld(keyIndex) + ".trash");
                }
            }
            
            nextIndex = _keys.Count > 0 ? _keys.Max(k => k.Value.Value) + 1 : 0;
        }

        public OutOfCoreHashmap()
        {
            _filename = Hash.GetHash();
            _keys = new AVLHashMap<TKey, long?>();
        }

        ~OutOfCoreHashmap()
        {
            SaveKeysToDisk();
        }

        private string GetFileNameOld(long keyValue) => $"{_filename}_/{keyValue}";
        private string GetFileName(long keyValue) => $"{_filename}_/{keyValue}.bin";
        private string GetCompressedFile() => $"{_filename}.compressed";

        public TValue this[TKey key]
        {
            get
            {
                long? value = _keys[key];
                
                if (value.HasValue)
                {
                    int tries = 0;
                    RETRY:

                    try
                    {
                        return (TValue)Serializer.ReadJsonDataInMemory(Compression.GZipCompression.DecompressString(System.IO.File.ReadAllBytes($"{GetFileName(value.Value)}")), typeof(TValue));
                        //return (TValue)Serializer.ReadJsonData($"{GetFileName(value.Value)}", typeof(TValue));
                    }
                    catch (Exception e)
                    {
                        tries++;

                        if(tries < 5)
                        {
                            Thread.Sleep(1);
                            goto RETRY;
                        }

                        LamestWebserver.Core.Logger.LogExcept($"Could not read from '{GetFileName(value.Value)}' in {nameof(OutOfCoreHashmap<TKey, TValue>)}. ({e.Message})", e);
                    }
                }

                return default(TValue);
            }

            set
            {
                long? _value = _keys[key];

                if (_value.HasValue)
                {
                    int tries = 0;
                    RETRY:

                    try
                    {
                        System.IO.File.WriteAllBytes(GetFileName(_value.Value), Compression.GZipCompression.CompressString(Serializer.WriteJsonDataInMemory(value, false)));
                        //Serializer.WriteJsonData(value, GetFileName(_value.Value));
                    }
                    catch (Exception e)
                    {
                        tries++;

                        if (tries < 5)
                        {
                            Thread.Sleep(1);
                            goto RETRY;
                        }

                        LamestWebserver.Core.Logger.LogExcept($"Could write to '{GetFileName(_value.Value)}' in {nameof(OutOfCoreHashmap<TKey, TValue>)}. ({e.Message})", e);
                    }
                }
                else
                {
                    long val = nextIndex++;
                    int tries = 0;
                    RETRY:

                    try
                    {
                        System.IO.File.WriteAllBytes(GetFileName(val), Compression.GZipCompression.CompressString(Serializer.WriteJsonDataInMemory(value, false)));
                        //Serializer.WriteJsonData(value, GetFileName(val));

                        _keys.Add(key, val);

                        if (SaveKeysAutomatically)
                            SaveKeysToDisk();
                    }
                    catch (Exception e)
                    {
                        tries++;

                        if (tries < 5)
                        {
                            Thread.Sleep(1);
                            goto RETRY;
                        }

                        LamestWebserver.Core.Logger.LogExcept($"Could write to '{GetFileName(val)}' in {nameof(OutOfCoreHashmap<TKey, TValue>)}. ({e.Message})", e);
                    }
                }
            }
        }

        public void SaveKeysToDisk()
        {
            Serializer.WriteJsonData(_keys, _filename);
        }

        public int Count =>_keys.Count;

        public bool IsReadOnly => false;

        public ICollection<TKey> Keys => (from k in _keys select k.Key).ToList();

        public ICollection<TValue> Values => (from k in _keys select this[k.Key]).ToList();

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this[item.Key] = item.Value;
        }

        public void Add(TKey key, TValue value)
        {
            this[key] = value;
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(TKey key) => _keys.ContainsKey(key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(TKey key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (!_keys.ContainsKey(key))
            {
                value = default(TValue);
                return false;
            }

            try
            {
                value = this[key];
                return true;
            }
            catch (Exception)
            {
                value = default(TValue);
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}

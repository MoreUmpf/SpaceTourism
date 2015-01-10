using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP;
using KSPAchievements;
using Contracts;
using Contracts.Parameters;
using UnityEngine;

namespace SpaceTourism
{
	public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		static T _instance;
		static object _lock = new object();
		static bool applicationIsQuitting = false;
	 
		public static T Instance
		{
			get
			{
				if (applicationIsQuitting) 
				{
					Debug.LogWarning("[Singleton] Instance '"+ typeof(T) +
						"' already destroyed on application quit." +
						" Won't create again - returning null.");
					return null;
				}
	 
				lock(_lock)
				{
					if (_instance == null)
					{
						_instance = (T) FindObjectOfType(typeof(T));
	 
						if ( FindObjectsOfType(typeof(T)).Length > 1 )
						{
							Debug.LogError("[Singleton] Something went really wrong " +
								" - there should never be more than 1 singleton!" +
								" Reopenning the scene might fix it.");
							return _instance;
						}
	 
						if (_instance == null)
						{
							GameObject singleton = new GameObject();
							_instance = singleton.AddComponent<T>();
							singleton.name = "(singleton) "+ typeof(T).ToString();
	 
							DontDestroyOnLoad(singleton);
	 
							Debug.Log("[Singleton] An instance of " + typeof(T) + 
								" is needed in the scene, so '" + singleton +
								"' was created with DontDestroyOnLoad.");
						} 
						else 
						{
							Debug.Log("[Singleton] Using instance already created: " +
								_instance.gameObject.name);
						}
					}
					return _instance;
				}
			}
		}
		
		public void OnDestroy () 
		{
			applicationIsQuitting = true;
		}
	}
	
	public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
	{
	    readonly IDictionary<TKey, TValue> _dictionary;
	
	    public ReadOnlyDictionary()
	    {
	        _dictionary = new Dictionary<TKey, TValue>();
	    }
	
	    public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
	    {
	        _dictionary = dictionary;
	    }
	
	    void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
	    {
	        throw ReadOnlyException();
	    }
	
	    public bool ContainsKey(TKey key)
	    {
	        return _dictionary.ContainsKey(key);
	    }
	
	    public ICollection<TKey> Keys
	    {
	        get { return _dictionary.Keys; }
	    }
	
	    bool IDictionary<TKey, TValue>.Remove(TKey key)
	    {
	        throw ReadOnlyException();
	    }
	
	    public bool TryGetValue(TKey key, out TValue value)
	    {
	        return _dictionary.TryGetValue(key, out value);
	    }
	
	    public ICollection<TValue> Values
	    {
	        get { return _dictionary.Values; }
	    }
	
	    public TValue this[TKey key]
	    {
	        get
	        {
	            return _dictionary[key];
	        }
	    }
	
	    TValue IDictionary<TKey, TValue>.this[TKey key]
	    {
	        get
	        {
	            return this[key];
	        }
	        set
	        {
	            throw ReadOnlyException();
	        }
	    }
	
	    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
	    {
	        throw ReadOnlyException();
	    }
	
	    void ICollection<KeyValuePair<TKey, TValue>>.Clear()
	    {
	        throw ReadOnlyException();
	    }
	
	    public bool Contains(KeyValuePair<TKey, TValue> item)
	    {
	        return _dictionary.Contains(item);
	    }
	
	    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	    {
	        _dictionary.CopyTo(array, arrayIndex);
	    }
	
	    public int Count
	    {
	        get { return _dictionary.Count; }
	    }
	
	    public bool IsReadOnly
	    {
	        get { return true; }
	    }
	
	    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
	    {
	        throw ReadOnlyException();
	    }
	
	    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	    {
	        return _dictionary.GetEnumerator();
	    }

	    IEnumerator IEnumerable.GetEnumerator()
	    {
	        return GetEnumerator();
	    }
	
	    private static Exception ReadOnlyException()
	    {
	        return new NotSupportedException("This dictionary is read-only");
	    }
	}
}

namespace SpaceTourism.Contracts
{
	public interface ITourismContract
	{
		List<KerbalTourist> KerbalTourists
		{
			get;
		}
		
		int NumberOfKerbals
		{
			get;
		}
	}
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Collections;

namespace BIAS.Framework.DeltaExtractor
{

    public class DECustomPropertyCollection
    {
        private ArrayList items = new ArrayList();

        public object this[string key]
        {
            get { return KeyToObject(key); }
            set { AddToArray(key, value); }
        }
        public ArrayList InnerArrayList
        {
            get { return this.items; }
        }
        protected int KeyToOrdinal(string key)
        {
            for (int n = 0; n < this.items.Count; ++n)
            {
                KeyValuePair<string,object> pair = (KeyValuePair<string,object>)this.items[n];
                if (key == pair.Key) { return n; }
            }
            return -1;
        }
        protected void AddToArray(string key, object item)
        {
            int n = KeyToOrdinal(key);
            if (n >= 0)
            {
                this.items[n] = new KeyValuePair<string,object>(key, item);
            }
            else
            {
                KeyValuePair<string,object> pair = new KeyValuePair<string,object>(key, item);
                this.items.Add(pair);
            }
        }
        protected object KeyToObject(string key)
        {
            int n = KeyToOrdinal(key);
            if (n >= 0)
            {
                KeyValuePair<string,object> pair = (KeyValuePair<string,object>)this.items[n];
                return pair.Value;
                    
            }
            return null;
        }
        public void Add(string key, object item)
        {
            AddToArray(key, item);
        }
        public bool hasProperty (string key)
        {
            foreach (KeyValuePair<string, object> pair in this.items)
            {
                if (pair.Key == key)
                    return true;
            }
            return false;
        }
    }
 }

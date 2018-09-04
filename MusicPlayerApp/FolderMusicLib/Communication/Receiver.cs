using System;
using Windows.Foundation.Collections;

namespace MusicPlayer.Communication
{
    class Receiver
    {
        private Action<ValueSet, string> messageReceived;

        public string Key { get; private set; }

        public Receiver(string key, Action<ValueSet, string> messageReceived)
        {
            Key = key;
            this.messageReceived = messageReceived;
        }

        public ValueSet GetValueSet(string value)
        {
            ValueSet valueSet = new ValueSet();
            valueSet.Add(Key, value);

            if (Key == null) { }

            return valueSet;
        }

        public bool Handle(ValueSet valueSet)
        {
            if (!valueSet.ContainsKey(Key)) return false;

            try
            {
                messageReceived(valueSet, valueSet[Key].ToString());
            }
            catch (Exception e)
            {
            }

            return true;
        }
    }
}

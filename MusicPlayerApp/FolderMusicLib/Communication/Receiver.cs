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

            return valueSet;
        }

        public void Handle(ValueSet valueSet)
        {
            try
            {
                MobileDebug.Service.WriteEvent("Handle1", Key, valueSet.ContainsKey(Key));
                if (valueSet.ContainsKey(Key)) messageReceived(valueSet, valueSet[Key].ToString());
                MobileDebug.Service.WriteEvent("Handle2", Key, valueSet.ContainsKey(Key));
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("HandleFail", e, Key, valueSet[Key].ToString());
            }
        }
    }
}

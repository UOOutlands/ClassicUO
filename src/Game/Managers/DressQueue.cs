using System;
using ClassicUO.Interfaces;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    internal class DressQueue : IUpdateable
    {
        const long NETWORK_OBJECT_USAGE_INTERVAL = 600;
        private readonly Deque<Action> _actions = new Deque<Action>();
        private long _timer;

        public DressQueue()
        {
            _timer = Time.Ticks + NETWORK_OBJECT_USAGE_INTERVAL;
        }

        public void Update(double totalMS, double frameMS)
        {
            if (_timer < Time.Ticks)
            {
                _timer = Time.Ticks + NETWORK_OBJECT_USAGE_INTERVAL;

                if (_actions.Count == 0)
                {
                    return;
                }

                var dressAction = _actions.RemoveFromFront();

                dressAction?.Invoke();
            }
        }

        public void Add(Action dressAction)
        {
            _actions.AddToBack(dressAction);
        }

        public void Clear()
        {
            _actions.Clear();
        }
    }
}

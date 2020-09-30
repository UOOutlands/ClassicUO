using System;
using ClassicUO.Interfaces;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    internal class DressQueue : IUpdateable
    {
        private readonly Deque<Action> _actions = new Deque<Action>();
        private long _timer;

        public DressQueue()
        {
            _timer = Time.Ticks + 1000;
        }

        public void Update(double totalMS, double frameMS)
        {
            if (_timer < Time.Ticks)
            {
                _timer = Time.Ticks + 1000;

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

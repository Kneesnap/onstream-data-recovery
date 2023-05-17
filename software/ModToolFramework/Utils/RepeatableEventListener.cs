// ReSharper disable EventNeverSubscribedTo.Global
namespace ModToolFramework.Utils
{
    /// <summary>
    /// Represents an event listener which can repeat or fire only once. Has no parameters.
    /// </summary>
    public struct RepeatableEventListenerManager
    {
        /// <summary>
        /// These listeners are fired once and forgotten.
        /// </summary>
        public event EventListener OneShotListeners;

        /// <summary>
        /// These listeners run every time the event is fired.
        /// </summary>
        public event EventListener RepeatingListeners;
        
        /// <summary>
        /// A definition of what the events should expect to receive.
        /// </summary>
        public delegate void EventListener();

        /// <summary>
        /// Fires the event.
        /// </summary>
        public void FireEvent() {
            if (this.OneShotListeners != null) {
                this.OneShotListeners.Invoke();
                this.OneShotListeners = null;
            }
            
            this.RepeatingListeners?.Invoke();
        }
    }
    
    /// <summary>
    /// Represents an event listener which can repeat or fire only once. Has one parameter.
    /// </summary>
    public struct RepeatableEventListenerManager<TParamA>
    {
        /// <summary>
        /// These listeners are fired once and forgotten.
        /// </summary>
        public event EventListener OneShotListeners;

        /// <summary>
        /// These listeners run every time the event is fired.
        /// </summary>
        public event EventListener RepeatingListeners;

        /// <summary>
        /// Gets whether there are any active listeners or not.
        /// </summary>
        public bool HasAnyActiveListeners => (this.OneShotListeners != null) || (this.RepeatingListeners != null);
        
        /// <summary>
        /// A definition of what the events should expect to receive.
        /// </summary>
        public delegate void EventListener(TParamA paramA);

        /// <summary>
        /// Fires the event.
        /// </summary>
        public void FireEvent(TParamA paramA) {
            if (this.OneShotListeners != null) {
                this.OneShotListeners.Invoke(paramA);
                this.OneShotListeners = null;
            }
            
            this.RepeatingListeners?.Invoke(paramA);
        }
    }
    
    /// <summary>
    /// Represents an event listener which can repeat or fire only once. Has two parameters.
    /// </summary>
    public struct RepeatableEventListenerManager<TParamA, TParamB>
    {
        /// <summary>
        /// These listeners are fired once and forgotten.
        /// </summary>
        public event EventListener OneShotListeners;

        /// <summary>
        /// These listeners run every time the event is fired.
        /// </summary>
        public event EventListener RepeatingListeners;
        
        /// <summary>
        /// A definition of what the events should expect to receive.
        /// </summary>
        public delegate void EventListener(TParamA paramA, TParamB paramB);

        /// <summary>
        /// Fires the event.
        /// </summary>
        public void FireEvent(TParamA paramA, TParamB paramB) {
            if (this.OneShotListeners != null) {
                this.OneShotListeners.Invoke(paramA, paramB);
                this.OneShotListeners = null;
            }
            
            this.RepeatingListeners?.Invoke(paramA, paramB);
        }
    }
    
    /// <summary>
    /// Represents an event listener which can repeat or fire only once. Has three parameters.
    /// </summary>
    public struct RepeatableEventListenerManager<TParamA, TParamB, TParamC>
    {
        /// <summary>
        /// These listeners are fired once and forgotten.
        /// </summary>
        public event EventListener OneShotListeners;

        /// <summary>
        /// These listeners continue to fire even after
        /// </summary>
        public event EventListener RepeatingListeners;
        
        /// <summary>
        /// A definition of what the events should expect to receive.
        /// </summary>
        public delegate void EventListener(TParamA paramA, TParamB paramB, TParamC paramC);

        /// <summary>
        /// Fires the event.
        /// </summary>
        public void FireEvent(TParamA paramA, TParamB paramB, TParamC paramC) {
            if (this.OneShotListeners != null) {
                this.OneShotListeners.Invoke(paramA, paramB, paramC);
                this.OneShotListeners = null;
            }
            
            this.RepeatingListeners?.Invoke(paramA, paramB, paramC);
        }
    }
}
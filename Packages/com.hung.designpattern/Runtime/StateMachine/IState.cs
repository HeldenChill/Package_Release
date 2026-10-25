namespace Hung.DesignPattern
{
    /// <summary>Product behavior driven by a generic machine; key and callback ownership stay with its host.</summary>
    public interface IState<TKey>
    {
        /// <summary>Enter this behavior.</summary>
        void Enter();
        /// <summary>Update this behavior; the product interprets the return value.</summary>
        bool Update();
        /// <summary>Run the fixed-step update.</summary>
        bool FixedUpdate();
        /// <summary>Exit this behavior.</summary>
        void Exit();
    }
}

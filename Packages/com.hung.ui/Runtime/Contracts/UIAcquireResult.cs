namespace Hung.UI
{
    /// <summary>Failure category for explicit UI prefab acquisition.</summary>
    public enum UIAcquireFailure
    {
        None,
        InvalidAddress,
        AddressMismatch,
        LoadFailed,
        MissingCanvasComponent,
        InstantiationFailed,
        ManagerDestroyed
    }

    /// <summary>Typed outcome of loading and instantiating a canvas by address.</summary>
    public readonly struct UIAcquireResult<T> where T : UICanvas
    {
        private UIAcquireResult(string address, T canvas, UIAcquireFailure failure, string diagnostic)
        {
            Address = address;
            Canvas = canvas;
            Failure = failure;
            Diagnostic = diagnostic ?? string.Empty;
        }

        /// <summary>Requested prefab address.</summary>
        public string Address { get; }
        /// <summary>Loaded canvas, or null on failure.</summary>
        public T Canvas { get; }
        /// <summary>Failure category, or None on success.</summary>
        public UIAcquireFailure Failure { get; }
        /// <summary>Human-readable failure detail.</summary>
        public string Diagnostic { get; }
        /// <summary>Whether acquisition returned a live canvas.</summary>
        public bool Succeeded => Failure == UIAcquireFailure.None && Canvas != null;

        /// <summary>Build a successful result.</summary>
        public static UIAcquireResult<T> Success(string address, T canvas) =>
            new UIAcquireResult<T>(address, canvas, UIAcquireFailure.None, string.Empty);

        /// <summary>Build a failed result with an explicit category.</summary>
        public static UIAcquireResult<T> Failed(string address, UIAcquireFailure failure, string diagnostic) =>
            new UIAcquireResult<T>(address, null, failure, diagnostic);
    }
}

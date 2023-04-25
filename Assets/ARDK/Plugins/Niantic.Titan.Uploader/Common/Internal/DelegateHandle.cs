using System;
using System.Runtime.InteropServices;

namespace Niantic.Titan.Uploader.Internal {

  /// <summary>
  /// Wraps a delegate that can be passed as a function pointer and called from unmanaged code.
  /// This wrapper prevents the delegate from being collected or relocated in memory by the GC.
  /// </summary>
  internal class DelegateHandle : IDisposable {

    private readonly Delegate _delegate;
    private IntPtr _delegatePointer;
    private GCHandle _gcHandle;
    private bool _isDisposed;

    /// <summary>
    /// Gets the delegate's function pointer, which can be called from unmanaged code
    /// </summary>
    public IntPtr FunctionPointer => _delegatePointer == IntPtr.Zero
      ? _delegatePointer = Marshal.GetFunctionPointerForDelegate(_delegate)
      : _delegatePointer;

    /// <summary>
    /// Constructs a new <see cref="DelegateHandle"/>
    /// </summary>
    /// <param name="delegate">The delegate that can be called from unmanaged code</param>
    public DelegateHandle(Delegate @delegate) {
      _delegate = @delegate;
      _gcHandle = GCHandle.Alloc(@delegate);
    }

    private void ReleaseUnmanagedResources() {
      if (!_isDisposed) {
        _gcHandle.Free();
        _isDisposed = true;
      }
    }

    public void Dispose() {
      ReleaseUnmanagedResources();
      GC.SuppressFinalize(this);
    }

    ~DelegateHandle() {
      ReleaseUnmanagedResources();
    }
  }
}
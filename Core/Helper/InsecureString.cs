// Core (c) by Tangram Inc
// 
// Core is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Tangram.Core.Helper
{
    [CLSCompliant(false)]
#pragma warning disable CS3021 // Type or member does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
    public sealed class InsecureString : IDisposable, IEnumerable<char>
#pragma warning restore CS3021 // Type or member does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
    {
        public string Value { get; private set; }

        private SecureString _secureString;
        private GCHandle _gcHandle;

        internal InsecureString(SecureString secureString)
        {
            _secureString = secureString;

            Initialize();
        }

#if !DEBUG
        [DebuggerHidden]
#endif
        private void Initialize()
        {
            unsafe
            {
                _gcHandle = new GCHandle();
                var insecurePointer = IntPtr.Zero;

                void code(object userData)
                {
                    Value = new string((char)0, _secureString.Length);
                    Action alloc = delegate { _gcHandle = GCHandle.Alloc(Value, GCHandleType.Pinned); };

                    alloc.ExecuteInConstrainedRegion();

                    Action toBSTR = delegate { insecurePointer = SecureStringMarshal.SecureStringToGlobalAllocUnicode(_secureString); };

                    toBSTR.ExecuteInConstrainedRegion();

                    var value = (char*)_gcHandle.AddrOfPinnedObject();
                    var charPointer = (char*)insecurePointer;

                    for (int i = 0; i < _secureString.Length; i++)
                    {
                        value[i] = charPointer[i];
                    }
                }

                RuntimeHelpers.CleanupCode cleanup = delegate
                {
                    if (insecurePointer != IntPtr.Zero)
                        Marshal.ZeroFreeGlobalAllocUnicode(insecurePointer);
                };

                RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(code, cleanup, null);
            }
        }

#if !DEBUG
        [DebuggerHidden]
#endif
        public void Dispose()
        {
            unsafe
            {
                if (_gcHandle.IsAllocated)
                {
                    var insecurePointer = (char*)_gcHandle.AddrOfPinnedObject();
                    for (int i = 0; i < _secureString.Length; i++)
                    {
                        insecurePointer[i] = (char)0;
                    }
#if DEBUG
                    var disposed = "¡DISPOSED¡";
                    disposed = disposed.Substring(0, System.Math.Min(disposed.Length, _secureString.Length));
                    for (int i = 0; i < disposed.Length; ++i)
                    {
                        insecurePointer[i] = disposed[i];
                    }
#endif
                    _gcHandle.Free();
                }
            }
        }

        public IEnumerator<char> GetEnumerator()
        {
            return _gcHandle.IsAllocated ? Value.GetEnumerator() : Enumerable.Empty<char>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

// Polyfill enabling init-only setters (used by records) on netstandard2.0; the namespace is mandated
// by the compiler and the type is referenced only by compiler-generated code.
#pragma warning disable IDE0130, MA0182

namespace System.Runtime.CompilerServices;

internal static class IsExternalInit;

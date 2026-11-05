// The .NET interop objects as JavaScript receives them: a DotNetObjectReference and a DotNetStreamReference.
interface DotNetObject {
    invokeMethodAsync<T = unknown>(methodName: string, ...args: unknown[]): Promise<T>;
}

interface DotNetStream {
    arrayBuffer(): Promise<ArrayBuffer>;
    stream(): Promise<ReadableStream<Uint8Array>>;
}

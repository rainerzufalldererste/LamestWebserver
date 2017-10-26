using System;

namespace LamestWebserver.RequestHandlers.DebugView
{
    public interface IDebugUpdateableResponse<T>
    {
        void UpdateDebugResponseData(T data);
    }

    public interface IDebugUpdateableResponse<T1, T2>
    {
        void UpdateDebugResponseData(T1 data1, T2 data2);
    }

    public interface IDebugUpdateableResponse<T1, T2, T3>
    {
        void UpdateDebugResponseData(T1 data1, T2 data2, T3 data3);
    }

    public interface IDebugUpdateableResponse<T1, T2, T3, T4>
    {
        void UpdateDebugResponseData(T1 data1, T2 data2, T3 data3, T4 data4);
    }

    public interface IDebugUpdateableResponse<T1, T2, T3, T4, T5>
    {
        void UpdateDebugResponseData(T1 data1, T2 data2, T3 data3, T4 data4, T5 data5);
    }

    public interface IDebugUpdateableResponse<T1, T2, T3, T4, T5, T6>
    {
        void UpdateDebugResponseData(T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6);
    }

    public interface IDebugUpdateableResponse<T1, T2, T3, T4, T5, T6, T7>
    {
        void UpdateDebugResponseData(T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7);
    }

    public interface IDebugUpdateableResponse<T1, T2, T3, T4, T5, T6, T7, T8>
    {
        void UpdateDebugResponseData(T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8);
    }

    public interface IDebugUpdateableResponse<T1, T2, T3, T4, T5, T6, T7, T8, T9>
    {
        void UpdateDebugResponseData(T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8, T9 data9);
    }
}

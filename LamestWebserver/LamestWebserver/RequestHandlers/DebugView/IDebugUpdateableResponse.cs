using System;

namespace LamestWebserver.RequestHandlers.DebugView
{
    /// <summary>
    /// An interface to display that a class is able to Update it's DebugResponse with a variety of parameters.
    /// </summary>
    public interface IDebugUpdateableResponse<T>
    {
        /// <summary>
        /// Updates the DebugView information of this DebugResponse.
        /// </summary>
        /// <param name="data">The data to update with.</param>
        void UpdateDebugResponseData(T data);
    }

    /// <summary>
    /// An interface to display that a class is able to Update it's DebugResponse with a variety of parameters.
    /// </summary>
    public interface IDebugUpdateableResponse<T1, T2>
    {
        /// <summary>
        /// Updates the DebugView information of this DebugResponse.
        /// </summary>
        /// <param name="data1">The data parameter 1 to update with.</param>
        /// <param name="data2">The data parameter 2 to update with.</param>
        void UpdateDebugResponseData(T1 data1, T2 data2);
    }

    /// <summary>
    /// An interface to display that a class is able to Update it's DebugResponse with a variety of parameters.
    /// </summary>
    public interface IDebugUpdateableResponse<T1, T2, T3>
    {
        /// <summary>
        /// Updates the DebugView information of this DebugResponse.
        /// </summary>
        /// <param name="data1">The data parameter 1 to update with.</param>
        /// <param name="data2">The data parameter 2 to update with.</param>
        /// <param name="data3">The data parameter 3 to update with.</param>
        void UpdateDebugResponseData(T1 data1, T2 data2, T3 data3);
    }

    /// <summary>
    /// An interface to display that a class is able to Update it's DebugResponse with a variety of parameters.
    /// </summary>
    public interface IDebugUpdateableResponse<T1, T2, T3, T4>
    {
        /// <summary>
        /// Updates the DebugView information of this DebugResponse.
        /// </summary>
        /// <param name="data1">The data parameter 1 to update with.</param>
        /// <param name="data2">The data parameter 2 to update with.</param>
        /// <param name="data3">The data parameter 3 to update with.</param>
        /// <param name="data4">The data parameter 4 to update with.</param>
        void UpdateDebugResponseData(T1 data1, T2 data2, T3 data3, T4 data4);
    }

    /// <summary>
    /// An interface to display that a class is able to Update it's DebugResponse with a variety of parameters.
    /// </summary>
    public interface IDebugUpdateableResponse<T1, T2, T3, T4, T5>
    {
        /// <summary>
        /// Updates the DebugView information of this DebugResponse.
        /// </summary>
        /// <param name="data1">The data parameter 1 to update with.</param>
        /// <param name="data2">The data parameter 2 to update with.</param>
        /// <param name="data3">The data parameter 3 to update with.</param>
        /// <param name="data4">The data parameter 4 to update with.</param>
        /// <param name="data5">The data parameter 5 to update with.</param>
        void UpdateDebugResponseData(T1 data1, T2 data2, T3 data3, T4 data4, T5 data5);
    }

    /// <summary>
    /// An interface to display that a class is able to Update it's DebugResponse with a variety of parameters.
    /// </summary>
    public interface IDebugUpdateableResponse<T1, T2, T3, T4, T5, T6>
    {
        /// <summary>
        /// Updates the DebugView information of this DebugResponse.
        /// </summary>
        /// <param name="data1">The data parameter 1 to update with.</param>
        /// <param name="data2">The data parameter 2 to update with.</param>
        /// <param name="data3">The data parameter 3 to update with.</param>
        /// <param name="data4">The data parameter 4 to update with.</param>
        /// <param name="data5">The data parameter 5 to update with.</param>
        /// <param name="data6">The data parameter 6 to update with.</param>
        void UpdateDebugResponseData(T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6);
    }

    /// <summary>
    /// An interface to display that a class is able to Update it's DebugResponse with a variety of parameters.
    /// </summary>
    public interface IDebugUpdateableResponse<T1, T2, T3, T4, T5, T6, T7>
    {
        /// <summary>
        /// Updates the DebugView information of this DebugResponse.
        /// </summary>
        /// <param name="data1">The data parameter 1 to update with.</param>
        /// <param name="data2">The data parameter 2 to update with.</param>
        /// <param name="data3">The data parameter 3 to update with.</param>
        /// <param name="data4">The data parameter 4 to update with.</param>
        /// <param name="data5">The data parameter 5 to update with.</param>
        /// <param name="data6">The data parameter 6 to update with.</param>
        /// <param name="data7">The data parameter 7 to update with.</param>
        void UpdateDebugResponseData(T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7);
    }

    /// <summary>
    /// An interface to display that a class is able to Update it's DebugResponse with a variety of parameters.
    /// </summary>
    public interface IDebugUpdateableResponse<T1, T2, T3, T4, T5, T6, T7, T8>
    {
        /// <summary>
        /// Updates the DebugView information of this DebugResponse.
        /// </summary>
        /// <param name="data1">The data parameter 1 to update with.</param>
        /// <param name="data2">The data parameter 2 to update with.</param>
        /// <param name="data3">The data parameter 3 to update with.</param>
        /// <param name="data4">The data parameter 4 to update with.</param>
        /// <param name="data5">The data parameter 5 to update with.</param>
        /// <param name="data6">The data parameter 6 to update with.</param>
        /// <param name="data7">The data parameter 7 to update with.</param>
        /// <param name="data8">The data parameter 8 to update with.</param>
        void UpdateDebugResponseData(T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8);
    }

    /// <summary>
    /// An interface to display that a class is able to Update it's DebugResponse with a variety of parameters.
    /// </summary>
    public interface IDebugUpdateableResponse<T1, T2, T3, T4, T5, T6, T7, T8, T9>
    {
        /// <summary>
        /// Updates the DebugView information of this DebugResponse.
        /// </summary>
        /// <param name="data1">The data parameter 1 to update with.</param>
        /// <param name="data2">The data parameter 2 to update with.</param>
        /// <param name="data3">The data parameter 3 to update with.</param>
        /// <param name="data4">The data parameter 4 to update with.</param>
        /// <param name="data5">The data parameter 5 to update with.</param>
        /// <param name="data6">The data parameter 6 to update with.</param>
        /// <param name="data7">The data parameter 7 to update with.</param>
        /// <param name="data8">The data parameter 8 to update with.</param>
        /// <param name="data9">The data parameter 9 to update with.</param>
        void UpdateDebugResponseData(T1 data1, T2 data2, T3 data3, T4 data4, T5 data5, T6 data6, T7 data7, T8 data8, T9 data9);
    }
}

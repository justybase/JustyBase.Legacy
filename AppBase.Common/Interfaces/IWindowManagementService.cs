using AppBase.Common.WindowManagement;
using System.Diagnostics;

namespace AppBase.Common;

/// <summary>
/// Provides window management services including inter-process communication, 
/// window manipulation, and custom frame handling.
/// </summary>
public interface IWindowManagementService
{
    /// <summary>
    /// Sends a data message to another process using Windows messaging.
    /// </summary>
    /// <param name="targetProcess">The target process to send the message to</param>
    /// <param name="msg">The message content to send</param>
    void SendDataMessage(Process targetProcess, string msg);

    /// <summary>
    /// Sends a message to all other instances of the current process.
    /// </summary>
    /// <param name="args">Arguments to send to other instances</param>
    /// <returns>Process information or null</returns>
    Process SendMessageToAnotherInstances(string[] args);

    /// <summary>
    /// Performs hit testing for custom window frames.
    /// </summary>
    /// <param name="form">The form to test</param>
    /// <param name="FrameWidth">Width of the frame border</param>
    /// <param name="FrameHeight">Height of the frame border</param>
    /// <param name="iFrameOffset">Frame offset value</param>
    /// <param name="_tMargins">Margin information</param>
    /// <returns>Hit test result indicating which part of the window was hit</returns>
    HIT_CONSTANTS HitTest(Form form, int FrameWidth, int FrameHeight, int iFrameOffset, ref MARGINS _tMargins);

    /// <summary>
    /// Notifies the system that the window frame has changed.
    /// </summary>
    /// <param name="form">The form whose frame has changed</param>
    void FrameChanged(Form form);

    /// <summary>
    /// Flashes the window to get user attention.
    /// </summary>
    /// <param name="form">The form to flash</param>
    /// <returns>True if successful, false otherwise</returns>
    bool FlashWindowEx(Form form);
}

